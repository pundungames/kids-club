using UnityEditor;

namespace nickeltin.InternalBridge.Editor
{
    public static class _ProjectBrowser
    {
        public static void SearchInProjectWindow(string searchText)
        {
            EditorUtility.FocusProjectWindow();
            var window = EditorWindow.GetWindow<ProjectBrowser>();
            window.SetSearch(searchText);
        }
    }
}