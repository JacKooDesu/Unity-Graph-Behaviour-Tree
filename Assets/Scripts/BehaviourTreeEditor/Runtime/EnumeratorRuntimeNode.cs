using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UExtensions;
using UnityEngine;

namespace BehaviourTreeEditor.Runtime
{
    /// <summary>
    /// 遍歷集合的 RuntimeNode，只有一個連接點有效
    /// </summary>
    [Serializable]
    public class EnumeratorRuntimeNode : CompilableNode
    {
        [field: SerializeReference]
        public IValueSource ValueSource { get; private set; }
        [field: SerializeField]
        public SType ElementType { get; private set; }

        /// <summary>
        /// 基於 Stack 取得當前 scope 的 handle 暫存的 ValueSource
        /// </summary>
        [Serializable]
        public class EnumerableScopeValueSource : IValueSource
        {
            public Type TargetType => null;

            public string DeclareVariableBinding(VariableHandler varHandler, ScriptBuilder sb)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<Type> GetRequiredResolveTypes() => Enumerable.Empty<Type>();

            public Expression ToExpression(Dictionary<Type, object> resolver)
            {
                if (!resolver.TryGetValue(typeof(Stack<EnumeratingExpressionHandle>), out var handleDictObj) ||
                    handleDictObj is not Stack<EnumeratingExpressionHandle> handleStack)
                    throw new InvalidOperationException("Handle stack not found in resolver");

                if (!handleStack.TryPeek(out var handle))
                    throw new InvalidOperationException("Handle stack is empty");

                return handle.ItemExp;
            }
        }

        class EnumeratingExpressionHandle
        {
            public Expression ItemExp { get; set; }
            public Expression IndexExp { get; set; }
        }

        public EnumeratorRuntimeNode(IValueSource targetSource)
        {
            var sourceType = targetSource.TargetType;

            if (!sourceType.TryGetEnumerableType(out var elementType))
                throw new ArgumentException($"Invalid source type: {sourceType}");

            ValueSource = targetSource;
            ElementType = new(elementType);
        }

        public override Expression ToExpression(Dictionary<Type, object> resolver)
        {
            if (NextNodeIndices.Count == 0)
                return default;

            var nodeList = GetNodeList(resolver);

            if (!ValueSource.TargetType.TryGetEnumerableType(out var elementType))
                throw new InvalidOperationException("Target type is not enumerable");

            if (!resolver.TryGetValue(typeof(Stack<EnumeratingExpressionHandle>), out var handleDictObj))
            {
                handleDictObj = new Stack<EnumeratingExpressionHandle>();
                resolver[typeof(Stack<EnumeratingExpressionHandle>)] = handleDictObj;
            }

            if (handleDictObj is not Stack<EnumeratingExpressionHandle> handleStack)
                throw new InvalidOperationException("Handle list not found in resolver");

            var nextNode = nodeList[NextNodeIndices[0]];

            EnumeratingExpressionHandle handle = new();
            handleStack.Push(handle);

            var enumerateTargetExp = ValueSource.ToExpression(resolver);
            // assign index to handle
            var indexVar = Expression.Variable(typeof(int), "index");
            handle.IndexExp = indexVar;

            var mode = GetExpressionEnumerateMode(ValueSource.TargetType);

            Expression block =
                mode switch
                {
                    ArrayMode => ArrayExpression(
                        () => nextNode.ToExpression(resolver),
                        enumerateTargetExp,
                        indexVar,
                        handle),
                    IndexerMode(var indexerProperty, var countProperty) => IndexerExpression(
                        () => nextNode.ToExpression(resolver),
                        enumerateTargetExp,
                        indexerProperty,
                        countProperty,
                        indexVar,
                        handle),
                    EnumeratorMode => EnumeratorExpression(
                        () => nextNode.ToExpression(resolver),
                        ValueSource.TargetType,
                        elementType,
                        enumerateTargetExp,
                        indexVar,
                        handle),
                    _ => throw new InvalidProgramException("Cannot convert to expression"),
                };

            // Exit, pop handle
            if (!handleStack.TryPop(out var topHandle) ||
                topHandle != handle)
                throw new InvalidProgramException("Cannot convert to expression");

            return block is null ?
                throw new InvalidProgramException("Cannot convert to expression") : block;
        }

        Expression ArrayExpression(
            Func<Expression> deferInnerExpFunc,
            Expression enumerateTargetExp,
            ParameterExpression indexVar,
            EnumeratingExpressionHandle handle)
        {
            // Indexer 實作表示可 RW，使用 ArrayAccess
            var itemAccessExp = Expression.ArrayAccess(enumerateTargetExp, indexVar);
            handle.ItemExp = itemAccessExp;
            var innerExp = deferInnerExpFunc();

            var lengthVar = Expression.Variable(typeof(int), "length");

            var breakLabel = Expression.Label("Break");
            var continueLabel = Expression.Label("Continue");

            return Expression.Block(
                new[] { indexVar, lengthVar },
                Expression.Assign(indexVar, Expression.Constant(0)),
                Expression.Assign(lengthVar, Expression.ArrayLength(enumerateTargetExp)),
                Expression.Loop(
                    Expression.IfThenElse(
                        Expression.LessThan(indexVar, lengthVar),
                        Expression.Block(
                            innerExp,
                            Expression.PostIncrementAssign(indexVar)
                        ),
                        Expression.Break(breakLabel)
                    ),
                    breakLabel,
                    continueLabel
                )
            );
        }

