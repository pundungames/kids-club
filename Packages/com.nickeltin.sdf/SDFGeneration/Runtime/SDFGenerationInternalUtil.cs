using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;

namespace nickeltin.SDF.Runtime
{
    internal static class SDFGenerationInternalUtil
    {
        private static class ProfilerMarkers
        {
            public static ProfilerMarker GeneratePackedTexturesMarker { get; } = new("GeneratePackedTextures");
            public static ProfilerMarker GenerateSDFMarker { get; } = new("GenerateSDF");
            public static ProfilerMarker ConvertToAlpha8Marker { get; } = new("ConvertToAlpha8");
            
            public static ProfilerMarker CopyTextureGPUMarker { get; } = new("CopyTextureGPU");
            public static ProfilerMarker CopyTextureCPUMarker { get; } = new("CopyTextureCPU");
            
            public static ProfilerMarker ResizeTextureGPUMarker { get; } = new("ResizeTextureGPU");
            public static ProfilerMarker ResizeTextureCPUMarker { get; } = new("ResizeTextureCPU");

            public static ProfilerMarker GetOutputTextureGPUMarker { get; } = new("GetOutputTextureGPU");
            
            public static ProfilerMarker CreateWorkingTextureGPUMarker { get; } = new("CreateWorkingTextureGPU");
            public static ProfilerMarker CreateWorkingTextureCPUMarker { get; } = new("CreateWorkingTextureCPU");
        }

        public const int MIN_TEX_SIZE = 4;
        public const int REFERENCE_GRADIENT_IMAGE_SIZE = 512;
        public const string SDF_TEXTURE_POSTFIX = "(SDFTexture)";
        public const string SDF_SPRITE_POSTFIX = "(SDFSprite)";
        public const string SDF_SPRITE_META_ASSET_POSTFIX = "(SDFSpriteMetadataAsset)";

        public readonly struct SourceSpriteData
        {
            public readonly Rect rect;
            public readonly Vector2 pivot;

            public SourceSpriteData(Rect rect, Vector2 pivot)
            {
                this.rect = rect;
                this.pivot = pivot;
            }
        }

        #region Math Helpers

        /// <summary>
        /// To keep sdf effect size on texture consistent between different resolutions
        /// we need to modify <see cref="SDFGenerationSettings.GradientSize"/> using current resolution and reference resolution. 
        /// </summary>
        public static float GetAdjustedGradientSize(float gradientSize, int width, int height, Vector4 borderOffset)
        {
            var x = width + (int)borderOffset.x + (int)borderOffset.z;
            var y = height + (int)borderOffset.y + (int)borderOffset.w;
            var v = Mathf.Max(x, y);
            return gradientSize * ((float)REFERENCE_GRADIENT_IMAGE_SIZE / v);
        }

        public static float GetGradientSizeAdjustment(int width, int height)
        {
            var v = Mathf.Max(width, height);
            return v / (float)REFERENCE_GRADIENT_IMAGE_SIZE;
        }

        public static float GetAdjustedGradientSize(float gradientSize, int width, int height)
        {
            return GetAdjustedGradientSize(gradientSize, width, height, Vector4.zero);
        }

        /// <summary>
        /// Remaps pivot of sprite to remain in same spot, even when border offset is added
        /// </summary>
        /// <param name="pivot">Original pivot</param>
        /// <param name="spriteRect">Sprite rect with applied border offset</param>
        /// <param name="borderOffset">Applied border offset</param>
        private static void RemapPivot(ref Vector2 pivot, Rect spriteRect, Vector4 borderOffset)
        {
            var spriteSizeWithoutBorderOffset =
                new Vector2(spriteRect.width - borderOffset.x - borderOffset.z,
                    spriteRect.height - borderOffset.y - borderOffset.w);

            var positionInSpriteWithoutBorder =
                new Vector2(Mathf.Lerp(0, spriteSizeWithoutBorderOffset.x, pivot.x),
                    Mathf.Lerp(0, spriteSizeWithoutBorderOffset.y, pivot.y));

            var spriteBoundsWithBorderOffsetInPlace = new Rect(-borderOffset.x, -borderOffset.y,
                spriteRect.width, spriteRect.height);

            pivot = SDFMath.InverseLerp(spriteBoundsWithBorderOffsetInPlace.min,
                spriteBoundsWithBorderOffsetInPlace.max, positionInSpriteWithoutBorder);
        }

