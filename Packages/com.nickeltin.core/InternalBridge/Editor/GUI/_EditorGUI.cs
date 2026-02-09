using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace nickeltin.InternalBridge.Editor
{
    /// <summary>
    /// Provides access to internal unity <see cref="EditorGUI"/> and <see cref="EditorGUIUtility"/> members.
    /// </summary>
    public static class _EditorGUI
    {
        public delegate Object ObjectFieldValidator(
            Object[] references,
            Type objType,
            SerializedProperty property,
            ObjectFieldValidatorOptions options);
        
        [Flags]
        public enum ObjectFieldValidatorOptions
        {
            None = 0,
            ExactObjectTypeValidation = 1,
        }

        /// <summary>
        /// Readonly exposed internal property <see cref="EditorGUIUtility.s_LastControlID"/>
        /// </summary>
        public static int LastControlID => EditorGUIUtility.s_LastControlID;
        
        /// <summary>
        /// Public accessor for internal method.
        /// <see cref="EditorGUI.DoObjectField(Rect, Rect, int, Object, Object, Type, EditorGUI.ObjectFieldValidator, bool, GUIStyle)"/> 
        /// </summary>
        /// <param name="position">Total field position</param>
        /// <param name="dropRect">Rect that drag and drop of object can be performed</param>
        /// <param name="id">Control id</param>
        /// <param name="obj">Current object value</param>
        /// <param name="objBeingEdited">Target serialized object/host</param>
        /// <param name="objType">Allowed object type</param>
        /// <param name="validator">Validate what objects is actually can be referenced</param>
        /// <param name="allowSceneObjects"></param>
        /// <param name="style"></param>
        /// <returns>New value for object</returns>
        public static Object DoObjectField(Rect position, Rect dropRect, int id, Object obj, Object objBeingEdited,
            Type objType, ObjectFieldValidator validator, bool allowSceneObjects, GUIStyle style = null)
        {
            Object NativeValidator(Object[] references, Type type, SerializedProperty property, EditorGUI.ObjectFieldValidatorOptions options)
            {
                return validator(references, type, property, (ObjectFieldValidatorOptions)options);
            }

            return EditorGUI.DoObjectField(position, dropRect, id, obj, objBeingEdited, objType, NativeValidator, allowSceneObjects, style);
            
        }
        
        public static void Popup(Rect position, SerializedProperty property,
            GUIContent[] displayedOptions, GUIContent label)
        {
            EditorGUI.Popup(position, property, displayedOptions, label);
        }
        
        public static int Popup(Rect position, GUIContent label,
            int selectedIndex, string[] displayedOptions)
        {
            return EditorGUI.Popup(position, label, selectedIndex, displayedOptions);
        }
    }
}