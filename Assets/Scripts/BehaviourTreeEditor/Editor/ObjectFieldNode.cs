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
    internal class ObjectFieldNode : Node, IReferenceNode, IValueSourceConvertable
    {
        const string TYPE_PORT = "type";
        const string FIELD_PORT = "field";
        const string OUTPUT_VALUE_PORT = "outputValue";
        const string OUTPUT_FIELD_PORT = "outputField";
        const string OUTPUT_OBJECT_PORT = "outputObject";
        const string OUTPUT_ENUMERABLE_PORT = "enumerableType";

        public IValueSource ToValueSource()
        {
            var typePort = GetInputPortByName(TYPE_PORT);
            var fieldPort = GetInputPortByName(FIELD_PORT);

            if (fieldPort is null ||
                !fieldPort.TryGetValue<SField>(out var fieldData))
            {
                Debug.LogError("Failed to get field data");
                return default;
            }

            var objectSource = typePort.ToValueSource() as IValueSource;

            return new InstanceFieldValueSource(
                objectSource,
                fieldData);
        }
        public IEnumerable<IPort> OutputPorts()
        {
            var valuePort = GetOutputPortByName(OUTPUT_VALUE_PORT);
            if (valuePort != null)
                yield return valuePort;
            var fieldPort = GetOutputPortByName(OUTPUT_FIELD_PORT);
            if (fieldPort != null)
                yield return fieldPort;
            var typePort = GetOutputPortByName(OUTPUT_OBJECT_PORT);
            if (typePort != null)
                yield return typePort;
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

            var field = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).First();
            var sfield = new SFieldLocked(type, field.Name);

            context.AddInputPort<SFieldLocked>(FIELD_PORT)
                .WithDefaultValue(sfield)
                .WithDisplayName(string.Empty)
                .Build();

            if (GetInputPortByName(FIELD_PORT)?.TryGetValue<SField>(out var fieldData) == true &&
                fieldData != null &&
                fieldData.Type != SField.DefaultField.Type)
            {
                fieldData.TryUpdateType(type);
                
                context.AddOutputPort(OUTPUT_VALUE_PORT)
                    .WithDisplayName("Value")
                    .WithDataType(fieldData.Field.FieldType)
                    .Build();
                context.AddOutputPort(OUTPUT_FIELD_PORT)
                    .WithDisplayName("Field")
                    .WithDataType(typeof(SField))
                    .Build();

                context.AddOutputPort(OUTPUT_OBJECT_PORT)
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
            if (GetInputPortByName(FIELD_PORT)?.TryGetValue<SFieldLocked>(out var fieldData) == true &&
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
