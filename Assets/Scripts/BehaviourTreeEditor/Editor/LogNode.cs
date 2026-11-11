using System;
using BehaviourTreeEditor.Runtime;
using UExtensions;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace BehaviourTreeEditor.Editor
{
    [Serializable]
    internal class LogNode : Node
    {
        internal override RuntimeNode Translate()
        {
            if (!GetInputPortByName(LOG_STRING).TryGetValue<string>(out var logString))
            {
                Debug.LogError("LogString option not found");
                return new UndefinedRuntimeNode();
            }
            var logStringValueSource = new ConstantValueSource<string>(logString);

            var targetPort = GetInputPortByName(TARGET);
            if (targetPort.IsConnected &&
                targetPort?.ToValueSource() is IValueSource source)
            {
                var genericType = new SType(targetPort.DataType);
                var method = new SGenericMethod(
                    typeof(UnityMethodBridge),
                    nameof(UnityMethodBridge.LogWithObject),
                    new[] { genericType });
                return new MethodCallRuntimeNode(
                    method,
                    new[] { logStringValueSource, source });
            }

            return new MethodCallRuntimeNode(
                new(typeof(UnityMethodBridge), nameof(UnityMethodBridge.Log)),
                new[] { logStringValueSource });
        }

        const string INPUT_PORT = "Port";
        internal const string LOG_STRING = "LogString";
        internal const string TYPE_OPTION = "LogString";
        internal const string TARGET = "Target";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<EValue>(TYPE_OPTION)
                .WithDefaultValue(EValue.String)
                .WithDisplayName("Type")
                .ShowInInspectorOnly()
                .Delayed();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.DefineInputPort();

            context.AddInputPort<string>(LOG_STRING)
                .WithDisplayName(String.Empty)
                .Build();

            if (GetNodeOptionByName(TYPE_OPTION).TryGetValue<EValue>(out var valueType))
            {
                context.AddInputPort(TARGET)
                    .WithDisplayName(String.Empty)
                    .WithConnectorUI(PortConnectorUI.Arrowhead)
                    .WithDataType(valueType.TypeMapper())
                    .Build();
            }
        }
    }
}
