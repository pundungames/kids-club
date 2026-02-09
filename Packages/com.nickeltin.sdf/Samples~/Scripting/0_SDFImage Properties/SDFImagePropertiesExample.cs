using nickeltin.SDF.Runtime;
using UnityEngine;
using UImage = UnityEngine.UI.Image;

namespace nickeltin.SDF.Samples.Runtime
{
    [RequireComponent(typeof(SDFImage))]
    internal class SDFImagePropertiesExample : SDFImageModifier
    {
        [Header("Built-in Image properties")] 
        [SerializeField] internal UImage.Type _imageType = UImage.Type.Simple;
        [SerializeField] internal bool _preserveAspect = false;
        [SerializeField] internal bool _useSpriteMesh = false;
        [SerializeField] internal float _pixelsPerUnitMultiplier = 1.0f;
        [SerializeField] internal bool _fillCenter = true;
        
        [Header("Filled")]
        [SerializeField] internal UImage.FillMethod _fillMethod = UImage.FillMethod.Radial360;
        [SerializeField, Range(0, 1)] internal float _fillAmount = 1.0f;
        [SerializeField] internal bool _fillClockwise = true;
        [SerializeField] internal int _fillOrigin = 0;
        
        [Header("Non serialized")]
        [SerializeField] internal float _alphaHitTestMinimumThreshold = 0;
        
        
        [Header("Unique properties")]
        [SerializeField] internal SDFRendererSettings _sdfRendererSettings = SDFRendererSettings.Default;
        [SerializeField] internal SDFSpriteReference _sdfSpriteReference = new SDFSpriteReference();
        
        [Header("2023 only, for regular pipeline")] 
        [SerializeField] internal bool _setDirectSprite2023 = false;
        [SerializeField] internal Sprite _directSprite2023 = null;
        
        
        [SampleButton]
        public void LogReadonlyOnlyProperties()
        {
            Debug.Log($"Image readonly properties\n" +
                      $"=======Built-in=======\n" +
                      $"    HasBorder: {SDFImage.HasBorder}\n" +
                      $"    PixelsPerUnit: {SDFImage.PixelsPerUnit}\n" +
                      $"=======Unique=========\n" +
                      $"    Active Sprite: {SDFImage.ActiveSprite}\n" +
                      $"    SDFTexture: {SDFImage.SDFTexture}\n" +
                      $"    ActiveSDFSprite: {SDFImage.ActiveSDFSprite}\n" +
                      $"    BorderOffset: {SDFImage.BorderOffset}\n");
        }
        
        private void OnValidate()
        {
            // Read/Write properties recreation of built-in Image
            SDFImage.FillCenter = _fillCenter;
            SDFImage.FillMethod = _fillMethod;
            SDFImage.FillAmount = _fillAmount;
            SDFImage.ImageType = _imageType;
            SDFImage.PreserveAspect = _preserveAspect;
            SDFImage.FillClockwise = _fillClockwise;
            SDFImage.FillOrigin = _fillOrigin;
            SDFImage.UseSpriteMesh = _useSpriteMesh;
            SDFImage.PixelsPerUnitMultiplier = _pixelsPerUnitMultiplier;
            
            // Non serialized
            SDFImage.AlphaHitTestMinimumThreshold = _alphaHitTestMinimumThreshold;
            
            // Unique properties
            SDFImage.MainColor = _sdfRendererSettings.MainColor;
            SDFImage.RenderRegular = _sdfRendererSettings.RenderRegular;
            SDFImage.RegularColor = _sdfRendererSettings.RegularColor;
            SDFImage.RenderOutline = _sdfRendererSettings.RenderOutline;
            SDFImage.OutlineColor = _sdfRendererSettings.OutlineColor;
            SDFImage.OutlineWidth = _sdfRendererSettings.OutlineWidth;
            SDFImage.RenderShadow = _sdfRendererSettings.RenderShadow;
            SDFImage.ShadowColor = _sdfRendererSettings.ShadowColor;
            SDFImage.ShadowWidth = _sdfRendererSettings.ShadowWidth;
            SDFImage.ShadowOffset = _sdfRendererSettings.ShadowOffset;
            
            // More efficient way is to change sdf renderer settings as struct
            SDFImage.SDFRendererSettings = _sdfRendererSettings;
            
            // Settings sprite is bit different
            SDFImage.SDFSpriteReference = _sdfSpriteReference;
            
            // For 2023 we can extract metadata asset directly from sprite and create reference from it
            // See more in next sample 1_RuntimeSpriteChange
            if (_setDirectSprite2023)
            {
#if UNITY_2023_1_OR_NEWER
                if (_directSprite2023 != null && _directSprite2023.TryGetSpriteMetadataAsset(out var metadataAsset))
                {
                    SDFImage.SDFSpriteReference = new SDFSpriteReference(metadataAsset);
                }
                else
                {
                    SDFImage.SDFSpriteReference = new SDFSpriteReference();
                }
#else
                Debug.LogError($"Unity before 2023 can't get sdf metadata directly from sprite");
#endif
            }
        }
    }
}
