using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UExtensions;
using UnityEngine;

namespace BehaviourTreeEditor.Runtime
{
    [Serializable]
    public class InstancePropertyValueSource : IValueSource
    {
        public Type TargetType => PropertyData.Property.PropertyType;

        [field: SerializeReference]
        public IValueSource ObjectSource { get; private set; }
        [field: SerializeField]
        public SProperty PropertyData { get; private set; }

        public InstancePropertyValueSource(IValueSource objectSource, SProperty propData)
        {
            ObjectSource = objectSource;
            PropertyData = propData;
        }

        public IEnumerable<Type> GetRequiredResolveTypes()
        {
            return ObjectSource.GetRequiredResolveTypes();
        }

        public Expression ToExpression(Dictionary<Type, object> resolver)
        {
            var property = PropertyData.Property;

            if (property.GetGetMethod().IsStatic)
                return Expression.Property(null, property);

            var objectExp = ObjectSource.ToExpression(resolver);
            return Expression.Property(objectExp, property);
        }

        public string DeclareVariableBinding(VariableHandler varHandler, ScriptBuilder sb)
        {
            var property = PropertyData.Property;
            var name = varHandler.GetCurrentLocalName();

            if (!property.CanRead)
                throw new ArgumentException($"Property {property.Name} is not readable");

            if (property.GetGetMethod().IsStatic)
            {
                sb.Declare(name, $"{TargetType.FullName}.{property.Name}");
                return name;
            }

            var sourceName = varHandler.GetOrRegisterVariableBinding(ObjectSource, sb);
            sb.Declare(name, $"{sourceName}.{property.Name}");
            return name;
        }
    }
}
