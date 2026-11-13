using System.Linq.Expressions;


namespace BehaviourTreeEditor.Runtime
{
    /// <summary>
    /// Expression Resolver interface for graph context variables (read constants / write local variables) when compile phase.
    /// <para>Also see <see cref="IExecuteNodeBuilder{T}.GetVariableExpressionResolver()"/></para>
    /// </summary>
    public interface IVariableExpressionResolver
    {
        Expression WriteVariableExpression(int index, Expression value);
        Expression ReadVariableExpression(int index);
        Expression ConstantExpression(int index);
    }

    /// <summary>
    /// Expression Resolver interface for graph context variables (read constants / write local variables) when compile phase.
    /// <para>Also see <see cref="IExecuteNodeBuilder{T}.GetVariableExpressionResolver()"/></para>
    /// </summary>
    public interface IVariableExpressionResolver<TContext> : IVariableExpressionResolver { }
}
