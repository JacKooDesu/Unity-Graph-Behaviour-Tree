using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UExtensions
{
    [Serializable]
    public class SMethod : ISerializationCallbackReceiver
    {
        public const string STYPE_FIELD = nameof(_type);
        public const string METHOD_NAME_FIELD = nameof(_methodName);
        public static SMethod DefaultMethod => SType.None.DefaultMethod;

        [SerializeField] SType _type;
        [SerializeField] string _methodName;
        public MethodInfo Method { get; protected set; }
        public Type Type => _type.Type;

        public IEnumerable<Type> ParameterTypes =>
            Method.GetParameters()?.Select(p => p.ParameterType) ?? Enumerable.Empty<Type>();
        public Type ReturnType =>
            Method.ReturnType;

        // 私有建構式 (Unity 預設值使用)
        SMethod()
        {
            this._type = DefaultMethod._type;
            this._methodName = DefaultMethod._methodName;
            this.Method = DefaultMethod.Method;
        }

        public SMethod(Type type, string methodName)
        {
            _type = new SType(type);
            TryGetMethod(type, _methodName = methodName, out var method);
            Method = method;
        }

        public bool TryUpdateType(Type type)
        {
            if (!_type.TryUpdate(type)) return false;

            Method = type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault();
            _methodName = Method?.Name ?? _methodName;

            return Method != null;
        }

        public void OnAfterDeserialize()
        {
            _type.OnAfterDeserialize();

            if (string.IsNullOrEmpty(_methodName))
            {
                Debug.LogError($"SMemberMethod: {_methodName} is null or empty");
                return;
            }

            if (!TryGetMethod(Type, _methodName, out var method))
            {
                Debug.LogError($"SMemberMethod: {_methodName} is not found on type {Type.FullName}");
                return;
            }

            Method = method;
        }

        public void OnBeforeSerialize()
        {
            _type.OnBeforeSerialize();
            _methodName = Method?.Name ?? _methodName;
        }

        static bool TryGetMethod(Type type, string methodName, out MethodInfo method)
        {
            method = type.GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return method != null;
        }
    }

    [Serializable]
    public class SGenericMethod : SMethod, ISerializationCallbackReceiver
    {
        [SerializeField] SType[] _genericTypes;

        public SGenericMethod(
            Type type, string methodName, SType[] genericTypes) : base(type, methodName)
        {
            _genericTypes = genericTypes;
            Method = Method.MakeGenericMethod(_genericTypes.Select(t => t.Type).ToArray());
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            foreach (var t in _genericTypes)
                t.OnAfterDeserialize();
            Method = Method.MakeGenericMethod(_genericTypes.Select(t => t.Type).ToArray());
        }
    }

    [Serializable]
    public class SMethodLocked : SMethod, ISerializationCallbackReceiver
    {
        public SMethodLocked(Type type, string methodName) : base(type, methodName) { }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
        }
    }
}