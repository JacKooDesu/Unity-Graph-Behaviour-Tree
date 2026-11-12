using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

namespace BehaviourTreeEditor.Runtime
{
    [Serializable]
    public class LogicalValueSource : ValueSource<bool>
    {
        [field: SerializeField]
        public ELogic Operator { get; private set; }
        [field: SerializeReference]
        public IValueSource Left { get; private set; }
        [field: SerializeReference]
        public IValueSource Right { get; private set; }

        public LogicalValueSource(
            ELogic operatorType,
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
                ELogic.And => Expression.AndAlso(left, right),
                ELogic.Or => Expression.OrElse(left, right),
                ELogic.Xor => Expression.ExclusiveOr(left, right),
                ELogic.Not => Expression.Not(left),
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

        public override VariableAccess DeclareVariableBinding(
            VariableHandler varHandler, ScriptBuilder sb)
        {
            var left = varHandler.GetOrRegisterVariableBinding(Left, sb);
            var right = varHandler.GetOrRegisterVariableBinding(Right, sb);

            var op = Operator switch
            {
                ELogic.And => "&&",
                ELogic.Or => "||",
                ELogic.Xor => "^",
                ELogic.Not => "!",
                _ => throw new NotImplementedException(),
            };

            var name = varHandler.GetCurrentLocalName();

            var value = Operator switch
            {
                ELogic.Not => $"{op}{left}",
                _ => $"{left} {op} {right}",
            };

            sb.Declare(name, value, false);

            return new LocalVariableAccess(name);
        }
    }
}
