using System;
using BehaviourTreeEditor.Runtime;

namespace BehaviourTreeEditor.Editor
{
    [Serializable]
    internal abstract class StateEventNode : Node
    {
        internal override RuntimeNode Translate() =>
            new StateEventRuntimeNode();
        internal const string OUTPUT_PORT = "OUTPUT";
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.DefineOutputPort();
        }
    }

    [Serializable]
    internal class OnEnterNode : StateEventNode { }

    [Serializable]
    internal class OnExitNode : StateEventNode { }

    [Serializable]
    internal class OnTickNode : StateEventNode { }
}
