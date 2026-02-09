using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace nickeltin.SDF.Runtime
{
    public partial class SDFImage
    {
        private SDFFirstLayerRenderer _cachedFlr = null;
        private bool _isFlrDirty;
        
        
        /// <summary>
        /// Renderer used to render regular part of the image (regular sprite)
        /// Its needed since for correct batching one renderer can have one texture,
        /// and we're using two (possible, depending on atlas packing, but most likely) textures - sdf texture and source texture.
        /// <remarks>
        ///     This property will try to find previous FLR or will instantiate new upon access if no cached instance is presented. 
        /// </remarks>
        /// </summary>
        internal SDFFirstLayerRenderer FLR
        {
            get
            {
                EnsureFirstLayerRenderer();
                return _cachedFlr;
            }
        }
        
        internal SDFFirstLayerRenderer FLRNoVerify
        {
            get => _cachedFlr;
            private set => _cachedFlr = value;
        }
        
        private void EnsureFirstLayerRenderer()
        {
            if (FLRNoVerify != null)
            {
                return;
            }

            FLRNoVerify = GetAllFirstLayerRenderers().FirstOrDefault();

            if (FLRNoVerify == null)
            {
                FLRNoVerify = SDFFirstLayerRenderer.Create(this);
            }
            else
            {
                FLRNoVerify.Owner = this;
            }
            
            EnsureFirstLayerRendererProperties();
        }

        private void EnsureFirstLayerRendererProperties()
        {
            if (FLRNoVerify == null)
            {
                return;
            }
            
            var rt = FLRNoVerify.rectTransform;
            rt.SetParent(transform);
            _layerTracker.Clear();
            _layerTracker.Add(this, FLRNoVerify.rectTransform, DrivenTransformProperties.All);
            rt.localPosition = Vector3.zero;
            rt.pivot = rectTransform.pivot;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.SetSiblingIndex(0);
        }
        
        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            // For some reason Upon duplicating in editor, 
            _isFlrDirty = true;
        }
        
        private void Update()
        {
            if (_isFlrDirty)
            {
                EnsureFirstLayerRendererProperties();
                _isFlrDirty = false;
            }
        }

        public override void SetVerticesDirty()
        {
            if (!IsActive())
                return;

            _geometryConstructed = false;
            
            m_VertsDirty = true;
            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);

            if (m_OnDirtyVertsCallback != null)
                m_OnDirtyVertsCallback();
            
            
            // Checking for regular field to prevent first layer renderer instantiation at this point.
            if (FLRNoVerify != null)
            {
                FLR.SetVerticesDirty();
            }
        }

        public override void SetMaterialDirty()
        {
            // Checking for regular field to prevent first layer renderer instantiation at this point.
            if (FLRNoVerify != null)
            {
                FLR.SetMaterialDirty();
            }
            
            base.SetMaterialDirty();
        }
        
        /// <summary>
        /// Get first layer renderers, some might remain in editor due to un-consistent lifecycle of prefabs
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<SDFFirstLayerRenderer> GetAllFirstLayerRenderers()
        {
            for (var i = 0; i <  transform.childCount; i++)
            {
                if (transform.GetChild(i).TryGetComponent(out SDFFirstLayerRenderer flr))
                {
                    yield return flr;
                }
            }
        }
        
        internal void DisposeAllFirstLayerRenderers()
        {
            SDFFirstLayerRenderer.Dispose(_cachedFlr);
            _cachedFlr = null;
            
            foreach (var flr in GetAllFirstLayerRenderers())
            {
                SDFFirstLayerRenderer.Dispose(flr);
            }
        }
    }
}