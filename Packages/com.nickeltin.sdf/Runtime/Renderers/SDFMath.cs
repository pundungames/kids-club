using System;
using UnityEngine;
using UnityEngine.Sprites;

namespace nickeltin.SDF.Runtime
{
    public static class SDFMath
    {
        /// <summary>
        /// Gets UV rect for sprite area with border.
        /// </summary>
        /// <param name="sprite"></param>
        /// <param name="i">[0 - 8] 0 - to left, 8 - bottom right</param>
        public static Rect GetUVArea(this Sprite sprite, int i)
        {
            if (sprite == null)
            {
                return Rect.zero;
            }
            
            var outer = DataUtility.GetOuterUV(sprite);
            var inner = DataUtility.GetInnerUV(sprite);

            switch (i)
            {
                case 0: // top left corner
                    return new Vector4(outer.x, inner.w, inner.x, outer.w).ToRect();
                case 1: // top edge
                    return new Vector4(inner.x, inner.w, inner.z, outer.w).ToRect();
                case 2: // top right corner
                    return new Vector4(inner.z, inner.w, outer.z, outer.w).ToRect();
                
                case 3: // left edge
                    return new Vector4(outer.x, inner.y, inner.x, inner.w).ToRect();
                case 4: // center
                    return inner.ToRect();
                case 5: // right edge
                    return new Vector4(inner.z, inner.y, outer.z, inner.w).ToRect();
                
                case 6: // bottom left corner
                    return new Vector4(outer.x, outer.y, inner.x, inner.y).ToRect();
                case 7: // bottom edge
                    return new Vector4(inner.x, outer.y, inner.z, inner.y).ToRect();
                case 8: // bottom right corner
                    return new Vector4(inner.z, outer.y, outer.z, inner.y).ToRect();
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        #region Utility

        public static float InverseLerp(float a, float b, float t)
        {
            return Mathf.InverseLerp(a, b, t);
        }
        
        public static float InverseLerpUnclamped(float a, float b, float t)
        {
            return Math.Abs((double) a - b) > 0 ? (float) ((t - (double) a) / (b - (double) a)) : 0.0f;
        }

        public static float Lerp(float a, float b, float t)
        {
            return Mathf.Lerp(a, b, t);
        }
        
     
        private static float LerpUnclamped(float a, float b, float t)
        {
            return Mathf.LerpUnclamped(a, b, t);
        }

        public static Vector2 Lerp(Vector2 a, Vector2 b, Vector2 v)
        {
            return new Vector2(Lerp(a.x, b.x, v.x), Lerp(a.y, b.y, v.y));
        }
        
        public static Vector2 LerpUnclamped(Vector2 a, Vector2 b, Vector2 v)
        {
            return new Vector2(LerpUnclamped(a.x, b.x, v.x), LerpUnclamped(a.y, b.y, v.y));
        }

        public static Vector2 InverseLerp(Vector2 a, Vector2 b, Vector2 v)
        {
            return new Vector2(InverseLerp(a.x, b.x, v.x), InverseLerp(a.y, b.y, v.y));
        }
        
        public static Vector2 InverseLerpUnclamped(Vector2 a, Vector2 b, Vector2 v)
        {
            return new Vector2(InverseLerpUnclamped(a.x, b.x, v.x), InverseLerpUnclamped(a.y, b.y, v.y));
        }

       

        public static Vector2 Min(this Vector4 vec)
        {
            return new Vector2(vec.x, vec.y);
        }

        public static Vector2 Max(this Vector4 vec)
        {
            return new Vector2(vec.z, vec.w);
        }

        public static float Width(this Vector4 vec)
        {
            return Mathf.Abs(vec.x - vec.z);
        }

        public static float Height(this Vector4 vec)
        {
            return Mathf.Abs(vec.y - vec.w);
        }

        /// <summary>
        /// Returns two vectors where 'min' is xy, and 'max' is zw.
        /// x = left, y = down, z = right, w = up
        /// </summary>
        /// <param name="vec">Vector of sprite data, padding, border, drawing dimensions, all that property using Vector4</param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public static void UnpackMinMax(this Vector4 vec, out Vector2 min, out Vector2 max)
        {
            min = vec.Min();
            max = vec.Max();
        }

        public static Vector4 PackMinMax(Vector2 min, Vector2 max)
        {
            return new Vector4(min.x, min.y, max.x, max.y);
        }

        public static Rect ToRect(this Vector4 vec)
        {
            return new Rect(vec.x, vec.y, vec.Width(), vec.Height());
        }

        public static Rect MinMaxToRect(Vector2 min, Vector2 max)
        {
            var width = Mathf.Abs(min.x - max.x);
            var height = Mathf.Abs(min.y - max.y);

            return new Rect(min.x, min.y, width, height);
        }

        public static Vector4 ToVec4(this Rect rect)
        {
            return new Vector4(rect.xMin, rect.yMin, rect.xMax, rect.yMax);
        }

        public static RectInt ToRectInt(this Rect rect)
        {
            return new RectInt((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
        }

        #endregion

        public static Vector2 GetSizeScale(Vector2 sourceSize, Vector2 size)
        {
            return new Vector2((float)size.x / sourceSize.x, (float)size.y / sourceSize.y);
        }

        public static Vector4 ScaleBorderOffset(Vector4 borderOffset, Vector2 scale)
        {
            return new Vector4(borderOffset.x * scale.x,
                borderOffset.y * scale.y,
                borderOffset.z * scale.x,
                borderOffset.w * scale.y);
        }

        /// <summary>
        /// Changes the border to maintain aspect ratio of original rect.
        /// Don't decrease border, only increases.
        /// </summary>
        public static Vector4 MaintainBorderAspectRatio(Rect originalRect, Vector4 borderOffset)
        {
            // Calculate the aspect ratio of the original rectangle
            var originalAspectRatio = originalRect.width / originalRect.height;

            // Calculate the desired width and height of the resulting rectangle
            var desiredWidth = originalRect.width + borderOffset.x + borderOffset.z;
            var desiredHeight = originalRect.height + borderOffset.y + borderOffset.w;

            // Calculate the aspect ratio of the resulting rectangle
            var resultingAspectRatio = desiredWidth / desiredHeight;

            // Adjust the border offset to maintain the aspect ratio
            if (resultingAspectRatio > originalAspectRatio)
            {
                // Increase the top and bottom border offsets
                var heightDifference = (desiredWidth / originalAspectRatio) - desiredHeight;
                borderOffset.w += heightDifference / 2;
                borderOffset.y += heightDifference / 2;
            }
            else if (resultingAspectRatio < originalAspectRatio)
            {
                // Increase the left and right border offsets
                var widthDifference = (desiredHeight * originalAspectRatio) - desiredWidth;
                borderOffset.z += widthDifference / 2;
                borderOffset.x += widthDifference / 2;
            }

            return borderOffset;
        }

        public static Vector4 AddBorderOffset(Vector4 drawingDim, Vector4 borderOffset)
        {
            return AddBorderOffset(drawingDim, borderOffset, Vector4.one);
        }

        public static Vector4 AddBorderOffset(Vector4 drawingDim, Vector4 borderOffset, Vector4 borderMultiplier)
        {
            UnpackMinMax(drawingDim, out var min, out var max);
            AddBorderOffset(ref min, ref max, borderOffset, borderMultiplier);
            return PackMinMax(min, max);
        }

        public static void AddBorderOffset(ref Vector2 min, ref Vector2 max, Vector4 borderOffset)
        {
            AddBorderOffset(ref min, ref max, borderOffset, Vector4.one);
        }

        public static void AddBorderOffset(ref Vector2 min, ref Vector2 max, Vector4 borderOffset,
            Vector4 borderMultiplier)
        {
            min = new Vector2(min.x - borderOffset.x * borderMultiplier.x, min.y - borderOffset.y * borderMultiplier.y);
            max = new Vector2(max.x + borderOffset.z * borderMultiplier.z, max.y + borderOffset.w * borderMultiplier.w);
        }

        /// <summary>
        /// Adds border offset but keeps uv - effectively clips them
        /// </summary>
        public static void AddBorderOffset(ref Vector4 drawingDim, Vector4 borderOffset, Vector4 borderMultiplier,
            ref Vector4 uv)
        {
            var fullRect = AddBorderOffset(drawingDim, borderOffset, Vector4.one).ToRect();
            var borderRect = AddBorderOffset(drawingDim, borderOffset, borderMultiplier).ToRect();

            // min-max of partial-bordered vector in normalized space [0 - 1] of full-bordered rect 
            var minT = InverseLerp(fullRect.min, fullRect.max, borderRect.min);
            var maxT = InverseLerp(fullRect.min, fullRect.max, borderRect.max);


            var uvRect = uv.ToRect();
            // clipping uv
            var uvMin = Lerp(uvRect.min, uvRect.max, minT);
            var uvMax = Lerp(uvRect.min, uvRect.max, maxT);

            uv = PackMinMax(uvMin, uvMax);
            drawingDim = borderRect.ToVec4();
        }
        
        /// <summary>
        /// Im not sure whats the difference between this and above one, but they work differently.
        /// TODO: merge methods, check how their use-cases can be adjusted
        /// </summary>
        public static void AddBorderOffsetButKeepUV(ref Vector4 drawingDim, ref Vector4 uv, 
            Vector4 borderOffset, Vector4 borderMultiplier)
        {
            var fullRect = AddBorderOffset(drawingDim, borderOffset, borderMultiplier).ToRect();
            var actualRect = drawingDim.ToRect();

            // min-max of partial-bordered vector in normalized space [0 - 1] of full-bordered rect 
            var minT = InverseLerpUnclamped(actualRect.min, actualRect.max, fullRect.min);
            var maxT = InverseLerpUnclamped(actualRect.min, actualRect.max, fullRect.max);

            var uvRect = uv.ToRect();
            // clipping uv
            var uvMin = LerpUnclamped(uvRect.min, uvRect.max, minT);
            var uvMax = LerpUnclamped(uvRect.min, uvRect.max, maxT);

            uv = PackMinMax(uvMin, uvMax);
            drawingDim = fullRect.ToVec4();
        }
        
        private static readonly Vector4[] _slicedIndexToBorderMultAll =
        {
            new(1, 0, 0, 1), new(0, 0, 0, 1), new(0, 0, 1, 1),
            new(1, 0, 0, 0), new(0, 0, 0, 0), new(0, 0, 1, 0),
            new(1, 1, 0, 0), new(0, 1, 0, 0), new(0, 1, 1, 0)
        };

        public static Vector4 GetSlicedBorderMult(int x, int y, Vector4 spriteBorder)
        {
            var mult = _slicedIndexToBorderMultAll[TransformSlicedMeshPartIndex(x, y)];

            void CheckChannel(int i)
            {
                if (spriteBorder[i] <= 0) mult[i] = 1;
            }

            // need to account to case when border is not defined for for some sides
            // middle row
            if (y == 1)
            {
                CheckChannel(3);
                CheckChannel(1);
            }

            // middle column
            if (x == 1)
            {
                CheckChannel(0);
                CheckChannel(2);
            }

            return mult;
        }

        public static Vector4 GetSlicedBorderMult(int i, Vector4 spriteBorder)
        {
            return GetSlicedBorderMult(i / 3, i % 3, spriteBorder);
        }

        private static readonly int[] _slicedIndexDisplacement = { 6, 3, 0, 7, 4, 1, 8, 5, 2 };

        /// <summary>
        /// Transforms XY from sliced mesh generation loops to...
        /// TODO: not sure what space is it transferring, docs was kinda lost...
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int TransformSlicedMeshPartIndex(int x, int y)
        {
            return _slicedIndexDisplacement[x * 3 + y];
        }
        
        public static int TransformSlicedMeshPartIndex(int i)
        {
            return _slicedIndexDisplacement[i];
        }

        #region Private Image Methods
        
      
        public static void PreserveSpriteAspectRatio(ref Rect rect, Vector2 spriteSize, Vector2 rectTransformPivot)
        {
            var spriteRatio = spriteSize.x / spriteSize.y;
            var rectRatio = rect.width / rect.height;

            if (spriteRatio > rectRatio)
            {
                var oldHeight = rect.height;
                rect.height = rect.width * (1.0f / spriteRatio);
                rect.y += (oldHeight - rect.height) * rectTransformPivot.y;
            }
            else
            {
                var oldWidth = rect.width;
                rect.width = rect.height * spriteRatio;
                rect.x += (oldWidth - rect.width) * rectTransformPivot.x;
            }
        }
        
        public static Vector4 GetAdjustedBorders(Vector4 border, Rect adjustedRect, Rect rectTransformRect)
        {
            Rect originalRect = rectTransformRect;

            for (int axis = 0; axis <= 1; axis++)
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
                float combinedBorders = border[axis] + border[axis + 2];
                if (adjustedRect.size[axis] < combinedBorders && combinedBorders != 0)
                {
                    borderScaleRatio = adjustedRect.size[axis] / combinedBorders;
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }
            }
            return border;
        }
        
        public static bool RadialCut(Vector3[] xy, Vector3[] uv, float fill, bool invert, int corner)
        {
            // Nothing to fill
            if (fill < 0.001f) return false;

            // Even corners invert the fill direction
            if ((corner & 1) == 1) invert = !invert;

            // Nothing to adjust
            if (!invert && fill > 0.999f) return true;

            // Convert 0-1 value into 0 to 90 degrees angle in radians
            float angle = Mathf.Clamp01(fill);
            if (invert) angle = 1f - angle;
            angle *= 90f * Mathf.Deg2Rad;

            // Calculate the effective X and Y factors
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            RadialCut(xy, cos, sin, invert, corner);
            RadialCut(uv, cos, sin, invert, corner);
            return true;
        }

        /// <summary>
        /// Adjust the specified quad, making it be radially filled instead.
        /// </summary>

        public static void RadialCut(Vector3[] xy, float cos, float sin, bool invert, int corner)
        {
            int i0 = corner;
            int i1 = ((corner + 1) % 4);
            int i2 = ((corner + 2) % 4);
            int i3 = ((corner + 3) % 4);

            if ((corner & 1) == 1)
            {
                if (sin > cos)
                {
                    cos /= sin;
                    sin = 1f;

                    if (invert)
                    {
                        xy[i1].x = Mathf.Lerp(xy[i0].x, xy[i2].x, cos);
                        xy[i2].x = xy[i1].x;
                    }
                }
                else if (cos > sin)
                {
                    sin /= cos;
                    cos = 1f;

                    if (!invert)
                    {
                        xy[i2].y = Mathf.Lerp(xy[i0].y, xy[i2].y, sin);
                        xy[i3].y = xy[i2].y;
                    }
                }
                else
                {
                    cos = 1f;
                    sin = 1f;
                }

                if (!invert) xy[i3].x = Mathf.Lerp(xy[i0].x, xy[i2].x, cos);
                else xy[i1].y = Mathf.Lerp(xy[i0].y, xy[i2].y, sin);
            }
            else
            {
                if (cos > sin)
                {
                    sin /= cos;
                    cos = 1f;

                    if (!invert)
                    {
                        xy[i1].y = Mathf.Lerp(xy[i0].y, xy[i2].y, sin);
                        xy[i2].y = xy[i1].y;
                    }
                }
                else if (sin > cos)
                {
                    cos /= sin;
                    sin = 1f;

                    if (invert)
                    {
                        xy[i2].x = Mathf.Lerp(xy[i0].x, xy[i2].x, cos);
                        xy[i3].x = xy[i2].x;
                    }
                }
                else
                {
                    cos = 1f;
                    sin = 1f;
                }

                if (invert) xy[i3].y = Mathf.Lerp(xy[i0].y, xy[i2].y, sin);
                else xy[i1].x = Mathf.Lerp(xy[i0].x, xy[i2].x, cos);
            }
        }
        
        #endregion
    }
}