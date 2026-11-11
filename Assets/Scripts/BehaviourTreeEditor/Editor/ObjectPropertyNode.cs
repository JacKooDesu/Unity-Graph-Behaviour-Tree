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
    [Node("Variable", "", "Object Property")]
    internal class ObjectPropertyNode : Node, IReferenceNode, IValueSourceConvertable
    {
        const string TYPE_PORT = "type";
        const string PROPERTY_PORT = "property";
        const string OUTPUT_VALUE_PORT = "outputValue";
        const string OUTPUT_PROPERTY_PORT = "outputProperty";
        const string OUTPUT_OBJECT_PORT = "outputObject";
        const string OUTPUT_ENUMERABLE_PORT = "enumerableType";

        public IValueSource ToValueSource()
        {
            var typePort = GetInputPortByName(TYPE_PORT);
            var propertyPort = GetInputPortByName(PROPERTY_PORT);

            if (propertyPort is null ||
                !propertyPort.TryGetValue<SProperty>(out var propertyData))
            {
                Debug.LogError("Failed to get property data");
                return default;
            }

            var objectSource = typePort.ToValueSource() as IValueSource;

            return new InstancePropertyValueSource(
                objectSource,
                propertyData);
        }
        public IEnumerable<IPort> OutputPorts()
        {
            var valuePort = GetOutputPortByName(OUTPUT_VALUE_PORT);
            if (valuePort != null)
                yield return valuePort;
            var propertyPort = GetOutputPortByName(OUTPUT_PROPERTY_PORT);
            if (propertyPort != null)
                yield return propertyPort;
            var objectPort = GetOutputPortByName(OUTPUT_OBJECT_PORT);
            if (objectPort != null)
                yield return objectPort;
            var enumerablePort = GetOutputPortByName(OUTPUT_ENUMERABLE_PORT);
            if (enumerablePort != null)
                yield return enumerablePort;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        { }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort<SType>(TYPE_PORT)
                .WithDisplayName(string.Empty)
                .Build();

            var typePort = GetInputPortByName(TYPE_PORT);
            if (!typePort.IsConnected)
                return;

            if (typePort.FirstConnectedPort.GetNode() is not IReferenceNode rn ||
                !rn.TryGetOutputType(out var type))
                return;

            var property = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).First();
            var sproperty = new SPropertyLocked(type, property.Name);

            context.AddInputPort<SPropertyLocked>(PROPERTY_PORT)
                .WithDefaultValue(sproperty)
                .WithDisplayName(string.Empty)
                .Build();

            if (GetInputPortByName(PROPERTY_PORT)?.TryGetValue<SPropertyLocked>(out var propertyData) == true &&
                propertyData != null &&
                propertyData.Type != SProperty.DefaultProperty.Type)
            {
                propertyData.TryUpdateType(type);

                property = propertyData.Property;

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
                    context.AddOutputPort(OUTPUT_OBJECT_PORT)
                        .WithDisplayName("Type")
                        .WithDataType(typeof(SType))
                        .Build();

                if (property.CanRead &&
                    propertyData.Property.PropertyType.TryGetEnumerableType(out _))
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
            if (GetInputPortByName(PROPERTY_PORT)?.TryGetValue<SPropertyLocked>(out var propertyData) == true &&
                propertyData != null &&
                propertyData.Type != SProperty.DefaultProperty.Type)
            {
                type = propertyData.Property.PropertyType;
                return true;
            }

            return false;
        }
    }
}
