using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;

namespace BehaviourTreeEditor.Runtime
{
    [Serializable]
    public class CompositionRuntimeNode : CompilableNode
    {
        public enum EMode
        {
            Default,
            ReturnOnSuccess,
            ReturnOnFailure,
        }

        public CompositionRuntimeNode(EMode mode)
        {
            Mode = mode;
        }

        [field: SerializeField]
        public EMode Mode { get; private set; }

        public override Expression ToExpression(Dictionary<Type, object> resolver)
        {
            var nodeList = GetNodeList(resolver);

            var returnLabel = Expression.Label();
            var returnExp = Expression.Return(returnLabel);

            var expressions = new Queue<Expression>();
            foreach (var nodeIndex in NextNodeIndices)
            {
                var node = nodeList[nodeIndex];

                switch (node)
                {
                    case ConditionRuntimeNode cn:
                        var afterConditionExp = Expression.Block(node
                            .NextNodeIndices
                            .Select(x => nodeList[x].ToExpression(resolver)));
                        expressions.Enqueue(BuildIfCondition(
                            resolver, cn, afterConditionExp, returnExp));
                        break;
                    default:
                        expressions.Enqueue(node.ToExpression(resolver));
                        break;
                }
            }
            expressions.Enqueue(Expression.Label(returnLabel));

            return Expression.Block(expressions);
        }

        public override void BuildScript(
            List<CompilableNode> contextNodes,
            VariableHandler varHandler,
            ScriptBuilder sb)
        {
            // sb.BeginScope();
            foreach (var nodeIndex in NextNodeIndices)
            {
                var node = contextNodes[nodeIndex];
                switch (node)
                {
                    case ConditionRuntimeNode cn:
                        BuildIfCondition(cn);
                        break;
                    default:
                        node.BuildScript(contextNodes, varHandler, sb);
                        break;
                }
            }
            // sb.EndScope();

            void BuildIfCondition(ConditionRuntimeNode node)
            {
                // if condition
                node.BuildScript(contextNodes, varHandler, sb);

                switch (Mode)
                {
                    case EMode.Default:
                        sb.BeginScope();
                        BuildInnerScript(node);
                        sb.EndScope();
                        break;
                    case EMode.ReturnOnSuccess:
                        sb.BeginScope();
                        BuildInnerScript(node);
                        sb.AppendLine("return;");
                        sb.EndScope();
                        break;
                    case EMode.ReturnOnFailure:
                        sb.BeginScope();
                        BuildInnerScript(node);
                        sb.EndScope();
                        sb.AppendLine("else { return; }");
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            void BuildInnerScript(ConditionRuntimeNode node) => node.NextNodeIndices
                .ForEach(index => contextNodes[index].BuildScript(contextNodes, varHandler, sb));
        }

        Expression BuildIfCondition(
            Dictionary<Type, object> resolver,
            ConditionRuntimeNode node,
            Expression afterConditionExp,
            GotoExpression returnExp)
        {
            var conditionExp = node.ToExpression(resolver);
            var exp = Mode switch
            {
                EMode.Default => Expression.IfThen(conditionExp, afterConditionExp),
                EMode.ReturnOnSuccess => Expression.IfThen(
                    conditionExp,
                    Expression.Block(
                        afterConditionExp,
                        returnExp)),
                EMode.ReturnOnFailure => Expression.IfThenElse(
                    conditionExp,
                    afterConditionExp,
                    returnExp),
                _ => throw new NotImplementedException()
            };
            return exp;
        }
    }
}
