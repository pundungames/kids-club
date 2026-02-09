using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace nickeltin.SDF.Runtime
{
    public abstract class SDFGraphic : MaskableGraphic
    {
        /// <summary>
        /// Default constructor called before serialized data fills object
        /// </summary>
        protected SDFGraphic()
        {
            useLegacyMeshGeneration = false;
        }
        
        public void DisableSpriteOptimizations()
        {
            m_SkipLayoutUpdate = false;
            m_SkipMaterialUpdate = false;
        }
        
#if UNITY_EDITOR
        internal static event Action<SDFGraphic> SDFGraphicOnEnable; 
        internal static event Action<SDFGraphic> SDFGraphicOnDisable; 
        /// <summary>
        /// Passing as interface instead of struct to change its fields.
        /// </summary>
        /// <returns></returns>
        internal abstract IEnumerable<string> GetUsedSpritesPropertyPaths();
        protected static string CreatePropertyPath(params string[] parts) => string.Join(".", parts);
#endif

        protected override void OnEnable()
        {
            base.OnEnable();
#if UNITY_EDITOR
            SDFGraphicOnEnable?.Invoke(this);
#endif
        }

        protected override void OnDisable()
        {
            base.OnDisable();
#if UNITY_EDITOR
            SDFGraphicOnDisable?.Invoke(this);
#endif
        }
    }
}