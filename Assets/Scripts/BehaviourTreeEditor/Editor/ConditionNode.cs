using System;
using System.Reflection;
using BehaviourTreeEditor.Runtime;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace BehaviourTreeEditor.Editor
{
    [Serializable]
    internal class ConditionNode : Node
    {
        internal override RuntimeNode Translate()
        {
            if (!GetNodeOptionByName(CONDITION_OPTION).TryGetValue<EConditionType>(out var condition))
            {
                Debug.LogError("Failed to get condition option");
                return new UndefinedRuntimeNode();
            }

            if (!GetNodeOptionByName(VALUE_TYPE_OPTION).TryGetValue<EValue>(out var valueType))
            {
                Debug.LogError("Failed to get value type option");
                return new UndefinedRuntimeNode();
            }

            var leftPort = GetInputPortByName(LEFT_INPUT);
            var rightPort = GetInputPortByName(RIGHT_INPUT);

            return new ConditionRuntimeNode(
                condition,
                leftPort.ToValueSource() as IValueSource,
                rightPort.ToValueSource() as IValueSource);
        }
        internal const string CONDITION_OPTION = "condition";
        internal const string VALUE_TYPE_OPTION = "valueType";
        internal const string LEFT_INPUT = "left";
        internal const string RIGHT_INPUT = "right";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<EConditionType>(CONDITION_OPTION)
                .WithDisplayName("Type")
                .WithDefaultValue(EConditionType.Equal)
                .Build();
            context.AddOption<EValue>(VALUE_TYPE_OPTION)
                .WithDisplayName("Value Type")
                .WithDefaultValue(EValue.Bool)
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.DefineInputPort();
            context.DefineOutputPort();

            if (!GetNodeOption(0).TryGetValue<EConditionType>(out var condition))
            {
                Debug.LogError("Failed to get condition type");
                return;
            }

            if (!GetNodeOption(1).TryGetValue<EValue>(out var valueType))
            {
                Debug.LogError("Failed to get value type");
                return;
            }

            var type = valueType.TypeMapper();
            context.AddInputPort(LEFT_INPUT)
                .WithDataType(type)
                .WithDisplayName(String.Empty)
                .Build();
            context.AddInputPort(RIGHT_INPUT)
                .WithDataType(type)
                .WithDisplayName(String.Empty)
                .Build();
        }
    }
}
