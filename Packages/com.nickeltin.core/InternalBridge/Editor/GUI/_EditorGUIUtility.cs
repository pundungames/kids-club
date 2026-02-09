using UnityEditor;
using UnityEngine;

namespace nickeltin.InternalBridge.Editor
{
    public class _EditorGUIUtility
    {
        public static GUIContent TempContent(string t, Texture i)
        {
            return EditorGUIUtility.TempContent(t, i);
        }
    }
}