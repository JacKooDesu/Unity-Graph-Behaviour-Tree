using System;

namespace BehaviourTreeEditor.Editor
{
    /// <summary>
    /// 順序執行，任意 Condtion 節點失敗即返回
    /// </summary>
    [Serializable]
    internal class SequenceNode : CompositionNode { }
}
