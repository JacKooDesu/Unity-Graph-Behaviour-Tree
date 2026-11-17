using System;
using Unity.GraphToolkit.Editor;

namespace BehaviourTreeEditor.Editor
{
    [Serializable]
    [UseWithContext(typeof(VariableBindings))]
    internal class IntBinding : VariableBindingBlockBase<int> { }
}
