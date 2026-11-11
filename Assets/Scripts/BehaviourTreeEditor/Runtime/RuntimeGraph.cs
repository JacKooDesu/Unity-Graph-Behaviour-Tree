using System;
using System.Collections.Generic;
using System.Linq;
using UExtensions;
using UnityEngine;

namespace BehaviourTreeEditor.Runtime
{
    [Serializable]
    public class RuntimeGraph : ScriptableObject
    {
        public List<SType> RequiredResolveTypes = new();
        public List<StateRuntimeNode> States;
        [SerializeReference]
        public List<IVariableBinding> Variables = new();
        [SerializeReference]
        public List<IVariableBinding> Constants = new();
#if UNITY_EDITOR
        static readonly Dictionary<string, Action<RuntimeGraph>> OnGraphUpdatedHandlers = new();
#endif

        public IEnumerable<CompilableNode> GetAllCompilableNodes() =>
            States.SelectMany(s => s.GetAllCompilableNodes());

        public int CalMaxStatePathLength()
        {
            // depth is start from 0
            return States.Max(x => x.Depth) + 1;
        }

#if UNITY_EDITOR
        public void RegisterOnGraphUpdatedHandler(Action<RuntimeGraph> action)
        {
            if (!Application.isPlaying)
                return;

            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (!OnGraphUpdatedHandlers.TryGetValue(assetPath, out _))
                OnGraphUpdatedHandlers[assetPath] = _ => { };

            OnGraphUpdatedHandlers[assetPath] += action;
        }

        public static void AssetUpdated(string path, RuntimeGraph runtimeAsset)
        {
            if (!Application.isPlaying)
                return;

            if (OnGraphUpdatedHandlers.TryGetValue(path, out var handler))
                handler(runtimeAsset);
        }
#endif
    }

    public interface IVariableBinding
    {
        Type ValueType { get; }
        string Key { get; }
    }

    [Serializable]
    public abstract class VariableBinding<T> : IVariableBinding
    {
        public Type ValueType => typeof(T);
        [field: SerializeField]
        [field: HideInInspector]
        public string Key { get; private set; }
        [field: SerializeField]
        // [field: HideInInspector]
        // 暫定給外部 Scriptable Object 設定使用，故不 Hide
        public T Value { get; private set; }

        public static IVariableBinding Create(string key)
        {
            switch (typeof(T))
            {
                case var t when t == typeof(int):
                    return new IntBinding { Key = key };
                case var t when t == typeof(float):
                    return new FloatBinding { Key = key };
                case var t when t == typeof(uint):
                    return new UIntBinding { Key = key };
                default:
                    throw new ArgumentException($"Unsupported type: {typeof(T)}");
            }
        }
    }

    [Serializable]
    public class IntBinding : VariableBinding<int> { }
    [Serializable]
    public class FloatBinding : VariableBinding<float> { }
    [Serializable]
    public class UIntBinding : VariableBinding<uint> { }
}
