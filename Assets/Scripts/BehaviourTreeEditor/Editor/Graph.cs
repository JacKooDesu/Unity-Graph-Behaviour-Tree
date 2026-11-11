using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UGraph = Unity.GraphToolkit.Editor.Graph;

namespace BehaviourTreeEditor.Editor
{
    [Serializable]
    [Graph(EXTENSION_NAME, GraphOptions.SupportsSubgraphs)]
    internal class Graph : UGraph
    {
        internal const string EXTENSION_NAME = "btr";

        static List<IVariableBindingBlock> _variableBindings = new();
        static List<IVariableBindingBlock> _constantBindings = new();
        internal static ReadOnlyCollection<IVariableBindingBlock> VariableBindings => _variableBindings.AsReadOnly();
        internal static ReadOnlyCollection<IVariableBindingBlock> ConstantBindings => _constantBindings.AsReadOnly();

        public Type GetBindingType(string key, bool isConstant)
        {
            var bindings = isConstant ? _constantBindings : _variableBindings;
            return bindings.Find(x => x.GetKey() == key)?.ValueType;
        }

        [MenuItem("Assets/Create/Behaviour Tree/Graph")]
        static void CreateAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<Graph>("Behaviour Tree Graph");
        }

        public override void OnGraphChanged(GraphLogger logger)
        {
            base.OnGraphChanged(logger);
            FetchVariableBindings();
            // InjectContextVariables(logger);
            foreach (var node in GetNodes())
            {
                if ((node is ISubgraphNode subGraph && subGraph.GetSubgraph() is StateTreeGraph) ||
                    node is StartNode ||
                    node is ISubgraphNode ||
                    node is IConstantNode ||
                    node is VariableBindings)
                {
                    // support type
                }
                else
                {
                    logger.LogError($"Unsupported node type: {node.GetType()}");
                }
            }
        }

        public override void OnEnable()
        {
            FetchVariableBindings();
            // InjectContextVariables(null);
        }

        public override void OnDisable()
        {
            _constantBindings.Clear();
            _variableBindings.Clear();
        }

        void FetchVariableBindings()
        {
            _constantBindings.Clear();
            _variableBindings.Clear();

            var bindingContexts = GetNodes().OfType<VariableBindings>();
            foreach (var bindingContext in bindingContexts)
            {
                var list = bindingContext.IsConstant ?
                    _constantBindings : _variableBindings;

                foreach (var block in bindingContext.BlockNodes)
                {
                    if (block is IVariableBindingBlock vb)
                        list.Add(vb);
                }
            }
        }

        void InjectContextVariables(GraphLogger logger)
        {
            var subGraphNodes = GetNodes().OfType<ISubgraphNode>();
            foreach (var node in subGraphNodes)
            {
                if (node.GetSubgraph() is not StateTreeGraph stateTreeGraph)
                    continue;

                var varNodes = stateTreeGraph.GetNodes().OfType<VariableBindingReferenceBase>();
                foreach (var vn in varNodes)
                {
                    vn.UpdateBindings();
                    if (logger is not null)
                        vn.CheckSourceBindingChanged(logger);
                    vn.DefineNode();
                }
            }
        }
    }

    [Serializable]
    [Subgraph(typeof(Graph))]
    [Graph(EXTENSION_NAME, GraphOptions.SupportsSubgraphs)]
    internal class StateTreeGraph : UGraph
    {
        internal const string EXTENSION_NAME = "sttgraph";
        public override void OnGraphChanged(GraphLogger logger)
        {
            base.OnGraphChanged(logger);

            var nodes = GetNodes();
            foreach (var node in nodes)
            {
                if (node is ReferenceFieldNode rn)
                    rn.DefineNode();
                else if (node is ReferencePropertyNode rpn)
                    rpn.DefineNode();
                else if (node is ObjectFieldNode ofn)
                    ofn.DefineNode();
                else if (node is ObjectPropertyNode opn)
                    opn.DefineNode();
                else if (node is WriteValueNode wn)
                    wn.DefineNode();
                else if (node is MethodCallNode mcn)
                    mcn.DefineNode();
                else if (node is LogicalNode ln)
                    ln.DefineNode();
                else if (node is EnumeratorNode en)
                {
                    en.Validate(logger);
                    en.DefineNode();
                }
                else if (node is VariableBindingReferenceBase vr)
                {
                    vr.DefineNode();
                    vr.UpdateBindings();
                }
            }
        }
    }
}
