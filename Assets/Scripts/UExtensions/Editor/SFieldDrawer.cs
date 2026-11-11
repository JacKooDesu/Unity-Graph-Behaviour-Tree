using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UExtensions.Editor
{
    [CustomPropertyDrawer(typeof(SField), true)]
    public class SFieldDrawer : PropertyDrawer
    {
        string[] _fieldNames;
        string _targetTypeNameLast;

        void SetupFieldNames(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return;
            if (_targetTypeNameLast == typeName) return;

            _fieldNames = Type.GetType(typeName)?
                .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Select(f => f.Name)
                .ToArray();

            _targetTypeNameLast = typeName;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            // Type property field
            var typeProperty = property.FindPropertyRelative(SField.STYPE_FIELD);
            var typeIdProperty = typeProperty.FindPropertyRelative(SType.TYPE_NAME_FIELD);
            position.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(position, typeProperty);
            position.y += EditorGUIUtility.singleLineHeight;

            // Apply field names
            var fieldIdProperty = property.FindPropertyRelative(SField.MEMBER_NAME_FIELD);
            SetupFieldNames(typeIdProperty.stringValue);

            var fieldIndex = Array.IndexOf(_fieldNames, fieldIdProperty.stringValue);
            EditorGUI.BeginChangeCheck();
            var selectedFieldIndex = EditorGUI.Popup(position, "Field", fieldIndex, _fieldNames);
            if (EditorGUI.EndChangeCheck())
            {
                fieldIdProperty.stringValue = _fieldNames[selectedFieldIndex];
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var typeProperty = property.FindPropertyRelative(SField.STYPE_FIELD);
            return EditorGUI.GetPropertyHeight(typeProperty) +
                EditorGUIUtility.singleLineHeight;
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement container = new();

            var typeProperty = property.FindPropertyRelative(SField.STYPE_FIELD);
            var typeIdProperty = typeProperty.FindPropertyRelative(SType.TYPE_NAME_FIELD);

            var typeContainer = new PropertyField(typeProperty);

            var fieldIdProperty = property.FindPropertyRelative(SField.MEMBER_NAME_FIELD);
            SetupFieldNames(typeIdProperty.stringValue);
            var dropdown = new DropdownField(
                _fieldNames.ToList(),
                Array.IndexOf(_fieldNames, fieldIdProperty.stringValue)
            );
            dropdown.BindProperty(fieldIdProperty);

            dropdown.RegisterValueChangedCallback(e =>
            {
                int newIndex = dropdown.index is -1 ?
                    0 : dropdown.index;
                fieldIdProperty.stringValue = _fieldNames[newIndex];

                fieldIdProperty.serializedObject.ApplyModifiedProperties();
            });

            typeContainer.RegisterValueChangeCallback(e =>
            {
                SetupFieldNames(typeIdProperty.stringValue);
                dropdown.choices.Clear();
                dropdown.choices.AddRange(_fieldNames);
                dropdown.index = 0;
            });

            typeContainer.style.display = property.type != nameof(SFieldLocked) ?
                DisplayStyle.Flex : DisplayStyle.None;

            container.Add(typeContainer);
            container.Add(dropdown);

            return container;
        }
    }

}