using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

namespace BehaviourTreeEditor.Runtime
{
    [Serializable]
    public class EqValueSource : ValueSource<bool>
    {
        [field: SerializeReference]
        public IValueSource Left { get; private set; }
        [field: SerializeReference]
        public IValueSource Right { get; private set; }
        [field: SerializeField]
        public EEquality Operator { get; private set; }

        public EqValueSource(
            EEquality operatorType,
            IValueSource left,
            IValueSource right)
        {
            Operator = operatorType;
            Left = left;
            Right = right;
        }

        public override Expression ToExpression(Dictionary<Type, object> resolver)
        {
            var left = Left.ToExpression(resolver);
            var right = Right.ToExpression(resolver);
            return Operator switch
            {
                EEquality.Equal => Expression.Equal(left, right),
                EEquality.Less => Expression.LessThan(left, right),
                EEquality.Greater => Expression.GreaterThan(left, right),
                EEquality.LessOrEqual => Expression.LessThanOrEqual(left, right),
                EEquality.GreaterOrEqual => Expression.GreaterThanOrEqual(left, right),
                _ => throw new NotImplementedException(),
            };
        }

        public override IEnumerable<Type> GetRequiredResolveTypes()
        {
            foreach (var type in Left.GetRequiredResolveTypes())
                yield return type;
            foreach (var type in Right.GetRequiredResolveTypes())
                yield return type;
        }

        public override VariableAccess DeclareVariableBinding(VariableHandler varHandler, ScriptBuilder sb)
        {
            var left = varHandler.GetOrRegisterVariableBinding(Left, sb);
            var right = varHandler.GetOrRegisterVariableBinding(Right, sb);
            var op = Operator switch
            {
                EEquality.Equal => "==",
                EEquality.Less => "<",
                EEquality.Greater => ">",
                EEquality.LessOrEqual => "<=",
                EEquality.GreaterOrEqual => ">=",
                _ => throw new NotImplementedException(),
            };

            var name = varHandler.GetCurrentLocalName();

            sb.Declare(name, $"{left} {op} {right}", false);

            return new LocalVariableAccess(name);
        }
    }
}
