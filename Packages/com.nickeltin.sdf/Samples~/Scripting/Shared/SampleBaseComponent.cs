using nickeltin.SDF.Runtime;
using UnityEngine;
using nickeltin.SDF.Samples.Runtime;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace nickeltin.SDF.Samples.Runtime
{
    public abstract class SampleBaseComponent : MonoBehaviour { }
    
    [RequireComponent(typeof(SDFImage))]
    public abstract class SDFImageModifier : SampleBaseComponent
    {
        private SDFImage _sdfImage;
        
        public SDFImage SDFImage
        {
            get
            {
                if (_sdfImage == null) _sdfImage = GetComponent<SDFImage>();
                return _sdfImage;
            }
        }
    }
    
    public abstract class SampleBaseAsset : ScriptableObject { }
}

#if UNITY_EDITOR
namespace nickeltin.SDF.Samples.Editor
{
    [CustomEditor(typeof(SampleBaseComponent), true)]
    internal class SampleComponentEditor : SampleBaseEditor { }
    
    [CustomEditor(typeof(SampleBaseAsset), true)]
    internal class SampleAssetEditor : SampleBaseEditor { }
}
#endif