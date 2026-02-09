using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace nickeltin.SDF.Runtime
{
    public sealed partial class SDFImage
    {
        #region Private members remake
        
        /// <inheritdoc cref="GetDrawingDimensions"/>
        /// <remarks>
        /// Does not calculate padding, so for tight sprites will return full rect.
        /// TODO: in future check how is that work with atlases
        /// </remarks>
        public Vector4 GetFullDrawingDimensions(bool lPreserveAspect)
        {
            var size = ActiveSprite == null ? Vector2.zero : new Vector2(ActiveSprite.rect.width, ActiveSprite.rect.height);
            var r = GetPixelAdjustedRect();
            if (lPreserveAspect && size.sqrMagnitude > 0.0f)
            {
                SDFMath.PreserveSpriteAspectRatio(ref r, size, rectTransform.pivot);
            }
            return r.ToVec4();
        }
        
        public float GetMultipliedPixelsPerUnit()
        {
            return PixelsPerUnit * PixelsPerUnitMultiplier;
        }
        
        #endregion
        

        protected override void UpdateGeometry()
        {
            VerifyGeometry();
            
            _sdfVh.FillMesh(workerMesh);
            canvasRenderer.SetMesh(workerMesh);
        }

        private bool _geometryConstructed = false;
        
        /// <summary>
        /// Verifies that geometry is generated.
        /// First layer may need geometry before main (sdf) layer gets rebuild,
        /// therefore geometry is cached and then built from cached vertex builders.
        /// </summary>
        internal void VerifyGeometry()
        {
            if (_geometryConstructed)
                return;

            _geometryConstructed = false;
            ConstructGeometry();
        }
        
        private void ConstructGeometry()
        {
            _firstLayerState.Clear();
            _sdfVh.Clear();
            
            if (rectTransform != null && rectTransform.rect.width >= 0 && rectTransform.rect.height >= 0)
            {
                if (ActiveSprite == null)
                {
                    OnPopulateMesh(_firstLayerState);
                }
                else
                {
                    SDFMeshBuilder.BuildMesh(_sdfVh, _firstLayerState, this);
                }
            }

            var modifiers = ListPool<IMeshModifier>.Get();
            GetComponents(modifiers);

            foreach (var modifier in modifiers)
            {
                modifier.ModifyMesh(_firstLayerState);
                if (modifier is ISDFMeshModifier sdfModifier)
                {
                    sdfModifier.ModifySDFMesh(_sdfVh);
                }
            }

            ListPool<IMeshModifier>.Release(modifiers);
            _geometryConstructed = true;
        }
        
        protected override void UpdateMaterial()
        {
            if (!IsActive())
                return;

            canvasRenderer.materialCount = 1;
            canvasRenderer.SetMaterial(materialForRendering, 0);
            canvasRenderer.SetTexture(SDFTexture);

            // check if this sprite has an associated alpha texture (generated when splitting RGBA = RGB + A as two textures without alpha)
            if (ActiveSDFSprite == null)
            {
                canvasRenderer.SetAlphaTexture(null);
                return;
            }

            var alphaTex = ActiveSDFSprite.associatedAlphaSplitTexture;

            if (alphaTex != null)
            {
                canvasRenderer.SetAlphaTexture(alphaTex);
            }
        }
    }
}