        Expression IndexerExpression(
            Func<Expression> deferInnerExpFunc,
            Expression enumerateTargetExp,
            PropertyInfo indexerProperty,
            PropertyInfo countProperty,
            ParameterExpression indexVar,
            EnumeratingExpressionHandle handle)
        {
            // index property rw, auto use set if on left
            var itemAccessExp = Expression.Property(
                enumerateTargetExp,
                indexerProperty,
                indexVar);
            handle.ItemExp = itemAccessExp;
            var innerExp = deferInnerExpFunc();

            var countVar = Expression.Variable(typeof(int), "count");

            var breakLabel = Expression.Label("Break");
            var continueLabel = Expression.Label("Continue");

            return Expression.Block(
                new[] { indexVar, countVar },
                Expression.Assign(indexVar, Expression.Constant(0)),
                Expression.Assign(countVar, Expression.Property(enumerateTargetExp, countProperty)),
                Expression.Loop(
                    Expression.IfThenElse(
                        Expression.LessThan(indexVar, countVar),
                        Expression.Block(
                            innerExp,
                            Expression.PostIncrementAssign(indexVar)
                        ),
                        Expression.Break(breakLabel)
                    ),
                    breakLabel,
                    continueLabel
                )
            );
        }

        Expression EnumeratorExpression(
            Func<Expression> deferInnerExpFunc,
            Type targetType,
            Type elementType,
            Expression enumerateTargetExp,
            ParameterExpression indexVar,
            EnumeratingExpressionHandle handle)
        {
            // RO（只能在表達式右側）
            var itemVarExp = Expression.Variable(elementType, "item");
            handle.ItemExp = itemVarExp;
            var innerExp = deferInnerExpFunc();

            var enumeratorType = typeof(IEnumerator<>).MakeGenericType(elementType);
            var getEnumeratorMethod = targetType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .First(m => m.ReturnType == enumeratorType);
            // move next is in IEnumerator interface, not IEnumerator<>
            var moveNextMethod = typeof(IEnumerator)
                .GetMethod(nameof(IEnumerator.MoveNext));
            var currentProperty = enumeratorType
                .GetProperty(nameof(IEnumerator.Current));

            var enumeratorVar = Expression.Variable(enumeratorType, "enumerator");

            var breakLabel = Expression.Label("break");

            return Expression.Block(
                new[] { enumeratorVar, itemVarExp, indexVar },
                Expression.Assign(indexVar, Expression.Constant(0)),
                Expression.Assign(enumeratorVar, Expression.Call(enumerateTargetExp, getEnumeratorMethod)),
                Expression.TryFinally(
                    Expression.Loop(
                        Expression.IfThenElse(
                            Expression.Call(enumeratorVar, moveNextMethod),
                            Expression.Block(
                                Expression.Assign(
                                    itemVarExp,
                                    Expression.Property(enumeratorVar, currentProperty)),
                                innerExp,
                                Expression.PostIncrementAssign(indexVar)
                            ),
                            Expression.Break(breakLabel)
                        ),
                        breakLabel
                    ),
                    Expression.Call(
                        enumeratorVar,
                        typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose))
                    )
                )
            );
        }

        public override void BuildScript(
            List<CompilableNode> contextNodes, VariableHandler varHandler, ScriptBuilder sb)
        {
            var enumerableVar = varHandler.GetOrRegisterVariableBinding(ValueSource, sb);

            var mode = GetEnumerateMode(ValueSource.TargetType);
            // enumerate begin
            var iterScope = varHandler.BeginIterScope(enumerableVar);
            var iterIndexName = varHandler.GetCurrentIterIndexName();
            var itemName = varHandler.GetCurrentIterItemName();
            switch (mode)
            {
                case EEnumerateMode.Array:
                    sb.AppendLine($"for (int {iterIndexName} = 0; {iterIndexName} < {enumerableVar}.Length; {iterIndexName}++)");
                    sb.BeginScope();
                    BuildInnerScript();
                    sb.EndScope();
                    break;

                case EEnumerateMode.Indexer:
                    sb.AppendLine($"for (int {iterIndexName} = 0; {iterIndexName} < {enumerableVar}.Count; {iterIndexName}++)");
                    sb.BeginScope();
                    BuildInnerScript();
                    sb.EndScope();
                    break;

                case EEnumerateMode.Enumerator:
                    sb.AppendLine($"foreach (var {itemName} in {enumerableVar})");
                    sb.BeginScope();
                    BuildInnerScript();
                    sb.EndScope();
                    break;
            }
            varHandler.EndIterScope(iterScope);

            void BuildInnerScript() => NextNodeIndices
                .ForEach(index => contextNodes[index].BuildScript(contextNodes, varHandler, sb));
        }

        static ExpressionEnumerateMode GetExpressionEnumerateMode(Type targetType)
        {
            if (targetType.IsArray)
                return new ArrayMode();
            else if (targetType.TryGetIndexer(out var indexerProperty, out var countProperty))
                return new IndexerMode(indexerProperty, countProperty);
            else
                return new EnumeratorMode();
        }

        static EEnumerateMode GetEnumerateMode(Type targetType)
        {
            if (targetType.IsArray)
                return EEnumerateMode.Array;
            else if (targetType.TryGetIndexer(out _, out _))
                return EEnumerateMode.Indexer;
            else
                return EEnumerateMode.Enumerator;
        }

        private abstract record ExpressionEnumerateMode();
        private record ArrayMode : ExpressionEnumerateMode;
        private record IndexerMode(PropertyInfo IndexerProperty, PropertyInfo CountProperty) : ExpressionEnumerateMode;
        private record EnumeratorMode : ExpressionEnumerateMode;

        private enum EEnumerateMode
        {
            Array,
            Indexer,
            Enumerator,
        }
    }
}
