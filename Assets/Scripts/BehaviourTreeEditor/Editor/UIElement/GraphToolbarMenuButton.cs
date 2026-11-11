using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;
using UGraph = Unity.GraphToolkit.Editor.Graph;

namespace BehaviourTreeEditor.Editor.UIElement
{
    [GraphToolbarElement("GraphToolBar", typeof(Graph), 1)]
    internal class GraphToolbarMenuButton : UnityEditor.UIElements.ToolbarMenu, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }

        public GraphToolbarMenuButton()
        {
            text = "Graph Action";

            menu.AppendAction("Create Sub State", CreateSubStateGraph);
        }

        private void CreateSubStateGraph(DropdownMenuAction action)
        {
            if (containerWindow is not IGraphWindow graphWindow ||
                graphWindow.Graph is not Graph graph)
            {
                UnityEngine.Debug.LogError("Cannot fetch graph window");
                return;
            }

            graph.UndoBeginRecordGraph("Add Sub State");

            var subGraph = graph.CreateLocalSubgraphNode<StateTreeGraph>("Name", UnityEngine.Vector2.zero).GetSubgraph();
            subGraph.CreateVariable<Untyped>("In", kind: VariableKind.Input);
            subGraph.CreateVariable<Untyped>("Out", kind: VariableKind.Output);

            graph.UndoEndRecordGraph();
        }
    }
}
