using UnityEngine;
using UnityEngine.UI;

namespace nickeltin.SDF.Runtime
{
    internal class FilledMeshBuilder : SDFMeshBuilder
    {
        private static readonly Vector3[] _xy = new Vector3[4];
        private static readonly Vector3[] _uv = new Vector3[4];
        

        public override void Build(BuildContext ctx)
        {
            var drawingDim = ctx.DrawingDim;
            
            if (ctx.OutlineLayer.Render || ctx.ShadowLayer.Render)
            {
                // Scaling border offset and adding original amount
                var scale = SDFMath.GetSizeScale(ctx.RegularLayer.Size, drawingDim.ToRect().size);
                var borderOffset = SDFMath.ScaleBorderOffset(ctx.BorderOffset, scale);
                var scaledRect = SDFMath.AddBorderOffset(drawingDim, borderOffset);
            
                // Calculating border offset that needed to maintain aspect ratio
                // And adding diff between thees border offsets but keeping UV
                var aspectedBorderOffset = SDFMath.MaintainBorderAspectRatio(drawingDim.ToRect(), borderOffset);
                var borderOffsetDiff = aspectedBorderOffset - borderOffset;
                
                TryAddSDFLayer(ctx.ShadowLayer);
                TryAddSDFLayer(ctx.OutlineLayer);

                void TryAddSDFLayer(LayerInfo layer)
                {
                    if (!layer.Render) return;
                    var sdfSpriteUV = layer.OuterUV;

                    var lScaledRect = scaledRect;
                    SDFMath.AddBorderOffsetButKeepUV(ref lScaledRect, ref sdfSpriteUV, 
                        borderOffsetDiff, Vector4.one);

                    GenerateFilledSpriteMesh(ctx, lScaledRect, layer.WithOuterUV(sdfSpriteUV));

                }
            }

            if (ctx.RendererSettings.RenderRegular)
            {
                GenerateFilledSpriteMesh(ctx, drawingDim, ctx.RegularLayer);
            }
        }
        
