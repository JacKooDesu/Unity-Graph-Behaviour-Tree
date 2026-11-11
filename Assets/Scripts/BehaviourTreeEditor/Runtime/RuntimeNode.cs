using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;

namespace BehaviourTreeEditor.Runtime
{
    [Serializable]
    public abstract class RuntimeNode
    {
        public List<int> NextNodeIndices = new();
    }

    [Serializable]
    public abstract class CompilableNode : RuntimeNode
    {
        public abstract Expression ToExpression(Dictionary<Type, object> resolver);
        public virtual void BuildScript(
            List<CompilableNode> contextNodes,
            VariableHandler varHandler,
            ScriptBuilder sb)
        { }
        public virtual IEnumerable<Type> GetRequiredResolveTypes() => Enumerable.Empty<Type>();

        protected static List<CompilableNode> GetNodeList(Dictionary<Type, object> resolver)
        {
            if (!resolver.TryGetValue(typeof(List<CompilableNode>), out var nodeListObj) ||
                nodeListObj is not List<CompilableNode> nodeList)
                throw new InvalidOperationException("Node list not found in resolver");

            return nodeList;
        }
    }

    [Serializable]
    public class UndefinedRuntimeNode : RuntimeNode { }

    [Serializable]
    public class StateRuntimeNode : RuntimeNode
    {
        /// <summary>
        /// State 深度，快取用於 LCA 運算。
        /// </summary>
        [field: SerializeField]
        public int Depth { get; private set; }

        [field: SerializeField]
        public string StateName { get; private set; }
        [field: SerializeField]
        public int StateIndex { get; private set; }
        [field: SerializeField]
        public int ParentIndex { get; private set; }
        [field: SerializeReference]
        public List<CompilableNode> OnEnter { get; private set; } = new();
        [field: SerializeReference]
        public List<CompilableNode> OnExit { get; private set; } = new();
        [field: SerializeReference]
        public List<CompilableNode> OnTick { get; private set; } = new();

        public IEnumerable<CompilableNode> GetAllCompilableNodes() =>
            OnEnter.Concat(OnExit).Concat(OnTick);

        public StateRuntimeNode(
            string stateName, int stateIndex)
        {
            StateName = stateName;
            StateIndex = stateIndex;
        }
        public void SetParentIndex(int index) => ParentIndex = index;
        public void SetDepth(int depth) => Depth = depth;
    }
}
