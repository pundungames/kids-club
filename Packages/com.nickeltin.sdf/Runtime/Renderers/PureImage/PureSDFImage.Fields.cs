using UnityEngine;

namespace nickeltin.SDF.Runtime
{
    public partial class PureSDFImage
    {
        [SerializeField] internal Vector2 _offset = Vector2.zero;
        [SerializeField] internal bool _lerpLayers = false; 
        [SerializeField, Range(0,1)] internal float _layersLerp = 0;
        
        [SerializeField] internal Layer _layerA = Layer.Default;
        [SerializeField] internal Layer _layerB = Layer.Default;
    }
}