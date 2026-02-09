using System;
using System.Collections.Generic;
using UnityEngine;

namespace nickeltin.SDF.Runtime
{
    internal class TiledMeshBuilder : SDFMeshBuilder
    {
        private readonly struct InnerQuad
        {
            public readonly Vector2 posMin;
            public readonly Vector2 posMax;
            
            public InnerQuad(Vector2 posMin, Vector2 posMax)
            {
                this.posMin = posMin;
                this.posMax = posMax;
            }
        }
        
        private readonly struct OuterQuad
        {
            public readonly Vector2 posMin;
            public readonly Vector2 posMax;
            public readonly Vector2 uvMin;
            public readonly Vector2 uvMax;
            public readonly Vector2 uvMinSdf;
            public readonly Vector2 uvMaxSdf;
            public readonly int meshPartIndex;

            public OuterQuad(Vector2 posMin, Vector2 posMax, 
                Vector2 uvMin, Vector2 uvMax, 
                int meshPartIndex,
                Vector2 uvMinSdf, Vector2 uvMaxSdf)
            {
                this.posMin = posMin;
                this.posMax = posMax;
                this.uvMin = uvMin;
                this.uvMax = uvMax;
                this.meshPartIndex = meshPartIndex;
                this.uvMinSdf = uvMinSdf;
                this.uvMaxSdf = uvMaxSdf;
            }
        }
        
        
        public override void Build(BuildContext ctx)
        {
            GenerateTiledSDFMesh(ctx);
        }

