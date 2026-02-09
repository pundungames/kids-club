using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace nickeltin.Core.Editor
{
    public static class ToolbarExtensions
    {
        public enum Align
        {
            Left,
            PlayMode,
            Right
        }
        
        public enum Order
        {
            Insert, 
            Append
        }

        private struct ExtensionDefinition
        {
            public readonly VisualElement Extension;
            public readonly Align Alignment;
            public readonly Order Order;
            
            public ExtensionDefinition(VisualElement extension, Align alignment, Order order)
            {
                Extension = extension;
                Alignment = alignment;
                Order = order;
            }
        }

        private static readonly Dictionary<Align, string> _alignToElementName = new Dictionary<Align, string>()
        {
            { Align.Left,  "ToolbarZoneLeftAlign"},
            { Align.PlayMode,  "ToolbarZonePlayMode"},
            { Align.Right,  "ToolbarZoneRightAlign"}
        };

        private static Object _toolbar;
        private static FieldInfo _rootVisualField;
        private static FieldInfo _getField;
        private static readonly List<ExtensionDefinition> _extensionDefinitions = new List<ExtensionDefinition>();
        private static bool _initialized = false;
        
        
        [InitializeOnLoadMethod]
        private static void Init()
        {
            var toolbarType = Type.GetType("UnityEditor.Toolbar, UnityEditor");
            _rootVisualField = toolbarType!.GetField("m_Root", BindingFlags.Instance | BindingFlags.NonPublic);
            _getField = toolbarType!.GetField("get", BindingFlags.Static | BindingFlags.Public);
            ToolbarRecreated(null);
        }

        /// <summary>
        /// You can put any visual element here and select where on toolbar it will be.
        /// Commonly used types is <see cref="EditorToolbarButton"/>, <see cref="EditorToolbarDropdown"/>
        /// </summary>
        /// <param name="element"></param>
        /// <param name="align">Toolbar divided in three groups:
        ///     Left - where account, services and plastic vcs buttons is,
        ///     PlayMode - three play mode related buttons,
        ///     Right - undo, layers and layouts
        /// </param>
        /// <param name="order">In each of groups there are already elements, this parameter determines how to add extension element:
        ///     Insert - element will be instead into hierarchy as first, usually on the left
        ///     Append - element will be added as last one, usually on the right
        /// </param>
        public static void RegisterExtension(VisualElement element, Align align, Order order)
        {
            _extensionDefinitions.Add(new ExtensionDefinition(element, align, order));
            if (_initialized)
            {
                ResolveExtensions();
            }
        }
        
        private static void DelayedInit()
        {
            EditorApplication.update -= DelayedInit;
            _toolbar = (Object)_getField!.GetValue(null);
            var rootVisualElement = (VisualElement)_rootVisualField!.GetValue(_toolbar);
            rootVisualElement.RegisterCallback<DetachFromPanelEvent>(ToolbarRecreated);
            ResolveExtensions();
            _initialized = true;
        }

        private static void ToolbarRecreated(DetachFromPanelEvent evt)
        {
            _initialized = false;
            EditorApplication.update += DelayedInit;
        }

        private static void ResolveExtensions()
        {
            var rootVisualElement = (VisualElement)_rootVisualField!.GetValue(_toolbar);
            foreach (var extensionDefinition in _extensionDefinitions)
            {
                extensionDefinition.Extension.RemoveFromHierarchy();
            }

            foreach (var extensionDefinition in _extensionDefinitions)
            {
                var parentElement = rootVisualElement.Q(_alignToElementName[extensionDefinition.Alignment]);
                switch (extensionDefinition.Order)
                {
                    case Order.Append:
                        parentElement.Add(extensionDefinition.Extension);
                        break;
                    case Order.Insert:
                        parentElement.Insert(0, extensionDefinition.Extension);
                        break;
                }
            }
        }
    }
}