        public static Rect UVToTextureRect(Texture tex, Rect uvRect)
        {
            var minX = (int)Mathf.Lerp(0, tex.width, uvRect.xMin);
            var minY = (int)Mathf.Lerp(0, tex.height, uvRect.yMin);
            var maxX = (int)Mathf.Lerp(0, tex.width, uvRect.xMax);
            var maxY = (int)Mathf.Lerp(0, tex.height, uvRect.yMax);
            var width = Mathf.Abs(minX - maxX);
            var height = Mathf.Abs(minY - maxY);
            return new Rect(minX, minY, width, height);
        }

        public static Vector4 GetBorderOffset(float textureScale, int borderOffset)
        {
            borderOffset = Mathf.Max(0, borderOffset);
            borderOffset = (int)(borderOffset * textureScale);
            return Vector4.one * borderOffset;
        }

        #endregion

        #region Texture Operations
        
        public static RenderTexture CopyTextureGPU(Texture2D source)
        {
            ProfilerMarkers.CopyTextureGPUMarker.Begin();
            var rtDesc = new RenderTextureDescriptor(source.width, source.height, GraphicsFormat.R8G8B8A8_UNorm, 0, 0);
            var rt = RenderTexture.GetTemporary(rtDesc);
            rt.filterMode = source.filterMode;
            rt.wrapMode = source.wrapMode;

            Graphics.Blit(Texture2D.blackTexture, rt);
            var texCpy = Object.Instantiate(source);
            Graphics.Blit(texCpy, rt);
            Object.DestroyImmediate(texCpy);
            rt.name = source.name;
            ProfilerMarkers.CopyTextureGPUMarker.End();
            return rt;
        }

        /// <summary>
        /// Make sure that texture is readable
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Texture2D CopyTextureCPU(Texture2D source)
        {
            ProfilerMarkers.CopyTextureCPUMarker.Begin();
            var width = source.width;
            var height = source.height;

            var dstTexture = new Texture2D(width, height, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None)
            {
                filterMode = source.filterMode,
                wrapMode = source.wrapMode,
                name = source.name
            };

            if (source.isReadable)
            {
                var pixels = source.GetPixels32();
                dstTexture.SetPixels32(pixels);
                dstTexture.Apply(false, false);
            }
            else
            {
                Debug.LogError($"Texture {source.name} is not readable, can't copy");
            }

            dstTexture.name = source.name;
            ProfilerMarkers.CopyTextureCPUMarker.End();
            return dstTexture;
        }


        /// <summary>
        /// Creates render texture from the whole original texture.
        /// Use this to extract sprite from texture and apply some border offset.
        /// Created texture configured to work with SDF.
        /// </summary>
        /// <param name="source">Whole texture</param>
        /// <param name="area">Which area of texture needs to be copied, in pixels?</param>
        /// <param name="offset">How much offset you want to add to the final texture, in pixels</param>
        /// <returns>Render texture, you need to dispose of it yourself</returns>
        public static RenderTexture CreateWorkingTextureGPU(Texture source, RectInt area, Vector4 offset)
        {
            ProfilerMarkers.CreateWorkingTextureGPUMarker.Begin();
            var width = (int)(area.width + offset.x + offset.z);
            var height = (int)(area.height + offset.y + offset.w);
            var rtDesc = new RenderTextureDescriptor(width, height, GraphicsFormat.R8G8B8A8_UNorm, 0, 0);
            var rt = RenderTexture.GetTemporary(rtDesc);
            // For some reason rt might come dirty so blitting black tex
            Graphics.Blit(Texture2D.blackTexture, rt);
            Graphics.CopyTexture(source, 0, 0, area.x, area.y, area.width,
                area.height, rt, 0, 0, (int)offset.x, (int)offset.y);
            rt.name = source.name;
            ProfilerMarkers.CreateWorkingTextureGPUMarker.End();
            return rt;
        }

