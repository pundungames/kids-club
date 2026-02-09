using UnityEditor;
using UnityEngine;

namespace nickeltin.InternalBridge.Editor
{
    public static class _SerializedProperty
    {
        /// <summary>
        /// Just adds array element.
        /// </summary>
        public static void AppendFoldoutPPtrValue(this SerializedProperty property, Object value)
        {
            property.Verify(SerializedProperty.VerifyFlags.IteratorNotAtEnd);
            property.AppendFoldoutPPtrValue(value);
        }
    }
}