using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace nickeltin.SDF.Runtime
{
    public partial class SDFImage
    {
        #region Old Serialized Fields

#if UNITY_EDITOR
        /// <summary>
        /// This field used only to restore reference to sprite
        /// </summary>
        [SerializeField, FormerlySerializedAs("m_Sprite"), FormerlySerializedAs("m_Frame")]
        internal Sprite _legacySprite = null;
#endif

        #endregion
        
        #region Serialized Fields
        
        [SerializeField] 
        internal SDFRendererSettings _sdfRendererSettings = SDFRendererSettings.Default;
        
        [SerializeField] 
        internal SDFSpriteReference _sdfSpriteReference = new SDFSpriteReference();
        
        
        [SerializeField, FormerlySerializedAs("m_Type")]
        internal Image.Type _imageType = Image.Type.Simple;
        
        [SerializeField, FormerlySerializedAs("m_PreserveAspect")] 
        internal bool _preserveAspect = false;
        
        [SerializeField, FormerlySerializedAs("m_FillCenter")] 
        internal bool _fillCenter = true;
        
        [SerializeField, FormerlySerializedAs("m_FillMethod")] 
        internal Image.FillMethod _fillMethod = Image.FillMethod.Radial360;
        
        [SerializeField, Range(0, 1), FormerlySerializedAs("m_FillAmount")] 
        internal float _fillAmount = 1.0f;
        
        [SerializeField, FormerlySerializedAs("m_FillClockwise")] 
        internal bool _fillClockwise = true;
        
        [SerializeField, FormerlySerializedAs("m_FillOrigin")] 
        internal int _fillOrigin = 0;
        
        [SerializeField, FormerlySerializedAs("m_UseSpriteMesh")] 
        internal bool _useSpriteMesh = false;
        
        [SerializeField, FormerlySerializedAs("m_PixelsPerUnitMultiplier")] 
        internal float _pixelsPerUnitMultiplier = 1.0f;
        
        #endregion

        #region Non Serialzied Fields
        
        // Not serialized until we support read-enabled sprites better.
        [NonSerialized] private float _alphaHitTestMinimumThreshold = 0;

        // Whether this is being tracked for Atlas Binding.
        [NonSerialized] private bool _tracked = false;

        // case 1066689 cache referencePixelsPerUnit when canvas parent is disabled;
        [NonSerialized] private float _cachedReferencePixelsPerUnit = 100;
        
        /// <summary>
        /// Tracker used to change first layer renderer rect transform properties.
        /// <see cref="EnsureFirstLayerRendererProperties"/>
        /// </summary>
        [NonSerialized] private DrivenRectTransformTracker _layerTracker = new DrivenRectTransformTracker();
        
        /// <summary>
        /// Vertex helper to fill sdf mesh.
        /// Differs from regular Graphic shader vertex helper, it is not static,
        /// it stores mesh state per-instance, increasing memory, but allowing to access it at anytime. 
        /// </summary>
        [NonSerialized] private readonly VertexHelper _sdfVh = new VertexHelper();
        

        /// <summary>
        /// Second vertex helper that manages only regular sprite mesh.
        /// It stores state for first layer and when it need to get a rebuilt first layer takes its state.
        /// </summary>
        [NonSerialized] internal readonly VertexHelper _firstLayerState = new VertexHelper();
        
        [NonSerialized] private static Material _defaultMaterial;
        
        #endregion
    }
}