using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using static nickeltin.SDF.Runtime.SDFGenerationInternalUtil;

namespace nickeltin.SDF.Runtime
{
    /// <summary>
    /// Exposed methods for runtime sdf generation
    /// </summary>
    public static class SDFGenerationUtil
    {
        private static class ProfilerMarkers
        {
            public static ProfilerMarker GenerateSDFSpriteMarker { get; } = new("GenerateSDFSprite");
            public static ProfilerMarker GenerateSDFTextureMarker { get; } = new("GenerateSDFTexture");
        }
        
        
        public const int DEFAULT_BORDER_OFFSET = 64;
        public const int DEFAULT_MAX_SDF_TEXTURE_SIZE = 2048;


        public const string AUTO_EDITOR_BACKEND_ID = "auto";
        
        
        public static readonly SDFGPUBackend DefaultGPUBackend = new();
        public static readonly SDFCPUBackend DefaultCPUBackend = new();

        /// <summary>
        /// This can't be called from anywhere, for example, in constructors.
        /// Mainly used to match backend in runtime.
        /// Determine is <see cref="SDFGPUBackend"/> is accessible.
        /// It can be unaccessible for build machines, for unity started with -nographics flag, or WebGL builds.
        /// </summary>
        public static bool IsGPUAvailable()
        {
            // Well, it seems that for WebGL GPU operations are very limited, and burst doesn't work...
            // without any of that the only thing left is use cpu backend in even lower performance mode with no burst😢
#if UNITY_WEBGL && !UNITY_EDITOR
            return false;
#else
            return SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
#endif
        }
        
        /// <summary>
        /// There are two backends for GPU and CPU for when the build machine supports GPU or no.
        /// <see cref="SDFGPUBackend"/> is a default for SDF generation and is lightning fast, but requires GPU, and it's not available on all build machines.
        /// <see cref="SDFCPUBackend"/> is a fallback for machines without GPU. It's still pretty fast, due to utilization of Jobs, but not as fast as GPU.   
        /// </summary>
        /// <returns></returns>
        public static SDFGenerationBackend GetCurrentDefaultBackend()
        {
            return IsGPUAvailable() ? DefaultGPUBackend : DefaultCPUBackend;
        }

        public static IEnumerable<SDFGenerationBackend> GetAvailableDefaultBackends()
        {
            yield return DefaultGPUBackend;
            yield return DefaultCPUBackend;
        }
        
        public static Texture2D GenerateSDFTexture(Texture2D texture, SDFGenerationSettingsBase settings, SDFGenerationBackend backend = null)
        {
            ProfilerMarkers.GenerateSDFTextureMarker.Begin();
            backend ??= GetCurrentDefaultBackend();
            var texCopy = backend.CopyTexture(texture);
            var textureScale = 1f;
            textureScale *= GetGradientSizeAdjustment(texture.width, texture.height);
            var borderOffset = GetBorderOffset(textureScale, settings.BorderOffset);
            var backendSettings = backend.GetSettings(settings, borderOffset, texture.width, texture.height, textureScale);
            var output = GenerateSDF(texCopy, backendSettings, backend);
            output.filterMode = texture.filterMode;
            output.wrapMode = texture.wrapMode;
            DisposeTexture(texCopy);
            ProfilerMarkers.GenerateSDFTextureMarker.End();
            return output;
        }

        public static SDFSpritesGenerationResult GenerateSDFSprite(Sprite sprite, SDFGenerationSettingsBase settings,
            SDFGenerationBackend backend = null,
            int maxTextureSize = DEFAULT_MAX_SDF_TEXTURE_SIZE)
        {
            return GenerateSDFSprite(sprite.texture, new [] { sprite }, settings, backend, maxTextureSize);
        }
        
        public static SDFSpritesGenerationResult GenerateSDFSprite(Texture2D texture, 
            Sprite[] sprites, SDFGenerationSettingsBase settings, 
            SDFGenerationBackend backend = null,
            int maxTextureSize = DEFAULT_MAX_SDF_TEXTURE_SIZE)
        {
            ProfilerMarkers.GenerateSDFSpriteMarker.Begin();
            backend ??= GetCurrentDefaultBackend();
            
            var texCopy = backend.CopyTexture(texture);
            
            var sourceSpritesData = sprites
                .Select(sprite => new SourceSpriteData(sprite.rect, sprite.pivot))
                .ToArray();

            const float textureScale = 1f;
            var borderOffset = GetBorderOffset(textureScale, settings.BorderOffset);
            var result = GeneratePackedTextures(texCopy, borderOffset, 
                textureScale, maxTextureSize, sprites, sourceSpritesData, backend, settings);
            DisposeTexture(texCopy);
            
            var metadataAssets = new SDFSpriteMetadataAsset[sprites.Length];

            for (var i = 0; i < sprites.Length; i++)
            {
                var sdfSprite = result.PackedSprites[i];
                var sprite = sprites[i];
                var metadataAsset = CreateSpriteMetadataAsset(sprite, sdfSprite, borderOffset, backend.BaseData.Identifier);
                metadataAssets[i] = metadataAsset;
            }
            ProfilerMarkers.GenerateSDFSpriteMarker.End();
            return new SDFSpritesGenerationResult(result, metadataAssets);
        }
        
        private static SDFSpriteMetadataAsset CreateSpriteMetadataAsset(Sprite sourceSprite, 
            Sprite sdfSprite, Vector4 borderOffset, string generationBackend)
        {
            var asset = ScriptableObject.CreateInstance<SDFSpriteMetadataAsset>();
            asset._metadata = new SDFSpriteMetadata(sourceSprite, sdfSprite, borderOffset);
            asset.name = sourceSprite.name + SDF_SPRITE_META_ASSET_POSTFIX;
            asset._generationBackend = generationBackend;
#if SDF_NEW_SPRITE_METADATA
            sdfSprite.AddScriptableObject(asset);
#endif
            return asset;
        }
    }
}