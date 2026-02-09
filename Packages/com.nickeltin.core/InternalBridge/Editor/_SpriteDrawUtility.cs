using UnityEditor.UI;
using UnityEngine;

namespace nickeltin.InternalBridge.Editor
{
    public static class _SpriteDrawUtility
    {
        public static void DrawSprite(Sprite sprite, Rect drawArea, Color color)
        {
            SpriteDrawUtility.DrawSprite(sprite, drawArea, color);
        }
    }
}