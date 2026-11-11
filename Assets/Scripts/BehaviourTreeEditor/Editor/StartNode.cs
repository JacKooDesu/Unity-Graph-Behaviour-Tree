using System;
using BehaviourTreeEditor.Runtime;
using Unity.GraphToolkit.Editor;

namespace BehaviourTreeEditor.Editor
{
    [Serializable]
    [Node("Flow", "", "Graph Root")]
    internal class StartNode : Node
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.DefineOutputPort();
        }
    }
}
