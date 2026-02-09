using System;
using UnityEditor;

namespace nickeltin.InternalBridge.Editor
{
    public class _GameView
    {
        public static Type NativeType => typeof(GameView);

        public static EditorWindow GetWindow()
        {
            return EditorWindow.GetWindow(NativeType);
        }
    }
}