using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

namespace BehaviourTreeEditor.Runtime
{
    [Serializable]
    public class ConditionRuntimeNode : CompilableNode
    {
        [field: SerializeField]
        public EConditionType Condition { get; private set; }
        // [field: SerializeField]
        // public ValueType ValueType { get; private set; }
        [field: SerializeReference]
        public IValueSource Left { get; private set; }
        [field: SerializeReference]
        public IValueSource Right { get; private set; }

        public override Expression ToExpression(Dictionary<Type, object> resolver)
        {
            var left = Left.ToExpression(resolver);
            var right = Right.ToExpression(resolver);
            return Condition switch
            {
                EConditionType.Equal => Expression.Equal(left, right),
                EConditionType.NotEqual => Expression.NotEqual(left, right),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public override void BuildScript(
            List<CompilableNode> contextNodes,
            VariableHandler varHandler,
            ScriptBuilder sb)
        {
            var left = varHandler.GetOrRegisterVariableBinding(Left,sb);
            var right = varHandler.GetOrRegisterVariableBinding(Right, sb);

            sb.AppendLine($"if ({left} == {right})");
        }

        public override IEnumerable<Type> GetRequiredResolveTypes()
        {
            foreach (var type in Left.GetRequiredResolveTypes())
                yield return type;
            foreach (var type in Right.GetRequiredResolveTypes())
                yield return type;
        }

        public ConditionRuntimeNode(
            EConditionType condition,
            IValueSource left,
            IValueSource right)
        {
            Condition = condition;
            Left = left;
            Right = right;
        }
    }

    [Serializable]
    public class ConditionRuntimeNode<T> : ConditionRuntimeNode
    {
        public ConditionRuntimeNode(
            EConditionType condition,
            IValueSource<T> left,
            IValueSource<T> right) : base(condition, left, right) { }
    }
}
