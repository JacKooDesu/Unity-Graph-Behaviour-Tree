using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UExtensions;
using UnityEngine;

namespace BehaviourTreeEditor.Runtime
{
    [Serializable]
    public class MethodCallRuntimeNode : CompilableNode, IValueSource
    {
        public Type TargetType => MethodData.Method.ReturnType;
        [field: SerializeReference]
        public SMethod MethodData;
        [field: SerializeReference]
        public IValueSource[] Args { get; private set; }

        public MethodCallRuntimeNode(
            SMethod methodData,
            IValueSource[] args)
        {
            MethodData = methodData;
            Args = args;
        }

        public MethodCallRuntimeNode(SMethod methodData)
        {
            MethodData = methodData;
            Args = Array.Empty<IValueSource>();
        }

        public override Expression ToExpression(Dictionary<Type, object> resolver)
        {
            var method = MethodData.Method;

            var argExps = Args.Select(arg => arg.ToExpression(resolver))
                .ToArray();

            if (method.IsStatic)
                return Expression.Call(method, argExps);

            var instanceType = MethodData.Type;
            if (!resolver.TryGetValue(instanceType, out var obj))
                throw new InvalidOperationException($"No instance of type {instanceType} found in resolver");
            return Expression.Call(
                Expression.Constant(obj),
                method,
                argExps);
        }

        public override IEnumerable<Type> GetRequiredResolveTypes()
        {
            if (!MethodData.Method.IsStatic)
                yield return MethodData.Type;

            foreach (var arg in Args)
            {
                foreach (var type in arg.GetRequiredResolveTypes())
                {
                    yield return type;
                }
            }
        }

        public override void BuildScript(
            List<CompilableNode> contextNodes, VariableHandler varHandler, ScriptBuilder sb)
        {
            var method = MethodData.Method;

            // get parameter or static
            var paramterName = method.IsStatic ?
                MethodData.Type.FullName :
                varHandler.GetMethodParameter(MethodData.Type);

            var argList = GenerateArgList(varHandler, sb);

            sb.AppendLine($"{paramterName}.{method.Name}({argList});");
        }

        public string DeclareVariableBinding(VariableHandler varHandler, ScriptBuilder sb)
        {
            var method = MethodData.Method;
            if (method.ReturnType == typeof(void))
                throw new InvalidOperationException($"Method {method.Name} returns void");

            // get parameter
            var paramterName = varHandler.GetMethodParameter(TargetType);

            var argList = GenerateArgList(varHandler, sb);
            var name = varHandler.GetCurrentLocalName();

            sb.AppendLine($"var {name} = {paramterName}.{method.Name}({argList});");
            return name;
        }

        string GenerateArgList(
            VariableHandler varHandler,
            ScriptBuilder sb)
        {
            var parameters = MethodData.Method.GetParameters();
            if (Args.Length != parameters.Length)
                throw new InvalidOperationException($"Method {MethodData.Method.Name} expects {parameters.Length} arguments, but got {Args.Length}");

            Queue<string> argNames = new();

            foreach (var (arg, p) in Args.Zip(parameters, (a, p) => (a, p)))
            {
                var name = varHandler.GetOrRegisterVariableBinding(arg, sb);
                if (p.ParameterType.IsByRef)
                {
                    // if (p.IsOut)
                    //     name = $"out {name}";
                    // else if (p.IsIn)
                    //     name = $"in {name}";
                    name = $"ref {name}";
                }

                argNames.Enqueue(name);
            }

            return string.Join(", ", argNames);
        }
    }

    [Serializable]
    public class MethodCallRuntimeNode<TReturn> : MethodCallRuntimeNode
    {
        public MethodCallRuntimeNode(SMethod methodData, IValueSource[] args) : base(methodData, args) { }
        public MethodCallRuntimeNode(SMethod methodData) : base(methodData) { }
    }
}
