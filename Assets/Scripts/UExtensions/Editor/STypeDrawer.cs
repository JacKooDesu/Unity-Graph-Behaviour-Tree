using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UExtensions.Editor
{
    [CustomPropertyDrawer(typeof(SType))]
    public class STypeDrawer : PropertyDrawer
    {
        STypeFilterAttribute _attribute;

        void SetupAttribute()
        {
            if (_attribute is not null) return;
            _attribute = Attribute.GetCustomAttribute(fieldInfo, typeof(STypeFilterAttribute)) as STypeFilterAttribute ??
                STypeFilterAttribute.Default;
        }

        ReflectionCache.TypeNameCollection TypeCollection => ReflectionCache.GetTypeNameCollection(_attribute.Types);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            SetupAttribute();
            var typeIdProperty = property.FindPropertyRelative(SType.TYPE_NAME_FIELD);
            if (string.IsNullOrEmpty(typeIdProperty.stringValue))
            {
                typeIdProperty.stringValue = TypeCollection.FullNames.First();
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }

            var typeIndex = TypeCollection.IndexOfFullName(typeIdProperty.stringValue);
            EditorGUI.BeginChangeCheck();
            var selectedIndex = EditorGUI.Popup(position, label.text, typeIndex, TypeCollection.Names);
            if (EditorGUI.EndChangeCheck())
            {
                typeIdProperty.stringValue = TypeCollection.FullNames[selectedIndex];
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // 建立容器
            VisualElement container = new();

            SetupAttribute();
            var typeIdProperty = property.FindPropertyRelative(SType.TYPE_NAME_FIELD);

            if (string.IsNullOrEmpty(typeIdProperty.stringValue))
            {
                typeIdProperty.stringValue = TypeCollection.FullNames.First();
                typeIdProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            var dropdown = new DropdownField(
                TypeCollection.Names.ToList(),
                TypeCollection.IndexOfFullName(typeIdProperty.stringValue));

            dropdown.RegisterValueChangedCallback(_ =>
            {
                if (dropdown.index != -1 &&
                    TypeCollection.FullNames.Length > 0)
                {
                    int newIndex = dropdown.index;
                    typeIdProperty.stringValue = TypeCollection.FullNames[newIndex];
                }

                typeIdProperty.serializedObject.ApplyModifiedProperties();
            });
            dropdown.TrackPropertyValue(typeIdProperty, e =>
            {
                dropdown.index = TypeCollection.IndexOfFullName(e.stringValue);
            });

            container.Add(dropdown);
            return container;
        }
    }

}