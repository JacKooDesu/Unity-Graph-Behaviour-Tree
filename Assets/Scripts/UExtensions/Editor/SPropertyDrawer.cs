using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UExtensions.Editor
{
    [CustomPropertyDrawer(typeof(SProperty), true)]
    public class SPropertyDrawer : PropertyDrawer
    {
        string[] _propNames;
        string _targetTypeNameLast;

        void SetupPropertyNames(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return;
            if (_targetTypeNameLast == typeName) return;

            _propNames = Type.GetType(typeName)?
                .GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Select(m => m.Name)
                .ToArray();

            _targetTypeNameLast = typeName;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            // Type property field
            var typeProperty = property.FindPropertyRelative(SProperty.STYPE_FIELD);
            var typeIdProperty = typeProperty.FindPropertyRelative(SType.TYPE_NAME_FIELD);
            position.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(position, typeProperty);
            position.y += EditorGUIUtility.singleLineHeight;

            // Apply field names
            var propIdProperty = property.FindPropertyRelative(SProperty.MEMBER_NAME_FIELD);
            SetupPropertyNames(typeIdProperty.stringValue);

            var methodIndex = Array.IndexOf(_propNames, propIdProperty.stringValue);
            EditorGUI.BeginChangeCheck();
            var selectedPropIndex = EditorGUI.Popup(position, "Method", methodIndex, _propNames);
            if (EditorGUI.EndChangeCheck())
            {
                propIdProperty.stringValue = _propNames[selectedPropIndex];
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var typeProperty = property.FindPropertyRelative(SProperty.STYPE_FIELD);
            return EditorGUI.GetPropertyHeight(typeProperty) +
                EditorGUIUtility.singleLineHeight;
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement container = new();

            var typeProperty = property.FindPropertyRelative(SProperty.STYPE_FIELD);
            var typeIdProperty = typeProperty.FindPropertyRelative(SType.TYPE_NAME_FIELD);

            var typeContainer = new PropertyField(typeProperty);

            var propIdProperty = property.FindPropertyRelative(SProperty.MEMBER_NAME_FIELD);
            SetupPropertyNames(typeIdProperty.stringValue);
            var dropdown = new DropdownField(
                _propNames.ToList(),
                Array.IndexOf(_propNames, propIdProperty.stringValue)
            );
            dropdown.BindProperty(propIdProperty);

            dropdown.RegisterValueChangedCallback(e =>
            {
                int newIndex = dropdown.index is -1 ?
                    0 : dropdown.index;
                propIdProperty.stringValue = _propNames[newIndex];

                propIdProperty.serializedObject.ApplyModifiedProperties();
            });

            typeContainer.RegisterValueChangeCallback(e =>
            {
                SetupPropertyNames(typeIdProperty.stringValue);
                dropdown.choices.Clear();
                dropdown.choices.AddRange(_propNames);
                dropdown.index = 0;
            });

            typeContainer.style.display = property.type != nameof(SPropertyLocked) ?
                DisplayStyle.Flex : DisplayStyle.None;

            container.Add(typeContainer);
            container.Add(dropdown);

            return container;
        }
    }

}