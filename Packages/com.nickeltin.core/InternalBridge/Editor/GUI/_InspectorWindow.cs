using UnityEditor;

namespace nickeltin.InternalBridge.Editor
{
    public static class _InspectorWindow
    {
        public static void RepaintAllInspectorsImmediately()
        {
            var inspectors = InspectorWindow.GetInspectors();
            foreach (var inspectorWindow in inspectors)
            {
                inspectorWindow.RepaintImmediately();
            }
        }
    }
}