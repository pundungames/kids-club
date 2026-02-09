using UnityEditor;

namespace nickeltin.SDF.InternalBridge.Editor
{
    internal class _SpriteInspector : SpriteInspector
    {
        public static bool IsInstance(UnityEditor.Editor editor) => editor is SpriteInspector;
    }
}