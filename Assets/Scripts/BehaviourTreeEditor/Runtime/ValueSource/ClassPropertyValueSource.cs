using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UExtensions;
using UnityEngine;

namespace BehaviourTreeEditor.Runtime
{
    [Serializable]
    public class ClassPropertyValueSource : IValueSource
    {
        public Type TargetType => PropertyData.Property.PropertyType;
        [field: SerializeField]
        public SProperty PropertyData { get; private set; }
        public ClassPropertyValueSource(SProperty property) => PropertyData = property;

        public Expression ToExpression(Dictionary<Type, object> resolver)
        {
            var property = PropertyData.Property;

            if (!property.CanRead)
                throw new ArgumentException($"Property {property.Name} is not readable");

            if (property.GetGetMethod().IsStatic)
                return Expression.Property(null, property);

            if (!resolver.TryGetValue(PropertyData.Type, out var reference))
                throw new ArgumentException($"No resolver for type {TargetType}");

            // is method parameter
            if (reference is Expression parameter)
                return Expression.Property(parameter, property);

            var constExp = Expression.Constant(reference);
            return Expression.Property(constExp, property);
        }

        public IEnumerable<Type> GetRequiredResolveTypes()
        {
            if (!PropertyData.Property.GetGetMethod().IsStatic)
                yield return PropertyData.Type;
        }

        public VariableAccess DeclareVariableBinding(
            VariableHandler varHandler, ScriptBuilder sb)
        {
            var property = PropertyData.Property;
            var name = varHandler.GetCurrentLocalName();

            if (!property.CanRead)
                throw new ArgumentException($"Property {property.Name} is not readable");

            if (property.GetGetMethod().IsStatic)
            {
                return new DirectChainedAccess($"{TargetType.FullName}.{property.Name}");
            }

            var paramterName = varHandler.GetMethodParameter(PropertyData.Type);

            return new DirectChainedAccess($"{paramterName}.{property.Name}");
        }
    }

    [Serializable]
    public class ClassPropertyValueSource<T> : ClassPropertyValueSource, IValueSource<T>
    {
        public ClassPropertyValueSource(SProperty property) : base(property) { }
    }
}
