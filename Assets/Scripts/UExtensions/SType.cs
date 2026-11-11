using System;
using System.Collections.Generic;
using UnityEngine;

namespace UExtensions
{
    public class STypeFilterAttribute : PropertyAttribute
    {
        public HashSet<Type> Types;

        public STypeFilterAttribute(params Type[] types)
        {
            this.Types = new HashSet<Type>(types);
        }

        readonly static STypeFilterAttribute _default = new(typeof(ISTypeProvider));
        public static STypeFilterAttribute Default => _default;
    }


    [Serializable]
    public class SType : ISerializationCallbackReceiver
    {
        public const string TYPE_NAME_FIELD = nameof(_typeName);
        static readonly SType _default = new();
        public static SType Default => _default;

        [SerializeField] string _typeName;
        public Type Type { get; private set; }

        // 私有建構式 (Unity 預設值使用)
        SType()
        {
            _typeName = typeof(SType.None).AssemblyQualifiedName;
            Type = typeof(SType.None);
        }

        public SType(Type type)
        {
            _typeName = type.AssemblyQualifiedName;
            Type = type;
        }

        public bool TryUpdate(Type type)
        {
            if (type == Type) return false;

            _typeName = type.AssemblyQualifiedName;
            Type = type;

            return true;
        }

        public virtual void OnAfterDeserialize()
        {
            if (string.IsNullOrEmpty(_typeName))
            {
                Debug.LogError($"SType: {_typeName} is null or empty");
                return;
            }

            if (!TryGetType(_typeName, out var type))
            {
                Debug.LogError($"SType: {_typeName} could not be resolved to a Type");
                return;
            }

            Type = type;
        }

        public virtual void OnBeforeSerialize()
        {
            _typeName = Type?.AssemblyQualifiedName ?? _typeName;
        }

        public T CreateInstance<T>() where T : class
        {
            if (Type != typeof(T))
                throw new InvalidOperationException($"SType: {Type} is not of type {typeof(T)}");

            return Activator.CreateInstance<T>();
        }

        public object CreateInstance() =>
            Activator.CreateInstance(Type);

        static bool TryGetType(string typeName, out Type type)
        {
            type = Type.GetType(typeName);
            return type != null;
        }

        internal class None
        {
            public static SField DefaultField => new(typeof(None), nameof(__));
            public static SProperty DefaultProperty => new(typeof(None), nameof(___));
            static readonly _ __ = default;
            public static _ ___ {get; set;}
            public class _ { }
            public static SMethod DefaultMethod => new(typeof(None), nameof(_method));
            static void _method() { }
        }
    }

    public interface ISTypeProvider { }
}