        public static RenderTexture CreateWorkingTextureGPU(Texture source)
        {
            return CreateWorkingTextureGPU(source, 
                new RectInt(0, 0, source.width, source.height), 
                Vector4.zero);
        }
        
        [BurstCompile]
        private struct CopyRegionJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Color32> Src;
            public NativeArray<Color32> Dst;

            public int SrcWidth;
            public int SrcHeight;
            public int DstWidth;

            public int SrcStartX;
            public int SrcStartY;
            public int DstOffsetX;
            public int DstOffsetY;

            public void Execute(int index)
            {
                int x = index % DstWidth;
                int y = index / DstWidth;

                int srcX = x - DstOffsetX + SrcStartX;
                int srcY = y - DstOffsetY + SrcStartY;

                if (srcX >= 0 && srcY >= 0 && srcX < SrcWidth && srcY < SrcHeight)
                {
                    int srcIndex = srcX + srcY * SrcWidth;
                    Dst[index] = Src[srcIndex];
                }
                else
                {
                    Dst[index] = new Color32(0, 0, 0, 0); // transparent black
                }
            }
        }
        
        public static Texture2D CreateWorkingTextureCPU(Texture source, RectInt area, Vector4 offset)
        {
            ProfilerMarkers.CreateWorkingTextureCPUMarker.Begin();
            if (source is not Texture2D tex2D)
                throw new System.ArgumentException("Texture must be Texture2D");

            if (!tex2D.isReadable)
                throw new System.InvalidOperationException("Texture must be readable");

            if (tex2D.format != TextureFormat.RGBA32)
                throw new System.NotSupportedException("Only RGBA32 format is supported");

            var offsetLeft = Mathf.RoundToInt(offset.x);
            var offsetBottom = Mathf.RoundToInt(offset.y);
            var offsetRight = Mathf.RoundToInt(offset.z);
            var offsetTop = Mathf.RoundToInt(offset.w);

            var dstWidth = area.width + offsetLeft + offsetRight;
            var dstHeight = area.height + offsetTop + offsetBottom;

            var result = new Texture2D(dstWidth, dstHeight, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None);
            var srcData = tex2D.GetRawTextureData<Color32>();
            var dstData = result.GetRawTextureData<Color32>();

            var job = new CopyRegionJob
            {
                Src = srcData,
                Dst = dstData,
                SrcWidth = tex2D.width,
                SrcHeight = tex2D.height,
                DstWidth = dstWidth,
                SrcStartX = area.x,
                SrcStartY = area.y,
                DstOffsetX = offsetLeft,
                DstOffsetY = offsetBottom
            };

            job.Schedule(dstWidth * dstHeight, 64).Complete();

            result.Apply(false, false);
            result.wrapMode = tex2D.wrapMode;
            result.filterMode = tex2D.filterMode;
            result.name = tex2D.name;

            ProfilerMarkers.CreateWorkingTextureCPUMarker.End();
            return result;
        }

        /// <summary>
        /// Will copy original RT to RT with new resolution, resampling it.
        /// Passed RT will be released by default.
        /// </summary>s
        public static RenderTexture ResizeTextureGPU(RenderTexture source, int width, int height,
            bool releaseRT = true)
        {
            ProfilerMarkers.ResizeTextureGPUMarker.Begin();
            var rtDesc = new RenderTextureDescriptor(width, height, GraphicsFormat.R8G8B8A8_UNorm, 0, 0);
            var scaledRT = RenderTexture.GetTemporary(rtDesc);
            Graphics.Blit(source, scaledRT);
            if (releaseRT) RenderTexture.ReleaseTemporary(source);
            ProfilerMarkers.ResizeTextureGPUMarker.End();
            return scaledRT;
        }

        [BurstCompile]
        private struct ResampleJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Color32> Src;
            public NativeArray<Color32> Dst;

            public int SrcWidth;
            public int SrcHeight;
            public int DstWidth;
            public int DstHeight;

