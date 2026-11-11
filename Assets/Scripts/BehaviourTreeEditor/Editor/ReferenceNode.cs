using System;
using System.Collections.Generic;
using BehaviourTreeEditor.Runtime;
using UExtensions;
using Unity.GraphToolkit.Editor;

namespace BehaviourTreeEditor.Editor
{
    internal interface IReferenceNode
    {
        bool TryGetOutputType(out Type type);
    }

    [Serializable]
    internal class ReferenceNode : Node, IReferenceNode, IValueSourceConvertable
    {
        const string REF_TYPE_PORT = "TYPE";
        const string OUTPUT_TYPE_PORT = "OUT_TYPE";
        const string OUTPUT_REF_PORT = "OUT_REF";

        public IValueSource ToValueSource()
        {
            var inputPort = GetInputPortByName(REF_TYPE_PORT);
            if (inputPort.TryGetValue<SType>(out var sType))
                return new ClassValueSource(sType);

            return default;
        }

        public IEnumerable<IPort> OutputPorts()
        {
            yield return GetOutputPortByName(OUTPUT_REF_PORT);
        }

        public bool TryGetOutputType(out Type type)
        {
            type = default;
            if (GetInputPortByName(REF_TYPE_PORT).TryGetValue<SType>(out var sType))
            {
                type = sType.Type;
                return true;
            }

            return false;
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort<SType>(REF_TYPE_PORT)
                .WithDisplayName("Type")
                .WithDefaultValue(SType.Default)
                .Build();

            context.AddOutputPort<SType>(OUTPUT_TYPE_PORT)
                .WithDisplayName("Type")
                .Build();

            if (GetInputPortByName(REF_TYPE_PORT).TryGetValue<SType>(out var sType))
            {
                context.AddOutputPort(OUTPUT_REF_PORT)
                    .WithDisplayName(sType.Type.Name)
                    .WithDataType(sType.Type)
                    .Build();
            }
        }
    }
}
