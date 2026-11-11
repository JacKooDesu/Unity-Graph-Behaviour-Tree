using System;
using BehaviourTreeEditor.Runtime;
using Unity.GraphToolkit.Editor;

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
    [Node("Callback", "", "On Enter")]
    internal class OnEnterNode : StateEventNode { }

    [Serializable]
    [Node("Callback", "", "On Exit")]
    internal class OnExitNode : StateEventNode { }

    [Serializable]
    [Node("Callback", "", "On Tick")]
    internal class OnTickNode : StateEventNode { }
}
