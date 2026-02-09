using nickeltin.SDF.InternalBridge.Editor;
using nickeltin.SDF.Runtime;
using UnityEditor;
using UnityEngine;
using DataUtility = UnityEngine.Sprites.DataUtility;

namespace nickeltin.SDF.Editor
{
    [CustomEditor(typeof(SDFSpriteMetadataAsset)), CanEditMultipleObjects]
    internal class SDFSpriteMetadataEditor : UnityEditor.Editor
    {
        private SerializedProperty _metadata;
        private SerializedProperty _isImportedDecoupled;
        private SerializedProperty _generationBackend;
        
        private void OnEnable()
        {
            _metadata = serializedObject.FindProperty(nameof(SDFSpriteMetadataAsset._metadata));
            _isImportedDecoupled = serializedObject.FindProperty(nameof(SDFSpriteMetadataAsset._isImportedDecoupled));
            _generationBackend = serializedObject.FindProperty(nameof(SDFSpriteMetadataAsset._generationBackend));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_metadata);
            EditorGUILayout.PropertyField(_isImportedDecoupled);
            EditorGUILayout.PropertyField(_generationBackend);
            if (targets.Length > 1)
            {
                EditorGUILayout.HelpBox("Sprites can't be drawn for multi-selection", MessageType.Info);
            }
            else
            {
                DrawSprites(target as SDFSpriteMetadataAsset);
            }
        }
        
        public static void DrawSprites(SDFSpriteMetadataAsset asset)
        {
            DrawSprite(asset._metadata.SourceSprite, "Source sprite");
            DrawSprite(asset._metadata.SDFSprite, "Generated SDF sprite");
        }
        
        private static void DrawSprite(Sprite sprite, string name)
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(false));
            EditorGUILayout.LabelField(name);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(sprite, typeof(Sprite), false);
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            if (sprite != null)
            {
                var uv = DataUtility.GetOuterUV(sprite).ToRect();
                var outerRect = DrawTex(sprite.texture, uv);
                if (Event.current.type == EventType.Repaint)
                {
                    var innerUv = DataUtility.GetInnerUV(sprite).ToRect();
                    var min = SDFMath.InverseLerp(uv.min, uv.max, innerUv.min);
                    var max = SDFMath.InverseLerp(uv.min, uv.max, innerUv.max);
                    var innerUvNorm = SDFMath.PackMinMax(min, max);
                    var innerRectMin = SDFMath.Lerp(outerRect.min, outerRect.max, innerUvNorm.Min());
                    var innerRectMax = SDFMath.Lerp(outerRect.min, outerRect.max, innerUvNorm.Max());
                    _SpriteUtility.BeginLines(Color.green);
                    // Low horizontal
                    if (sprite.border.y > 0)
                    {
                        _SpriteUtility.DrawLine(outerRect.xMin, innerRectMin.y, outerRect.xMax, innerRectMin.y);
                    }
                    // High horizontal
                    if (sprite.border.w > 0)
                    {
                        _SpriteUtility.DrawLine(outerRect.xMin, innerRectMax.y, outerRect.xMax, innerRectMax.y);
                    }
                    // Left vertical
                    if (sprite.border.x > 0)
                    {
                        _SpriteUtility.DrawLine(innerRectMin.x, outerRect.yMin, innerRectMin.x, outerRect.yMax);
                    }
                    // Right vertical
                    if (sprite.border.z > 0)
                    {
                        _SpriteUtility.DrawLine(innerRectMax.x, outerRect.yMin, innerRectMax.x, outerRect.yMax);
                    }
                    _SpriteUtility.EndLines();
                }
            }
            EditorGUILayout.EndVertical();
        }
        
        private static Rect DrawTex(Texture tex, Rect uv, float maxHeight = 240)
        {
            var pixelRect = SDFGenerationInternalUtil.UVToTextureRect(tex, uv);
            var texRect = GUILayoutUtility.GetAspectRect(pixelRect.width / pixelRect.height, 
                GUILayout.MaxHeight(maxHeight), GUILayout.MaxWidth(maxHeight));
            GUI.DrawTextureWithTexCoords(texRect, tex, uv);
            return texRect;
        }
    }
}