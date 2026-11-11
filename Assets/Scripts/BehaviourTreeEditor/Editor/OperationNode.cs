using System;
using System.Collections.Generic;
using BehaviourTreeEditor.Runtime;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace BehaviourTreeEditor.Editor
{
    [Serializable]
    internal class OperationNode : Node, IValueSourceConvertable
    {
        internal override RuntimeNode Translate()
        {
            if (!GetNodeOptionByName(VALUE_TYPE).TryGetValue<EValue>(out var valueType))
            {
                Debug.LogError("Failed to get value type");
                return new UndefinedRuntimeNode();
            }

            if (!GetNodeOptionByName(OPERATION).TryGetValue<EOperation>(out var operation))
            {
                Debug.LogError("Failed to get operation");
                return new UndefinedRuntimeNode();
            }

            if (GetInputPortByName(INPUT_1)?.ToValueSource() is not IValueSource input1 ||
                GetInputPortByName(INPUT_2)?.ToValueSource() is not IValueSource input2)
            {
                Debug.LogError("Failed to get input values");
                return new UndefinedRuntimeNode();
            }

            return new OperationRuntimeNode(
                input1,
                input2,
                operation);
        }

        public IValueSource ToValueSource() => Translate() as IValueSource;
        public IEnumerable<IPort> OutputPorts()
        {
            yield return GetOutputPortByName(OUTPUT);
        }

        internal const string VALUE_TYPE = "valuetype";
        internal const string OPERATION = "OPERATION";
        internal const string INPUT_1 = "INPUT_1";
        internal const string INPUT_2 = "INPUT_2";
        internal const string OUTPUT = "OUTPUT";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<EValue>(VALUE_TYPE)
                .WithDisplayName("Value Type")
                .Build();
            context.AddOption<EOperation>(OPERATION)
                .WithDisplayName("Operation")
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            if (!GetNodeOptionByName(VALUE_TYPE).TryGetValue<EValue>(out var valueType))
            {
                Debug.LogError("Failed to get value type");
                return;
            }

            if (!GetNodeOptionByName(OPERATION).TryGetValue<EOperation>(out var operation))
            {
                Debug.LogError("Failed to get operation");
                return;
            }

            var type = valueType.TypeMapper();

            context.AddInputPort(INPUT_1)
                .WithDisplayName(string.Empty)
                .WithDataType(type)
                .Build();

            context.AddInputPort(INPUT_2)
                .WithDisplayName(string.Empty)
                .WithDataType(type)
                .Build();

            context.AddOutputPort(OUTPUT)
                .WithDisplayName(string.Empty)
                .WithDataType(type)
                .Build();
        }
    }
}
