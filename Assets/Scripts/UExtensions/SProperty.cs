using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UExtensions
{
    [Serializable]
    public class SProperty : ISerializationCallbackReceiver
    {
        public const string STYPE_FIELD = nameof(_type);
        public const string MEMBER_NAME_FIELD = nameof(_propName);
        public static SProperty DefaultProperty => SType.None.DefaultProperty;

        [SerializeField] SType _type;
        [SerializeField] string _propName;
        public PropertyInfo Property { get; private set; }
        public Type Type => _type.Type;

        // 私有建構式 (Unity 預設值使用)
        SProperty()
        {
            this._type = DefaultProperty._type;
            this._propName = DefaultProperty._propName;
            this.Property = DefaultProperty.Property;
        }

        public SProperty(Type type, string propName)
        {
            _type = new SType(type);
            TryGetProperty(type, _propName = propName, out var prop);
            Property = prop;
        }

        public bool TryUpdateType(Type type)
        {
            if (!_type.TryUpdate(type)) return false;

            Property = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault();
            _propName = Property?.Name ?? _propName;

            return Property != null;
        }

        public void OnAfterDeserialize()
        {
            _type.OnAfterDeserialize();
            if (string.IsNullOrEmpty(_propName))
            {
                Debug.LogError($"SMemberVariable: {_propName} is null or empty");
                return;
            }

            if (!TryGetProperty(Type, _propName, out var prop))
            {
                Debug.LogError($"SMemberVariable: {_propName} is not a valid field on {Type.Name}");
                return;
            }

            Property = prop;
        }

        public void OnBeforeSerialize()
        {
            _type.OnBeforeSerialize();
            _propName = Property?.Name ?? _propName;
        }

        static bool TryGetProperty(Type type, string propName, out PropertyInfo prop)
        {
            prop = type.GetProperty(propName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return prop != null;
        }
    }

    [Serializable]
    public class SPropertyLocked : SProperty
    {
        public SPropertyLocked(Type type, string propName) : base(type, propName) { }
    }
}