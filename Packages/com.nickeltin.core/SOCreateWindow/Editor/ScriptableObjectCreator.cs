using System.Collections.Generic;
using System.Reflection;
using nickeltin.Core.Editor;
using nickeltin.InternalBridge.Editor;
using nickeltin.SOCreateWindow.Runtime;
using UnityEditor;
using UnityEngine;

namespace nickeltin.SOCreateWindow.Editor
{
    internal static class ScriptableObjectCreator
    {
        private static bool _cacheValid;
        private static List<ISearchWindowEntry> _entries;
        private static MenuCommand _lastCommand;
       
        private static void ValidateCache()
        {
            if (_cacheValid) return;
            
            _cacheValid = true;
            _entries = new List<ISearchWindowEntry>();
            var typeCollection = TypeCache.GetTypesWithAttribute<CreateAssetWindowAttribute>();
            foreach (var type in typeCollection)
            {
                if (!ScriptableObjectCreateWindow.IsTypeValid(type))
                {
                    continue;
                }

                var attr = type.GetCustomAttribute<CreateAssetWindowAttribute>();
                var entryData = new ScriptableObjectCreateWindow.ObjectData(attr, type);
                var entry = new ScriptableObjectCreateWindow.Entry(entryData);
                _entries.Add(entry);
            }

            var methodCollection = TypeCache.GetMethodsWithAttribute<CustomCreateAssetWindowAttribute>();
            foreach (var method in methodCollection)
            {
                if (!method.IsStatic) continue;
                
                var attr = method.GetCustomAttribute<CustomCreateAssetWindowAttribute>();
                var entryData = new ScriptableObjectCreateWindow.ObjectData(attr, method);
                var entry = new ScriptableObjectCreateWindow.Entry(entryData);
                _entries.Add(entry);
            }
        }

        public const string PATH = "Assets/Create Window";

        [InitializeOnLoadMethod]
        private static void VerifyCreateMenuInit()
        {
            EditorApplication.delayCall += VerifyCreateMenu;
        }

        private const int MenuPriority =
#if UNITY_2022_1_OR_NEWER
                15
#else
                19
#endif
            ;
        
        public static void VerifyCreateMenu()
        {
            var menuExist = _Menu.MenuItemExists(PATH);
            var menuShouldExist = NickeltinCoreProjectSettings.ShowCreateWindow;
            
            // Menu is removed but should be added
            if (!menuExist && menuShouldExist)
            {
                _Menu.AddMenuItem(PATH, "", false, MenuPriority,
                    ShowDropdown, () => true);
            }
            // Menu exist but should be removed
            else if (menuExist && !menuShouldExist)
            {
                _Menu.RemoveMenuItem(PATH);
            }
        }


        private static void ShowDropdown()
        {
            ValidateCache();
            _ProjectWindowUtil.TryGetActiveFolderPath(out var path);
            ScriptableObjectCreateWindow.Open(_entries, entry =>
            {
                var data = (ScriptableObjectCreateWindow.ObjectData)entry.GetData();
                if (data.CustomAssetCreateHandler != null)
                {
                    data.CustomAssetCreateHandler(path);
                }
                else
                {
                    path = AssetDatabase.GenerateUniqueAssetPath(path + "/" + data.FileName + ".asset");
                    var instance = ScriptableObject.CreateInstance(data.Type);
                    ProjectWindowUtil.CreateAsset(instance, path);
                }
                
            }, false, true, size: new Vector2(400, 600), topLabel: "Create Scriptable Object");
        }
        
    }
}