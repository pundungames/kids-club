using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace nickeltin.SDF.Runtime
{
    /// <summary>
    /// SDF image that uses only SDF sprite
    /// Differences in mesh generation
    /// Uses UV0 and UV1
    /// uv0.z = width
    /// uv0.w = softness
    /// uv1.z = layers lerp
    /// </summary>
    [DefaultExecutionOrder(-2000)]
    [RequireComponent(typeof(CanvasRenderer)), AddComponentMenu("UI/Pure SDF Image")]
    public partial class PureSDFImage : SDFGraphic
    {
        [Serializable]
        public struct Layer
        {
            public SDFSpriteReference SpriteReference;
            public Color Color;
            [Range(0,1)] public float Width;
            [Range(0,1)] public float Softness;

            public Texture SDFTexture => SDFSprite?.texture;
            public Sprite SDFSprite => SpriteReference.Metadata.SDFSprite;
            
            public static Layer Default =>
                new()
                {
                    SpriteReference = new SDFSpriteReference(),
                    Color = Color.white,
                    Width = 0.5f,
                };

            internal readonly SDFMeshBuilder.LayerInfo GetInfo(VertexHelper vh)
            {
                return new SDFMeshBuilder.LayerInfo(SpriteReference.Metadata.SDFSprite, true, Color, Width, 
                    Vector2.zero, vh, Softness);
            }

            internal static void SetSpriteReference(ref Layer layer, SDFSpriteReference value)
            {
                layer.SpriteReference = value;
            }
        }
        
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (canvas.additionalShaderChannels != AdditionalCanvasShaderChannels.TexCoord1)
                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
                
            vh.Clear();
            AppendSimpleSDFMesh(vh);
        }

        internal void AppendSimpleSDFMesh(VertexHelper vh)
        {
            var drawingRect = GetPixelAdjustedRect();
            var minPos = drawingRect.min + _offset;
            var maxPos = drawingRect.max + _offset;

            var layerAInfo = _layerA.GetInfo(vh);
            layerAInfo.OuterUV.UnpackMinMax(out var uvMinA, out var uvMaxA);
            var width = layerAInfo.SDFWidth; //uv0.z
            var softness = layerAInfo.SDFFlag; //uv0.w
            var lColor = layerAInfo.Color;
            var layersLerp = _lerpLayers ? _layersLerp : 0;
            var uvMinB = Vector2.zero;
            var uvMaxB = Vector2.zero;
            
            if (_lerpLayers)
            {
                var layerBInfo = _layerB.GetInfo(vh);
                layerBInfo.OuterUV.UnpackMinMax(out uvMinB, out uvMaxB);
                lColor = Color.LerpUnclamped(lColor, layerBInfo.Color, layersLerp);
                width = Mathf.LerpUnclamped(width, layerBInfo.SDFWidth, layersLerp);
                softness = Mathf.LerpUnclamped(softness, layerBInfo.SDFFlag, layersLerp);
            }

            lColor *= color;
            
            AddQuad(vh, minPos, maxPos, 
                lColor,
                uvMinA, uvMaxA, 
                uvMinB, uvMaxB, 
                0,
                width, softness, 
                layersLerp, 0);
        }
        
        /// <summary>
        /// Adds quad with vertex data required for pure sdf ui shader.
        /// </summary>
        /// <param name="vh">Vertex stream</param>
        /// <param name="posMin">Left lower point of rect</param>
        /// <param name="posMax">Right top point of rect</param>
        /// <param name="color">Color of rect</param>
        /// <param name="uvMinA">First layer left lower point of UV</param>
        /// <param name="uvMaxA">First layer right top point of UV</param>
        /// <param name="uvMinB">Second layer left lower point of UV</param>
        /// <param name="uvMaxB">Second layer right top point of UV</param>
        /// <param name="z">Offset of vertex Z position (for debugging)</param>
        /// <param name="width">SDF width coded into uv0.z</param>
        /// <param name="softness">SDF softness coded into uv0.w</param>
        /// <param name="layersLerp">SDF layers lerp coded into uv1.z</param>
        /// <param name="uv1W">Currently unused uv1.w variable</param>
        public static void AddQuad(VertexHelper vh, 
            Vector2 posMin, Vector2 posMax, 
            Color32 color,
            Vector2 uvMinA, Vector2 uvMaxA, 
            Vector2 uvMinB, Vector2 uvMaxB, 
            float z = 0, 
            float width = 0.5f, float softness = 0,
            float layersLerp = 0, float uv1W = 0)
        {
            var startIndex = vh.currentVertCount;
            
            
            vh.AddVert(new Vector3(posMin.x, posMin.y, z), color,
                new Vector4(uvMinA.x, uvMinA.y, width, softness),
                new Vector4(uvMinB.x, uvMinB.y, layersLerp, uv1W), 
                Vector3.zero, Vector4.zero);
            
            vh.AddVert(new Vector3(posMin.x, posMax.y, z), color,
                new Vector4(uvMinA.x, uvMaxA.y, width, softness),
                new Vector4(uvMinB.x, uvMaxB.y, layersLerp, uv1W),
                Vector3.zero, Vector4.zero);
            
            vh.AddVert(new Vector3(posMax.x, posMax.y, z), color,
                new Vector4(uvMaxA.x, uvMaxA.y, width, softness),
                new Vector4(uvMaxB.x, uvMaxB.y, layersLerp, uv1W),
                Vector3.zero, Vector4.zero);
            
            vh.AddVert(new Vector3(posMax.x, posMin.y, z), color,
                new Vector4(uvMaxA.x, uvMinA.y, width, softness),
                new Vector4(uvMaxB.x, uvMinB.y, layersLerp, uv1W),
                Vector3.zero, Vector4.zero);

            vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
        }
        
#if UNITY_EDITOR
        internal override IEnumerable<string> GetUsedSpritesPropertyPaths()
        {
            yield return CreatePropertyPath(nameof(_layerA), nameof(Layer.SpriteReference));
            yield return CreatePropertyPath(nameof(_layerB), nameof(Layer.SpriteReference));
        }
#endif
    }
}