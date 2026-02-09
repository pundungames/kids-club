using UnityEditor;

namespace nickeltin.InternalBridge.Editor
{
    public class _GenericInspector
    {
        public static bool IsInstance(UnityEditor.Editor editor) => editor is GenericInspector;
    }
}