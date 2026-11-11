using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UExtensions;
using UnityEngine;

namespace BehaviourTreeEditor.Runtime
{
    [CreateAssetMenu(menuName = "Behaviour Tree Editor/Transpiler")]
    public class Transpiler : ScriptableObject
    {
        [SerializeField]
        List<SType> _contextTypes = new();

        [SerializeField]
        List<RuntimeGraph> _runtimeGraphs = new();

        [ContextMenu("Test Transpile")]
        public void Transpile()
        {
            var graph = _runtimeGraphs.FirstOrDefault();

            if (graph == null)
                return;

            var state = graph.States.FirstOrDefault(x => x.ParentIndex != -1);
            if (state == null)
                return;

            var nodes = state.OnEnter;
            using var sb = new ScriptBuilder();
            nodes[0].BuildScript(nodes, new DefaultVariableHandler(graph, _contextTypes.Select(x => x.Type)), sb);
            Debug.Log(sb.GetScript());
            GUIUtility.systemCopyBuffer = sb.GetScript();
        }
    }

    public abstract class TranspilerBase : ScriptableObject
    {
        public abstract VariableHandler VariableHandler();
    }
}
