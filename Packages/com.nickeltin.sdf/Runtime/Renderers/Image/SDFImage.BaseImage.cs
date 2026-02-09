using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.U2D;
using UnityEngine.UI;


namespace nickeltin.SDF.Runtime
{
    public partial class SDFImage
    {
        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (_fillOrigin < 0)
                _fillOrigin = 0;
            else if (_fillMethod == Image.FillMethod.Horizontal && _fillOrigin > 1)
                _fillOrigin = 0;
            else if (_fillMethod == Image.FillMethod.Vertical && _fillOrigin > 1)
                _fillOrigin = 0;
            else if (_fillOrigin > 3)
                _fillOrigin = 0;

            _fillAmount = Mathf.Clamp(_fillAmount, 0f, 1f);
        }
        
        private void PreserveSpriteAspectRatio(ref Rect rect, Vector2 spriteSize)
        {
            var spriteRatio = spriteSize.x / spriteSize.y;
            var rectRatio = rect.width / rect.height;

            if (spriteRatio > rectRatio)
            {
                var oldHeight = rect.height;
                rect.height = rect.width * (1.0f / spriteRatio);
                rect.y += (oldHeight - rect.height) * rectTransform.pivot.y;
            }
            else
            {
                var oldWidth = rect.width;
                rect.width = rect.height * spriteRatio;
                rect.x += (oldWidth - rect.width) * rectTransform.pivot.x;
            }
        }

        internal Vector4 GetDrawingDimensions(bool shouldPreserveAspect)
        {
            var padding = ActiveSprite == null ? Vector4.zero : DataUtility.GetPadding(ActiveSprite);
            var size = ActiveSprite == null
                ? Vector2.zero
                : new Vector2(ActiveSprite.rect.width, ActiveSprite.rect.height);

            var r = GetPixelAdjustedRect();
            // Debug.Log(string.Format("r:{2}, size:{0}, padding:{1}", size, padding, r));

            var spriteW = Mathf.RoundToInt(size.x);
            var spriteH = Mathf.RoundToInt(size.y);

            var v = new Vector4(
                padding.x / spriteW,
                padding.y / spriteH,
                (spriteW - padding.z) / spriteW,
                (spriteH - padding.w) / spriteH);

            if (shouldPreserveAspect && size.sqrMagnitude > 0.0f) PreserveSpriteAspectRatio(ref r, size);

            v = new Vector4(
                r.x + r.width * v.x,
                r.y + r.height * v.y,
                r.x + r.width * v.z,
                r.y + r.height * v.w
            );

            return v;
        }

