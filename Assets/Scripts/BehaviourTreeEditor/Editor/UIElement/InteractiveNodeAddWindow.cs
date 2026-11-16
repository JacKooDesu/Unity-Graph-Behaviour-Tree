using System;
using System.Collections.Generic;
using UExtensions;
using UExtensions.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace BehaviourTreeEditor.Editor.UIElement
{
    internal class InteractiveNodeAddWindow : PopupWindowContent
    {
        StateTreeGraph _graph;

        VisualElement _root;
        List<SerializedObject> _targetChain = new();
        List<VisualElement> _containers = new();

        public InteractiveNodeAddWindow(StateTreeGraph graph)
        {
            _graph = graph;
        }

        public override VisualElement CreateGUI()
        {
            _root = new VisualElement();
            _root.style.paddingTop = 8;
            _root.style.paddingBottom = 8;
            _root.style.paddingLeft = 8;
            _root.style.paddingRight = 8;

            BuildTypeSelector();
            // ReBuildTargetChainedFromIndex(0);

            foreach (var container in _containers)
                _root.Add(container);

            _root.Add(BuildAddRemoveButton());
            _root.Add(new Button(BuildNodes) { text = "Build Nodes" });

            return _root;
        }

        public override void OnClose()
        {
            Cleanup();
        }

        void Cleanup()
        {
            foreach (var c in _containers)
                c?.Unbind();

            foreach (var data in _targetChain)
            {
                UnityEngine.Object.DestroyImmediate(data.targetObject);
                data.Dispose();
            }

            _containers.Clear();
            _targetChain.Clear();
        }

        void BuildTypeSelector()
        {
            if (_targetChain.Count != 0)
                return;

            var element = new VisualElement();

            var target = SType.Default.AsSerializedObject();
            var data = target.FindProperty("Data");

            var propertyField = new PropertyField(data.Copy());
            propertyField.RegisterValueChangeCallback(e =>
            {
                target.ApplyModifiedProperties();
            });
            element.Add(propertyField);

            element.Bind(target);

            _targetChain.Add(target);
            _containers.Add(element);
        }

        void ReBuildTargetChainedFromIndex(int index)
        {
            if (_targetChain.Count is 0)
                return;

            if (index is 0 &&
                (GetSTargetData(0) as SType).IsDefault())
                return;

            // remove obsolete container
            for (int i = _containers.Count - 1; i >= index; --i)
            {
                var container = _containers[i];
                container.Unbind();
                container.Clear();
                container.RemoveFromHierarchy();

                _containers.RemoveAt(i);
            }

            for (int i = index; i < _targetChain.Count; ++i)
            {
                var tmp = i;
                var target = _targetChain[i];
                var element = new VisualElement();

                var propertyField = new PropertyField(target.FindProperty("Data").Copy());
                propertyField.RegisterValueChangeCallback(e =>
                {
                    target.ApplyModifiedProperties();
                });
                element.Add(propertyField);
                element.Bind(target);

                _containers.Add(element);

                _root.Insert(_containers.Count - 1, element);
            }
        }

        VisualElement BuildAddRemoveButton()
        {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.alignContent = Align.Auto;

            var removeButton = new Button() { text = "-" };
            removeButton.clicked += RemoveChain;
            var addButton = new Button() { text = "+" };
            addButton.clicked += () => ShowAddChainDropdown(addButton);

            addButton.style.flexGrow = 1;
            removeButton.style.flexGrow = 1;

            root.Add(addButton);
            root.Add(removeButton);

            return root;
        }

        object GetSTargetData(int index)
        {
            if (index < 0 || index >= _targetChain.Count)
                return null;

            return _targetChain[index].FindProperty("Data").boxedValue;
        }

        void ShowAddChainDropdown(VisualElement button)
        {
            var menu = new GenericDropdownMenu();

            menu.AddItem("Field", false, AddField);
            menu.AddItem("Property", false, AddProperty);

            menu.DropDown(button.worldBound, button, DropdownMenuSizeMode.Auto);

            Type GetLastType()
            {
                var last = GetSTargetData(_targetChain.Count - 1);
                return last switch
                {
                    SType t => t.Type,
                    SField f => f.Field.FieldType,
                    SProperty p => p.Property.PropertyType,
                    _ => SType.Default.Type
                };
            }

            void AddField()
            {
                var type = GetLastType();
                var target = SFieldLocked.FromType(type);

                if (target.IsDefault())
                    return;

                _targetChain.Add(target.AsSerializedObject());

                ReBuildTargetChainedFromIndex(_targetChain.Count - 1);
            }

            void AddProperty()
            {
                var type = GetLastType();
                var target = SPropertyLocked.FromType(type);

                if (target.IsDefault())
                    return;

                _targetChain.Add(target.AsSerializedObject());

                ReBuildTargetChainedFromIndex(_targetChain.Count - 1);
            }
        }

        void RemoveChain()
        {
            // base type cannot remove
            if (_targetChain.Count <= 1)
                return;

            var data = _targetChain[^1];
            UnityEngine.Object.DestroyImmediate(data.targetObject);
            data.Dispose();

            _targetChain.RemoveAt(_targetChain.Count - 1);
            ReBuildTargetChainedFromIndex(_targetChain.Count - 1);
        }

        void BuildNodes()
        {
            _graph.UndoBeginRecordGraph("Build Nodes");

            // base node should be reference
            var baseType = (GetSTargetData(0) as SType);
            var baseNode = new ReferenceNode();
            _graph.AddNode(baseNode);
            ReferenceNode.BuildFromSType(baseNode, baseType);

            Node lastNode = baseNode;

            for (int i = 1; i < _targetChain.Count; ++i)
            {
                var data = GetSTargetData(i);
                Node node = data switch
                {
                    SField f => AddFieldNode(),
                    SProperty p => AddPropertyNode(),
                    _ => throw new Exception("Invalid node")
                };

                lastNode = data switch
                {
                    SField f => ObjectFieldNode.BuildFromSField(node as ObjectFieldNode, f),
                    SProperty p => ObjectPropertyNode.BuildFromSProperty(node as ObjectPropertyNode, p),
                    _ => throw new Exception("Invalid node")
                };
            }

            _graph.UndoEndRecordGraph();

            Node AddFieldNode()
            {
                var node = new ObjectFieldNode();
                _graph.AddNode(node);
                ConnectTypeInfo(lastNode as IReferenceNode, node);
                node.DefineNode();
                return node;
            }

            Node AddPropertyNode()
            {
                var node = new ObjectPropertyNode();
                _graph.AddNode(node);
                ConnectTypeInfo(lastNode as IReferenceNode, node);
                node.DefineNode();
                return node;
            }

            void ConnectTypeInfo(IReferenceNode last, IReferenceNode current) =>
                _graph.Connect(last.GetOutputTypePort(), current.GetInputTypePort());
        }
    }
}
