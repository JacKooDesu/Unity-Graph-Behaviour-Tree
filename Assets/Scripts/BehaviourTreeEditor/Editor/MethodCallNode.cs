using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BehaviourTreeEditor.Runtime;
using UExtensions;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace BehaviourTreeEditor.Editor
{
    [Serializable]
    internal class MethodCallNode : Node, IValueSourceConvertable
    {
        internal override RuntimeNode Translate()
        {
            if (!GetInputPortByName(METHOD).TryGetValue<SMethod>(out var methodData))
            {
                Debug.LogError($"Failed to get method data from input port {METHOD}");
                return new UndefinedRuntimeNode();
            }

            var inputs = GetInputPorts().Where(p =>
                p.Name != INPUT &&
                p.Name != METHOD &&
                p.Name != TYPE).ToArray();
            var parameterTypes = methodData.ParameterTypes.ToArray();
            if (inputs.Length != parameterTypes.Length)
            {
                Debug.LogError($"Parameter count mismatch: expected {parameterTypes.Length}, got {inputs.Length}");
                return new UndefinedRuntimeNode();
            }

            IValueSource[] valueSources = new IValueSource[parameterTypes.Length];
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                var parameterType = parameterTypes[i];
                var input = inputs[i];

                if (parameterType.IsByRef)
                {
                    if (input.DataType != typeof(SField))
                    {
                        Debug.LogError($"Parameter type mismatch: should use SField type as input, got {input.DataType}");
                        return new UndefinedRuntimeNode();
                    }

                    if (!input.IsConnected)
                    {
                        Debug.LogError($"Parameter reference missing: should pass an field as a reference");
                        return new UndefinedRuntimeNode();
                    }
                }

                if (!parameterType.IsByRef &&
                    input.DataType != parameterType)
                {
                    Debug.LogError($"Parameter type mismatch: expected {parameterType}, got {input.DataType}");
                    return new UndefinedRuntimeNode();
                }

                if (input.ToValueSource() is not IValueSource valueSource)
                {
                    Debug.LogError($"Input {i} is not a value source");
                    return new UndefinedRuntimeNode();
                }

                // post check type match if by ref
                if (parameterType.IsByRef &&
                    valueSource.TargetType != parameterType.GetElementType())
                {
                    Debug.LogError($"Parameter type mismatch: expected {parameterType}, got {valueSource.TargetType}");
                    return new UndefinedRuntimeNode();
                }

                valueSources[i] = valueSource;
            }

            return new MethodCallRuntimeNode(methodData, valueSources);
        }

        // 當 MethodCallNode 被視為 Input 使用
        public IValueSource ToValueSource()
        {
            var outputPort = GetOutputPortByName(OUTPUT) ??
                throw new InvalidOperationException($"Output port {OUTPUT} is not defined or method not return value");

            if (!GetInputPortByName(METHOD).TryGetValue<SMethod>(out var methodData))
                throw new InvalidOperationException($"Method port {METHOD} is not defined");

            if (methodData.ReturnType != outputPort.DataType)
                throw new InvalidOperationException($"Return type {methodData.ReturnType} does not match output port type {outputPort.DataType}");

            // use generic ctor
            return this.Translate() as IValueSource;
        }
        public IEnumerable<IPort> OutputPorts()
        {
            foreach (var p in GetOutputPorts())
            {
                if (p.Name == OUTPUT)
                {
                    yield return p;
                    break;
                }
            }
        }

        const string INPUT = "INPUT";
        internal const string OUTPUT = "OUTPUT";

        const string TYPE = "TYPE";
        const string METHOD = "METHOD";
        internal const string PARAMETER_BASE = "parameter";

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.DefineInputPort();

            context.AddInputPort<SType>(TYPE)
                .WithDisplayName("Type")
                .WithDefaultValue(SType.Default)
                .Build();

            Type type;
            var inputPort = GetInputPortByName(TYPE);

            if (inputPort.IsConnected)
            {
                if (inputPort?.FirstConnectedPort?.GetNode() is not IReferenceNode rn ||
                    !rn.TryGetOutputType(out type))
                    return;
            }
            else if (inputPort.TryGetValue<SType>(out var typeData))
            {
                type = typeData.Type;
            }
            else
            {
                return;
            }

            context.AddInputPort<SMethodLocked>(METHOD)
                .WithDisplayName("Method")
                .Build();

            if (GetInputPortByName(METHOD).TryGetValue<SMethod>(out var methodData) &&
                methodData != null)
            {
                methodData.TryUpdateType(type);

                var iter = 0;
                foreach (var paramType in methodData.ParameterTypes)
                {
                    context.AddInputPort($"{PARAMETER_BASE}_{iter}")
                        .WithDisplayName(paramType.Name + (paramType.IsByRef ? " (ref)" : ""))
                        .WithDataType(paramType.IsByRef ? typeof(SField) : paramType)
                        .Build();
                    iter++;
                }

                if (methodData.ReturnType != typeof(void))
                {
                    context.AddOutputPort(OUTPUT)
                        .WithDisplayName(methodData.ReturnType.Name)
                        .WithDataType(methodData.ReturnType)
                        .Build();
                }
            }
        }
    }
}
