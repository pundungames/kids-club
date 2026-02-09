using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace nickeltin.SDF.Runtime
{
    /// <summary>
    /// Rendering stack used to control properties of multiple <see cref="PureSDFImage"/>'s
    /// rendered in stack as different sdf layers.
    /// Will control sprites and blending settings.
    /// </summary>
    public class PureSDFImageRenderingStack : MonoBehaviour
    {
        [SerializeField] internal List<PureSDFImage> _stack = new List<PureSDFImage>();
        [SerializeField] internal bool _lerpLayers = false;
        [SerializeField, Range(0,1)] internal float _layersLerp = 0;
        [SerializeField] internal SDFSpriteReference _spriteReferenceA = new SDFSpriteReference();
        [SerializeField] internal SDFSpriteReference _spriteReferenceB = new SDFSpriteReference();


        public IEnumerable<PureSDFImage> GetStack() => _stack;

        public bool Contains(PureSDFImage image) => GetStack().Contains(image);

        public void AddToStack(PureSDFImage image)
        {
            _stack.Add(image);
            SendProperties();
        }

        public void RemoveFromStack(PureSDFImage image)
        {
            _stack.Remove(image);
        }

        public bool LerpLayers
        {
            get => _lerpLayers;
            set => SetProperty(ref _lerpLayers, value);
        }

        public float LayersLerp
        {
            get => _layersLerp;
            set => SetProperty(ref _layersLerp, Mathf.Clamp01(value));
        }
        
        public SDFSpriteReference SpriteReferenceA
        {
            get => _spriteReferenceA;
            set => SetProperty(ref _spriteReferenceA, value);
        }

        public SDFSpriteReference SpriteReferenceB
        {
            get => _spriteReferenceB;
            set => SetProperty(ref _spriteReferenceB, value);
        }

        private void OnValidate()
        {
            if (_stack.Count == 0) GetComponentsInChildren(_stack);

            SendProperties();
        }

        public void SendProperties()
        {
            foreach (var image in _stack)
            {
                if (image == null)
                    return;
                
                var layer = image.LayerA;
                layer.SpriteReference = SpriteReferenceA;
                image.LayerA = layer;
                
                layer = image.LayerB;
                layer.SpriteReference = SpriteReferenceB;
                image.LayerB = layer;

                image.LerpLayers = LerpLayers;
                image.LayersLerp = LayersLerp;
                
#if UNITY_EDITOR
                image.RegisterRenderingStack(this);
#endif
            }
        }

        private bool SetProperty<T>(ref T property, T value)
        {
            if (EqualityComparer<T>.Default.Equals(property, value)) return false;

            property = value;
            SendProperties();
            return true;
        }
    }
}