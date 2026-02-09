using System;
using nickeltin.InternalBridge.Editor;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace nickeltin.SDF.Editor
{
    internal abstract class SDFGraphicsEditor : GraphicEditor
    {
        internal static class Defaults
        {
            public static readonly GUIStyle SDFHeader;
            public static readonly GUIContent SDFTexContent = new GUIContent("SDF texture", 
                "Current SDF texture used, might be original or atlas");

            public static readonly GUIContent TexContent = new GUIContent("Regular Texture",
                "Current texture used, might be original or atlas");

            public static readonly GUIContent ColorTint = new GUIContent("Color Tint",
                "Color that will be applied to all layers. " +
                "This is sent to shader, and it shared by Graphic.CrossFadeColor() method. " +
                "So if Button with color tint added it will override it.");
            
            public static readonly GUIContent CorrectButtonContent;
            public static readonly GUIContent CreateMaterialButton;
            public static readonly GUIContent CopyMaterialButton;
            public static readonly GUIStyle IconButton;

            public static readonly GUIContent SpriteContent;
            public static readonly GUIContent SpriteTypeContent;
            public static readonly GUIContent ClockwiseContent;
            
            public static readonly GUIContent FillOriginContent = EditorGUIUtility.TrTextContent("Fill Origin");
            
            public static readonly GUIContent[] OriginHorizontalStyle =
            {
                EditorGUIUtility.TrTextContent("Left"),
                EditorGUIUtility.TrTextContent("Right")
            };

            public static readonly GUIContent[] OriginVerticalStyle =
            {
                EditorGUIUtility.TrTextContent("Bottom"),
                EditorGUIUtility.TrTextContent("Top")
            };

            public static readonly GUIContent[] Origin90Style =
            {
                EditorGUIUtility.TrTextContent("BottomLeft"),
                EditorGUIUtility.TrTextContent("TopLeft"),
                EditorGUIUtility.TrTextContent("TopRight"),
                EditorGUIUtility.TrTextContent("BottomRight")
            };

            public static readonly GUIContent[] Origin180Style =
            {
                EditorGUIUtility.TrTextContent("Bottom"),
                EditorGUIUtility.TrTextContent("Left"),
                EditorGUIUtility.TrTextContent("Top"),
                EditorGUIUtility.TrTextContent("Right")
            };

            public static readonly GUIContent[] Origin360Style =
            {
                EditorGUIUtility.TrTextContent("Bottom"),
                EditorGUIUtility.TrTextContent("Right"),
                EditorGUIUtility.TrTextContent("Top"),
                EditorGUIUtility.TrTextContent("Left")
            };
            
            static Defaults()
            {
                SDFHeader = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter
                };
                
                CorrectButtonContent = EditorGUIUtility.TrTextContent("Set Native Size", "Sets the size to match the content.");
                CreateMaterialButton = new GUIContent(EditorGUIUtility.IconContent("Toolbar Plus More"))
                {
                    tooltip = "Create new SDF Material"
                };
                
                CopyMaterialButton = new GUIContent(EditorGUIUtility.IconContent("RectMask2D Icon"))
                {
                    tooltip = "Copy current SDF Material"
                };

                IconButton = new GUIStyle(EditorStyles.iconButton)
                {
                    imagePosition = ImagePosition.ImageOnly,
                    margin = new RectOffset(2,2,2,2),
                    fixedWidth = 20,
                    fixedHeight = 18
                };
                
                SpriteContent = EditorGUIUtility.TrTextContent("SDF Metadata");
                SpriteTypeContent = EditorGUIUtility.TrTextContent("Image Type");
                ClockwiseContent = EditorGUIUtility.TrTextContent("Clockwise");
            }
        }
        
        protected abstract Texture[] GetTargetedTextures();
        
        protected void MaterialGUI(Func<Material> defaultMaterialGetter)
        {
            DrawGroup(m_Material, "Material", () =>
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(m_Material);
                if (GUILayout.Button(Defaults.CreateMaterialButton, Defaults.IconButton))
                {
                    _ProjectWindowUtil.TryGetActiveFolderPath(out var path);
                    Debug.Log(path);
                    path = EditorUtility.SaveFilePanelInProject("Save new SDF Material", "New SDF UI Material",
                        "mat",
                        $"Create new SDF Material for {target}", path);

                    if (!string.IsNullOrEmpty(path))
                    {
                        path = AssetDatabase.GenerateUniqueAssetPath(path);
                        var mat = new Material(defaultMaterialGetter());
                        AssetDatabase.CreateAsset(mat, path);
                        ProjectWindowUtil.ShowCreatedAsset(mat);
                        m_Material.objectReferenceValue = mat;
                        serializedObject.ApplyModifiedProperties();
                    }
                    GUIUtility.ExitGUI();
                }


                if (m_Material.objectReferenceValue != null
                    && GUILayout.Button(Defaults.CopyMaterialButton, Defaults.IconButton))
                {
                    var mat = m_Material.objectReferenceValue as Material;
                    var path = AssetDatabase.GetAssetPath(mat);
                    var newPath = AssetDatabase.GenerateUniqueAssetPath(path);
                    AssetDatabase.CopyAsset(path, newPath);
                    mat = AssetDatabase.LoadAssetAtPath<Material>(newPath);
                    ProjectWindowUtil.ShowCreatedAsset(mat);
                    m_Material.objectReferenceValue = mat;
                    serializedObject.ApplyModifiedProperties();
                }

                GUILayout.EndHorizontal();

                // EditorGUILayout.HelpBox("Materials update coming soon", MessageType.Info);
            });
        }
        
        protected static void DrawGroup(SerializedProperty isExpanded, string title, Action drawer)
        {
            _EditorGUILayout.DrawSectionWithFoldout(isExpanded, title, drawer);
            // isExpanded.isExpanded = _EditorGUILayout.DrawSectionFoldout(isExpanded.isExpanded, title);
            // if (isExpanded.isExpanded)
            // {
            //     drawer();
            // }
        }
        
    }
}