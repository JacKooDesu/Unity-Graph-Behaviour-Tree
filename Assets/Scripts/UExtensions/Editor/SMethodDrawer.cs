using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UExtensions.Editor
{
    [CustomPropertyDrawer(typeof(SMethod), true)]
    public class SMethodDrawer : PropertyDrawer
    {
        string[] _methodNames;
        string _targetTypeNameLast;

        void SetupFieldNames(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return;
            if (_targetTypeNameLast == typeName) return;

            _methodNames = Type.GetType(typeName)?
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Select(m => m.Name)
                .ToArray();

            _targetTypeNameLast = typeName;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            // Type property field
            var typeProperty = property.FindPropertyRelative(SMethod.STYPE_FIELD);
            var typeIdProperty = typeProperty.FindPropertyRelative(SType.TYPE_NAME_FIELD);
            position.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(position, typeProperty);
            position.y += EditorGUIUtility.singleLineHeight;

            // Apply field names
            var methodIdProperty = property.FindPropertyRelative(SMethod.METHOD_NAME_FIELD);
            SetupFieldNames(typeIdProperty.stringValue);

            var methodIndex = Array.IndexOf(_methodNames, methodIdProperty.stringValue);
            EditorGUI.BeginChangeCheck();
            var selectedFieldIndex = EditorGUI.Popup(position, "Method", methodIndex, _methodNames);
            if (EditorGUI.EndChangeCheck())
            {
                methodIdProperty.stringValue = _methodNames[selectedFieldIndex];
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var typeProperty = property.FindPropertyRelative(SMethod.STYPE_FIELD);
            return EditorGUI.GetPropertyHeight(typeProperty) +
                EditorGUIUtility.singleLineHeight;
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement container = new();

            var typeProperty = property.FindPropertyRelative(SMethod.STYPE_FIELD);
            var typeIdProperty = typeProperty.FindPropertyRelative(SType.TYPE_NAME_FIELD);

            var typeContainer = new PropertyField(typeProperty);

            var methodIdProperty = property.FindPropertyRelative(SMethod.METHOD_NAME_FIELD);
            SetupFieldNames(typeIdProperty.stringValue);
            var dropdown = new DropdownField(
                _methodNames.ToList(),
                Array.IndexOf(_methodNames, methodIdProperty.stringValue)
            );
            dropdown.BindProperty(methodIdProperty);

            dropdown.RegisterValueChangedCallback(e =>
            {
                int newIndex = dropdown.index is -1 ?
                    0 : dropdown.index;
                methodIdProperty.stringValue = _methodNames[newIndex];

                methodIdProperty.serializedObject.ApplyModifiedProperties();
            });

            typeContainer.RegisterValueChangeCallback(e =>
            {
                SetupFieldNames(typeIdProperty.stringValue);
                dropdown.choices.Clear();
                dropdown.choices.AddRange(_methodNames);
                dropdown.index = 0;
            });

            typeContainer.style.display = property.type != nameof(SMethodLocked) ?
                DisplayStyle.Flex : DisplayStyle.None;

            container.Add(typeContainer);
            container.Add(dropdown);

            return container;
        }
    }

}