            public void Execute(int index)
            {
                var x = index % DstWidth;
                var y = index / DstWidth;

                var u = (x + 0.5f) * SrcWidth / DstWidth - 0.5f;
                var v = (y + 0.5f) * SrcHeight / DstHeight - 0.5f;

                var x0 = math.clamp((int)math.floor(u), 0, SrcWidth - 1);
                var y0 = math.clamp((int)math.floor(v), 0, SrcHeight - 1);
                var x1 = math.min(x0 + 1, SrcWidth - 1);
                var y1 = math.min(y0 + 1, SrcHeight - 1);

                var tx = u - x0;
                var ty = v - y0;

                var c00 = Src[x0 + y0 * SrcWidth];
                var c10 = Src[x1 + y0 * SrcWidth];
                var c01 = Src[x0 + y1 * SrcWidth];
                var c11 = Src[x1 + y1 * SrcWidth];

                Color32 Lerp(Color32 a, Color32 b, float t)
                {
                    return new Color32(
                        (byte)(a.r + (b.r - a.r) * t),
                        (byte)(a.g + (b.g - a.g) * t),
                        (byte)(a.b + (b.b - a.b) * t),
                        (byte)(a.a + (b.a - a.a) * t)
                    );
                }

                var cx0 = Lerp(c00, c10, tx);
                var cx1 = Lerp(c01, c11, tx);
                var result = Lerp(cx0, cx1, ty);

                Dst[index] = result;
            }
        }

        public static Texture2D ResizeTextureCPU(Texture2D source, int width, int height)
        {
            ProfilerMarkers.ResizeTextureCPUMarker.Begin();

            if (!source.isReadable)
                throw new System.InvalidOperationException("Texture must be readable");

            if (source.format != TextureFormat.RGBA32)
                throw new System.NotSupportedException("Only RGBA32 format is supported for raw access.");

            var srcWidth = source.width;
            var srcHeight = source.height;

            var result = new Texture2D(width, height, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None)
            {
                filterMode = source.filterMode,
                wrapMode = source.wrapMode,
                name = source.name + "_ResampledCPU"
            };

            var srcNative = source.GetRawTextureData<Color32>(); // zero-GC
            var dstNative = new NativeArray<Color32>(width * height, Allocator.TempJob);

            var job = new ResampleJob
            {
                Src = srcNative,
                Dst = dstNative,
                SrcWidth = srcWidth,
                SrcHeight = srcHeight,
                DstWidth = width,
                DstHeight = height
            };

            job.Schedule(dstNative.Length, 64).Complete();

            result.LoadRawTextureData(dstNative);
            result.Apply(false, false);

            dstNative.Dispose();
            ProfilerMarkers.ResizeTextureCPUMarker.End();
            return result;
        }

        /// <summary>
        /// Converts RenderTexture to Texture2D, basically copies pixels from GPU to CPU.
        /// Passed RT will be released by default.
        /// Created texture configured to work with SDF.
        /// </summary>
        public static Texture2D GetOutputTextureGPU(RenderTexture rt, bool releaseRT = true,
            TextureFormat format = TextureFormat.Alpha8)
        {
            ProfilerMarkers.GetOutputTextureGPUMarker.Begin();
            var tex = new Texture2D(rt.width, rt.height, format, false);
            tex.wrapMode = rt.wrapMode;
            tex.filterMode = rt.filterMode;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            if (releaseRT) RenderTexture.ReleaseTemporary(rt);
            ProfilerMarkers.GetOutputTextureGPUMarker.End();
            return tex;
        }


        /// <summary>
        /// The combination of all functions above will generate singular SDF Tex2D from the area in source Tex2D.
        /// All in between steps done with Render Texture minimizing CPU involvement.
        /// </summary>
        /// <param name="source">Source texture, imported</param>
        /// <param name="area">Area on source texture, sprite, for example</param>
        /// <param name="settings">Import settings to use in the backend. Also used in applying 'Border Offset' and 'Resolution Scale'</param>
        /// <param name="backend">Backend used in generation</param>
        /// <returns></returns>
        public static Texture2D GenerateSDF(Texture source, RectInt area,
            SDFGenerationBackend.Settings settings, SDFGenerationBackend backend)
        {
            ProfilerMarkers.GenerateSDFMarker.Begin();
            var backendProfilerMarker = backend.GetProfilerMarker();
            backendProfilerMarker.Begin();
            var workingTexture = backend.CreateWorkingTexture(source, area, settings.BorderOffset);
            backend.Generate(workingTexture, settings);
            var scaledW = Mathf.Clamp((int)(workingTexture.width * settings.ResolutionScale), MIN_TEX_SIZE, int.MaxValue);
            var scaledH = Mathf.Clamp((int)(workingTexture.height * settings.ResolutionScale), MIN_TEX_SIZE, int.MaxValue);
            if (scaledW != workingTexture.width || scaledH != workingTexture.height)
            {
                workingTexture = backend.ResizeTexture(workingTexture, scaledW, scaledH);
            }
            
            var tex = backend.GetOutputTexture(workingTexture);

            backendProfilerMarker.End();
            ProfilerMarkers.GenerateSDFMarker.End();
            return tex;
        }

        /// <inheritdoc cref="GenerateSDF(UnityEngine.Texture,UnityEngine.RectInt,SDFGenerationBackend.Settings,SDFGenerationBackend)"/>
        public static Texture2D GenerateSDF(Texture source,
            SDFGenerationBackend.Settings settings, SDFGenerationBackend backend)
        {
            return GenerateSDF(source,
                new RectInt(0, 0, source.width, source.height),
                settings, backend);
        }


        /// <summary>
        /// Re-creates texture to be <see cref="TextureFormat.Alpha8"/>
        /// </summary>
        public static Texture2D ConvertToAlpha8(Texture2D tex, bool destroyInTex = true)
        {
            if (tex.format == TextureFormat.Alpha8)
                return tex;
            
            ProfilerMarkers.ConvertToAlpha8Marker.Begin();

            var width = tex.width;
            var height = tex.height;
            var pixelCount = width * height;

            var alpha8Texture = new Texture2D(width, height, TextureFormat.Alpha8, false)
            {
                filterMode = tex.filterMode,
                name = tex.name,
                wrapMode = tex.wrapMode
            };

            var sourceData = tex.GetPixelData<Color32>(0);
            // Using bytes since texture is alpha only.
            var destData = alpha8Texture.GetPixelData<byte>(0);

            new CopyAlphaJob
            {
                source = sourceData,
                destination = destData
            }.Schedule(pixelCount, 64).Complete();

            alpha8Texture.Apply();

            if (destroyInTex)
                Object.DestroyImmediate(tex);

            ProfilerMarkers.ConvertToAlpha8Marker.End();
            return alpha8Texture;
        }

        [BurstCompile]
        private struct CopyAlphaJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Color32> source;
            [WriteOnly] public NativeArray<byte> destination;

            public void Execute(int index)
            {
                var s = source[index];
                destination[index] = s.a;
            }
        }

        /// <summary>
        /// Generates sdf for each sprite and packs all sprites from texture to one atlas texture.
        /// </summary>
        public static SDFSpritesGenerationResult GeneratePackedTextures(Texture texture, Vector4 borderOffset,
            float textureScale, int maxTextureSize,
            Sprite[] sprites, SourceSpriteData[] sourceData,
            SDFGenerationBackend backend, SDFGenerationSettingsBase settings)
        {
            ProfilerMarkers.GeneratePackedTexturesMarker.Begin();

            var spriteTextures = new Texture2D[sprites.Length];
            var packedSprites = new Sprite[sprites.Length];
            var spritesScales = new float[sprites.Length];

            for (var i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                var meta = sourceData[i];

                var adjustedTexScale =
                    textureScale * GetGradientSizeAdjustment((int)meta.rect.width, (int)meta.rect.height);

                var backendSettings = backend.GetSettings(settings, borderOffset,
                    (int)sprite.rect.width, (int)sprite.rect.height, adjustedTexScale);

                var sdfTex = GenerateSDF(texture, sprite.rect.ToRectInt(), backendSettings, backend);

                spriteTextures[i] = sdfTex;
            }

            Texture2D CreateTexForPacking()
            {
                return new Texture2D(MIN_TEX_SIZE, MIN_TEX_SIZE, TextureFormat.Alpha8, false)
                {
                    filterMode = texture.filterMode,
                    wrapMode = texture.wrapMode
                };
            }

            var packedSDF = CreateTexForPacking();
            var rects = packedSDF.PackTextures(spriteTextures, 0, maxTextureSize);

            // Packed texture will automatically be RGBA, so if using
            // if (!backend.UseRGBATexture) packedSDF = ConvertToAlpha8(packedSDF);
            packedSDF = ConvertToAlpha8(packedSDF);

            // If min size is to small packing might fail
            if (rects == null) Debug.LogError($"Can't pack to min size of {maxTextureSize}");

            // Create SDF sprites after packing all their texture to atlas
            for (var i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];


                var pixelRect = UVToTextureRect(packedSDF,
                    rects != null ? rects[i] : new Rect(0, 0, packedSDF.width, packedSDF.height));

                // Pixel per unity and borders should be multiplied by resolution scale.
                // Also by its scaling factor from packed texture, it might become smaller
                var spriteScale =
                    pixelRect.width /
                    spriteTextures[i].width; // How much smaller sprite has become after packing to atlas
                var scaledBorderOffset = borderOffset * settings.ResolutionScale * spriteScale;

                var pixelsPerUnit = sprite.pixelsPerUnit * settings.ResolutionScale * spriteScale;
                var border = sprite.border * settings.ResolutionScale * spriteScale + scaledBorderOffset;

                // Sprite might have custom pivot that is not center, then we need to offset its sprite with consideration of border offset
                var pivot = sourceData[i].pivot;
                RemapPivot(ref pivot, pixelRect, scaledBorderOffset);

                var packedSprite = Sprite.Create(packedSDF, pixelRect, pivot,
                    pixelsPerUnit, 0, SpriteMeshType.FullRect,
                    border, false);

                // Previously name of the sprite was Sprite.name + (SDF) but addressable for some reason referencing
                // sub-assets by parent asset GUID and sub-asset name, so unique names is required
                packedSprite.name = sprite.name + SDF_SPRITE_POSTFIX;
                packedSprites[i] = packedSprite;
                spritesScales[i] = spriteScale;
            }

            //Destroy temp textures that is now packed in two atlas textures
            foreach (var tex in spriteTextures) Object.DestroyImmediate(tex);

            // Previously name of the texture was Texture.name + (SDF) but addressable for some reason referencing
            // sub-assets by parent asset GUID and sub-asset name, so unique names is required
            packedSDF.name = texture.name + SDF_TEXTURE_POSTFIX;
            ProfilerMarkers.GeneratePackedTexturesMarker.End();
            return new SDFSpritesGenerationResult(packedSDF, packedSprites, spritesScales, null);
        }

        #endregion

        public static void DisposeTexture(Texture tex)
        {
            if (tex == null) return;
            if (tex is RenderTexture rt) RenderTexture.ReleaseTemporary(rt);
            else Object.DestroyImmediate(tex);
        }
        

        /// <summary>
        /// Divides sprite with a border for enumeration of 9 elements for each sprite region.
        /// </summary>
        /// <param name="sprite"></param>
        /// <returns></returns>
        public static IEnumerable<RectInt> EnumerateSpriteRects(Sprite sprite)
        {
            var r = sprite.rect;
            var b = sprite.border;

            var y1 = (int)(r.max.y - b.w);
            var h1 = (int)b.w;
            var y2 = (int)(r.min.y + b.y);
            var h2 = (int)(r.height - b.w - b.y);
            var y3 = (int)r.min.y;
            var h3 = (int)b.y;

            var x1 = (int)r.min.x;
            var w1 = (int)b.x;
            var x2 = (int)(r.min.x + b.x);
            var w2 = (int)(r.width - b.x - b.z);
            var x3 = (int)(r.max.x - b.z);
            var w3 = (int)b.z;

            yield return new RectInt(x1, y1, w1, h1);
            yield return new RectInt(x2, y1, w2, h1);
            yield return new RectInt(x3, y1, w3, h1);

            yield return new RectInt(x1, y2, w1, h2);
            yield return new RectInt(x2, y2, w2, h2);
            yield return new RectInt(x3, y2, w3, h2);

            yield return new RectInt(x1, y3, w1, h3);
            yield return new RectInt(x2, y3, w2, h3);
            yield return new RectInt(x3, y3, w3, h3);
        }
    }
}