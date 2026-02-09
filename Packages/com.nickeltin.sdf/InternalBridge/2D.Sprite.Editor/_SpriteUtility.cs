using UnityEngine;
using SpriteEditorUtility = UnityEditorInternal.SpriteEditorUtility;
using SpriteUtility = UnityEditor.SpriteUtility;

namespace nickeltin.SDF.InternalBridge.Editor
{
    internal static class _SpriteUtility
    {
        public static void BeginLines(Color color) => SpriteEditorUtility.BeginLines(color);

        public static void DrawBox(Rect position) => SpriteEditorUtility.DrawBox(position);

        public static void DrawLine(Vector3 p1, Vector3 p2) => SpriteEditorUtility.DrawLine(p1, p2);
        
        public static void DrawLine(Vector2 p1, Vector2 p2) => SpriteEditorUtility.DrawLine(p1, p2);
        public static void DrawLine(float xA, float yA, float xB, float yB) => DrawLine(new Vector2(xA, yA), new Vector2(xB, yB));
        
        public static void EndLines() => SpriteEditorUtility.EndLines();

        public static Texture2D RenderStaticPreview(Sprite sprite, Color color, int width, int height)
        {
            return SpriteUtility.RenderStaticPreview(sprite, color, width, height);
        }
    }
}