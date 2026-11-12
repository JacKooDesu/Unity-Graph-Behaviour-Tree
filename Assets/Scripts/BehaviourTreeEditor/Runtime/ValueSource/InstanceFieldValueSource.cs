using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UExtensions;
using UnityEngine;

namespace BehaviourTreeEditor.Runtime
{
    [Serializable]
    public class InstanceFieldValueSource : IValueSource
    {
        public Type TargetType => FieldData.Field.FieldType;

        [field: SerializeReference]
        public IValueSource ObjectSource { get; private set; }
        [field: SerializeField]
        public SField FieldData { get; private set; }

        public InstanceFieldValueSource(IValueSource objectSource, SField fieldData)
        {
            ObjectSource = objectSource;
            FieldData = fieldData;
        }

        public IEnumerable<Type> GetRequiredResolveTypes()
        {
            return ObjectSource.GetRequiredResolveTypes();
        }

        public Expression ToExpression(Dictionary<Type, object> resolver)
        {
            if (FieldData.Field.IsStatic)
                return Expression.Field(null, FieldData.Field);

            var objectExp = ObjectSource.ToExpression(resolver);
            return Expression.Field(objectExp, FieldData.Field);
        }

        public VariableAccess DeclareVariableBinding(VariableHandler varHandler, ScriptBuilder sb)
        {
            var fi = FieldData.Field;

            if (fi.IsStatic)
            {
                var name = varHandler.GetCurrentLocalName();

                return new DirectChainedAccess($"{TargetType.FullName}.{fi.Name}");
            }
            else
            {
                var sourceName = varHandler.GetOrRegisterVariableBinding(ObjectSource, sb);
                var name = varHandler.GetCurrentLocalName();

                sb.Declare(name, $"{sourceName}.{fi.Name}");

                return new LocalVariableAccess(name);
            }
        }
    }
}
