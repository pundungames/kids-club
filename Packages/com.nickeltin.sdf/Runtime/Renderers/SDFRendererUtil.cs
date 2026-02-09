using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace nickeltin.SDF.Runtime
{
    internal static class SDFRendererUtil
    {
        public const float MIN_OUTLINE = 0;
        public const float MAX_OUTLINE = 1;
        
        public const float MIN_SHADOW = 0;
        public const float MAX_SHADOW= 1;

        public static bool SetFloat(Graphic graphic, ref float property, float value, float min, float max)
        {
            value = Mathf.Clamp(value, min, max);
            return SetProperty(graphic, ref property, value);
        }
        
        public static bool SetProperty<T>(Graphic graphic, ref T property, T value)
        {
            if (EqualityComparer<T>.Default.Equals(property, value)) return false;

            property = value;
            graphic.SetVerticesDirty();
            return true;
        }
        
        public static bool SetProperty<T>(ref T property, T value)
        {
            if (EqualityComparer<T>.Default.Equals(property, value)) return false;

            property = value;
            return true;
        }
    }
}