using nickeltin.InternalBridge.Editor;
using nickeltin.SDF.Runtime;
using UnityEditor;
using UnityEngine;

namespace nickeltin.SDF.Editor
{
    /// <summary>
    /// Handles dragNDrop for all cases possible cases, see more <see cref="SDFDragAndDrop.HandleListDragAndDrop(SerializedProperty, bool, bool, bool)"/>
    /// </summary>
    [CustomPropertyDrawer(typeof(SDFSpriteReferenceList))]
    internal class SDFSpriteReferenceListDrawer : PropertyDrawer
    {
        public static float LINE => EditorGUIUtility.singleLineHeight;
        private static class Defaults
        {
            public static readonly GUIContent Hidden;
            public static readonly GUIContent Visible;
            public static readonly GUIContent ClearCollection;
            public static readonly GUIContent CompactModeContext;
            public static readonly GUIContent CompactMode;

            public static readonly GUIStyle CompactModeButton;
            
            static Defaults()
            {
                CompactModeContext = new GUIContent("Compact mode");
                
                ClearCollection = new GUIContent("Clear");
                // Icon button style but it will stretch to width
                CompactModeButton = new GUIStyle(EditorStyles.iconButton)
                {
                    fixedWidth = 0,
                    stretchWidth = true,
                    stretchHeight = true,
                    alignment = TextAnchor.MiddleCenter,
                };
                
                Hidden = EditorGUIUtility.IconContent("animationvisibilitytoggleoff");
                Visible = EditorGUIUtility.IconContent("animationvisibilitytoggleon");

                CompactMode = new GUIContent("", "Toggle compact mode");
            }

            public static GUIContent GetExpandButtonContent(SerializedProperty property)
            {
                CompactMode.image = property.isExpanded ? Visible.image : Hidden.image;
                return CompactMode;
            }
        }
        
        [InitializeOnLoadMethod]
        private static void Init()
        {
            // Context menus for better list UX
            EditorApplication.contextualPropertyMenu += (menu, property) =>
            {
                _ScriptAttributeUtility.GetFieldInfoFromProperty(property, out var type);

                if (typeof(SDFSpriteReferenceList).IsAssignableFrom(type))
                {
                    menu.AddItem(Defaults.ClearCollection, false, () =>
                    {
                        var list = GetList(property);
                        list.ClearArray();
                        list.serializedObject.ApplyModifiedProperties();
                    });
                            
                    menu.AddItem(Defaults.CompactModeContext, !property.isExpanded, () =>
                    {
                        property.isExpanded = !property.isExpanded;
                    });
                }
            };
        }
        
        
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var list = GetList(property);
            if (!list.hasMultipleDifferentValues)
                EditorGUI.BeginProperty(rect, label, property);
            
            var headerRect = new Rect(rect)
            {
                height = LINE
            };

            // Handling dnd before list drawing to prevent header from stealing events
            if (SDFDragAndDrop.HandleListDragAndDrop(list, headerRect))
            {
                Event.current.Use();
            }

            var foldoutRect = EditorGUI.IndentedRect(headerRect);
            
            // Drawing button before foldout header 
            var compactModeRect = new Rect(foldoutRect);
            compactModeRect.width = 24;
            compactModeRect.x += foldoutRect.width - 30 + 4;

            if (GUI.Button(compactModeRect, Defaults.GetExpandButtonContent(property), Defaults.CompactModeButton))
            {
                Event.current.Use();
                property.isExpanded = !property.isExpanded;
                _InspectorWindow.RepaintAllInspectorsImmediately();
                GUIUtility.ExitGUI();
            }
            
            list.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(foldoutRect, list.isExpanded, label);
            EditorGUI.EndFoldoutHeaderGroup();
            if (list.isExpanded)
            {
                using (new SDFSpriteReferenceDrawer.DrawExtendedScope(property.isExpanded))
                {
                    var listRect = new Rect(rect);
                    listRect.height -= LINE - 1;
                    listRect.y += LINE + 1;
                    listRect = EditorGUI.IndentedRect(listRect);
                    var drawer = _PropertyHandler.GetReorderableList(list, list);
                    drawer.DoList(listRect);
                }
            }
            
            
            if (!list.hasMultipleDifferentValues)
                EditorGUI.EndProperty();
        }

        private static SerializedProperty GetList(SerializedProperty property)
        {
            return property.FindPropertyRelative(nameof(SDFSpriteReferenceList._list));
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var list = GetList(property);
            if (list.isExpanded)
            {
                float propH;
                using (new SDFSpriteReferenceDrawer.DrawExtendedScope(property.isExpanded))
                {
                    propH = EditorGUI.GetPropertyHeight(list, label, true);
                }
                
                return propH;
            }

            return EditorGUIUtility.singleLineHeight;
        }
    }
}