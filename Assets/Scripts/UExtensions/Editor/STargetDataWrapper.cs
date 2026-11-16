using UnityEditor;
using UnityEngine;

namespace UExtensions.Editor
{
    internal class STargetDataWrapper<T> : ScriptableObject
    {
        [SerializeField]
        [SerializeReference]
        public T Data;
    }

    internal class STypeWrapper : STargetDataWrapper<SType> { }
    internal class SFieldWrapper : STargetDataWrapper<SField> { }
    internal class SFieldLockedWrapper : STargetDataWrapper<SFieldLocked> { }
    internal class SPropertyWrapper : STargetDataWrapper<SProperty> { }
    internal class SPropertyLockedWrapper : STargetDataWrapper<SPropertyLocked> { }

    public static class STargetExtensions
    {
        public static SerializedObject AsSerializedObject(this SType type) =>
            type.ToSerializedObject<STypeWrapper, SType>();

        public static SerializedObject AsSerializedObject(this SField field) =>
            field.ToSerializedObject<SFieldWrapper, SField>();
        public static SerializedObject AsSerializedObject(this SFieldLocked field) =>
            field.ToSerializedObject<SFieldLockedWrapper, SFieldLocked>();

        public static SerializedObject AsSerializedObject(this SProperty property) =>
            property.ToSerializedObject<SPropertyWrapper, SProperty>();
        public static SerializedObject AsSerializedObject(this SPropertyLocked property) =>
            property.ToSerializedObject<SPropertyLockedWrapper, SPropertyLocked>();

        static SerializedObject ToSerializedObject<TWrapper, TData>(this TData data)
            where TWrapper : STargetDataWrapper<TData>
        {
            var instance = ScriptableObject.CreateInstance<TWrapper>();
            instance.hideFlags = HideFlags.DontSave;
            instance.Data = data;
            return new(instance);
        }
    }
}