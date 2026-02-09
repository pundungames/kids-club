using System;
using System.Reflection;
using UnityEditor;

namespace nickeltin.InternalBridge.Editor
{
    public static class _ScriptAttributeUtility
    {
        public static FieldInfo GetFieldInfoAndStaticTypeFromProperty(SerializedProperty property, out Type type)
        {
            return ScriptAttributeUtility.GetFieldInfoAndStaticTypeFromProperty(property, out type);
        }

        public static FieldInfo GetFieldInfoFromProperty(SerializedProperty property, out Type type)
        {
            return ScriptAttributeUtility.GetFieldInfoFromProperty(property, out type);
        }
    }
}