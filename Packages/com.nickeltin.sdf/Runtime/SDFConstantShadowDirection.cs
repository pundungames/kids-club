using UnityEngine;
using UnityEngine.EventSystems;

namespace nickeltin.SDF.Runtime
{
    [RequireComponent(typeof(SDFImage)), ExecuteAlways, DisallowMultipleComponent]
    public class SDFConstantShadowDirection : UIBehaviour
    {
        [SerializeField, Tooltip(
             "Override for ShadowOffset specified in world space. " +
             "Changing rotation of SDFImage will trigger geometry update to recreate shadow mesh with new coordinates.")] 
        private Vector2 _constantShadowDirection = new Vector2(10, -10);
        private SDFImage _sdfImage;

        public SDFImage SDFImage
        {
            get
            {
                if (_sdfImage == null)
                {
                    _sdfImage = GetComponent<SDFImage>();
                }

                return _sdfImage;
            }
        }
        
        private void Update()
        {
            var dir = SDFImage.transform.InverseTransformDirection(_constantShadowDirection);
            SDFImage.ShadowOffset = dir; 
        }
    }
}