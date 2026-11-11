using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UExtensions;
using UnityEngine;

namespace BehaviourTreeEditor.Runtime
{
    [Serializable]
    public class WriteValueRuntimeNode : CompilableNode
    {
        [field: SerializeReference]
        public IValueSource Left { get; private set; }
        [field: SerializeReference]
        public IValueSource Right { get; private set; }

        public WriteValueRuntimeNode(
            IValueSource left,
            IValueSource right)
        {
            Left = left;
            Right = right;
        }

        public override Expression ToExpression(Dictionary<Type, object> resolver)
        {
            var leftExp = Left.ToExpression(resolver);
            var rightExp = Right.ToExpression(resolver);
            return Expression.Assign(leftExp, rightExp);
        }

        public override void BuildScript(
            List<CompilableNode> contextNodes,
            VariableHandler varHandler,
            ScriptBuilder sb)
        {
            var leftName = varHandler.GetOrRegisterVariableBinding(Left, sb);
            var rightName = varHandler.GetOrRegisterVariableBinding(Right, sb);
            sb.AppendLine($"{leftName} = {rightName};");
        }

        public override IEnumerable<Type> GetRequiredResolveTypes()
        {
            foreach (var type in Left.GetRequiredResolveTypes())
                yield return type;
            foreach (var type in Right.GetRequiredResolveTypes())
                yield return type;
        }
    }

    [Serializable]
    public class WriteValueRuntimeNode<T> : WriteValueRuntimeNode
    {
        public WriteValueRuntimeNode(
            ClassFieldValueSource<T> left,
            IValueSource<T> right) : base(left, right) { }
    }

    [Serializable]
    public class WriteContextVariableRuntimeNode : CompilableNode
    {
        [field: SerializeField]
        public int Index { get; private set; }
        [field: SerializeReference]
        public IValueSource Right { get; private set; }

        public WriteContextVariableRuntimeNode(int index, IValueSource valueSource)
        {
            Index = index;
            Right = valueSource;
        }

        public override void BuildScript(
            List<CompilableNode> contextNodes,
            VariableHandler varHandler,
            ScriptBuilder sb)
        {
            var right = varHandler.GetOrRegisterVariableBinding(Right, sb);
            // always not constant
            var left = varHandler.BuildContextVariable(Index, false);

            sb.AppendLine($"{left} = {right};");
        }

        public override Expression ToExpression(Dictionary<Type, object> resolver)
        {
            if (!resolver.TryGetValue(typeof(IVariableExpressionResolver), out var vExpResolver) ||
                vExpResolver is not IVariableExpressionResolver vr)
                throw new InvalidOperationException("IVariableExpressionResolver not found in resolver");

            return vr.WriteVariableExpression(Index, Right.ToExpression(resolver));
        }
    }
}
