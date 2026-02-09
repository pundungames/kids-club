using UnityEngine;

namespace nickeltin.SDF.InternalBridge.Editor
{
    internal static class _GUILayoutUtility
    {
        internal static GUILayoutGroup topLevel => GUILayoutUtility.current.topLevel;

        public static Rect topLevel_GetLast() => topLevel.GetLast();
    }
}