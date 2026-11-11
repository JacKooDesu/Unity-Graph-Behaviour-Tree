using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UExtensions;
using UnityEngine;

namespace BehaviourTreeEditor.Runtime
{
    [Serializable]
    public class DefaultContext : ISTypeProvider
    {
        [Serializable]
        public class IVarWrapper { }
        [Serializable]
        public abstract class VarWrapper<T> : IVarWrapper
        {
            [SerializeField]
            public T Value;
        }
        [Serializable]
        public class IntVar : VarWrapper<int> { }
        [Serializable]
        public class FloatVar : VarWrapper<float> { }
        [Serializable]
        public class UIntVar : VarWrapper<uint> { }

        [field: SerializeField]
        public List<int> Path { get; private set; } = new();
        [field: SerializeReference]
        public List<IVarWrapper> Variables { get; private set; } = new();
        [field: SerializeReference]
        public List<IVarWrapper> Constants { get; private set; } = new();

        public static IExecuteNodeBuilder<DefaultContext> GetBuilder(RuntimeGraph graph) =>
            new DefaultBuilder(graph);
        private class DefaultBuilder : IExecuteNodeBuilder<DefaultContext>
        {
            readonly RuntimeGraph _graph;
            readonly ParameterExpression context_pExp;

            public DefaultBuilder(RuntimeGraph graph)
            {
                this._graph = graph;
                context_pExp = Expression.Parameter(typeof(DefaultContext).MakeByRefType());
            }

            public IEnumerable<(Type type, ParameterExpression exp)> GetParameters() { yield break; }
            public IVariableExpressionResolver<DefaultContext> GetVariableExpressionResolver() =>
                new DefaultVariableExpressionResolver(context_pExp, _graph.Variables, _graph.Constants);
            public StateRuntimeNodeExecutor<DefaultContext>.ExecuteDelegate CompileDelegate(Expression exp)
            {
                return Expression.Lambda<StateRuntimeNodeExecutor<DefaultContext>.ExecuteDelegate>(exp, context_pExp).Compile();
            }

            private class DefaultVariableExpressionResolver : IVariableExpressionResolver<DefaultContext>
            {
                List<Expression> readVariableExpressions = new();
                List<Expression> constantExpressions = new();

                public DefaultVariableExpressionResolver(
                    ParameterExpression context_pExp,
                    List<IVariableBinding> variables,
                    List<IVariableBinding> constants)
                {
                    if (!typeof(List<IVarWrapper>).TryGetIndexer(out var indexerProperty, out _))
                        throw new InvalidOperationException("List<IVarWrapper> does not have an indexer");

                    // context_exp > variables_exp
                    var variablesProperty = typeof(DefaultContext).GetProperty(nameof(Variables));
                    var enumExp = Expression.Property(context_pExp, variablesProperty);

                    readVariableExpressions = variables.Select((x, index) => BuildExp(x, index, enumExp)).ToList();

                    // context_exp > constants_exp
                    var constantsProperty = typeof(DefaultContext).GetProperty(nameof(Constants));
                    var enumExp_constants = Expression.Property(context_pExp, constantsProperty);
                    constantExpressions = constants.Select((x, index) => BuildExp(x, index, enumExp_constants)).ToList();

                    Expression BuildExp(IVariableBinding variable, int index, Expression sourceExp)
                    {
                        var type = typeof(DefaultContext.VarWrapper<>).MakeGenericType(variable.ValueType);
                        var itemAccessExp = Expression.Property(
                            enumExp,
                            indexerProperty,
                            Expression.Constant(index)
                        );

                        return Expression.Field
                        (
                            Expression.Convert(itemAccessExp, type),
                            type.GetField("Value")
                        );
                    }
                }

                public Expression WriteVariableExpression(int index, Expression value) =>
                    Expression.Assign(readVariableExpressions[index], value);
                public Expression ReadVariableExpression(int index) => readVariableExpressions[index];
                public Expression ConstantExpression(int index) => constantExpressions[index];
            }
        }
    }

    /// <summary>
    /// 測試用 MonoBehaviour
    /// </summary>
    public class DefaultRuntimeGraphExecutor : MonoBehaviour, IRuntimeGraphExecutor<DefaultContext>
    {
        public IRuntimeGraphExecutor<DefaultContext>.ResetDelegate ResetContextDelegate { get; private set; }

        [SerializeField]
        RuntimeGraph _runtimeGraph;
        public int CurrentStateIndex => Path[^1];

        [SerializeField]
        DefaultContext _context;

        List<int> Path => _context.Path;

        [SerializeField]
        List<SType> AutoInstanceTypes = new();
        public readonly List<object> AutoInstancesValues = new();

        readonly List<StateRuntimeNodeExecutor<DefaultContext>> _stateExecutors = new();

        readonly Dictionary<Type, object> _resolver = new();

        int _version;
        int _tempStateIndex;

        public void CompileGraph(
            RuntimeGraph graph,
            IEnumerable<IVariableBinding> variables,
            IEnumerable<IVariableBinding> constants)
        {
            _resolver.Add(typeof(RuntimeGraph), graph);
            _resolver.Add(typeof(IRuntimeGraphExecutor), this);

            foreach (var type in AutoInstanceTypes)
            {
                var obj = type.CreateInstance();
                AutoInstancesValues.Add(obj);
                _resolver.Add(type.Type, obj);
            }

            CompileNodes(graph);

            var defaultVariables = variables.Select(x => CreateParameter(x));
            var defaultConstants = constants.Select(x => CreateParameter(x));


            ResetContextDelegate = (ref DefaultContext context) =>
            {
                context.Variables.Clear();
                context.Variables.AddRange(defaultVariables);
                context.Constants.Clear();
                context.Constants.AddRange(defaultConstants);

                // 0 will be skipped
                AutoPathToLeafExecutor(0, ref context);
            };
        }

