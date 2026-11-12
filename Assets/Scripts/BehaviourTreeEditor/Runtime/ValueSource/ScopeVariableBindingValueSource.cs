using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

namespace BehaviourTreeEditor.Runtime
{
    [Serializable]
    public class ScopeVariableBindingValueSource : IValueSource
    {
        [field: SerializeField]
        public int Index { get; private set; }
        [field: SerializeField]
        public bool IsConstant { get; private set; }

        public Type TargetType => throw new NotImplementedException();

        public ScopeVariableBindingValueSource(int index, bool isConst)
        {
            Index = index;
            IsConstant = isConst;
        }

        public VariableAccess DeclareVariableBinding(VariableHandler varHandler, ScriptBuilder sb)
        {
            var name = varHandler.GetCurrentLocalName();
            var declare = varHandler.BuildContextVariable(Index, IsConstant);

            sb.Declare(name, declare, true);
            return new LocalVariableAccess(name);
        }

        public IEnumerable<Type> GetRequiredResolveTypes() { yield break; }

        public Expression ToExpression(Dictionary<Type, object> resolver)
        {
            if (!resolver.TryGetValue(typeof(IVariableExpressionResolver), out var vexpResolver) ||
                vexpResolver is not IVariableExpressionResolver vr)
                throw new InvalidOperationException("IVariableExpressionResolver not found in resolver");

            return IsConstant ?
                vr.ConstantExpression(Index) : vr.ReadVariableExpression(Index);
        }
    }
}
