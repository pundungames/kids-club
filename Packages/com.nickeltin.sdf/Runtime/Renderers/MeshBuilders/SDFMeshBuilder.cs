using System;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

namespace nickeltin.SDF.Runtime
{
    /// <summary>
    /// Image might function in different modes, to decouple mesh generation process this class exist.
    /// To generate mesh for SDFImage just call <see cref="BuildMesh"/>,
    /// or call <see cref="Build"/> on instance of builder and pass image state manually.
    /// </summary>
    internal abstract class SDFMeshBuilder
    {
        public enum LayerType
        {
            Regular,
            Outline,
            Shadow
        }
        
        /// <summary>
        /// Represents layer of mesh, holds sprite info and other layer settings.
        /// Most of the fields on sprite used here is internal so we getting them trough <see cref="DataUtility"/>.
        /// </summary>
        public readonly struct LayerInfo
        {
            public readonly Sprite Sprite;
            public readonly Vector4 InnerUV;
            public readonly Vector4 OuterUV;
            public readonly Vector4 Padding;
            public readonly Color Color;
            public readonly Vector4 Border;
            public readonly Vector2 Size;
            public readonly Vector2 MeshOffset;
            public readonly VertexHelper VertexHelper;
            public readonly bool Render;
            /// <summary>
            /// uv.z
            /// </summary>
            public readonly float SDFWidth;
            /// <summary>
            /// uv.w
            /// </summary>
            public readonly float SDFFlag;
            
            public LayerInfo(Sprite sprite, bool render, Color color, float sdfWidth, Vector2 meshOffset, 
                VertexHelper vh, float sdfFlag, Vector4? outerUV = null)
            {
                Render = render && sprite != null;
                VertexHelper = vh;
                MeshOffset = meshOffset;
                Sprite = sprite;
                Color = color;
                SDFWidth = sdfWidth;
                SDFFlag = sdfFlag;
               
                if (Sprite != null)
                {
                    InnerUV = DataUtility.GetInnerUV(sprite);
                    OuterUV = DataUtility.GetOuterUV(sprite);
                    Padding = DataUtility.GetPadding(sprite);
                    Border = sprite.border;
                    Size = sprite.rect.size;
                }
                else
                {
                    InnerUV = default;
                    OuterUV = default;
                    Padding = default;
                    Border = default;
                    Size = Vector2.one * 100;
                }

                if (outerUV != null)
                {
                    OuterUV = outerUV.GetValueOrDefault();
                }
            }

            public LayerInfo WithOuterUV(Vector4 outerUV)
            {
                return new LayerInfo(Sprite, Render, Color, SDFWidth, MeshOffset, VertexHelper, SDFFlag, outerUV);
            }
        }
        
