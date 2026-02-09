using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.Assertions;

namespace nickeltin.InternalBridge.Editor
{
    public static class _MenuOptions
    {
        private delegate void PlaceUIElementRootDelegate(GameObject element, MenuCommand command);

        private static readonly PlaceUIElementRootDelegate PlaceUIElementRoot_delegate;
        
        static _MenuOptions()
        {
            var methodInfo = typeof(MenuOptions)?.GetMethod("PlaceUIElementRoot", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(methodInfo);
            PlaceUIElementRoot_delegate = (PlaceUIElementRootDelegate)Delegate.CreateDelegate(typeof(PlaceUIElementRootDelegate), methodInfo);
        }
        
        /// <summary>
        /// Places newly created UI element inside any existent Canvas, or creates new one.
        /// Uses internal unity methods used in UI creation context menus.
        /// </summary>
        public static void PlaceUIElementRoot(GameObject element, MenuCommand command)
        {
            PlaceUIElementRoot_delegate(element, command);
        }
    }
}