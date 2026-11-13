using System;
using System.Collections.Generic;
using System.Linq.Expressions;


namespace BehaviourTreeEditor.Runtime
{
    public interface IExecuteNodeBuilder<TContext>
    {
        IEnumerable<(Type type, ParameterExpression exp)> GetParameters();
        IVariableExpressionResolver<TContext> GetVariableExpressionResolver();
        StateRuntimeNodeExecutor<TContext>.ExecuteDelegate CompileDelegate(Expression exp);
    }
}
