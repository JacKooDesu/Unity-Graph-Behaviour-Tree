using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using BehaviourTreeEditor.Runtime;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BehaviourTreeEditor.Editor
{
    [Serializable]
    internal class ContextVariableSource
    {
        [NonSerialized]
        public ReadOnlyCollection<IVariableBindingBlock> bindings;
        [NonSerialized]
        public IVariableBindingBlock bindingTo;
        public int Index => bindings?.IndexOf(bindingTo) ?? -1;

        public Type ValueType => bindingTo?.ValueType;
        [SerializeField]
        public string Key;
    }

    // [Serializable]
    // [UseWithGraph(typeof(StateTreeGraph))]
    internal abstract class VariableBindingReferenceBase : Node, IValueSourceConvertable
    {
        #region Interface
        public IValueSource ToValueSource()
        {
            // ToValueSource 前，Importer 會一併將 graph 資訊設定完畢
            // 所以可以使用 binding 資訊

            var source = GetSource();
            if (source is null) throw new InvalidOperationException("Source is null");

            return new ScopeVariableBindingValueSource(
                source.Index, this is ConstantReference);
        }

        public IEnumerable<IPort> OutputPorts()
        {
            var source = GetSource();
            if (source is null) throw new InvalidOperationException("Source is null");

            yield return GetOutputPortByName(VALUE_OUTPUT_PORT);
        }
        #endregion

        const string REFERENCE_OPTION = "REF";
        const string VALUE_OUTPUT_PORT = "VALUE";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<ContextVariableSource>(REFERENCE_OPTION)
                .WithDisplayName("Key")
                .WithDefaultValue(new())
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            var source = GetSource();
            if (source is null) return;
            var type = source.ValueType;
            if (type is null) return;

            context.AddOutputPort(VALUE_OUTPUT_PORT)
                .WithDataType(type)
                .WithDisplayName(source.Key)
                .Build();
        }

        protected abstract ReadOnlyCollection<BehaviourTreeEditor.Editor.IVariableBindingBlock> FetchBindings(Graph baseGraph);

        public void UpdateBindings(Graph baseGraph)
        {
            var bindings = FetchBindings(baseGraph);
            var source = GetSource();
            source.bindings = bindings;

            if (source.bindingTo is null)
                source.bindingTo = bindings.FirstOrDefault(x => x.GetKey() == source.Key);
        }

        public void CheckSourceBindingChanged(GraphLogger logger)
        {
            var source = GetSource();
            if (source is null) return;

            if (source.bindingTo != null)
                source.Key = source.bindingTo.GetKey();
            else if (!string.IsNullOrEmpty(source.Key))
                logger.LogError($"VariableReference: {source.Key} is no longer exist");
        }

        protected ContextVariableSource GetSource() =>
            GetNodeOptionByName(REFERENCE_OPTION)?
                .TryGetValue<ContextVariableSource>(out var source) ?? false ?
                source : null;
    }

    [CustomPropertyDrawer(typeof(ContextVariableSource))]
    internal class VariableReferenceSourceDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();
            var targetObject = property.serializedObject.targetObject;
            var targetObjectType = targetObject.GetType();

            // `Value` 是 wrapper class 的內部欄位
            var reference = targetObjectType.GetProperty("Value").GetGetMethod()
                .Invoke(targetObject, Array.Empty<object>()) as ContextVariableSource;
            var bindings = reference.bindings;
            var list = reference.bindings?.Select(x => x.GetKey()).ToList();
            if (list is null)
                return root;

            var dropdown = new DropdownField(
                list,
                list.IndexOf(reference.Key)
            );

            dropdown.RegisterValueChangedCallback(str =>
            {
                reference.Key = str.newValue;
                reference.bindingTo = bindings[list.IndexOf(str.newValue)];
                property.serializedObject.Update();
                property.serializedObject.ApplyModifiedProperties();
            });

            root.Add(dropdown);
            return root;
        }
    }

    [Serializable]
    [UseWithGraph(typeof(StateTreeGraph))]
    internal class VariableReference : VariableBindingReferenceBase
    {
        const string WRITE_PORT = "WRITE";

        internal override RuntimeNode Translate()
        {
            var source = GetSource();
            if (source is null) return new UndefinedRuntimeNode();

            var right = GetInputPortByName(WRITE_PORT).ToValueSource();
            return new WriteContextVariableRuntimeNode(source.Index, right as IValueSource);
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            base.OnDefinePorts(context);

            var source = GetSource();
            if (source is null) return;

            // 寫入流程
            context.DefineInputPort();
            if (!GetInputPortByName(NodeBuildUtil.INPUT_PORT).IsConnected)
                return;

            context.AddInputPort(WRITE_PORT)
                .WithDisplayName("Write")
                .WithDataType(source.ValueType)
                .Build();
        }

        protected override ReadOnlyCollection<IVariableBindingBlock> FetchBindings(Graph baseGraph) =>
            baseGraph.VariableBindings;
    }

    [Serializable]
    [UseWithGraph(typeof(StateTreeGraph))]
    internal class ConstantReference : VariableBindingReferenceBase
    {
        protected override ReadOnlyCollection<IVariableBindingBlock> FetchBindings(Graph baseGraph) =>
            baseGraph.ConstantBindings;
    }
}
