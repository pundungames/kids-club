using System.Collections.Generic;
using System.Linq;
using nickeltin.SDF.Runtime;
using UnityEditor;
using UnityEngine;

namespace nickeltin.SDF.Editor
{
    [CustomEditor(typeof(PureSDFImage))]
    [CanEditMultipleObjects]
    internal class PureSDFImageEditor : SDFGraphicsEditor
    {
        private static class lDefaults
        {
            public static readonly GUIContent ControlledByStack = new(EditorGUIUtility.IconContent("d_orangeLight"))
            {
                tooltip = "Property controlled by stack"
            };
        }
        
        private SerializedProperty _layerA;
        private SerializedProperty _offset;
        private SerializedProperty _lerpLayers;
        private SerializedProperty _layerB;
        private SerializedProperty _layersLerp;

        private PureSDFImage _image;
        private bool _controlledByRenderingStack;
        
        protected override void OnEnable()
        {
            _image = target as PureSDFImage;
            
            _layerA = serializedObject.FindProperty(nameof(PureSDFImage._layerA));
            _offset = serializedObject.FindProperty(nameof(PureSDFImage._offset));
            _lerpLayers = serializedObject.FindProperty(nameof(PureSDFImage._lerpLayers));
            _layerB = serializedObject.FindProperty(nameof(PureSDFImage._layerB));
            _layersLerp = serializedObject.FindProperty(nameof(PureSDFImage._layersLerp));

            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("This is experimental component, it might change drastically in future or even get removed", MessageType.Warning);
            serializedObject.Update();

            StackGUI();
            
            BaseImageGUI();
            
            LayersGUI();
            
            MaterialGUI(() => PureSDFImage.DefaultMaterial);
            
            // OtherGUI();
            
            serializedObject.ApplyModifiedProperties();
        }

        private void StackGUI()
        {
            _controlledByRenderingStack = false;
            var anyHasMoreThenOneStack = false;
            foreach (var img in GetTargets())
            {
                var stackCount = img.CountValidRenderingStacks();
                if (stackCount > 0) _controlledByRenderingStack = true;
                if (stackCount > 1) anyHasMoreThenOneStack = true;
                if (_controlledByRenderingStack && anyHasMoreThenOneStack) break;
            }
            
            if (_controlledByRenderingStack)
            {
                EditorGUILayout.HelpBox("Some of the images controlled by Rendering Stack", MessageType.Info);
            }

            if (anyHasMoreThenOneStack)
            {
                EditorGUILayout.HelpBox("Some of the images controlled by more then one Rendering Stack", MessageType.Error);
            }
        }
        
        #region Groups

        private void BaseImageGUI()
        {
            DrawGroup(m_Color, "Image", () =>
            {
                RaycastControlsGUI();
                MaskableControlsGUI();

                // TypeGUI();
            });
        }

       

        private void LayersGUI()
        {
            DrawGroup(_layerA, "Layers", () =>
            {
                EditorGUILayout.PropertyField(m_Color);
                EditorGUILayout.PropertyField(_offset);
                
                StackControlledProperty(_lerpLayers);
                
                using (new EditorGUI.DisabledScope(!_lerpLayers.boolValue))
                using (new EditorGUI.IndentLevelScope())
                {
                    StackControlledProperty(_layersLerp);
                }
                DrawLayer(_layerA);
                
                if (_lerpLayers.boolValue)
                {
                    DrawLayer(_layerB);
                }
            });
        }

        private void StackControlledProperty(SerializedProperty property)
        {
            using (new EditorGUI.DisabledScope(_controlledByRenderingStack))
            {
                EditorGUILayout.PropertyField(property);
            }
            
            if (_controlledByRenderingStack)
            {
                var rect = GUILayoutUtility.GetLastRect();
                var l = 16;
                rect.width = rect.height = l;
                rect.x -= l;
                rect.y += 2;
                GUI.Label(rect, lDefaults.ControlledByStack);
            }
        }
        
        // private void OtherGUI()
        // {
        //     DrawGroup(_fillAmount, "Other", DrawSDFHelperFields);
        // }

        #endregion

        private void DrawLayer(SerializedProperty layer)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            var sprite = layer.FindPropertyRelative(nameof(PureSDFImage.Layer.SpriteReference));
            var color = layer.FindPropertyRelative(nameof(PureSDFImage.Layer.Color));
            var width = layer.FindPropertyRelative(nameof(PureSDFImage.Layer.Width));
            var softness = layer.FindPropertyRelative(nameof(PureSDFImage.Layer.Softness));
            EditorGUILayout.LabelField(EditorGUIUtility.TrTextContent(layer.displayName, layer.tooltip), EditorStyles.centeredGreyMiniLabel);
            
            EditorGUI.BeginChangeCheck();
            StackControlledProperty(sprite);
            if (EditorGUI.EndChangeCheck()) _image.DisableSpriteOptimizations();
            
            EditorGUILayout.PropertyField(color);
            EditorGUILayout.PropertyField(width);
            EditorGUILayout.PropertyField(softness);
            EditorGUILayout.EndVertical();
        }

        private IEnumerable<PureSDFImage> GetTargets()
        {
            return targets.OfType<PureSDFImage>();
        }

        protected override Texture[] GetTargetedTextures()
        {
            return new Texture[] { };
        }
    }
}