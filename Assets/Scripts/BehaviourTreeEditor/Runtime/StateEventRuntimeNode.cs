using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

namespace BehaviourTreeEditor.Runtime
{
    [Serializable]
    public class StateEventRuntimeNode : CompilableNode
    {
        public override Expression ToExpression(Dictionary<Type, object> resolver)
        {
            var nodeList = GetNodeList(resolver);

            var returnLabel = Expression.Label();
            var returnExp = Expression.Return(returnLabel);

            resolver[typeof(GotoExpression)] = returnExp;

            var expressions = new Queue<Expression>();
            foreach (var nodeIndex in NextNodeIndices)
            {
                var node = nodeList[nodeIndex];
                expressions.Enqueue(node.ToExpression(resolver));
            }
            expressions.Enqueue(Expression.Label(returnLabel));

            return Expression.Block(expressions);
        }

        public override void BuildScript(
            List<CompilableNode> contextNodes,
            VariableHandler varHandler,
            ScriptBuilder sb)
        {
            sb.BeginScope();
            foreach (var nodeIndex in NextNodeIndices)
            {
                var node = contextNodes[nodeIndex];
                node.BuildScript(contextNodes, varHandler, sb);
            }
            sb.EndScope();
        }
    }
}
