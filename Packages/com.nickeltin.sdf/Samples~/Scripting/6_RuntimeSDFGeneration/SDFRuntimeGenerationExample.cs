using System;
using System.IO;
using System.Linq;
using nickeltin.SDF.Runtime;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace nickeltin.SDF.Samples.Runtime
{
    [RequireComponent(typeof(SDFImage))]
    public class SDFRuntimeGenerationExample : SDFImageModifier
    {
        public enum BackendType
        {
            Auto,
            GPU,
            CPU
        }

        [SerializeField] private BackendType _backendType = BackendType.Auto;
        [SerializeField] private SDFGenerationSettingsBase _sdfGenerationSettings = new();

        [SampleButton]
        private void GenerateSDF()
        {
            if (_loadedImage == null)
            {
                Debug.LogError("First load the image");
                return;
            }
            
            // Make sure settings values are in safe ranges
            SDFGenerationSettingsBase.Validate(_sdfGenerationSettings);
            
            // Simplest way (Minimal settings)
            // SDFImage.Sprite = _loadedSprite;
            
            // More elaborate way, you can provide generation settings and backend here 
            // SDFImage.SetRawSprite(_loadedSprite, _sdfGenerationSettings, GetBackend());
            
            // Most low-level way, allows generating multiple sprites from a single texture
            var result = SDFGenerationUtil.GenerateSDFSprite(_loadedSprite, _sdfGenerationSettings, GetBackend());
            _generatedSDF = result.MetadataAssets.FirstOrDefault();
            SDFImage.SDFSpriteReference = _generatedSDF;
        }

        /// <summary>
        /// Mimics how in editor settings selects backend
        /// </summary>
        private SDFGenerationBackend GetBackend()
        {
            switch (_backendType)
            {
                case BackendType.Auto:
                    return SDFGenerationUtil.GetCurrentDefaultBackend();
                case BackendType.GPU:
                    return SDFGenerationUtil.DefaultGPUBackend;
                case BackendType.CPU:
                    return SDFGenerationUtil.DefaultCPUBackend;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [SampleReadonly] 
        private Texture2D _loadedImage => _loadedSprite != null ? _loadedSprite.texture : null;

        [SampleReadonly]
        private Sprite _loadedSprite { get; set; }

        [SampleReadonly]
        private SDFSpriteMetadataAsset _generatedSDF { get; set; }
        
#if UNITY_EDITOR
        [SampleButton]
        private void LoadImageAndGenerateSDF()
        {
            EditorPickPng(sprite =>
            {
                _loadedSprite = sprite;
                GenerateSDF();
            });
        }

        private static void EditorPickPng(Action<Sprite> onSpriteLoaded)
        {
            var path = EditorUtility.OpenFilePanel("Pick PNG (with alpha)", "", "png");
            if (string.IsNullOrEmpty(path)) return;

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SDFImage] Failed to read file: {e.Message}");
                return;
            }

            var baseName = Path.GetFileNameWithoutExtension(path);
            var sprite = CreateSpriteFromPngBytes(bytes, baseName);
            if (sprite == null) return;

            onSpriteLoaded?.Invoke(sprite);
        }

        private static Sprite CreateSpriteFromPngBytes(byte[] bytes, string baseName)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = baseName
            };

            if (!tex.LoadImage(bytes, false))
            {
                Debug.LogError("[SDFImage] Failed to decode PNG.");
                Destroy(tex);
                return null;
            }

            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect);

            sprite.name = baseName;
            return sprite;
        }
#endif
    }
}