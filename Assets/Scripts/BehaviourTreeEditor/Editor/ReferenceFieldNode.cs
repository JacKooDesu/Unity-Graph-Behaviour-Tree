using System;
using System.Collections.Generic;
using BehaviourTreeEditor.Runtime;
using UExtensions;
using Unity.GraphToolkit.Editor;

namespace BehaviourTreeEditor.Editor
{
    [Serializable]
    [Node("Variable", "", "Reference Field")]
    internal class ReferenceFieldNode : Node, IReferenceNode, IValueSourceConvertable
    {
        const string OPTION_NAME = "OPTION";
        internal const string OUTPUT_VALUE_PORT = "OUTPUT";
        internal const string OUTPUT_FIELD_PORT = "OUTPUT_FIELD";
        internal const string OUTPUT_TYPE_PORT = "OUTPUT_TYPE";
        const string OUTPUT_ENUMERABLE_PORT = "enumerableType";

        public IValueSource ToValueSource()
        {
            var optionPort = GetInputPortByName(OPTION_NAME);
            if (optionPort == null ||
                !optionPort.TryGetValue<SField>(out var fieldData) ||
                fieldData == null)
                return null;

            return new ClassFieldValueSource(fieldData);
        }
        public IEnumerable<IPort> OutputPorts()
        {
            var valuePort = GetOutputPortByName(OUTPUT_VALUE_PORT);
            if (valuePort != null)
                yield return valuePort;
            var fieldPort = GetOutputPortByName(OUTPUT_FIELD_PORT);
            if (fieldPort != null)
                yield return fieldPort;
            var typePort = GetOutputPortByName(OUTPUT_TYPE_PORT);
            if (typePort != null)
                yield return typePort;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context) { }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort<SField>(OPTION_NAME)
                .WithDisplayName("Value")
                .Build();
            if (GetInputPortByName(OPTION_NAME)?.TryGetValue<SField>(out var fieldData) == true &&
                fieldData != null &&
                fieldData.Type != SField.DefaultField.Type)
            {
                context.AddOutputPort(OUTPUT_VALUE_PORT)
                    .WithDisplayName("Value")
                    .WithDataType(fieldData.Field.FieldType)
                    .Build();
                context.AddOutputPort(OUTPUT_FIELD_PORT)
                    .WithDisplayName("Field")
                    .WithDataType(typeof(SField))
                    .Build();
                context.AddOutputPort(OUTPUT_TYPE_PORT)
                    .WithDisplayName("Type")
                    .WithDataType(typeof(SType))
                    .Build();

                if (fieldData.Field.FieldType.TryGetEnumerableType(out _))
                {
                    context.AddOutputPort(OUTPUT_ENUMERABLE_PORT)
                        .WithDisplayName("Enumerable")
                        .WithDataType(typeof(EnumeratorNode.EnumerableTypePort))
                        .Build();
                }
            }
        }

        public bool TryGetOutputType(out Type type)
        {
            type = default;
            if (GetInputPortByName(OPTION_NAME)?.TryGetValue<SField>(out var fieldData) == true &&
                fieldData != null &&
                fieldData.Type != SField.DefaultField.Type)
            {
                type = fieldData.Field.FieldType;
                return true;
            }

            return false;
        }
    }
}