        /// <summary>
        /// Captures current state of image with all necessary data for mesh-gen
        /// </summary>
        public readonly struct BuildContext
        {
            public readonly Image.Type Type;
            
            public readonly SDFRendererSettings RendererSettings;
            
            public readonly Vector4 BorderOffset;
            
            public readonly bool UseSpriteMesh;
            public readonly bool PreserveAspect;
            public readonly bool HasBorder;
            public readonly bool FillCenter;
            public readonly float MultipliedPixelsPerUnit;
            
            public readonly float FillAmount;
            public readonly Image.FillMethod FillMethod;
            public readonly int FillOrigin;
            public readonly bool FillClockwise;
            
            public readonly LayerInfo RegularLayer;
            public readonly LayerInfo OutlineLayer;
            public readonly LayerInfo ShadowLayer;
            
            public readonly Vector4 DrawingDim;
            /// <summary>
            /// Full differs from regular by being the full size of the sprite, without padding.
            /// Padding might occur for tight sprites. 
            /// </summary>
            public readonly Vector4 FullDrawingDim;
            public readonly Vector2 RectTransformPivot;
            public readonly Rect RectTransformRect;
            public readonly Rect PixelAdjustedRect;
            public readonly Vector4 AdjustedBorders;
            
            public BuildContext(SDFImage image, VertexHelper sdfVh, VertexHelper regularVh)
            {
                Type = image.ImageType;
                RendererSettings = image.SDFRendererSettings;
                BorderOffset = image.BorderOffset;

                HasBorder = image.HasBorder;
                UseSpriteMesh = image.UseSpriteMesh;
                
                // Unique case for sliced mesh, if image does not have borders
                // preserveAspect need to be false to calculate correct drawing dim 
                PreserveAspect = image.PreserveAspect && !(image.ImageType == Image.Type.Sliced && !image.HasBorder);

                DrawingDim = image.GetDrawingDimensions(PreserveAspect);
                FullDrawingDim = image.GetFullDrawingDimensions(PreserveAspect);

                
                RegularLayer = new LayerInfo(image.ActiveSprite, 
                    RendererSettings.RenderRegular,
                    image.RegularColor * image.MainColor, 
                    0, 
                    Vector2.zero, 
                    regularVh,
                    0);
                    
                // Converting to range that shader accepts
                var outlineWidth = Mathf.Lerp(0, 1, image.OutlineWidth);
                OutlineLayer = new LayerInfo(image.ActiveSDFSprite, 
                    RendererSettings.RenderOutline, 
                    image.OutlineColor * image.MainColor, 
                    outlineWidth, 
                    Vector2.zero, 
                    sdfVh,
                    1);
                
                // Converting to range that shader accepts
                var shadowWidth = Mathf.Lerp(0, 1, image.ShadowWidth);
                ShadowLayer = new LayerInfo(image.ActiveSDFSprite,
                    RendererSettings.RenderShadow, 
                    image.ShadowColor * image.MainColor,
                    shadowWidth, 
                    image.ShadowOffset, 
                    sdfVh,
                    2);

                
                RectTransformPivot = image.rectTransform.pivot;
                RectTransformRect = image.rectTransform.rect;
                PixelAdjustedRect = image.GetPixelAdjustedRect();
                FillCenter = image.FillCenter;
                FillAmount = image.FillAmount;
                MultipliedPixelsPerUnit = image.GetMultipliedPixelsPerUnit();
                
                AdjustedBorders = SDFMath.GetAdjustedBorders(RegularLayer.Border / MultipliedPixelsPerUnit, 
                    PixelAdjustedRect, RectTransformRect);
                
                // UseSlicedSDFTex = image.UsingSlicedSDFTexture;
                // SlicedPackedUV = image._spriteMetadata.Data.SlicedPackedUV;

                FillMethod = image.FillMethod;
                FillOrigin = image.FillOrigin;
                FillClockwise = image.FillClockwise;
            }

            public LayerInfo GetLayerInfo(LayerType type)
            {
                switch (type)
                {
                    case LayerType.Regular:
                        return RegularLayer;
                    case LayerType.Outline:
                        return OutlineLayer;
                    case LayerType.Shadow:
                        return ShadowLayer;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
            }
        }

        private class NotImplementedMeshBuilder : SDFMeshBuilder
        {
            public override void Build(BuildContext ctx)
            {
                throw new NotImplementedException($"Mesh type {ctx.Type} is not supported");
            }
        }
        
        public static readonly SimpleMeshBuilder Simple = new SimpleMeshBuilder();
        public static readonly SlicedMeshBuilder Sliced = new SlicedMeshBuilder();
        public static readonly TiledMeshBuilder Tiled = new TiledMeshBuilder();
        public static readonly FilledMeshBuilder Filled = new FilledMeshBuilder();
        private static readonly NotImplementedMeshBuilder Null = new NotImplementedMeshBuilder();

        /// <summary>
        /// Implement mesh generation here
        /// </summary>
        /// <param name="ctx">Data used for generation</param>
        public abstract void Build(BuildContext ctx);
        
        private SDFMeshBuilder BuildAndReturnItself(BuildContext ctx)
        {
            Build(ctx);
            return this;
        }
        
        /// <summary>
        /// Builds mesh for image in static context. Captures state of image automatically 
        /// </summary>
        public static SDFMeshBuilder BuildMesh(VertexHelper sdfVh, VertexHelper regularVh, SDFImage image)
        {
            var ctx = new BuildContext(image, sdfVh, regularVh);
            switch (image.ImageType)
            {
                case Image.Type.Simple:
                    return Simple.BuildAndReturnItself(ctx);
                case Image.Type.Sliced:
                    return Sliced.BuildAndReturnItself(ctx);
                case Image.Type.Tiled:
                    return Tiled.BuildAndReturnItself(ctx);
                case Image.Type.Filled:
                    return Filled.BuildAndReturnItself(ctx);
                default:
                    return Null.BuildAndReturnItself(ctx);
            }
        }
        
        
        // ReSharper disable InvalidXmlDocComment
        /// <summary>
        /// Appends quad to mesh
        /// </summary>
        /// <param name="z">Z offset, might be useful for debugging, usually</param>
        /// <param name="sdfFlag">
        ///     Marker for SDF shader that this quad should be rendered from sdf texture.
        ///     If equals 0 than quad will be rendered as regular Image sprite.
        ///     Passed along with UV0 at Z channel.
        ///     1 = Outline layer
        ///     2 = Shadow layer
        /// </param>
        /// <param name="sdfWidth">
        ///     If this is SDF quad this tells shader width of particular sdf effect layer.
        ///     Passed along with UV0 at W channel.
        /// </param>
        /// <seealso cref="Shaders/SDFDisplaySplitMesh.shader"/>
        public static void AddQuad(VertexHelper vh, Vector2 posMin, Vector2 posMax, Color32 color,
            Vector2 uvMin, Vector2 uvMax, float z = 0, float sdfFlag = 0, float sdfWidth = 0, 
            Vector2 sdfUVMin = default, Vector2 sdfUVMax = default)
        {
            var startIndex = vh.currentVertCount;

            var vert1 = new UIVertex()
            {
                position = new Vector3(posMin.x, posMin.y, z),
                color = color,
                uv0 = new Vector4(uvMin.x, uvMin.y, sdfWidth, sdfFlag),
                uv1 = new Vector4(sdfUVMin.x, sdfUVMin.y),
            };
            
            var vert2 = new UIVertex()
            {
                position = new Vector3(posMin.x, posMax.y, z),
                color = color,
                uv0 = new Vector4(uvMin.x, uvMax.y, sdfWidth, sdfFlag),
                uv1 = new Vector4(sdfUVMin.x, sdfUVMax.y),
            };
            
               
            var vert3 = new UIVertex()
            {
                position =  new Vector3(posMax.x, posMax.y, z),
                color = color,
                uv0 = new Vector4(uvMax.x, uvMax.y, sdfWidth, sdfFlag),
                uv1 = new Vector4(sdfUVMax.x, sdfUVMax.y),
            };
            
            var vert4 = new UIVertex()
            {
                position =  new Vector3(posMax.x, posMin.y, z),
                color = color,
                uv0 = new Vector4(uvMax.x, uvMin.y, sdfWidth, sdfFlag),
                uv1 = new Vector4(sdfUVMax.x, sdfUVMin.y),
            };
            
            vh.AddVert(vert1);
            vh.AddVert(vert2);
            vh.AddVert(vert3);
            vh.AddVert(vert4);

            vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
        }
        
        public static void AddQuad(LayerInfo layer, 
            Vector2 posMin, Vector2 posMax, 
            Vector2 uvMin, Vector2 uvMax, 
            Vector2 sdfUVMin = default, Vector2 sdfUVMax = default)
        {
            posMin += layer.MeshOffset;
            posMax += layer.MeshOffset;
            AddQuad(layer.VertexHelper,
                posMin, posMax, 
                layer.Color, 
                uvMin, uvMax, 
                0, layer.SDFFlag, layer.SDFWidth, 
                sdfUVMin, sdfUVMax);
        }
    }
}