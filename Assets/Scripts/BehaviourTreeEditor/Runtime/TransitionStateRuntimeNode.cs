using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

namespace BehaviourTreeEditor.Runtime
{
    [Serializable]
    public class TransitionStateRuntimeNode : CompilableNode
    {
        [field: SerializeField]
        public string TargetState { get; private set; }
        [field: SerializeField]
        public int TargetStateIndex { get; private set; }
        public TransitionStateRuntimeNode(string state)
        {
            TargetState = state;
        }

        public void SetTargetStateIndex(int targetStateIndex)
        {
            TargetStateIndex = targetStateIndex;
        }

        public override Expression ToExpression(Dictionary<Type, object> resolver)
        {
            if (!resolver.TryGetValue(typeof(IRuntimeGraphExecutor), out var executorObj) ||
                executorObj is not IRuntimeGraphExecutor executor)
                throw new InvalidOperationException("RuntimeGraph not found in resolver");

            // 要直接 Return，使用根節點的 ReturnExpression
            if (!resolver.TryGetValue(typeof(GotoExpression), out var gotoExpressionObj) ||
                gotoExpressionObj is not GotoExpression gotoExpression)
                throw new InvalidOperationException("GotoExpression not found in resolver");

            var callExp = Expression.Call(
                Expression.Constant(executor),
                IRuntimeGraphExecutor.ChangeStateMethod,
                Expression.Constant(TargetStateIndex));

            return Expression.Block(
                callExp,
                gotoExpression);
        }
    }
}