        /// <summary>
        /// Generates one mesh part, use for either original mesh or for sdf.
        /// </summary>
        private static void GenerateFilledSpriteMesh(BuildContext ctx, Vector4 drawingDim, LayerInfo layer)
        {
            if (ctx.FillAmount < 0.001f)
                return;
            
            var fillAmount = ctx.FillAmount;
            var fillOrigin = ctx.FillOrigin;
            var fillMethod = ctx.FillMethod;
            var fillClockwise = ctx.FillClockwise;
            var outer = layer.OuterUV;

            var tx0 = outer.x;
            var ty0 = outer.y;
            var tx1 = outer.z;
            var ty1 = outer.w;

            var inLinearMode = fillMethod == Image.FillMethod.Horizontal || fillMethod == Image.FillMethod.Vertical;
            // Horizontal and vertical filled sprites are simple -- just end the Image prematurely
            if (inLinearMode)
            {
                if (fillMethod == Image.FillMethod.Horizontal)
                {
                    var fill = (tx1 - tx0) * fillAmount;

                    if (fillOrigin == 1)
                    {
                        drawingDim.x = drawingDim.z - (drawingDim.z - drawingDim.x) * fillAmount;
                        tx0 = tx1 - fill;
                    }
                    else
                    {
                        drawingDim.z = drawingDim.x + (drawingDim.z - drawingDim.x) * fillAmount;
                        tx1 = tx0 + fill;
                    }
                }
                else if (fillMethod == Image.FillMethod.Vertical)
                {
                    var fill = (ty1 - ty0) * fillAmount;

                    if (fillOrigin == 1)
                    {
                        drawingDim.y = drawingDim.w - (drawingDim.w - drawingDim.y) * fillAmount;
                        ty0 = ty1 - fill;
                    }
                    else
                    {
                        drawingDim.w = drawingDim.y + (drawingDim.w - drawingDim.y) * fillAmount;
                        ty1 = ty0 + fill;
                    }
                }
            }

            _xy[0] = new Vector2(drawingDim.x, drawingDim.y);
            _xy[1] = new Vector2(drawingDim.x, drawingDim.w);
            _xy[2] = new Vector2(drawingDim.z, drawingDim.w);
            _xy[3] = new Vector2(drawingDim.z, drawingDim.y);

            _uv[0] = new Vector2(tx0, ty0);
            _uv[1] = new Vector2(tx0, ty1);
            _uv[2] = new Vector2(tx1, ty1);
            _uv[3] = new Vector2(tx1, ty0);

            // If has any fill amount and in radial mode
            if (fillAmount < 1f && !inLinearMode)
            {
                if (fillMethod == Image.FillMethod.Radial90)
                {
                    if (SDFMath.RadialCut(_xy, _uv, fillAmount, fillClockwise, fillOrigin))
                    {
                        AddQuad(layer);
                    }
                }
                else if (fillMethod == Image.FillMethod.Radial180)
                {
                    for (var side = 0; side < 2; ++side)
                    {
                        float fx0, fx1, fy0, fy1;
                        var even = fillOrigin > 1 ? 1 : 0;

                        if (fillOrigin == 0 || fillOrigin == 2)
                        {
                            fy0 = 0f;
                            fy1 = 1f;
                            if (side == even)
                            {
                                fx0 = 0f;
                                fx1 = 0.5f;
                            }
                            else
                            {
                                fx0 = 0.5f;
                                fx1 = 1f;
                            }
                        }
                        else
                        {
                            fx0 = 0f;
                            fx1 = 1f;
                            if (side == even)
                            {
                                fy0 = 0.5f;
                                fy1 = 1f;
                            }
                            else
                            {
                                fy0 = 0f;
                                fy1 = 0.5f;
                            }
                        }

                        _xy[0].x = Mathf.Lerp(drawingDim.x, drawingDim.z, fx0);
                        _xy[1].x = _xy[0].x;
                        _xy[2].x = Mathf.Lerp(drawingDim.x, drawingDim.z, fx1);
                        _xy[3].x = _xy[2].x;

                        _xy[0].y = Mathf.Lerp(drawingDim.y, drawingDim.w, fy0);
                        _xy[1].y = Mathf.Lerp(drawingDim.y, drawingDim.w, fy1);
                        _xy[2].y = _xy[1].y;
                        _xy[3].y = _xy[0].y;

                        _uv[0].x = Mathf.Lerp(tx0, tx1, fx0);
                        _uv[1].x = _uv[0].x;
                        _uv[2].x = Mathf.Lerp(tx0, tx1, fx1);
                        _uv[3].x = _uv[2].x;

                        _uv[0].y = Mathf.Lerp(ty0, ty1, fy0);
                        _uv[1].y = Mathf.Lerp(ty0, ty1, fy1);
                        _uv[2].y = _uv[1].y;
                        _uv[3].y = _uv[0].y;

                        var val = fillClockwise ? fillAmount * 2f - side : fillAmount * 2f - (1 - side);

                        if (SDFMath.RadialCut(_xy, _uv, Mathf.Clamp01(val), fillClockwise, (side + fillOrigin + 3) % 4))
                        {
                            AddQuad(layer);
                        }
                    }
                }
                else if (fillMethod == Image.FillMethod.Radial360)
                {
                    for (var corner = 0; corner < 4; ++corner)
                    {
                        float fx0, fx1, fy0, fy1;

                        if (corner < 2)
                        {
                            fx0 = 0f;
                            fx1 = 0.5f;
                        }
                        else
                        {
                            fx0 = 0.5f;
                            fx1 = 1f;
                        }

                        if (corner == 0 || corner == 3)
                        {
                            fy0 = 0f;
                            fy1 = 0.5f;
                        }
                        else
                        {
                            fy0 = 0.5f;
                            fy1 = 1f;
                        }

                        _xy[0].x = Mathf.Lerp(drawingDim.x, drawingDim.z, fx0);
                        _xy[1].x = _xy[0].x;
                        _xy[2].x = Mathf.Lerp(drawingDim.x, drawingDim.z, fx1);
                        _xy[3].x = _xy[2].x;
                        _xy[0].y = Mathf.Lerp(drawingDim.y, drawingDim.w, fy0);
                        _xy[1].y = Mathf.Lerp(drawingDim.y, drawingDim.w, fy1);
                        _xy[2].y = _xy[1].y;
                        _xy[3].y = _xy[0].y;

                        _uv[0].x = Mathf.Lerp(tx0, tx1, fx0);
                        _uv[1].x = _uv[0].x;
                        _uv[2].x = Mathf.Lerp(tx0, tx1, fx1);
                        _uv[3].x = _uv[2].x;
                        _uv[0].y = Mathf.Lerp(ty0, ty1, fy0);
                        _uv[1].y = Mathf.Lerp(ty0, ty1, fy1);
                        _uv[2].y = _uv[1].y;
                        _uv[3].y = _uv[0].y;
                        
                        var val = fillClockwise
                            ? fillAmount * 4f - (corner + fillOrigin) % 4
                            : fillAmount * 4f - (3 - (corner + fillOrigin) % 4);

                        if (SDFMath.RadialCut(_xy, _uv, Mathf.Clamp01(val), fillClockwise, (corner + 2) % 4))
                        {
                            AddQuad(layer);
                        }
                    }
                }
            }
            // Rendering either simple rect or verts modified for Horizontal or Vertical
            else
            {
                AddQuad(layer);
            }
        }


        /// <summary>
        /// Adds quad with current <see cref="_xy"/> and <see cref="_uv"/>
        /// </summary>
        private static void AddQuad(LayerInfo layer)
        {
            AddQuad(layer, _xy,_uv);
        }
        
        private static void AddQuad(LayerInfo layer, Vector3[] quadPositions, Vector3[] quadUVs)
        {
            var vertexHelper = layer.VertexHelper;
            var startIndex = vertexHelper.currentVertCount;

            for (var i = 0; i < 4; ++i)
            {
                var uv = (Vector4)quadUVs[i];
                uv.z = layer.SDFWidth;
                uv.w = layer.SDFFlag;
                Vector2 pos = quadPositions[i];
                pos += layer.MeshOffset;
                vertexHelper.AddVert(pos, layer.Color, uv);
            }

            vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
        }
    }
}