using System;
using System.Collections.Generic;
using BehaviourTreeEditor.Runtime;
using UExtensions;
using Unity.GraphToolkit.Editor;

namespace BehaviourTreeEditor.Editor
{
    [Serializable]
    internal class EnumeratorNode : CompositionNode, IReferenceNode
    {
        internal override RuntimeNode Translate()
        {
            var input = GetInputPortByName(ENUMERATOR_PORT);

            var sourceObj = input.ToValueSource();
            if (sourceObj is not IValueSource source)
                throw new ArgumentException($"Invalid source: {sourceObj}");

            var runtimeNode = new EnumeratorRuntimeNode(source);
            return runtimeNode;
        }

        internal IValueSource ToValueSource()
        {
            return new EnumeratorRuntimeNode.EnumerableScopeValueSource();
        }

        [Serializable]
        public class EnumerableTypePort { }

        internal const string ENUMERATOR_PORT = "ENUMERATOR";
        internal const string ITEM_PORT = "ITEM_VALUE";
        internal const string ITEM_REF_PORT = "ITEM_REF";
        internal const string ITEM_TYPE_PORT = "ITEM_TYPE";

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            base.OnDefinePorts(context);

            context.AddInputPort(ENUMERATOR_PORT)
                .WithDisplayName("Enumerator")
                .WithDataType<EnumerableTypePort>()
                .Build();

            var input = GetInputPortByName(ENUMERATOR_PORT);
            if (input == null) return;

            if (input.FirstConnectedPort.GetNode() is IReferenceNode refNode &&
                refNode.TryGetOutputType(out var type) &&
                type.TryGetEnumerableType(out var elementType))
            {
                context.AddOutputPort(ITEM_PORT)
                    .WithDisplayName("Value")
                    .WithDataType(elementType)
                    .Build();

                if (type.TryGetIndexer(out _, out _))
                    context.AddOutputPort(ITEM_REF_PORT)
                        .WithDisplayName("Ref")
                        .WithDataType(typeof(SField))
                        .Build();

                context.AddOutputPort(ITEM_TYPE_PORT)
                    .WithDisplayName("Type")
                    .WithDataType(typeof(SType))
                    .Build();
            }
        }

        public bool TryGetOutputType(out Type type)
        {
            var input = GetInputPortByName(ENUMERATOR_PORT);
            if (input == null)
            {
                type = null;
                return false;
            }

            if (input.FirstConnectedPort.GetNode() is IReferenceNode refNode &&
                refNode.TryGetOutputType(out var enumableType) &&
                enumableType.TryGetEnumerableType(out type))
                return true;

            type = null;
            return false;
        }

        internal void Validate(GraphLogger logger)
        {
            var output = this.GetFlowOutputPort();

            HashSet<INode> connectedNodeSet = new();

            List<IPort> connectedPorts = new();
            Queue<IPort> queue = new();
            foreach (var port in GetOutputPorts())
            {
                if (port == output)
                    continue;

                port.GetConnectedPorts(connectedPorts);
                foreach (var connectedPort in connectedPorts)
                    queue.Enqueue(connectedPort);
            }

            while (queue.TryDequeue(out var connectedPort))
            {
                var node = connectedPort.GetNode();
                if (node is IValueSourceConvertable convertableNode)
                {
                    foreach (var port in convertableNode.OutputPorts())
                    {
                        port.GetConnectedPorts(connectedPorts);
                        foreach (var connectPort in connectedPorts)
                            queue.Enqueue(connectPort);
                    }
                    continue;
                }

                connectedNodeSet.Add(node);
            }

            List<IPort> outputPorts = new();
            output.GetConnectedPorts(outputPorts);
            foreach (var connectedPort in outputPorts)
                Traverse(connectedPort);

            if (connectedNodeSet.Count > 0)
                foreach (var node in connectedNodeSet)
                    logger.LogError($"Node {node} cannot use enumerator item outside of an enumerator node");

            void Traverse(IPort port)
            {
                if (port.GetNode() is CompositionNode compositionNode)
                {
                    compositionNode.GetFlowOutputPort()?
                        .GetConnectedPorts(connectedPorts);
                    foreach (var connectedPort in connectedPorts)
                        Traverse(connectedPort);

                    return;
                }

                connectedNodeSet.Remove(port.GetNode());
            }
        }
    }
}
