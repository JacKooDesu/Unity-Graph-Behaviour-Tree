using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UExtensions
{
    [Serializable]
    public class SField : ISerializationCallbackReceiver
    {
        public const string STYPE_FIELD = nameof(_type);
        public const string MEMBER_NAME_FIELD = nameof(_fieldName);
        public static SField DefaultField => SType.None.DefaultField;

        [SerializeField] SType _type;
        [SerializeField] string _fieldName;
        public FieldInfo Field { get; private set; }
        public Type Type => _type.Type;

        // 私有建構式 (Unity 預設值使用)
        SField()
        {
            this._type = DefaultField._type;
            this._fieldName = DefaultField._fieldName;
            this.Field = DefaultField.Field;
        }

        public SField(Type type, string fieldName)
        {
            _type = new SType(type);
            TryGetField(type, _fieldName = fieldName, out var field);
            Field = field;
        }

        public bool TryUpdateType(Type type)
        {
            if (!_type.TryUpdate(type)) return false;

            // Auto update field
            Field = type
                .GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault();
            _fieldName = Field?.Name ?? _fieldName;

            return Field != null;
        }

        public void OnAfterDeserialize()
        {
            _type.OnAfterDeserialize();
            if (string.IsNullOrEmpty(_fieldName))
            {
                Debug.LogError($"SMemberVariable: {_fieldName} is null or empty");
                return;
            }

            if (!TryGetField(Type, _fieldName, out var field))
            {
                Debug.LogError($"SMemberVariable: {_fieldName} is not a valid field on {Type.Name}");
                return;
            }

            Field = field;
        }

        public void OnBeforeSerialize()
        {
            _type.OnBeforeSerialize();
            _fieldName = Field?.Name ?? _fieldName;
        }

        static bool TryGetField(Type type, string fieldName, out FieldInfo field)
        {
            field = type.GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return field != null;
        }
    }

    [Serializable]
    public class SFieldLocked : SField
    {
        public SFieldLocked(Type type, string fieldName) : base(type, fieldName) { }
    }
}