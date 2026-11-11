using System;
using BehaviourTreeEditor.Runtime;
using Unity.GraphToolkit.Editor;

namespace BehaviourTreeEditor.Editor
{
    internal static class UnityGTKExtensions
    {
        public static object ToValueSource(this IPort port)
        {
            object source = default;
            if (port.IsConnected)
            {
                source = port.FirstConnectedPort.GetNode() switch
                {
                    IVariableNode variableNode => variableNode.ToValueSource(),
                    IConstantNode constantNode => constantNode.ToValueSource(),
                    IValueSourceConvertable convertableNode => convertableNode.ToValueSource(),
                    EnumeratorNode enumeratorNode => enumeratorNode.ToValueSource(),
                    _ => throw new InvalidOperationException($"Failed to convert port to value source: {port}")
                };
            }
            else
            {
                var type = port.DataType;
                var method = typeof(IPort)
                    .GetMethod(nameof(IPort.TryGetValue))
                    .MakeGenericMethod(type);
                var parameters = new object[] { default };
                method.Invoke(port, parameters);

                // create value source with reflection
                var ctor = typeof(ConstantValueSource<>)
                    .MakeGenericType(type)
                    .GetConstructor(new[] { type });
                return ctor.Invoke(parameters);
            }

            if (source is null)
                throw new InvalidOperationException("Failed to convert port to value source");
            return source;
        }

        public static object ToValueSource(this IVariableNode node)
        {
            var type = node.Variable.DataType;
            var method = typeof(IVariable)
                .GetMethod(nameof(IVariable.TryGetDefaultValue))
                .MakeGenericMethod(type);
            var parameters = new object[] { default };
            method.Invoke(node.Variable, parameters);

            // create value source with reflection
            var ctor = typeof(ConstantValueSource<>)
                .MakeGenericType(type)
                .GetConstructor(new[] { type });
            return ctor.Invoke(parameters);
        }

        public static object ToValueSource(this IConstantNode node)
        {
            var type = node.DataType;
            var method = typeof(IConstantNode)
                .GetMethod(nameof(IConstantNode.TryGetValue))
                .MakeGenericMethod(type);
            var parameters = new object[] { default };
            method.Invoke(node, parameters);

            // create value source with reflection
            var ctor = typeof(ConstantValueSource<>)
                .MakeGenericType(type)
                .GetConstructor(new[] { type });
            return ctor.Invoke(parameters);
        }

        public static IPort GetFlowOutputPort(this INode node)
        {
            foreach (var port in node.GetOutputPorts())
            {
                if (port.Name == NodeBuildUtil.OUTPUT_PORT)
                    return port;
            }

            return null;
        }
    }
}
