using System;
using System.Linq;
using UnityEditor;

namespace nickeltin.InternalBridge.Editor
{
    public static class _Menu
    {
        public static void RemoveMenuItem(string name) => Menu.RemoveMenuItem(name);
        public static bool MenuItemExists(string menuPath)
        {
#if UNITY_2022_1_OR_NEWER
            return Menu.MenuItemExists(menuPath);
#else
            var menuParts = menuPath.Split('/');
            var parentMenu = string.Join('/', menuParts.SkipLast(1));
            var items = GetMenuItems(parentMenu, true, true);
            return items.Any(i => i.path.EndsWith(menuParts.Last()));
#endif
        }

        /// <summary>
        /// Will return menu items from path root, means it will not return end nodes of menu tree.
        /// </summary>
        public static _ScriptingMenuItem[] GetMenuItems(string menuPath, bool includeSeparators, bool localized)
        {
            return Menu.GetMenuItems(menuPath, includeSeparators, localized)
                .Select(item => new _ScriptingMenuItem(item)).ToArray();
        }
        
        public static void AddMenuItem(string name, string shortcut, bool @checked, int priority, 
            Action execute, Func<bool> validate)
        {
            Menu.AddMenuItem(name, shortcut, @checked, priority, execute, validate);
        }
    }
}