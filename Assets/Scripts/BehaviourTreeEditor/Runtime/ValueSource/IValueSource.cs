using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace BehaviourTreeEditor.Runtime
{
    public interface IValueSource
    {
        Type TargetType { get; }
        Expression ToExpression(Dictionary<Type, object> resolver);
        IEnumerable<Type> GetRequiredResolveTypes();
        VariableAccess DeclareVariableBinding(VariableHandler varHandler, ScriptBuilder sb);
    }
    public interface IValueSource<T> : IValueSource { }
    [Serializable]
    public abstract class ValueSource<T> : IValueSource<T>
    {
        public Type TargetType => typeof(T);
        // Expression ToExpression(Dictionary<Type, object> resolver);
        public abstract IEnumerable<Type> GetRequiredResolveTypes();
        public abstract Expression ToExpression(Dictionary<Type, object> resolver);
        public abstract VariableAccess DeclareVariableBinding(VariableHandler varHandler, ScriptBuilder sb);
    }

    internal static class ValueSourceExtensions
    { }
}
