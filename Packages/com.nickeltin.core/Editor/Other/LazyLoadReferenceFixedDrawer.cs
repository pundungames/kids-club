using nickeltin.Core.Runtime;
using nickeltin.InternalBridge.Editor;
using UnityEditor;
using UnityEngine;

namespace nickeltin.Core.Editor
{
    [CustomPropertyDrawer(typeof(LazyLoadReferenceFixedDrawerAttribute))]
    public class LazyLoadReferenceFixedDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _ScriptAttributeUtility.GetFieldInfoFromProperty(property, out var type);
            EditorGUI.BeginChangeCheck();
            var objectReferenceValue = property.objectReferenceValue;
            position = EditorGUI.PrefixLabel(position, label);
            // Debug.Log(property);
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            var @object = EditorGUI.ObjectField(position, objectReferenceValue, type.GetGenericArguments()[0], false);
            if (!EditorGUI.EndChangeCheck())
                return;
            property.objectReferenceValue = @object;
        }
    }
}