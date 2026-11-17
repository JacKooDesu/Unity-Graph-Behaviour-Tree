using System;
using System.Collections.ObjectModel;
using Unity.GraphToolkit.Editor;

namespace BehaviourTreeEditor.Editor
{
    [Serializable]
    [UseWithGraph(typeof(StateTreeGraph))]
    [Node("Variable", "", "Scope Constant")]
    internal class ConstantReference : VariableBindingReferenceBase
    {
        protected override ReadOnlyCollection<IVariableBindingBlock> FetchBindings() =>
            BehaviourTreeEditor.Editor.Graph.ConstantBindings;
    }
}
