using System;
using System.IO;
using System.Linq;
using nickeltin.InternalBridge.Editor;
using nickeltin.SDF.InternalBridge.Editor;
using UnityEditor;
using UnityEngine;

namespace nickeltin.SDF.Editor
{
    internal class SDFAssetsFamilyPopup : PopupWindowContent
    {
        private static class Defaults
        {
            private static readonly Color header_l = new Color32(223, 223, 223, byte.MaxValue);
            private static readonly Color header_d = new(0.5f, 0.5f, 0.5f, 0.2f);

            private static readonly Color[] rows_l = new Color[2]
            {
                new Color32(200, 200, 200, byte.MaxValue),
                new Color32(206, 206, 206, byte.MaxValue)
            };

            private static readonly Color[] rows_d = new Color[2]
            {
                new Color32(56, 56, 56, byte.MaxValue),
                new Color32(62, 62, 62, byte.MaxValue)
            };

            public static Color headerBackground => EditorGUIUtility.isProSkin ? header_d : header_l;

            public static Color rowBackground(int i)
            {
                return !EditorGUIUtility.isProSkin ? rows_l[i % 2] : rows_d[i % 2];
            }

            public static readonly GUIContent titlePrefixLabel = EditorGUIUtility.TrTextContent("SDFAsset's using ");

            public static readonly GUIStyle boldRightAligned = new(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = (int)(1.100000023841858 * EditorStyles.boldLabel.fontSize)
            };
        }

        public static bool isOpen { get; private set; }

        private const float _windowWidth = 300;
        private const float _windowHeight = 300;
        
        private Vector2 _scroll = Vector2.zero;

        private readonly TextureImporter _target;
        private readonly string[] _usedSDFAssetsPaths;

        public SDFAssetsFamilyPopup(TextureImporter target)
        {
            if (isOpen)
                throw new InvalidOperationException("PrefabFamilyPopup is already open");

            _target = target;
            _usedSDFAssetsPaths =
                SDFEditorUtil.FindSDFAssetsThatUsesTexture(
                        AssetDatabase.GUIDFromAssetPath(target.assetPath).ToString())
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .ToArray();
        }

        public override void OnOpen()
        {
            base.OnOpen();
            isOpen = true;
        }

        public override void OnClose()
        {
            base.OnClose();
            isOpen = false;
        }

        public override Vector2 GetWindowSize()
        {
            if (_target == null)
            {
                editorWindow.Close();
                return Vector2.one;
            }

            return new Vector2(_windowWidth, _windowHeight);
        }

        public override void OnGUI(Rect rect)
        {
            if (_target == null)
            {
                editorWindow.Close();
            }
            else
            {
                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                {
                    editorWindow.Close();
                    GUIUtility.ExitGUI();
                }

                DrawHeader();

                DrawHierarchy();
            }
        }

        private void DrawHierarchy()
        {
            _scroll = GUILayout.BeginScrollView(_scroll);
            for (var i = 0; i < _usedSDFAssetsPaths.Length; ++i)
            {
                var assetPath = _usedSDFAssetsPaths[i];
                var rect = GUILayoutUtility.GetRect(20f, _windowWidth, 28f, 28f);
                EditorGUI.DrawRect(rect, Defaults.rowBackground(i + 1));
                DoObjectLabel(rect, assetPath, EditorStyles.label);
            }

            GUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            var rect1 = GUILayoutUtility.GetRect(20f, _windowWidth, 28f, 28f);
            EditorGUI.DrawRect(rect1, Defaults.headerBackground);
            var x = Defaults.boldRightAligned.CalcSize(Defaults.titlePrefixLabel).x;
            var position = new Rect(6f, rect1.y + 3f, x, EditorGUIUtility.singleLineHeight);
            var rect2 = new Rect((float)(position.x + (double)position.width + 3.0), position.y, _windowWidth,
                position.height);
            GUI.Label(position, Defaults.titlePrefixLabel, Defaults.boldRightAligned);
            DoObjectLabel(rect2, _target.assetPath, EditorStyles.boldLabel);
            position.y = position.height + 6f;
        }

        private void DoObjectLabel(Rect rect, string assetPath, GUIStyle style)
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                rect.Contains(Event.current.mousePosition))
            {
                if (Event.current.clickCount == 1)
                {
                    GUIUtility.keyboardControl = GUIUtility.GetControlID(FocusType.Keyboard);
                    EditorGUIUtility.PingObject(_AssetDatabase.GetMainAssetInstanceID(assetPath));
                    Event.current.Use();
                }
                else if (Event.current.clickCount == 2)
                {
                    Selection.activeInstanceID = _AssetDatabase.GetMainAssetInstanceID(assetPath);
                    Event.current.Use();
                    editorWindow.Close();
                    GUIUtility.ExitGUI();
                }
            }

            var cachedIcon = AssetDatabase.GetCachedIcon(assetPath);
            var withoutExtension = Path.GetFileNameWithoutExtension(assetPath);
            GUI.Label(rect, _EditorGUIUtility.TempContent(withoutExtension, cachedIcon), style);
        }
    }
}