        /// <summary>
        /// Generates tiled (edges, corners, tiled center quads) for regular sprite and for sdf sprite.
        /// Might use packed or sliced tex.
        /// Border offset for sprite without border is not supported.
        /// </summary>
        private static void GenerateTiledSDFMesh(BuildContext ctx)
        {
            var activeSprite = ctx.RegularLayer.Sprite;

            var outerUV = ctx.RegularLayer.OuterUV;
            var innerUV = ctx.RegularLayer.InnerUV;
            var border = ctx.RegularLayer.Border;
            var spriteSize = ctx.RegularLayer.Size;
            
            var innerUVSdf = ctx.OutlineLayer.InnerUV;
            var outerUVSdf = ctx.OutlineLayer.OuterUV;
            
            var rect = ctx.PixelAdjustedRect;
            var multipliedPixelsPerUnit = ctx.MultipliedPixelsPerUnit;


            var tileWidth = (spriteSize.x - border.x - border.z) / multipliedPixelsPerUnit;
            var tileHeight = (spriteSize.y - border.y - border.w) / multipliedPixelsPerUnit;

            // border = SDFMath.GetAdjustedBorders(border / multipliedPixelsPerUnit, rect, ctx.RectTransformRect);
            border = ctx.AdjustedBorders;

            var uvMin = new Vector2(innerUV.x, innerUV.y);
            var uvMax = new Vector2(innerUV.z, innerUV.w);

            var uvMinSdf = new Vector2(innerUVSdf.x, innerUVSdf.y);
            var uvMaxSdf = new Vector2(innerUVSdf.z, innerUVSdf.w);

            // Min to max range for tiled region in coordinates relative to lower left corner.
            var xMin = border.x;
            var xMax = rect.width - border.z;
            var yMin = border.y;
            var yMax = rect.height - border.w;


            // if either width is zero we cant tile so just assume it was the full width.
            if (tileWidth <= 0)
                tileWidth = xMax - xMin;

            if (tileHeight <= 0)
                tileHeight = yMax - yMin;

            var borderOffset =
                SDFMath.ScaleBorderOffset(ctx.BorderOffset, Vector2.one / multipliedPixelsPerUnit);

            Vector2 clippedUV;
            Vector2 clippedUVSdf;
            


            if (activeSprite != null && (ctx.HasBorder || ctx.RegularLayer.Sprite.packed))
            {
                // Sprite has border
                // Evaluate how many vertices we will generate. Limit this number to something sane,
                // especially since meshes can not have more than 65000 vertices.

                CalculateTiledData(xMin, xMax, yMin, yMax, 
                    ctx.HasBorder, ctx.FillCenter, 
                    ref tileWidth, ref tileHeight, 
                    out var width, out var height);

                clippedUV = uvMax;
                clippedUVSdf = uvMaxSdf;

                // Center
                if (ctx.FillCenter)
                {
                    TryAddInnerSDFLayer(ctx.ShadowLayer, width, height);
                    
                    TryAddInnerSDFLayer(ctx.OutlineLayer, width, height);
                    
                    if (ctx.RegularLayer.Render)
                    {
                        clippedUV = uvMax;
                        clippedUVSdf = uvMaxSdf;
                        foreach (var it in LoopInner(width, height))
                        {
                            AddQuad(ctx.RegularLayer, it.posMin, it.posMax, 
                                uvMin, clippedUV, 
                                uvMinSdf, clippedUVSdf);
                        }
                    }
                }

                // Borders
                if (ctx.HasBorder)
                {
                    TryAddOuterSDFLayer(ctx.ShadowLayer, width, height);
                    
                    TryAddOuterSDFLayer(ctx.OutlineLayer, width, height);
                    
                    if (ctx.RegularLayer.Render)
                    {
                        foreach (var it in LoopOuter(width, height))
                        {
                            AddQuad(ctx.RegularLayer, 
                                it.posMin, it.posMax, 
                                it.uvMin, it.uvMax, 
                                it.uvMinSdf, it.uvMaxSdf);
                        }
                    }
                }
            }
            // Image don't have border then just tile it
            else
            {
                var width = (int)Math.Ceiling((xMax - xMin) / tileWidth);
                var height = (int)Math.Ceiling((yMax - yMin) / tileHeight);
                
                TryAddInnerSDFLayer(ctx.ShadowLayer, width, height);
                    
                TryAddInnerSDFLayer(ctx.OutlineLayer, width, height);
                    
                if (ctx.RegularLayer.Render)
                {
                    clippedUV = uvMax;
                    clippedUVSdf = uvMaxSdf;
                    foreach (var it in LoopInner(width, height))
                    {
                        AddQuad(ctx.RegularLayer, it.posMin, it.posMax, 
                            uvMin, clippedUV, 
                            uvMinSdf, clippedUVSdf);
                    }
                }
            }

            return;

            Vector2 ClippedUVT()
            {
                return SDFMath.InverseLerp(uvMin, uvMax, clippedUV);
            }

            void TryAddOuterSDFLayer(LayerInfo layer, int width, int height)
            {
                if (!layer.Render) return;
                
                foreach (var iteration in LoopOuter(width, height))
                {
                    var borderMult = SDFMath.GetSlicedBorderMult(iteration.meshPartIndex, border);
                    // Pos
                    var posMinMax = SDFMath.PackMinMax(iteration.posMin, iteration.posMax);
                    posMinMax = SDFMath.AddBorderOffset(posMinMax, borderOffset, borderMult);
                
                    var uv = layer.Sprite.GetUVArea(SDFMath.TransformSlicedMeshPartIndex(iteration.meshPartIndex));
                
                    // UV
                    var uvMinMax = SDFMath.PackMinMax(uv.min, uv.max);
                    var uvMax2 = SDFMath.Lerp(uvMinMax.Min(), uvMinMax.Max(), ClippedUVT());
                
                    AddQuad(layer, posMinMax.Min(), posMinMax.Max(), uvMinMax.Min(), uvMax2);
                }
            }

            IEnumerable<OuterQuad> LoopOuter(int width, int height)
            {
                // Borders
                clippedUV = uvMax;
                clippedUVSdf = uvMaxSdf;
                // Left and right tiled border
                for (var y = 0; y < height; y++)
                {
                    var y1 = yMin + y * tileHeight;
                    var y2 = yMin + (y + 1) * tileHeight;
                    if (y2 > yMax)
                    {
                        clippedUV.y = uvMin.y + (uvMax.y - uvMin.y) * (yMax - y1) / (y2 - y1);
                        clippedUVSdf.y = uvMinSdf.y + (uvMaxSdf.y - uvMinSdf.y) * (yMax - y1) / (y2 - y1);
                        y2 = yMax;
                    }

                    // Left
                    yield return new OuterQuad(new Vector2(0, y1) + rect.position,
                        new Vector2(xMin, y2) + rect.position,
                        new Vector2(outerUV.x, uvMin.y),
                        new Vector2(uvMin.x, clippedUV.y),
                        1,
                        new Vector2(outerUVSdf.x, uvMinSdf.y),
                        new Vector2(uvMinSdf.x, clippedUVSdf.y));
                    
                    // Right
                    yield return new OuterQuad(new Vector2(xMax, y1) + rect.position,
                        new Vector2(rect.width, y2) + rect.position,
                        new Vector2(uvMax.x, uvMin.y),
                        new Vector2(outerUV.z, clippedUV.y),
                        7,
                        new Vector2(uvMaxSdf.x, uvMinSdf.y),
                        new Vector2(outerUVSdf.z, clippedUVSdf.y));
                }


                clippedUV = uvMax;
                clippedUVSdf = uvMaxSdf;

                // Bottom and top tiled border
                for (var x = 0; x < width; x++)
                {
                    var x1 = xMin + x * tileWidth;
                    var x2 = xMin + (x + 1) * tileWidth;
                    if (x2 > xMax)
                    {
                        clippedUV.x = uvMin.x + (uvMax.x - uvMin.x) * (xMax - x1) / (x2 - x1);
                        clippedUVSdf.x = uvMinSdf.x + (uvMaxSdf.x - uvMinSdf.x) * (xMax - x1) / (x2 - x1);
                        x2 = xMax;
                    }


                    // Down
                    yield return new OuterQuad(new Vector2(x1, 0) + rect.position,
                        new Vector2(x2, yMin) + rect.position,
                        new Vector2(uvMin.x, outerUV.y),
                        new Vector2(clippedUV.x, uvMin.y),
                        3,
                        new Vector2(uvMinSdf.x, outerUVSdf.y),
                        new Vector2(clippedUVSdf.x, uvMinSdf.y));

                    // Up
                    yield return new OuterQuad(new Vector2(x1, yMax) + rect.position,
                        new Vector2(x2, rect.height) + rect.position,
                        new Vector2(uvMin.x, uvMax.y),
                        new Vector2(clippedUV.x, outerUV.w),
                        5,
                        new Vector2(uvMinSdf.x, uvMaxSdf.y),
                        new Vector2(clippedUVSdf.x, outerUVSdf.w));
                }


                clippedUV = Vector2.one;
                clippedUVSdf = Vector2.one;
                
                // Left bottom
                yield return new OuterQuad(new Vector2(0, 0) + rect.position,
                    new Vector2(xMin, yMin) + rect.position,
                    new Vector2(outerUV.x, outerUV.y),
                    new Vector2(uvMin.x, uvMin.y),
                    0,
                    new Vector2(outerUVSdf.x, outerUVSdf.y),
                    new Vector2(uvMinSdf.x, uvMinSdf.y));

                // Left upper
                yield return new OuterQuad(new Vector2(0, yMax) + rect.position,
                    new Vector2(xMin, rect.height) + rect.position,
                    new Vector2(outerUV.x, uvMax.y),
                    new Vector2(uvMin.x, outerUV.w),
                    2,
                    new Vector2(outerUVSdf.x, uvMaxSdf.y),
                    new Vector2(uvMinSdf.x, outerUVSdf.w));

                // Right upper
                yield return new OuterQuad(new Vector2(xMax, yMax) + rect.position,
                    new Vector2(rect.width, rect.height) + rect.position,
                    new Vector2(uvMax.x, uvMax.y),
                    new Vector2(outerUV.z, outerUV.w),
                    8,
                    new Vector2(uvMaxSdf.x, uvMaxSdf.y),
                    new Vector2(outerUVSdf.z, outerUVSdf.w));


                // Right bottom
                yield return new OuterQuad(new Vector2(xMax, 0) + rect.position,
                    new Vector2(rect.width, yMin) + rect.position,
                    new Vector2(uvMax.x, outerUV.y),
                    new Vector2(outerUV.z, uvMin.y),
                    6,
                    new Vector2(uvMaxSdf.x, outerUVSdf.y),
                    new Vector2(outerUVSdf.z, uvMinSdf.y));
            }
            
            void TryAddInnerSDFLayer(LayerInfo layer, int width, int height)
            {
                if (!layer.Render) return;
             
                clippedUV = uvMax;
                foreach (var iteration in LoopInner(width, height))
                {
                    var lUvMax = SDFMath.Lerp(uvMinSdf, uvMaxSdf, ClippedUVT());
                    AddQuad(layer, iteration.posMin, iteration.posMax, uvMinSdf, lUvMax);
                }
            }
            
            IEnumerable<InnerQuad> LoopInner(int width, int height)
            {
                for (var y = 0; y < height; y++)
                {
                    var y1 = yMin + y * tileHeight;
                    var y2 = yMin + (y + 1) * tileHeight;
                    if (y2 > yMax)
                    {
                        clippedUV.y = uvMin.y + (uvMax.y - uvMin.y) * (yMax - y1) / (y2 - y1);
                        y2 = yMax;
                    }

                    clippedUV.x = uvMax.x;

                    for (var x = 0; x < width; x++)
                    {
                        var x1 = xMin + x * tileWidth;
                        var x2 = xMin + (x + 1) * tileWidth;
                        if (x2 > xMax)
                        {
                            clippedUV.x = uvMin.x + (uvMax.x - uvMin.x) * (xMax - x1) / (x2 - x1);
                            x2 = xMax;
                        }

                        var posMin = new Vector2(x1, y1) + rect.position;
                        var posMax = new Vector2(x2, y2) + rect.position;
                        yield return new InnerQuad(posMin, posMax);
                    }
                }
            }
        }


