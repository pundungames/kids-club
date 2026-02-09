using System;
using nickeltin.SDF.Runtime;
using UnityEditor;
using UnityEngine;

namespace nickeltin.SDF.Editor
{
    [CustomPropertyDrawer(typeof(SDFSpriteReference))]
    internal class SDFSpriteReferenceDrawer : PropertyDrawer
    {
        public readonly struct DrawExtendedScope : IDisposable
        {
            private readonly bool _wasExtended;

            public DrawExtendedScope(bool drawExtended)
            {
                _wasExtended = DrawExtended;
                DrawExtended = drawExtended;
            }

            public void Dispose() => DrawExtended = _wasExtended;
        }
        
        public static bool DrawExtended = true;
        
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);
            var initRect = rect;
            rect.height = EditorGUIUtility.singleLineHeight;

            var metadataProp = GetMetadataAssetProperty(property);
            var metaAsset = metadataProp.objectReferenceValue as SDFSpriteMetadataAsset;
            
            rect = EditorGUI.PrefixLabel(rect, label);
            // For some reason PrefixLabel does not include indent level
            var offset = -EditorGUI.indentLevel * 15;
            rect.x += offset;
            rect.width -= offset;
            
            EditorGUI.PropertyField(rect, metadataProp, GUIContent.none);
            
            SDFDragAndDrop.HandleDragAndDrop(metadataProp, rect);

            if (DrawExtended)
            {
                using (new EditorGUI.IndentLevelScope())
                using (new EditorGUI.DisabledScope(true))
                {
                    initRect.height = EditorGUIUtility.singleLineHeight;
                    initRect.y += EditorGUIUtility.singleLineHeight + 2;

                    EditorGUI.showMixedValue = metadataProp.hasMultipleDifferentValues;
                    EditorGUI.ObjectField(initRect, "Source Sprite", metaAsset?.Metadata.SourceSprite, 
                        typeof(Sprite), false);

                    initRect.y += EditorGUIUtility.singleLineHeight + 2;

                    EditorGUI.showMixedValue = metadataProp.hasMultipleDifferentValues;
                    EditorGUI.ObjectField(initRect, "SDF Sprite", metaAsset?.Metadata.SDFSprite, 
                        typeof(Sprite), false);
                }
            }

            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        { 
            return (EditorGUIUtility.singleLineHeight + 2) * (DrawExtended ? 3 : 1);
        }

        public static SerializedProperty GetMetadataAssetProperty(SerializedProperty property)
        {
            return property.FindPropertyRelative(nameof(SDFSpriteReference._metadataAsset));
        }
    }
}