        public override void SetNativeSize()
        {
            if (ActiveSprite != null)
            {
                var w = ActiveSprite.rect.width / PixelsPerUnit;
                var h = ActiveSprite.rect.height / PixelsPerUnit;
                rectTransform.anchorMax = rectTransform.anchorMin;
                rectTransform.sizeDelta = new Vector2(w, h);
                SetAllDirty();
            }
        }
        
        
        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            if (canvas == null)
            {
                _cachedReferencePixelsPerUnit = 100;
            }
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            else if (canvas.referencePixelsPerUnit != _cachedReferencePixelsPerUnit)
            {
                _cachedReferencePixelsPerUnit = canvas.referencePixelsPerUnit;
                if (ImageType == Image.Type.Sliced || ImageType == Image.Type.Tiled)
                {
                    SetVerticesDirty();
                    SetLayoutDirty();
                }
            }
        }
        
        /// <summary>
        /// Calculate if the ray location for this image is a valid hit location. Takes into account a Alpha test threshold.
        /// </summary>
        /// <param name="screenPoint">The screen point to check against</param>
        /// <param name="eventCamera">The camera in which to use to calculate the coordinating position</param>
        /// <returns>If the location is a valid hit or not.</returns>
        /// <seealso cref="ICanvasRaycastFilter"/>
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (AlphaHitTestMinimumThreshold <= 0)
                return true;

            if (AlphaHitTestMinimumThreshold > 1)
                return false;

            if (ActiveSprite == null)
                return true;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera,
                    out var local))
                return false;

            var rect = GetPixelAdjustedRect();

            // Convert to have lower left corner as reference point.
            local.x += rectTransform.pivot.x * rect.width;
            local.y += rectTransform.pivot.y * rect.height;

            local = MapCoordinate(local, rect);

            // Convert local coordinates to texture space.
            var spriteRect = ActiveSprite.textureRect;
            var x = (spriteRect.x + local.x) / ActiveSprite.texture.width;
            var y = (spriteRect.y + local.y) / ActiveSprite.texture.height;

            try
            {
                return ActiveSprite.texture.GetPixelBilinear(x, y).a >= AlphaHitTestMinimumThreshold;
            }
            catch (UnityException e)
            {
                Debug.LogError(
                    "Using alphaHitTestMinimumThreshold greater than 0 on Image whose sprite texture cannot be read. " +
                    e.Message + " Also make sure to disable sprite packing for this sprite.", this);
                return true;
            }
        }

        private Vector2 MapCoordinate(Vector2 local, Rect rect)
        {
            var spriteRect = ActiveSprite.rect;
            if (ImageType == Image.Type.Simple || ImageType == Image.Type.Filled)
                return new Vector2(local.x * spriteRect.width / rect.width, local.y * spriteRect.height / rect.height);

            var border = ActiveSprite.border;
            var adjustedBorder = GetAdjustedBorders(border / PixelsPerUnit, rect);

            for (var i = 0; i < 2; i++)
            {
                if (local[i] <= adjustedBorder[i])
                    continue;

                if (rect.size[i] - local[i] <= adjustedBorder[i + 2])
                {
                    local[i] -= rect.size[i] - spriteRect.size[i];
                    continue;
                }

                if (ImageType == Image.Type.Sliced)
                {
                    var lerp = Mathf.InverseLerp(adjustedBorder[i], rect.size[i] - adjustedBorder[i + 2], local[i]);
                    local[i] = Mathf.Lerp(border[i], spriteRect.size[i] - border[i + 2], lerp);
                }
                else
                {
                    local[i] -= adjustedBorder[i];
                    local[i] = Mathf.Repeat(local[i], spriteRect.size[i] - border[i] - border[i + 2]);
                    local[i] += border[i];
                }
            }

            return local;
        }

        private Vector4 GetAdjustedBorders(Vector4 border, Rect adjustedRect)
        {
            var originalRect = rectTransform.rect;

            for (var axis = 0; axis <= 1; axis++)
            {
                float borderScaleRatio;

                // The adjusted rect (adjusted for pixel correctness)
                // may be slightly larger than the original rect.
                // Adjust the border to match the adjustedRect to avoid
                // small gaps between borders (case 833201).
                if (originalRect.size[axis] != 0)
                {
                    borderScaleRatio = adjustedRect.size[axis] / originalRect.size[axis];
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }

                // If the rect is smaller than the combined borders, then there's not room for the borders at their normal size.
                // In order to avoid artefacts with overlapping borders, we scale the borders down to fit.
                var combinedBorders = border[axis] + border[axis + 2];
                if (adjustedRect.size[axis] < combinedBorders && combinedBorders != 0)
                {
                    borderScaleRatio = adjustedRect.size[axis] / combinedBorders;
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }
            }

            return border;
        }
        
        protected override void OnDidApplyAnimationProperties()
        {
            SetMaterialDirty();
            SetVerticesDirty();
            SetRaycastDirty_Reflected();
        }
        
        #region Atlas Tracking

        // To track textureless images, which will be rebuild if sprite atlas manager registered a Sprite Atlas that will give this image new texture
        private static readonly List<SDFImage> m_TrackedTexturelessImages = new();
        private static bool s_Initialized;

        private void TrackSprite()
        {
            if (ActiveSprite != null && ActiveSprite.texture == null)
            {
                TrackImage(this);
                _tracked = true;
            }
        }
        
        private static void RebuildImage(SpriteAtlas spriteAtlas)
        {
            for (var i = m_TrackedTexturelessImages.Count - 1; i >= 0; i--)
            {
                var g = m_TrackedTexturelessImages[i];
                if (null != g.ActiveSprite && spriteAtlas.CanBindTo(g.ActiveSprite))
                {
                    g.SetAllDirty();
                    m_TrackedTexturelessImages.RemoveAt(i);
                }
            }
        }

        private static void TrackImage(SDFImage g)
        {
            if (!s_Initialized)
            {
                SpriteAtlasManager.atlasRegistered += RebuildImage;
                s_Initialized = true;
            }

            m_TrackedTexturelessImages.Add(g);
        }

        private static void UnTrackImage(SDFImage g)
        {
            m_TrackedTexturelessImages.Remove(g);
        }

        #endregion

        #region Layout
        public void CalculateLayoutInputHorizontal() { }
        
        public void CalculateLayoutInputVertical() { }
        
        public float minWidth => 0;

        /// <summary>
        /// If there is a sprite being rendered returns the size of that sprite.
        /// In the case of a slided or tiled sprite will return the calculated minimum size possible
        /// </summary>
        public float preferredWidth
        {
            get
            {
                if (ActiveSprite == null)
                    return 0;
                if (ImageType == Image.Type.Sliced || ImageType == Image.Type.Tiled)
                    return DataUtility.GetMinSize(ActiveSprite).x / PixelsPerUnit;
                return ActiveSprite.rect.size.x / PixelsPerUnit;
            }
        }
        
        public float flexibleWidth => -1;
        
        public float minHeight => 0;

        /// <summary>
        /// If there is a sprite being rendered returns the size of that sprite.
        /// In the case of a slided or tiled sprite will return the calculated minimum size possible
        /// </summary>
        public float preferredHeight
        {
            get
            {
                if (ActiveSprite == null)
                    return 0;
                if (ImageType == Image.Type.Sliced || ImageType == Image.Type.Tiled)
                    return DataUtility.GetMinSize(ActiveSprite).y / PixelsPerUnit;
                return ActiveSprite.rect.size.y / PixelsPerUnit;
            }
        }

        public float flexibleHeight => -1;
        
        public int layoutPriority => 0;

        #endregion
        
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            _pixelsPerUnitMultiplier = Mathf.Max(0.01f, _pixelsPerUnitMultiplier);
        }
#endif
    }
}