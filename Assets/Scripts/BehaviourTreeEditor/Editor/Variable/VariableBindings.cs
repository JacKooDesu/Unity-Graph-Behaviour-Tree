using System;
using System.Collections.Generic;
using System.Linq;
using BehaviourTreeEditor.Runtime;
using UExtensions;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace BehaviourTreeEditor.Editor
{
    [Serializable]
    [UseWithGraph(typeof(Graph))]
    internal class VariableBindings : ContextNode
    {
        const string CONST_KEY = "CONST";
        public bool IsConstant => GetNodeOptionByName(CONST_KEY).TryGetValue<bool>(out var b) && b;
        public IEnumerable<Runtime.IVariableBinding> RuntimeBindings => BlockNodes
                .Select(x => (x as IVariableBindingBlock)?.RuntimeBinding)
                .Where(x => x is not null);
        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<bool>(CONST_KEY)
                .WithDisplayName("Constant")
                .Build();
        }
    }

    internal interface IVariableBindingBlock
    {
        string GetKey();
        Type ValueType { get; }
        Runtime.IVariableBinding RuntimeBinding { get; }
    }

    [Serializable]
    [UseWithContext(typeof(VariableBindings))]
    internal abstract class VariableBindingBlockBase<T> : BlockNode, IVariableBindingBlock
    {
        protected const string KEY_PORT = "KEY";
        protected const string DEFAULT_VALUE_PORT = "DEFAULT_VALUE";
        public Type ValueType => typeof(T);
        public Runtime.IVariableBinding RuntimeBinding =>
            Runtime.VariableBinding<T>.Create(GetKey());

        public string GetKey() => GetInputPortByName(KEY_PORT).TryGetValue<string>(out var key) ?
            key : string.Empty;
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort<string>(KEY_PORT)
                .WithDisplayName("Key")
                .Build();
        }
    }

    [Serializable]
    [UseWithContext(typeof(VariableBindings))]
    internal class IntBinding : VariableBindingBlockBase<int> { }

    [Serializable]
    [UseWithContext(typeof(VariableBindings))]
    internal class FloatBinding : VariableBindingBlockBase<float> { }

    [Serializable]
    [UseWithContext(typeof(VariableBindings))]
    internal class UIntBinding : VariableBindingBlockBase<uint> { }
}
