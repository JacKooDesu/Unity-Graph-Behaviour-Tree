using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UExtensions;
using UnityEngine;

namespace BehaviourTreeEditor.Runtime
{
    [Serializable]
    public class ClassValueSource : IValueSource
    {
        [field: SerializeField]
        public SType Type { get; private set; }
        public Type TargetType => Type.Type;

        public ClassValueSource(SType type) => Type = type;

        public IEnumerable<Type> GetRequiredResolveTypes()
        {
            yield return TargetType;
        }

        public Expression ToExpression(Dictionary<Type, object> resolver)
        {
            var type = TargetType;
            if (!resolver.TryGetValue(type, out var reference))
                throw new ArgumentException($"No resolver for type {type}");

            // is method parameter
            if (reference is Expression parameter)
                return parameter;

            return Expression.Constant(reference, type);
        }

        public string DeclareVariableBinding(VariableHandler varHandler, ScriptBuilder sb)
        {
            return varHandler.GetMethodParameter(TargetType);
        }
    }
}
