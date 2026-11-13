using System;
using System.Collections.Generic;
using System.Linq.Expressions;


namespace BehaviourTreeEditor.Runtime
{
    /// <summary>
    /// Represents an executor for a state in a runtime behavior tree.
    /// This class handles the execution of state-specific logic during different lifecycle stages: enter, tick, and exit.
    /// It compiles executable delegates from given expressions and manages state transitions.
    /// </summary>
    public class StateRuntimeNodeExecutor<TContext>
    {
        public delegate void ExecuteDelegate(ref TContext d);

        StateRuntimeNode _node;
        public int StateIndex => _node.StateIndex;
        public int ParentIndex => _node.ParentIndex;
        public int Depth => _node.Depth;
        public string StateName => _node.StateName;
        int _defaultNextStateIndex = -1;
        public int DefaultNextStateIndex => _defaultNextStateIndex;

        public ExecuteDelegate OnEnter { get; private set; }
        // public void OnEnter() => OnEnter?.Invoke();
        public ExecuteDelegate OnTick { get; private set; }
        // public void OnTick() => OnTick?.Invoke();
        public ExecuteDelegate OnExit { get; private set; }
        // public void OnExit() => OnExit?.Invoke();

        StateRuntimeNodeExecutor() { }

        public static StateRuntimeNodeExecutor<TContext> Create(
            StateRuntimeNode node,
            Dictionary<Type, object> resolver,
            IExecuteNodeBuilder<TContext> builder)
        {
            resolver[typeof(StateRuntimeNode)] = node;
            resolver[typeof(IVariableExpressionResolver)] = builder.GetVariableExpressionResolver();
            foreach (var (type, exp) in builder.GetParameters())
                resolver[type] = exp;

            return new()
            {
                _node = node,
                OnEnter = CompileFromExpression(resolver, node.OnEnter, builder),
                OnExit = CompileFromExpression(resolver, node.OnExit, builder),
                OnTick = CompileFromExpression(resolver, node.OnTick, builder),
                _defaultNextStateIndex = node.NextNodeIndices.Count > 0 ? node.NextNodeIndices[0] : -1
            };
        }

        static ExecuteDelegate CompileFromExpression<TB>(
            Dictionary<Type, object> resolver,
            List<CompilableNode> nodes,
            TB builder)
            where TB : IExecuteNodeBuilder<TContext>
        {
            if (nodes.Count == 0)
                return builder.CompileDelegate(Expression.Block(Expression.Empty()));

            var startNode = nodes[0];
            resolver[typeof(List<CompilableNode>)] = nodes;
            var exp = startNode.ToExpression(resolver);
            return builder.CompileDelegate(exp);
        }
    }
}
