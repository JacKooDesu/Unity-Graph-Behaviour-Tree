using System;
using System.Collections.ObjectModel;
using BehaviourTreeEditor.Runtime;
using Unity.GraphToolkit.Editor;

namespace BehaviourTreeEditor.Editor
{
    [Serializable]
    [UseWithGraph(typeof(StateTreeGraph))]
    [Node("Variable", "", "Scope Variable Reference")]
    internal class VariableReference : VariableBindingReferenceBase
    {
        const string WRITE_PORT = "WRITE";

        internal override RuntimeNode Translate()
        {
            var source = GetSource();
            if (source is null) return new UndefinedRuntimeNode();

            var right = GetInputPortByName(WRITE_PORT).ToValueSource();
            return new WriteContextVariableRuntimeNode(source.Index, right as IValueSource);
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            base.OnDefinePorts(context);

            var source = GetSource();
            if (source is null) return;

            // 寫入流程
            context.DefineInputPort();
            if (!GetInputPortByName(NodeBuildUtil.INPUT_PORT).IsConnected)
                return;

            context.AddInputPort(WRITE_PORT)
                .WithDisplayName("Write")
                .WithDataType(source.ValueType)
                .Build();
        }

        protected override ReadOnlyCollection<IVariableBindingBlock> FetchBindings() =>
            BehaviourTreeEditor.Editor.Graph.VariableBindings;
    }
}
