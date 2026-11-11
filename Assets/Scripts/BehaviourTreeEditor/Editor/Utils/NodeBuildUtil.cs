using Unity.GraphToolkit.Editor;
using Context = Unity.GraphToolkit.Editor.Node.IPortDefinitionContext;

namespace BehaviourTreeEditor.Editor
{
    internal static class NodeBuildUtil
    {
        // flow port id
        public const string OUTPUT_PORT = "OUTPUT";
        public const string INPUT_PORT = "INPUT";

        public static void DefineInputPort(this Context context)
        {
            context.AddInputPort(INPUT_PORT)
                .WithDisplayName(string.Empty)
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        public static void DefineOutputPort(this Context context)
        {
            context.AddOutputPort(OUTPUT_PORT)
                .WithDisplayName(string.Empty)
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }
    }
}
