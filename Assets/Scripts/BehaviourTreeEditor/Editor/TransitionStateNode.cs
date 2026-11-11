using System;
using BehaviourTreeEditor.Runtime;
using UnityEngine;

namespace BehaviourTreeEditor.Editor
{
    [Serializable]
    internal class TransitionStateNode : Node
    {
        internal override RuntimeNode Translate()
        {
            if (!GetNodeOptionByName(OPTION).TryGetValue<string>(out var targetState))
            {
                Debug.LogError("Failed to get target state for TransitionStateNode");
                return new UndefinedRuntimeNode();
            }

            return new TransitionStateRuntimeNode(targetState);
        }

        internal const string OPTION = "OPTION";
        internal const string INPUT_PORT = "INPUT";
        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<string>(OPTION)
                .WithDisplayName("State")
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.DefineInputPort();
        }
    }
}
