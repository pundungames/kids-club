using nickeltin.Core.Runtime;
using nickeltin.InternalBridge.Editor;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace nickeltin.Core.Editor
{
    internal static class ContextMenus
    {
        [MenuItem(MenuPaths.TOOLBAR + "LockReloadAssemblies", priority = 0)]
        private static void LockReloadAssemblies_Context() => EditorApplication.LockReloadAssemblies();

        [MenuItem(MenuPaths.TOOLBAR + "UnlockReloadAssemblies", priority = 1)]
        private static void UnlockReloadAssemblies_Context() => EditorApplication.UnlockReloadAssemblies();

        // [MenuItem(MenuPaths.TOOLBAR + "-")] private static void Separator1() { }
        
        
        [MenuItem(MenuPaths.TOOLBAR + "LockReloadAssemblies", true)]
        private static bool LockReloadAssemblies_Validator() => _EditorApplication.CanReloadAssemblies();

        [MenuItem(MenuPaths.TOOLBAR + "UnlockReloadAssemblies", true)]
        private static bool UnlockReloadAssemblies_Validator() => !_EditorApplication.CanReloadAssemblies();


        
        [MenuItem(MenuPaths.TOOLBAR + "Recompile", priority = 2)]
        private static void Recompile_Context()
        {
            EditorApplication.UnlockReloadAssemblies();
            CompilationPipeline.RequestScriptCompilation();
        }
        
        [MenuItem(MenuPaths.TOOLBAR + "Recompile", true)]
        private static bool Recompile_Validator()
        {
            return !EditorApplication.isCompiling;
        }
        
        
        
        [MenuItem(MenuPaths.TOOLBAR + "StartAssetEditing", priority = 40)]
        private static void StartAssetEditing_Context() => AssetDatabase.StartAssetEditing();

        [MenuItem(MenuPaths.TOOLBAR + "StopAssetEditing", priority = 41)]
        private static void StopAssetEditing_Context()
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }


        [MenuItem(MenuPaths.TOOLBAR + "Open DataPath", priority = 60)]
        private static void PersistentDataPath_Context() => EditorUtility.RevealInFinder(Application.persistentDataPath);

        [MenuItem(MenuPaths.TOOLBAR + "Build And Run (Ignore Exceptions)", priority = 61)]
        private static void BuildAndRun_Context()
        {
            BuildPlayerWindow.ShowBuildPlayerWindow();
            _BuildPlayerWindow.CallBuildMethods(true, BuildOptions.AutoRunPlayer);
        }
    }
}