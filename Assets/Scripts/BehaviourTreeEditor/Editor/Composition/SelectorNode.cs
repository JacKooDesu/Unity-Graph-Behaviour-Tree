using System;

namespace BehaviourTreeEditor.Editor
{
    /// <summary>
    /// 順序執行，直到任意 Condition 節點成功即返回
    /// </summary>
    [Serializable]
    internal class SelectorNode : CompositionNode { }
}
