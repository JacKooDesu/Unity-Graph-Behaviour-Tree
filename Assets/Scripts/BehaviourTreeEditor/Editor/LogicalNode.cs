using System;
using System.Collections.Generic;
using BehaviourTreeEditor.Runtime;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace BehaviourTreeEditor.Editor
{
    [Serializable]
    internal class LogicalNode : Node, IValueSourceConvertable
    {
        public IValueSource ToValueSource()
        {
            var operation = GetNodeOptionByName(OPERATION);
            if (!operation.TryGetValue<ELogic>(out var operationType))
            {
                Debug.LogError("Cannot get logic operation type value!");
                return default;
            }

            var input1 = GetInputPortByName(INPUT_1);
            var input2 = GetInputPortByName(INPUT_2);

            var input1Value = input1.ToValueSource();
            var input2Value = input2.ToValueSource();

            return new LogicalValueSource(
                operationType,
                (IValueSource)input1Value,
                (IValueSource)input2Value);
        }
        public IEnumerable<IPort> OutputPorts()
        {
            var outputPort = GetOutputPortByName(OUTPUT);
            yield return outputPort;
        }

        const string OPERATION = "OPERATION";
        const string INPUT_1 = "INPUT_1";
        const string INPUT_2 = "INPUT_2";
        const string OUTPUT = "OUTPUT";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<ELogic>(OPERATION)
                .WithDisplayName("Operation")
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort<bool>(INPUT_1)
                .WithDisplayName(string.Empty)
                .Build();

            if (!GetNodeOptionByName(OPERATION).TryGetValue<ELogic>(out var logic))
            {
                Debug.LogError("Cannot get operation from node option");
                return;
            }

            if (logic != ELogic.Not)
                context.AddInputPort<bool>(INPUT_2)
                    .WithDisplayName(string.Empty)
                    .Build();

            context.AddOutputPort<bool>(OUTPUT)
                .WithDisplayName(logic switch
                {
                    ELogic.And => "AND",
                    ELogic.Or => "OR",
                    ELogic.Xor => "XOR",
                    ELogic.Not => "NOT",
                    _ => string.Empty,
                })
                .Build();
        }
    }
}
