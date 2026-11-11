using System;
using System.Collections.Generic;
using System.Text;

namespace BehaviourTreeEditor.Runtime
{
    public abstract class VariableHandler
    {
        protected List<IVariableBinding> _variableBindings = new();
        protected List<IVariableBinding> _constantBindings = new();

        Dictionary<Type, string> _methodParameters = new();
        Dictionary<IValueSource, string> _localVariables = new();
        int _currentIterScope = 0;
        Stack<string> _iterTargetStack = new();

        public VariableHandler(RuntimeGraph graph, IEnumerable<Type> methodParameterTypes = null)
        {
            if (methodParameterTypes == null)
                return;

            int i = 0;
            foreach (var type in methodParameterTypes)
            {
                _methodParameters[type] = $"param_{type.Name}_{i}";
                i++;
            }

            _variableBindings = graph.Variables;
            _constantBindings = graph.Constants;
        }

        public string GetMethodParameter(Type type)
        {
            if (!_methodParameters.TryGetValue(type, out var name))
                throw new InvalidOperationException($"No method parameter registered for type {type}");

            return name;
        }

        public string GetOrRegisterVariableBinding(
            IValueSource source,
            ScriptBuilder sb)
        {
            // if (source is ClassFieldValueSource ||
            //     source is ClassValueSource ||
            //     source is ClassPropertyValueSource)
            // {
            //     if (!_methodParameters.TryGetValue(source.TargetType, out var name))
            //         throw new InvalidOperationException($"No method parameter registered for type {source.TargetType}");

            //     return name;
            // }

            // enum only get scope var
            if (source is EnumeratorRuntimeNode.EnumerableScopeValueSource)
                return GetCurrentIterTargetName();

            if (_localVariables.TryGetValue(source, out var localName))
                return localName;

            var newName = source.DeclareVariableBinding(this, sb);
            _localVariables.Add(source, newName);

            return newName;
        }

        public int BeginIterScope(string targetName)
        {
            _currentIterScope++;
            _iterTargetStack.Push(targetName);
            return _currentIterScope;
        }

        public void EndIterScope(int scope)
        {
            if (scope != _currentIterScope)
                throw new InvalidOperationException("Scope mismatch");

            _currentIterScope--;
            _iterTargetStack.Pop();
        }

        public string GetCurrentIterIndexName() => $"iter_{_currentIterScope}";
        public string GetCurrentIterItemName() => $"item_{_currentIterScope}";
        public string GetCurrentIterTargetName() => $"{_iterTargetStack.Peek()}[{GetCurrentIterIndexName()}]";

        public string GetCurrentLocalName() => $"local_{_localVariables.Count}";
        public abstract string BuildContextVariable(int index, bool isConstant);
    }

    public class DefaultVariableHandler : VariableHandler
    {
        public DefaultVariableHandler(RuntimeGraph graph, IEnumerable<Type> methodParameterTypes = null) : base(graph, methodParameterTypes)
        {

        }

        public override string BuildContextVariable(int index, bool isConstant)
        {
            var binding = _variableBindings[index];

            var indexStr = $"[{index}]";
            var generic = $"<{binding.ValueType}>";
            var typeStr = $"{nameof(DefaultContext)}.{nameof(DefaultContext.VarWrapper<int>)}{generic}";

            var fieldStr = isConstant ?
                nameof(DefaultContext.Constants) :
                nameof(DefaultContext.Variables);
            var contextParam = GetMethodParameter(typeof(DefaultContext));

            return $"(({typeStr}){contextParam}.{fieldStr}{indexStr}).Value";
        }
    }
}
