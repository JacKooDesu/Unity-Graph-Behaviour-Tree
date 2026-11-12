using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

namespace BehaviourTreeEditor.Runtime
{
    [Serializable]
    public class OperationRuntimeNode : CompilableNode, IValueSource
    {
        public Type TargetType => Input1.TargetType;
        [field: SerializeReference]
        public IValueSource Input1 { get; private set; }
        [field: SerializeReference]
        public IValueSource Input2 { get; private set; }
        [field: SerializeField]
        public EOperation Operation { get; private set; }

        public OperationRuntimeNode(
            IValueSource input1,
            IValueSource input2,
            EOperation operation)
        {
            Input1 = input1;
            Input2 = input2;
            Operation = operation;
        }

        public override IEnumerable<Type> GetRequiredResolveTypes()
        {
            foreach (var type in Input1.GetRequiredResolveTypes())
                yield return type;
            foreach (var type in Input2.GetRequiredResolveTypes())
                yield return type;
        }

        public override Expression ToExpression(Dictionary<Type, object> resolver)
        {
            var input1 = Input1.ToExpression(resolver);
            var input2 = Input2.ToExpression(resolver);

            return Operation switch
            {
                EOperation.Add => Expression.Add(input1, input2),
                EOperation.Subtract => Expression.Subtract(input1, input2),
                EOperation.Multiply => Expression.Multiply(input1, input2),
                EOperation.Divide => Expression.Divide(input1, input2),
                _ => throw new NotImplementedException(),
            };
        }

        public VariableAccess DeclareVariableBinding(VariableHandler varHandler, ScriptBuilder sb)
        {
            var input1 = varHandler.GetOrRegisterVariableBinding(Input1, sb);
            var input2 = varHandler.GetOrRegisterVariableBinding(Input2, sb);

            var op = Operation switch
            {
                EOperation.Add => "+",
                EOperation.Subtract => "-",
                EOperation.Multiply => "*",
                EOperation.Divide => "/",
                _ => throw new NotImplementedException(),
            };
            var name = varHandler.GetCurrentLocalName();
            sb.Declare(name, $"{input1} {op} {input2}", false);
            return new LocalVariableAccess(name);
        }
    }
}
