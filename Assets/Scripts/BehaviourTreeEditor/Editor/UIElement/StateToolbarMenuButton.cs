using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace BehaviourTreeEditor.Editor.UIElement
{
    [GraphToolbarElement("StateTreeGraphToolbar", typeof(StateTreeGraph), 1)]
    internal class StateToolbarMenuButton : ToolbarMenu, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }

        public StateToolbarMenuButton()
        {
            text = "State Graph Action";
            menu.AppendAction("Interactive Creator", InteractiveCreator);
        }

        private void InteractiveCreator(DropdownMenuAction action)
        {
            if (containerWindow is not IGraphWindow graphWindow ||
                graphWindow.Graph is not StateTreeGraph graph)
            {
                UnityEngine.Debug.LogError("Cannot fetch graph window");
                return;
            }

            UnityEditor.PopupWindow.Show(this.worldBound, new InteractiveNodeAddWindow(graph));
        }
    }
}
