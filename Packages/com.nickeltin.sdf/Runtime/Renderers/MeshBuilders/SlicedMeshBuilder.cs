using System.Collections.Generic;
using UnityEngine;

namespace nickeltin.SDF.Runtime
{
    internal class SlicedMeshBuilder : SDFMeshBuilder
    {
        private readonly struct Quad
        {
            public readonly int x;
            public readonly int y;
            public readonly int x2;
            public readonly int y2;
            public readonly bool isCenter;
            public readonly Vector2 posMin;
            public readonly Vector2 posMax;
            
            public Quad(int x, int y, int x2, int y2, bool isCenter, Vector2 posMin, Vector2 posMax)
            {
                this.x = x;
                this.y = y;
                this.x2 = x2;
                this.y2 = y2;
                this.isCenter = isCenter;
                this.posMin = posMin;
                this.posMax = posMax;
            }
        }
        
        // Used for sliced sprite
        private static readonly Vector2[] _vertScratch = new Vector2[4];
        private static readonly Vector2[] _uvScratch = new Vector2[4];
        private static readonly Vector2[] _packedUvScratch = new Vector2[4];
        
        public override void Build(BuildContext ctx)
        {
            GenerateSlicedSDFMesh(ctx);
        }
        
        /// <summary>
        /// Generates sliced (edges, corners, center quad) for regular sprite and for sdf sprite
        /// </summary>
        private static void GenerateSlicedSDFMesh(BuildContext ctx)
        {
            if (!ctx.HasBorder)
            {
                // SimpleMeshBuilder.GenerateSimpleSDFMesh(vh, false, rendererSettings);
                // TODO: here we need check is aspect ratio value False is used, because its should be
                SimpleMeshBuilder.GenerateSimpleSDFMesh(ctx);
                return;
            }

            //x = left, y = down, z = right, w = up
            var activeSprite = ctx.RegularLayer.Sprite;
            var innerUV = ctx.RegularLayer.InnerUV;
            var outerUV = ctx.RegularLayer.OuterUV;
            var padding = ctx.RegularLayer.Padding;
            var border = activeSprite.border;
            var packedInnerUV = ctx.OutlineLayer.InnerUV;
            var packedOuterUV = ctx.OutlineLayer.OuterUV;
            

            var rect = ctx.PixelAdjustedRect;

            // var adjustedBorders = GetAdjustedBorders(border / ctx.MultipliedPixelsPerUnit, rect);
            var adjustedBorders = ctx.AdjustedBorders;
            padding /= ctx.MultipliedPixelsPerUnit;

            _vertScratch[0] = new Vector2(padding.x, padding.y);
            _vertScratch[3] = new Vector2(rect.width - padding.z, rect.height - padding.w);
            
            _vertScratch[1].x = adjustedBorders.x;
            _vertScratch[1].y = adjustedBorders.y;

            _vertScratch[2].x = rect.width - adjustedBorders.z;
            _vertScratch[2].y = rect.height - adjustedBorders.w;

            for (var i = 0; i < 4; ++i)
            {
                _vertScratch[i].x += rect.x;
                _vertScratch[i].y += rect.y;
            }

            _uvScratch[0] = new Vector2(outerUV.x, outerUV.y);
            _uvScratch[1] = new Vector2(innerUV.x, innerUV.y);
            _uvScratch[2] = new Vector2(innerUV.z, innerUV.w);
            _uvScratch[3] = new Vector2(outerUV.z, outerUV.w);

            _packedUvScratch[0] = new Vector2(packedOuterUV.x, packedOuterUV.y);
            _packedUvScratch[1] = new Vector2(packedInnerUV.x, packedInnerUV.y);
            _packedUvScratch[2] = new Vector2(packedInnerUV.z, packedInnerUV.w);
            _packedUvScratch[3] = new Vector2(packedOuterUV.z, packedOuterUV.w);


            var borderOffset =
                SDFMath.ScaleBorderOffset(ctx.BorderOffset, Vector2.one / ctx.MultipliedPixelsPerUnit);

            TryAddSDFLayer(ctx.ShadowLayer);
            TryAddSDFLayer(ctx.OutlineLayer);

            if (ctx.RegularLayer.Render)
            {
                foreach (var iteration in Loop())
                {
                    AddQuad(ctx.RegularLayer.VertexHelper, iteration.posMin, iteration.posMax,
                        ctx.RegularLayer.Color,
                        new Vector2(_uvScratch[iteration.x].x, _uvScratch[iteration.y].y),
                        new Vector2(_uvScratch[iteration.x2].x, _uvScratch[iteration.y2].y));
                }
            }
            
            return;

            void TryAddSDFLayer(LayerInfo layer)
            {
                if (!layer.Render) return;
                
                foreach (var iteration in Loop())
                {
                    var posMin = iteration.posMin;
                    var posMax = iteration.posMax;
                    var uvMin = new Vector2(_packedUvScratch[iteration.x].x, _packedUvScratch[iteration.y].y);
                    var uvMax = new Vector2(_packedUvScratch[iteration.x2].x, _packedUvScratch[iteration.y2].y);
                    var borderMult = SDFMath.GetSlicedBorderMult(iteration.x, iteration.y, border);
                    SDFMath.AddBorderOffset(ref posMin, ref posMax, borderOffset, borderMult);
                    AddQuad(layer, posMin, posMax, uvMin, uvMax);
                }
            }
            
            
            IEnumerable<Quad> Loop()
            {
                for (var x = 0; x < 3; ++x)
                {
                    var x2 = x + 1;

                    for (var y = 0; y < 3; ++y)
                    {
                        var isCenter = x == 1 && y == 1;
                        if (!ctx.FillCenter && isCenter)
                            continue;
                        
                        var y2 = y + 1;
                        var posMin = new Vector2(_vertScratch[x].x, _vertScratch[y].y);
                        var posMax = new Vector2(_vertScratch[x2].x, _vertScratch[y2].y);
                        yield return new Quad(x, y, x2, y2, isCenter, posMin, posMax);
                    }
                }
            }
        }
    }
}