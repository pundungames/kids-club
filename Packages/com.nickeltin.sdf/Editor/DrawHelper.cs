using System;
using UnityEditor;
using UnityEngine;

namespace nickeltin.SDF.Editor
{
    internal struct DrawHelper
    {
        private Rect _totalRect;
        private Rect _currentRect;

        public DrawHelper(Rect totalRect)
        {
            _totalRect = totalRect;
            _currentRect = totalRect;
            _currentRect.y += 1 - EditorGUIUtility.singleLineHeight;
            _currentRect.height = EditorGUIUtility.singleLineHeight;
            TotalHeight = 0;
        }

        public Rect CurrentRect => _currentRect;

        public float TotalHeight { get; private set; }

        public void AddRect(float height)
        {
            var lastHeight = _currentRect.height;
            _currentRect.height = height;
            _currentRect.y += lastHeight + 2;
            TotalHeight += height + 2;
        }
            
        public void AddRect()
        {
            AddRect(EditorGUIUtility.singleLineHeight);
        }

        public void AddProperty(SerializedProperty property)
        {
            var height = EditorGUI.GetPropertyHeight(property, true);
            AddRect(height);
        }

        public void DrawProperty(SerializedProperty property)
        {
            AddProperty(property);
            EditorGUI.PropertyField(CurrentRect, property, true);
        }
        
        public void DrawProperty(SerializedProperty property, GUIContent content)
        {
            AddProperty(property);
            EditorGUI.PropertyField(CurrentRect, property, content);
        }
        
        public void DrawLeftToggle(SerializedProperty property, GUIContent content)
        {
            if (property.propertyType != SerializedPropertyType.Boolean)
            {
                throw new Exception($"{property} is not of type bool!");
            }
            AddProperty(property);
            EditorGUI.BeginChangeCheck();
            var showMixedValues = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            var newValue = EditorGUI.ToggleLeft(_currentRect, content, property.boolValue);
            EditorGUI.showMixedValue = showMixedValues;
            if (EditorGUI.EndChangeCheck())
            {
                property.boolValue = newValue;
            }
        }

        public void AddHelpBoxRect(string message)
        {
            var content = new GUIContent(message);
            var height = EditorStyles.helpBox.CalcSize(content).y;
            AddRect(height);
        }
        
        public void DrawHelpBox(string text, MessageType messageType)
        {
            var content = new GUIContent(text);
            var height = EditorStyles.helpBox.CalcSize(content).y;
            AddRect(height);
            var rect = EditorGUI.IndentedRect(CurrentRect);
            EditorGUI.HelpBox(rect, text, messageType);
        }
    }
}