        /// <summary>
        /// Extracted from native image code for tiled mesh generation.
        /// </summary>
        private static void CalculateTiledData(float xMin, float xMax, float yMin, float yMax, 
            bool hasBorder, bool fillCenter,
            ref float tileWidth, ref float tileHeight, 
            out int width, out int height)
        {
            width = 0;
            height = 0;
            if (fillCenter)
            {
                width = (int)Math.Ceiling((xMax - xMin) / tileWidth);
                height = (int)Math.Ceiling((yMax - yMin) / tileHeight);

                double nVertices = 0;
                if (hasBorder)
                    nVertices = (width + 2.0) * (height + 2.0) * 4.0; // 4 vertices per tile
                else
                    nVertices = width * height * 4.0; // 4 vertices per tile

                if (nVertices > 65000.0)
                {
                    Debug.LogError(
                        "Too many sprite tiles on Image. The tile size will be increased. To remove the limit on the number of tiles, " +
                        "set the Wrap mode to Repeat in the Image Import Settings");

                    var maxTiles = 65000.0 / 4.0; // Max number of vertices is 65000; 4 vertices per tile.
                    double imageRatio;
                    if (hasBorder)
                        imageRatio = (width + 2.0) / (height + 2.0);
                    else
                        imageRatio = (double)width / height;

                    var targetTilesW = Math.Sqrt(maxTiles / imageRatio);
                    var targetTilesH = targetTilesW * imageRatio;
                    if (hasBorder)
                    {
                        targetTilesW -= 2;
                        targetTilesH -= 2;
                    }

                    width = (int)Math.Floor(targetTilesW);
                    height = (int)Math.Floor(targetTilesH);
                    tileWidth = (xMax - xMin) / width;
                    tileHeight = (yMax - yMin) / height;
                }
            }
            else
            {
                if (hasBorder)
                {
                    // Texture on the border is repeated only in one direction.
                    width = (int)Math.Ceiling((xMax - xMin) / tileWidth);
                    height = (int)Math.Ceiling((yMax - yMin) / tileHeight);
                    var nVertices = (height + width + 2.0 /*corners*/) * 2.0 /*sides*/ * 4.0 /*vertices per tile*/;
                    if (nVertices > 65000.0)
                    {
                        Debug.LogError(
                            "Too many sprite tiles on Image. The tile size will be increased. To remove the limit on the number of tiles, " +
                            "set the Wrap mode to Repeat in the Image Import Settings");

                        var maxTiles = 65000.0 / 4.0; // Max number of vertices is 65000; 4 vertices per tile.
                        var imageRatio = (double)width / height;
                        var targetTilesW = (maxTiles - 4 /*corners*/) / (2 * (1.0 + imageRatio));
                        var targetTilesH = targetTilesW * imageRatio;

                        width = (int)Math.Floor(targetTilesW);
                        height = (int)Math.Floor(targetTilesH);
                        tileWidth = (xMax - xMin) / width;
                        tileHeight = (yMax - yMin) / height;
                    }
                }
                else
                {
                    height = width = 0;
                }
            }
        }
    }
}