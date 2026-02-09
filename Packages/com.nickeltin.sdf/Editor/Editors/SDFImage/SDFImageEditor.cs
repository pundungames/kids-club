using System.Collections.Generic;
using System.Linq;
using nickeltin.InternalBridge.Editor;
using nickeltin.SDF.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace nickeltin.SDF.Editor
{
    [CustomEditor(typeof(SDFImage))]
    [CanEditMultipleObjects]
    internal partial class SDFImageEditor : SDFGraphicsEditor
    {
        private SerializedProperty _sdfRendererSettings;
        private SerializedProperty _sdfSpriteReference;
        private SerializedProperty _metadataProp;
        
        private SerializedProperty _type;
        private SerializedProperty _useSpriteMesh;
        private SerializedProperty _preserveAspect;
        private SerializedProperty _fillMethod;
        private SerializedProperty _fillOrigin;
        private SerializedProperty _fillAmount;
        private SerializedProperty _fillClockwise;
        private SerializedProperty _fillCenter;
        private SerializedProperty _pixelsPerUnitMultiplier;


        private bool _showSlicedOrTiled;
        private bool _showSliced;
        private bool _showTiled;
        private bool _showFilled;
        private bool _showType;
        private bool _isSliderDriven;
        
        private SDFImage _image;
        

        protected override void OnEnable()
        {
            base.OnEnable();
            
            _sdfRendererSettings = serializedObject.FindProperty(nameof(SDFImage._sdfRendererSettings));
            _sdfSpriteReference = serializedObject.FindProperty(nameof(SDFImage._sdfSpriteReference));
            _metadataProp = _sdfSpriteReference.FindPropertyRelative(nameof(SDFSpriteReference._metadataAsset));
            
            _type = serializedObject.FindProperty(nameof(SDFImage._imageType));
            _useSpriteMesh = serializedObject.FindProperty(nameof(SDFImage._useSpriteMesh));
            _preserveAspect = serializedObject.FindProperty(nameof(SDFImage._preserveAspect));
            _fillMethod = serializedObject.FindProperty(nameof(SDFImage._fillMethod));
            _fillOrigin = serializedObject.FindProperty(nameof(SDFImage._fillOrigin));
            _fillAmount = serializedObject.FindProperty(nameof(SDFImage._fillAmount));
            _fillClockwise = serializedObject.FindProperty(nameof(SDFImage._fillClockwise));
            _fillCenter = serializedObject.FindProperty(nameof(SDFImage._fillCenter));
            _pixelsPerUnitMultiplier = serializedObject.FindProperty(nameof(SDFImage._pixelsPerUnitMultiplier));
            
            _image = (SDFImage)target;
            
            SetShowNativeSize(true);
            
            _isSliderDriven = false;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var rect = _image.rectTransform;
            _isSliderDriven = (rect.drivenByObject as Slider)?.fillRect == rect;
            
            BaseImageGUI();
            
            LayersGUI();
            
            MaterialGUI(() => SDFImage.DefaultMaterial);
            
            OtherGUI();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        #region Image Editor methods re-creation

        
        
        private void TypeGUI()
        {
            EditorGUILayout.PropertyField(_type, Defaults.SpriteTypeContent);

            using (new EditorGUI.IndentLevelScope())
            {
                var typeEnum = (Image.Type)_type.enumValueIndex;

                var showSlicedOrTiled = !_type.hasMultipleDifferentValues &&
                                        (typeEnum == Image.Type.Sliced || typeEnum == Image.Type.Tiled);
                if (showSlicedOrTiled && targets.Length > 1)
                    showSlicedOrTiled = targets.OfType<SDFImage>().All(img => img.HasBorder);

                _showSlicedOrTiled = showSlicedOrTiled;
                _showSliced = showSlicedOrTiled && !_type.hasMultipleDifferentValues && typeEnum == Image.Type.Sliced;
                _showTiled = showSlicedOrTiled && !_type.hasMultipleDifferentValues && typeEnum == Image.Type.Tiled;
                _showFilled = !_type.hasMultipleDifferentValues && typeEnum == Image.Type.Filled;

                var image = (SDFImage)target;
                if (_showSlicedOrTiled)
                {
                    if (image.HasBorder)
                        EditorGUILayout.PropertyField(_fillCenter);
                    EditorGUILayout.PropertyField(_pixelsPerUnitMultiplier);
                }

                if (_showSliced)
                {
                    if (image.Sprite != null && !image.HasBorder)
                        EditorGUILayout.HelpBox("This Image doesn't have a border.", MessageType.Warning);
                }

                if (_showTiled)
                {
                    if (image.Sprite != null && !image.HasBorder &&
                        ((image.Sprite.texture != null && image.Sprite.texture.wrapMode != TextureWrapMode.Repeat) ||
                         image.Sprite.packed))
                    {
                        EditorGUILayout.HelpBox(
                            "It looks like you want to tile a sprite with no border. It would be more efficient to modify the Sprite properties, clear the Packing tag and set the Wrap mode to Repeat.",
                            MessageType.Warning);
                    }
                }

                if (_showFilled)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(_fillMethod);
                    if (EditorGUI.EndChangeCheck()) _fillOrigin.intValue = 0;
                    var shapeRect = EditorGUILayout.GetControlRect(true);
                    switch ((Image.FillMethod)_fillMethod.enumValueIndex)
                    {
                        case Image.FillMethod.Horizontal:
                            _EditorGUI.Popup(shapeRect, _fillOrigin, Defaults.OriginHorizontalStyle,
                                Defaults.FillOriginContent);
                            break;
                        case Image.FillMethod.Vertical:
                            _EditorGUI.Popup(shapeRect, _fillOrigin, Defaults.OriginVerticalStyle,
                                Defaults.FillOriginContent);
                            break;
                        case Image.FillMethod.Radial90:
                            _EditorGUI.Popup(shapeRect, _fillOrigin, Defaults.Origin90Style,
                                Defaults.FillOriginContent);
                            break;
                        case Image.FillMethod.Radial180:
                            _EditorGUI.Popup(shapeRect, _fillOrigin, Defaults.Origin180Style,
                                Defaults.FillOriginContent);
                            break;
                        case Image.FillMethod.Radial360:
                            _EditorGUI.Popup(shapeRect, _fillOrigin, Defaults.Origin360Style,
                                Defaults.FillOriginContent);
                            break;
                    }

                    if (_isSliderDriven)
                        EditorGUILayout.HelpBox("The Fill amount property is driven by Slider.", MessageType.None);
                    using (new EditorGUI.DisabledScope(_isSliderDriven))
                    {
                        EditorGUILayout.PropertyField(_fillAmount);
                    }

                    if ((Image.FillMethod)_fillMethod.enumValueIndex > Image.FillMethod.Vertical)
                        EditorGUILayout.PropertyField(_fillClockwise, Defaults.ClockwiseContent);
                    
                    EditorGUILayout.HelpBox("In filled mode only Full Rect sprites works correctly", MessageType.Info);
                }
            }
        }
        
        private new void NativeSizeButtonGUI()
        {
            if (!m_ShowNativeSize.target) return;
            
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(EditorGUIUtility.labelWidth);
                if (GUILayout.Button(Defaults.CorrectButtonContent, EditorStyles.miniButton))
                {
                    foreach (var graphic in targets.Select(obj => obj as Graphic))
                    {
                        Undo.RecordObject(graphic!.rectTransform, "Set Native Size");
                        graphic.SetNativeSize();
                        EditorUtility.SetDirty(graphic);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void SetShowNativeSize(bool instant)
        {
            var type = (Image.Type)_type.enumValueIndex;
            var showNativeSize = (type == Image.Type.Simple || type == Image.Type.Filled) &&
                                 _metadataProp.objectReferenceValue != null;
            base.SetShowNativeSize(showNativeSize, instant);
        }
        
        #endregion

        #region Groups
        
        private void BaseImageGUI()
        {
            DrawGroup(_type, "Image", () =>
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_sdfSpriteReference, Defaults.SpriteContent);
                if (EditorGUI.EndChangeCheck())
                {
                    _image.DisableSpriteOptimizations();
                }
                
                RaycastControlsGUI();
                MaskableControlsGUI();
                
                if (_metadataProp.objectReferenceValue != null) TypeGUI();
                
                SetShowNativeSize(false);

                if (m_ShowNativeSize.target)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        if ((Image.Type)_type.enumValueIndex == Image.Type.Simple)
                            EditorGUILayout.PropertyField(_useSpriteMesh);

                        EditorGUILayout.PropertyField(_preserveAspect);
                    }
                }

                NativeSizeButtonGUI();
            });
        }
        
        private void LayersGUI()
        {
            DrawGroup(_sdfRendererSettings, "Layers", () =>
            {
                SDFRendererSettingsDrawer.DrawLayout(_sdfRendererSettings);
            });
        }

        private void OtherGUI()
        {
            DrawGroup(_fillAmount, "Other", DrawSDFHelperFields);
        }
        
        #endregion
        
        private void DrawSDFHelperFields()
        {
            var anySpriteMesh = false;
            var unsupportedBorderOffset = false;
            var hasDifferentSDFTexs = false;
            var hasDifferentTexs = false;
            foreach (var sdfImage in GetTargets())
            {
                if (sdfImage.ImageType == Image.Type.Simple && sdfImage.UseSpriteMesh) anySpriteMesh = true;

                if (sdfImage.BorderOffset.sqrMagnitude > 0
                    && ((sdfImage.ImageType == Image.Type.Tiled && !sdfImage.HasBorder)
                        || (sdfImage.ImageType == Image.Type.Filled
                            && sdfImage.FillMethod != Image.FillMethod.Radial360)))
                    unsupportedBorderOffset = true;

                if (sdfImage.SDFTexture != _image.SDFTexture)
                {
                    hasDifferentSDFTexs = true;
                }

                if (sdfImage.mainTexture != _image.mainTexture)
                {
                    hasDifferentTexs = true;
                }
                
            }

            if (anySpriteMesh)
                EditorGUILayout.HelpBox("When using sprite mesh, SDF mesh will still be a simple quad",
                    MessageType.Info);

            if (unsupportedBorderOffset)
                EditorGUILayout.HelpBox(
                    $"Tiled images without border and Filled images in Filled90 & Filled180 modes don't support border offset",
                    MessageType.Warning);

            
            DrawUsedTexture(_image.SDFTexture, Defaults.SDFTexContent, hasDifferentSDFTexs);
            DrawUsedTexture(_image.mainTexture, Defaults.TexContent, hasDifferentTexs);

            var multiEditing = serializedObject.isEditingMultipleObjects;
            
            using (new EditorGUI.DisabledScope(true))
            {
                var mixedValues = EditorGUI.showMixedValue;
                if (multiEditing) EditorGUI.showMixedValue = true;
                EditorGUILayout.ObjectField("First Layer Renderer", _image.FLRNoVerify,
                    typeof(SDFFirstLayerRenderer), false);
                EditorGUI.showMixedValue = mixedValues;
            }
            
            if (!multiEditing && _image.FLRNoVerify != null)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    SDFFLRDebugWindow.DrawHideFlagsDropdown(_image.FLRNoVerify, new GUIContent("Hide Flags", "FLR is intentionally hidden, but you can change its hideFlags for debug purposes"));
                }
            }
        }

        private static void DrawUsedTexture(Texture tex, GUIContent content, bool showMixedValues)
        {
            using (new EditorGUILayout.HorizontalScope())
            using (new EditorGUI.DisabledScope(true))
            {
                var rect = EditorGUILayout.GetControlRect(true, 18);
                var mixedValues = EditorGUI.showMixedValue;
                EditorGUI.showMixedValue = showMixedValues;
                EditorGUI.ObjectField(rect, content, tex, typeof(Texture), false);
                EditorGUI.showMixedValue = mixedValues;
            }
        }
        
        private IEnumerable<SDFImage> GetTargets() => targets.OfType<SDFImage>();

        protected override Texture[] GetTargetedTextures()
        {
            return GetTargets().Select(i => i.mainTexture).Where(t => t != null).ToArray();
        }
        
        
        /// <summary>
        /// Draws only properties relative for sdf importer preview window.
        /// </summary>
        public bool OnImporterPreviewGUI()
        {
            EditorGUILayout.PropertyField(m_Color);
            var m_PreserveAspect = serializedObject.FindProperty(nameof(SDFImage._preserveAspect));
            EditorGUILayout.PropertyField(m_PreserveAspect);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("SDF", Defaults.SDFHeader);
            EditorGUILayout.PropertyField(_sdfRendererSettings);
            return serializedObject.ApplyModifiedProperties();
        }
    }
}