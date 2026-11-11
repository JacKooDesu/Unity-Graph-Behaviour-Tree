using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using BehaviourTreeEditor.Runtime;

namespace BehaviourTreeEditor.Test
{
    public class TestActionNode<T> : CompilableNode
    {
        public Action<T> Action { get; set; }
        public IValueSource Arg { get; set; }

        public TestActionNode(Action<T> action, IValueSource arg)
        {
            Action = action;
            Arg = arg;
        }

        public override Expression ToExpression(Dictionary<Type, object> resolver)
        {
            return Expression.Invoke(Expression.Constant(Action), Arg.ToExpression(resolver));
        }
    }
}