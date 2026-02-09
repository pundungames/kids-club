using UnityEditor;

namespace nickeltin.InternalBridge.Editor
{
    public static class _EditorApplication
    {
        public static event EditorApplication.CallbackFunction globalEventHandler
        {
            add => EditorApplication.globalEventHandler += value;
            remove => EditorApplication.globalEventHandler -= value;
        } 
        
        public static bool CanReloadAssemblies()
        {
            return EditorApplication.CanReloadAssemblies();
        }
    }
}