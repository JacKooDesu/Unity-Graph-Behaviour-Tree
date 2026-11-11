using System;
using System.Collections.Generic;
using System.Linq;
using BehaviourTreeEditor.Runtime;
using UnityEngine;

namespace BehaviourTreeEditor.Editor
{
    /// <summary>
    /// 組合節點，可包含多個子節點的執行邏輯
    /// </summary>
    [Serializable]
    internal class CompositionNode : Node
    {
        internal override RuntimeNode Translate() =>
             new CompositionRuntimeNode(
                this switch
                {
                    SequenceNode => CompositionRuntimeNode.EMode.ReturnOnFailure,
                    SelectorNode => CompositionRuntimeNode.EMode.ReturnOnSuccess,
                    _ => CompositionRuntimeNode.EMode.Default,
                });

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.DefineInputPort();
            context.DefineOutputPort();
        }
    }
}
