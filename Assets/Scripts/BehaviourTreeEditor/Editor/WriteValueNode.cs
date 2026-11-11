using System;
using BehaviourTreeEditor.Runtime;
using UExtensions;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace BehaviourTreeEditor.Editor
{
    [Serializable]
    [Node("Action", "", "Write Value")]
    internal class WriteValueNode : Node
    {
        internal override RuntimeNode Translate()
        {
            IValueSource left = default;
            var inputPort = GetInputPortByName(TARGET);
            if (inputPort.TryGetValue<SField>(out var fieldData))
            {
                left = new ClassFieldValueSource(fieldData);
            }
            else if (inputPort.TryGetValue<SProperty>(out var propertyData))
            {
                left = new ClassPropertyValueSource(propertyData);
            }
            else if (inputPort.ToValueSource() is IValueSource valueSource)
            {
                left = valueSource;
            }
            else
            {
                Debug.LogError("Failed to get value type option");
                return new UndefinedRuntimeNode();
            }

            if (left is InstancePropertyValueSource propertySource &&
                !propertySource.PropertyData.Property.CanWrite)
            {
                Debug.LogError("Property cannot be written");
                return new UndefinedRuntimeNode();
            }

            var right = GetInputPortByName(VALUE).ToValueSource() as IValueSource;

            return new WriteValueRuntimeNode(left, right);
        }

        internal const string PROPERTY_MODE = "PROPERTY_MODE";
        internal const string TARGET = "TARGET";
        internal const string VALUE = "VALUE";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<bool>(PROPERTY_MODE)
                .WithDisplayName("Property Mode")
                .WithDefaultValue(false)
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.DefineInputPort();

            if (GetNodeOptionByName(PROPERTY_MODE).TryGetValue<bool>(out var pMode) &&
                pMode)
                context.AddInputPort<SProperty>(TARGET)
                    .WithDisplayName("Target")
                    .Build();
            else
                context.AddInputPort<SField>(TARGET)
                    .WithDisplayName("Target")
                    .Build();

            var targetPort = GetInputPortByName(TARGET);
            if (targetPort is null)
                return;

            var type = GetTargetType();

            context.AddInputPort(VALUE)
                .WithDataType(type)
                .WithDisplayName("Value")
                .Build();
        }

        Type GetTargetType()
        {
            if (!GetNodeOptionByName(PROPERTY_MODE).TryGetValue<bool>(out var pMode))
                return SField.DefaultField.Type;

            var port = GetInputPortByName(TARGET);

            if (pMode &&
                port.TryGetValue<SProperty>(out var property) &&
                property.Property.PropertyType != SProperty.DefaultProperty.Type)
                return property.Property.PropertyType;

            if (port.TryGetValue<SField>(out var fieldData) &&
                fieldData != null &&
                fieldData.Field.FieldType != SField.DefaultField.Type)
                return fieldData.Field.FieldType;

            if (port.FirstConnectedPort.GetNode() is IReferenceNode rn &&
                rn.TryGetOutputType(out var sourceType))
                return sourceType;

            return SField.DefaultField.Type;
        }
    }
}