        static DefaultContext.IVarWrapper CreateParameter(IVariableBinding binding)
        {
            return binding switch
            {
                VariableBinding<int> i => new DefaultContext.IntVar { Value = i.Value },
                VariableBinding<float> f => new DefaultContext.FloatVar { Value = f.Value },
                VariableBinding<uint> ui => new DefaultContext.UIntVar { Value = ui.Value },
                _ => throw new NotImplementedException()
            };
        }

        void CompileNodes(RuntimeGraph graph)
        {
            _stateExecutors.Clear();

            string stateName = string.Empty;
            try
            {
                var builder = DefaultContext.GetBuilder(graph);

                foreach (var state in graph.States)
                {
                    stateName = state.StateName;
                    _stateExecutors.Add(StateRuntimeNodeExecutor<DefaultContext>
                        .Create(state, _resolver, builder));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to compile state {stateName}: {e}");
                throw;
            }
        }

        public void RegisterInstance<T>(T obj)
        {
            _resolver.Add(typeof(T), obj);
        }

        public void ChangeState(int index)
        {
            _tempStateIndex = index;
            _version++;
        }

        void Transition(
            int nextIndex,
            ref DefaultContext context)
        {
#if UNITY_EDITOR
            string fromPath = GetPathString();
#endif
            var currentIndex = CurrentStateIndex;
            var lca = Lca(currentIndex, nextIndex);

            var path = Path;
            int lcaInPathIndex = -1;
            for (int i = path.Count - 1; i >= 0; i--)
            {
                if (path[i] == lca)
                {
                    lcaInPathIndex = i;
                    break;
                }
                _stateExecutors[i].OnExit(ref context);
                path.RemoveAt(i);
            }

            lcaInPathIndex = lcaInPathIndex < 0 ? 0 : lcaInPathIndex;

            for (int i = nextIndex; i != lca; i = _stateExecutors[i].ParentIndex)
                Path.Add(_stateExecutors[i].StateIndex);

            for (int i = lcaInPathIndex; i < path.Count; i++)
                _stateExecutors[path[i]].OnEnter(ref context);

            // 如果不是葉節點，需要進入預設狀態
            AutoPathToLeafExecutor(nextIndex, ref context);

#if UNITY_EDITOR
            string newPath = GetPathString();
            Debug.Log($"[ChangeState] <color=cyan>{newPath}</color>\n[Old Path]\t<color=yellow>{fromPath}</color>");
#endif
        }

        string GetPathString() =>
            string.Join("<color=grey>→</color>", Path.Select(i => _stateExecutors[i].StateName));

        void AutoPathToLeafExecutor(int currentIndex, ref DefaultContext context)
        {
            var currentExecutor = _stateExecutors[currentIndex];
            while (currentExecutor.DefaultNextStateIndex != -1)
            {
                currentExecutor = _stateExecutors[currentExecutor.DefaultNextStateIndex];
                currentExecutor.OnEnter(ref context);

                Path.Add(currentExecutor.StateIndex);
            }
        }

        int Lca(int fromIndex, int toIndex)
        {
            var lca = _stateExecutors.LcaExecutor(fromIndex, toIndex);
            return lca.StateIndex;
            // HashSet<int> visited = new();
            // for (var executor = _stateExecutors[fromIndex];
            //     executor.ParentIndex != -1;
            //     executor = _stateExecutors[executor.ParentIndex])
            //     visited.Add(executor.StateIndex);

            // for (var executor = _stateExecutors[toIndex];
            //     executor.ParentIndex != -1;
            //     executor = _stateExecutors[executor.ParentIndex])
            //     if (visited.Contains(executor.StateIndex))
            //         return executor.StateIndex;

            // return -1;
        }

        void Start()
        {
            _Start();

#if UNITY_EDITOR
            _runtimeGraph.RegisterOnGraphUpdatedHandler(newGraph =>
            {
                Debug.Log("Recompile Triggered");
                this._runtimeGraph = newGraph;
                CompileNodes(_runtimeGraph);
            });
#endif
        }

        void _Start()
        {
            // FIXME: use compilable graph setting to override var / const
            var variables = _runtimeGraph.Variables;
            var constants = _runtimeGraph.Constants;
            CompileGraph(_runtimeGraph, variables, constants);
            ResetContextDelegate(ref _context);
        }

        public void Execute(ref DefaultContext context)
        {
            const int MAX_BREAK_PATH = 5;
            int breakPathCount = 0;

            while (breakPathCount < MAX_BREAK_PATH)
            {
                int oldVersion = _version;
                bool hasBreakPath = false;

                foreach (var index in Path)
                {
                    _stateExecutors[index].OnTick(ref context);
                    if (_version != oldVersion)
                    {
                        Transition(_tempStateIndex, ref context);
                        hasBreakPath = true;
                        break;
                    }
                }

                if (!hasBreakPath)
                    return;

                breakPathCount++;
            }

            Debug.LogError("Break path count exceeded maximum");
        }

        void FixedUpdate()
        {
            Execute(ref _context);
        }

        public unsafe void ChangeStatePath(int* path, int* newPath, int pathLength)
        {
            for (int i = 0; i < pathLength; i++)
                path[i] = newPath[i];
        }
    }
}
