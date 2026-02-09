using System;
using UnityEditor;
using UnityEngine;

namespace nickeltin.InternalBridge.Editor
{
    public static class _EditorGUILayout
    {
        private static GUIStyle _helpBoxRichText;
        static _EditorGUILayout()
        {
            _helpBoxRichText = new GUIStyle(EditorStyles.helpBox)
            {
                richText = true
            };
        }

        public readonly struct ExpandState
        {
            private readonly Action<bool> _set;
            private readonly Func<bool> _get;
            
            public ExpandState(Action<bool> set, Func<bool> get)
            {
                _set = set;
                _get = get;
            }
            
            public bool IsExpanded
            {
                get => _get();
                set => _set(value);
            }

            public static implicit operator ExpandState(SerializedProperty property)
            {
                return new ExpandState(b =>
                {
                    try
                    {
                        property.isExpanded = b;
                    }
                    catch (Exception e)
                    {
                        // ignored
                    }

                }, () =>
                {
                    try
                    {
                        return property.isExpanded;
                    }
                    catch (Exception e)
                    {
                        // ignored
                    }

                    return false;

                });
            }
            
            
            public static implicit operator ExpandState(_SavedBool savedBool)
            {
                return new ExpandState(b => savedBool.Value = b, () => savedBool.Value);
            }
            
            public static implicit operator ExpandState(bool staticValue)
            {
                return new ExpandState(_ => {}, () => staticValue);
            }
        } 
        
        public static void DrawSectionWithFoldout(ExpandState expandState, string title, Action drawer, bool withIndent = true)
        {
            expandState.IsExpanded = DrawSectionFoldout(expandState.IsExpanded, title);
            if (!expandState.IsExpanded) return;
            
            if (withIndent)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    drawer?.Invoke();
                }
            }
            else drawer?.Invoke();
        }
        
        public static bool DrawSectionFoldout(bool isExpanded, string title)
        {
            var guiWasEnabled = GUI.enabled;
            GUI.enabled = true;
            var rect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth, 17);
            var bgRect = rect;
            bgRect.width += 22;
            bgRect.x -= 18;
            EditorGUI.DrawRect(bgRect, new Color(0.196f, 0.196f, 0.196f));
            var splitterRect = bgRect;
            splitterRect.height = 1;
            EditorGUI.DrawRect(splitterRect, new Color(0.12f, 0.12f, 0.12f, 1.333f));
            var labelRect = rect;
            labelRect.x += 16;
            labelRect.width -= 16;
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);
            // Checking for change because otherwise something weird happens with toggle return value when have mixed values obejct picker...
            EditorGUI.BeginChangeCheck();
            var result = EditorGUI.Toggle(rect, GUIContent.none, isExpanded, EditorStyles.foldout);
            GUI.enabled = guiWasEnabled;
            return EditorGUI.EndChangeCheck() ? result : isExpanded;
        }

        public static void RichTextHelpBox(string message, MessageType type)
        {
            EditorGUILayout.LabelField(GUIContent.none, EditorGUIUtility.TempContent(message, EditorGUIUtility.GetHelpIcon(type)), _helpBoxRichText);
        }
    }
}