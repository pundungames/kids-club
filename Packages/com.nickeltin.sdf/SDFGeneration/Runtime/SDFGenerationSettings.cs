using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace nickeltin.SDF.Runtime
{
    /// <summary>
    /// Strip down version of generation settings, for runtime generations. Don't need fields like
    /// <see cref="SDFGenerationSettings.GenerateSDF"/> or <see cref="SDFGenerationSettings.SDFGenerationBackend"/>.
    ///
    /// For runtime generation you set backend directly, or it's automatically selected with <see cref="SDFGenerationUtil.GetCurrentDefaultBackend"/>.
    /// </summary>
    [Serializable]
    public class SDFGenerationSettingsBase
    {
        private const string BORDER_OFFSET_DESC =
            "How many pixels will be added to sprite border. SDF needs space to generate between sprite edges (filled pixels) and actual texture edges." +
            "Not consistent with sprite size, use smaller values for smaller sprite.";
        
        public const float MIN_RES_SCALE = 0.001f;
        public const float MAX_RES_SCALE = 1f;

        public const int MIN_BORDER_OFFSET = 0;
        public const int MAX_BORDER_OFFSET = 256;
        
        public const float MIN_GRAD_SIZE = 0;
        public const float MAX_GRAD_SIZE = 1;
        
        // Marking with the wrong serialization name to re-serialize with new settings.
        [Tooltip(BORDER_OFFSET_DESC)]
        [FormerlySerializedAs("obsolete_border_offset")] 
        [SerializeField, Range(MIN_BORDER_OFFSET, MAX_BORDER_OFFSET)] 
        public int BorderOffset = SDFGenerationUtil.DEFAULT_BORDER_OFFSET;
        
        
        [Tooltip("Allow to save SDF texture with lower resolution, for simple shapes 10% (0.1) is usually good enough value.")]
        [SerializeField, Range(MIN_RES_SCALE, MAX_RES_SCALE)] 
        public float ResolutionScale = 0.1f;
        
        [Tooltip("How big is SDF effect generated on texture. Consistent over texture sizes.")]
        [SerializeField, Range(MIN_GRAD_SIZE, MAX_GRAD_SIZE)] 
        public float GradientSize = 0.2f;

        public static void Validate(SDFGenerationSettingsBase settings)
        {
            settings.BorderOffset = Mathf.Clamp(settings.BorderOffset, MIN_BORDER_OFFSET, MAX_BORDER_OFFSET);
            settings.ResolutionScale = Mathf.Clamp(settings.ResolutionScale, MIN_RES_SCALE, MAX_RES_SCALE);
            settings.GradientSize = Mathf.Clamp(settings.GradientSize, MIN_GRAD_SIZE, MAX_GRAD_SIZE);
        }
    }
    
    [Serializable]
    public class SDFGenerationSettings : SDFGenerationSettingsBase
    {
        [Tooltip("Is SDF texture should be generated?")]
        [SerializeField]
        public bool GenerateSDF = false;

        [SerializeField] 
        public string SDFGenerationBackend = SDFGenerationUtil.AUTO_EDITOR_BACKEND_ID;
    }
}