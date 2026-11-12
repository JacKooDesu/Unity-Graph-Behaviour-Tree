using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

namespace BehaviourTreeEditor.Runtime
{
    [Serializable]
    public class ConstantValueSource<T> : ValueSource<T>
    {
        [field: SerializeField]
        public T Value { get; private set; }
        public ConstantValueSource(T value) => Value = value;
        public override Expression ToExpression(Dictionary<Type, object> resolver) =>
            Expression.Constant(Value, TargetType);
        public override IEnumerable<Type> GetRequiredResolveTypes()
        {
            if (Value is ValueSource<T> source)
                foreach (var type in source.GetRequiredResolveTypes())
                    yield return type;
        }

        public override VariableAccess DeclareVariableBinding(
            VariableHandler varHandler, ScriptBuilder sb)
        {
            var name = varHandler.GetCurrentLocalName();

            var valueStr = string.Format(Common.CustomFormatter.Singleton, "{0}", Value);

            sb.Declare(name, valueStr, false);

            return new LocalVariableAccess(name);
        }
    }
}
