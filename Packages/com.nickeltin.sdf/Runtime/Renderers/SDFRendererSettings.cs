using System;
using UnityEngine;

namespace nickeltin.SDF.Runtime
{
    [Serializable]
    public struct SDFRendererSettings
    {
        /// <summary>
        /// Color multiplied for each layer with their colors.
        /// </summary>
        public Color MainColor;
        
        
        /// <summary>
        /// Is regular sprite is rendered
        /// </summary>
        public bool RenderRegular;
        public Color RegularColor; 
        
        
        /// <summary>
        /// If enabled mesh for outline sdf part will be generated
        /// </summary>
        public bool RenderOutline;
        public Color OutlineColor;
        /// <summary>
        /// Width specific to outline layer, changes width of outline.
        /// </summary>
        [Range(SDFRendererUtil.MIN_OUTLINE, SDFRendererUtil.MAX_OUTLINE)]
        public float OutlineWidth;
        
        
        /// <summary>
        /// If enabled mesh for shadow sdf part will be generated
        /// </summary>
        public bool RenderShadow;
        public Color ShadowColor;
        /// <summary>
        /// Width specific to shadow layer, changes width of shadow.
        /// </summary>
        [Range(SDFRendererUtil.MIN_SHADOW, SDFRendererUtil.MAX_SHADOW)]
        public float ShadowWidth;
        /// <summary>
        /// How much shadow mesh will be offseted
        /// </summary>
        public Vector2 ShadowOffset;

        
        public static SDFRendererSettings Default =>
            new SDFRendererSettings()
            {
                MainColor = Color.white,
                
                RenderRegular = true,
                RegularColor = Color.white,
                
                RenderOutline = true,
                OutlineColor = Color.white,
                OutlineWidth = 0.65f,
                
                RenderShadow = true,
                ShadowColor = new Color(0f, 0f, 0f, 0.5f),
                ShadowWidth = 0.75f,
                ShadowOffset = new Vector2(10,-10),
            };
    }
}