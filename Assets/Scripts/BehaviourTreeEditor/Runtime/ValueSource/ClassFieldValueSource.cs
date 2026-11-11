using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UExtensions;
using UnityEngine;

namespace BehaviourTreeEditor.Runtime
{
    [Serializable]
    public class ClassFieldValueSource : IValueSource
    {
        public Type TargetType => FieldData.Field.FieldType;
        [field: SerializeField]
        public SField FieldData { private set; get; }
        public ClassFieldValueSource(SField field) => FieldData = field;
        public Expression ToExpression(Dictionary<Type, object> resolver)
        {
            var field = FieldData.Field;
            if (field.IsStatic)
                return Expression.Field(null, field);

            if (!resolver.TryGetValue(FieldData.Type, out var reference))
                throw new ArgumentException($"No resolver for type {TargetType}");

            // is method parameter
            if (reference is Expression parameter)
                return Expression.Field(parameter, field);

            var constExp = Expression.Constant(reference);
            return Expression.Field(constExp, field);
        }

        public IEnumerable<Type> GetRequiredResolveTypes()
        {
            if (!FieldData.Field.IsStatic)
                yield return FieldData.Type;
        }

        public string DeclareVariableBinding(
            VariableHandler varHandler, ScriptBuilder sb)
        {
            var fi = FieldData.Field;
            var name = varHandler.GetCurrentLocalName();

            if (fi.IsStatic)
            {
                sb.Declare(name, $"{TargetType.FullName}.{fi.Name}");
                return name;
            }

            var paramterName = varHandler.GetMethodParameter(FieldData.Type);

            sb.Declare(name, $"{paramterName}.{fi.Name}");

            return name;
        }
    }

    [Serializable]
    public class ClassFieldValueSource<T> : ClassFieldValueSource, IValueSource<T>
    {
        public ClassFieldValueSource(SField field) : base(field) { }
    }
}
