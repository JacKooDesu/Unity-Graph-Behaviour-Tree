using System;
using System.Collections.Generic;
using BehaviourTreeEditor.Runtime;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace BehaviourTreeEditor.Editor
{
    [Serializable]
    internal class EqualityNode : Node, IValueSourceConvertable
    {
        public IValueSource ToValueSource()
        {
            var eqType = GetNodeOptionByName(OPTION_EQ_TYPE);
            if (!eqType.TryGetValue<EEquality>(out var operationType))
            {
                Debug.LogError("Cannot get equality type value!");
                return default;
            }

            var input1 = GetInputPortByName(LEFT);
            var input2 = GetInputPortByName(RIGHT);

            var input1Value = input1.ToValueSource();
            var input2Value = input2.ToValueSource();

            return new EqValueSource(
                operationType,
                (IValueSource)input1Value,
                (IValueSource)input2Value);
        }
        public IEnumerable<IPort> OutputPorts()
        {
            var outputPort = GetOutputPortByName(OUTPUT);
            yield return outputPort;
        }

        const string OPTION_VALUE_TYPE = "OPTION_VALUE";
        const string OPTION_EQ_TYPE = "OPTION_EQ";
        const string LEFT = "LEFT";
        const string RIGHT = "RIGHT";
        const string OUTPUT = "OUTPUT";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<EValue>(OPTION_VALUE_TYPE)
                .WithDisplayName("Value Type")
                .WithDefaultValue(EValue.Bool)
                .Build();
            context.AddOption<EEquality>(OPTION_EQ_TYPE)
                .WithDisplayName("Equality Type")
                .WithDefaultValue(EEquality.Equal)
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            if (!GetNodeOptionByName(OPTION_VALUE_TYPE).TryGetValue<EValue>(out var valueTypeEnum))
            {
                Debug.LogError("Cannot get value type option!");
                return;
            }

            if (!GetNodeOptionByName(OPTION_EQ_TYPE).TryGetValue<EEquality>(out var eqTypeEnum))
            {
                Debug.LogError("Cannot get equality type option!");
                return;
            }

            var type = valueTypeEnum.TypeMapper();
            context.AddInputPort(LEFT)
                .WithDisplayName(string.Empty)
                .WithDataType(type)
                .Build();
            context.AddInputPort(RIGHT)
                .WithDisplayName(string.Empty)
                .WithDataType(type)
                .Build();

            context.AddOutputPort<bool>(OUTPUT)
                .WithDisplayName(eqTypeEnum switch
                {
                    EEquality.Less => "<",
                    EEquality.Greater => ">",
                    EEquality.LessOrEqual => "<=",
                    EEquality.GreaterOrEqual => ">=",
                    EEquality.Equal => "==",
                    _ => string.Empty,
                })
                .Build();
        }
    }
}
