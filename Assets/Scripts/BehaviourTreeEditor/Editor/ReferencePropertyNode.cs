using System;
using System.Collections.Generic;
using BehaviourTreeEditor.Runtime;
using UExtensions;
using Unity.GraphToolkit.Editor;

namespace BehaviourTreeEditor.Editor
{
    [Serializable]
    internal class ReferencePropertyNode : Node, IReferenceNode, IValueSourceConvertable
    {
        const string OPTION_NAME = "OPTION";
        internal const string OUTPUT_VALUE_PORT = "OUTPUT";
        internal const string OUTPUT_PROPERTY_PORT = "OUTPUT_PROPERTY";
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
            var propertyPort = GetOutputPortByName(OUTPUT_PROPERTY_PORT);
            if (propertyPort != null)
                yield return propertyPort;
            var typePort = GetOutputPortByName(OUTPUT_TYPE_PORT);
            if (typePort != null)
                yield return typePort;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context) { }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort<SProperty>(OPTION_NAME)
                .WithDisplayName("Value")
                .Build();
            if (GetInputPortByName(OPTION_NAME)?.TryGetValue<SProperty>(out var propData) == true &&
                propData != null &&
                propData.Type != SProperty.DefaultProperty.Type)
            {
                var property = propData.Property;

                if (property.CanRead)
                    context.AddOutputPort(OUTPUT_VALUE_PORT)
                        .WithDisplayName("Value")
                        .WithDataType(property.PropertyType)
                        .Build();

                if (property.CanWrite)
                    context.AddOutputPort(OUTPUT_PROPERTY_PORT)
                        .WithDisplayName("Property")
                        .WithDataType(typeof(SProperty))
                        .Build();

                if (property.CanRead)
                    context.AddOutputPort(OUTPUT_TYPE_PORT)
                        .WithDisplayName("Type")
                        .WithDataType(typeof(SType))
                        .Build();

                if (property.CanRead &&
                    property.PropertyType.TryGetEnumerableType(out _))
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
            if (GetInputPortByName(OPTION_NAME)?.TryGetValue<SProperty>(out var propData) == true &&
                propData != null &&
                propData.Type != SProperty.DefaultProperty.Type)
            {
                type = propData.Type;
                return true;
            }

            return false;
        }
    }
}
