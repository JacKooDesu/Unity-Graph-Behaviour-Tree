using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Unity.Collections;


namespace BehaviourTreeEditor.Runtime
{
    public interface IExecuteNodeBuilder<TContext>
    {
        IEnumerable<(Type type, ParameterExpression exp)> GetParameters();
        IVariableExpressionResolver<TContext> GetVariableExpressionResolver();
        StateRuntimeNodeExecutor<TContext>.ExecuteDelegate CompileDelegate(Expression exp);
    }

    public interface IExecutor<TContext>
    {
        void Execute(ref TContext context);
    }

    public interface IRuntimeGraphExecutor
    {
        public static MethodInfo ChangeStateMethod =
            typeof(IRuntimeGraphExecutor).GetMethod(nameof(ChangeState));

        /// <summary>
        /// Common Graph Executor Compiler Entry
        /// </summary>
        /// <param name="graph">Target Runtime Graph to compile</param>
        /// <param name="variables">Variables default value binding</param>
        /// <param name="constants">Constants defulat value binding</param>
        void CompileGraph(RuntimeGraph graph, IEnumerable<IVariableBinding> variables, IEnumerable<IVariableBinding> constants);
        unsafe void ChangeStatePath(int* path, int* newPath, int pathLength);
        void ChangeState(int index);
    }

    public interface IRuntimeGraphExecutor<TContext> : IRuntimeGraphExecutor
    {
        public delegate void ResetDelegate(ref TContext context);
        ResetDelegate ResetContextDelegate { get; }
        void Execute(ref TContext context);
    }

    public interface IVariableExpressionResolver
    {
        Expression WriteVariableExpression(int index, Expression value);
        Expression ReadVariableExpression(int index);
        Expression ConstantExpression(int index);
    }

    // constrait
    public interface IVariableExpressionResolver<TContext> : IVariableExpressionResolver { }

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

    public static class ExecutorExtension
    {
        /// <summary>
        /// 基於 Depth 搜索共同祖先
        /// </summary>
        public static StateRuntimeNodeExecutor<TContext> LcaExecutor<TContext>(
            this IList<StateRuntimeNodeExecutor<TContext>> executors,
            int fromIndex,
            int toIndex)
        {
            var a = executors[fromIndex];
            var b = executors[toIndex];

            // 對齊深度
            while (a.Depth > b.Depth)
                a = executors[a.ParentIndex];
            while (b.Depth > a.Depth)
                b = executors[b.ParentIndex];

            while (a != b)
            {
                a = executors[a.ParentIndex];
                b = executors[b.ParentIndex];
            }

            return a;
        }
    }
}
