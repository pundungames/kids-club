using UnityEngine;

namespace nickeltin.SDF.Runtime
{
    internal class SimpleMeshBuilder : SDFMeshBuilder
    {
        public override void Build(BuildContext ctx)
        {
            if (!ctx.UseSpriteMesh)
            {
                GenerateSimpleSDFMesh(ctx);
            }
            else
            {
                GenerateSDFMesh(ctx);
            }
        }
        
        /// <summary>
        /// Generates regular mesh (might be complex) and simple quad sdf mesh
        /// </summary>
        private static void GenerateSDFMesh(BuildContext ctx)
        {
            var activeSprite = ctx.RegularLayer.Sprite;
            var spriteSize = new Vector2(activeSprite.rect.width, activeSprite.rect.height);

            // Covert sprite pivot into normalized space.
            var spritePivot = activeSprite.pivot / spriteSize;
            var rectPivot = ctx.RectTransformPivot;
            var pixelAdjustedRect = ctx.PixelAdjustedRect;

            if (ctx.PreserveAspect & (spriteSize.sqrMagnitude > 0.0f))
            {
                SDFMath.PreserveSpriteAspectRatio(ref pixelAdjustedRect, spriteSize, ctx.RectTransformPivot);
            }

            var drawingSize = new Vector2(pixelAdjustedRect.width, pixelAdjustedRect.height);
            var spriteBoundSize = activeSprite.bounds.size;

            // Calculate the drawing offset based on the difference between the two pivots.
            var drawOffset = (rectPivot - spritePivot) * drawingSize;


            var vertices = activeSprite.vertices;
            var uvs = activeSprite.uv;
            var sdfUvs = ctx.OutlineLayer.Sprite.uv;
            var triangles = activeSprite.triangles;
            
            if (ctx.ShadowLayer.Render)
            {
                // Using simple mesh quad instead of complex mesh, this is just simpler solution.
                AppendSimpleSDFMesh(ctx.ShadowLayer, ctx);
            }

            if (ctx.OutlineLayer.Render)
            {
                // Using simple mesh quad instead of complex mesh, this is just simpler solution.
                AppendSimpleSDFMesh(ctx.OutlineLayer, ctx);
            }

            if (ctx.RendererSettings.RenderRegular)
            {
                var vh = ctx.RegularLayer.VertexHelper;
                var startIndex = vh.currentVertCount;

                for (var i = 0; i < vertices.Length; ++i)
                {
                    var pos = new Vector3(vertices[i].x / spriteBoundSize.x * drawingSize.x - drawOffset.x,
                        vertices[i].y / spriteBoundSize.y * drawingSize.y - drawOffset.y);
                    var uv = new Vector4(uvs[i].x, uvs[i].y, 0, 0);
                    var sdfUV = new Vector4(sdfUvs[i].x, sdfUvs[i].y, 0, 0);
                    var vert = new UIVertex
                    {
                        position = pos,
                        color = ctx.RegularLayer.Color,
                        uv0 = uv,
                        uv1 = sdfUV,
                    };
                    vh.AddVert(vert);
                }

                for (var i = 0; i < triangles.Length; i += 3)
                {
                    vh.AddTriangle(startIndex + triangles[i + 0], startIndex + triangles[i + 1],
                        startIndex + triangles[i + 2]);
                }
            }
        }

        /// <summary>
        /// Simple sdf mesh is two quads, regular image and sdf
        /// </summary>
        public static void GenerateSimpleSDFMesh(BuildContext ctx)
        {
            ctx.DrawingDim.UnpackMinMax(out var minPos, out var maxPos);
            ctx.RegularLayer.OuterUV.UnpackMinMax(out var uvMin, out var uvMax);
            ctx.OutlineLayer.InnerUV.UnpackMinMax(out var sdfUvMin, out var sdfUvMax);

            if (ctx.ShadowLayer.Render)
            {
                AppendSimpleSDFMesh(ctx.ShadowLayer, ctx);
            }

            if (ctx.OutlineLayer.Render)
            {
                AppendSimpleSDFMesh(ctx.OutlineLayer, ctx);
            }

            if (ctx.RendererSettings.RenderRegular)
            {
                AddQuad(ctx.RegularLayer.VertexHelper, 
                    minPos, maxPos, 
                    ctx.RegularLayer.Color, 
                    uvMin, uvMax, 
                    sdfUVMin: sdfUvMin, sdfUVMax: sdfUvMax);
            }
        }

        /// <summary>
        /// Adds sdf quad to mesh with adjusted border offset
        /// </summary>
        public static void AppendSimpleSDFMesh(LayerInfo layer, BuildContext ctx)
        {
            var drawingDim = ctx.FullDrawingDim;
            var drawingRect = drawingDim.ToRect();
            var scale = SDFMath.GetSizeScale(ctx.RegularLayer.Size, drawingRect.size);
            var borderOffset = SDFMath.ScaleBorderOffset(ctx.BorderOffset, scale);
            drawingDim = SDFMath.AddBorderOffset(drawingDim, borderOffset);
            drawingDim.UnpackMinMax(out var minPos, out var maxPos);
            layer.OuterUV.UnpackMinMax(out var minUV, out var maxUV);

            minPos += layer.MeshOffset;
            maxPos += layer.MeshOffset;
            AddQuad(layer.VertexHelper, minPos, maxPos, layer.Color, 
                minUV, maxUV, 0, layer.SDFFlag, layer.SDFWidth);
        }
    }
}