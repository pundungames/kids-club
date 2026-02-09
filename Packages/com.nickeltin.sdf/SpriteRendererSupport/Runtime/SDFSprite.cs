using UnityEngine;

namespace nickeltin.SDF.Runtime.SpriteRendererSupport
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class SDFSprite : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _sdfLayer;
        
        private SpriteRenderer _spriteRenderer;

        public SpriteRenderer SpriteRenderer
        {
            get
            {
                if (_spriteRenderer == null)
                    _spriteRenderer = GetComponent<SpriteRenderer>();
                return _spriteRenderer;
            }
        }

        private void OnEnable()
        {
            SpriteRenderer.UnregisterSpriteChangeCallback(SpriteChangeCallback);
            SpriteRenderer.RegisterSpriteChangeCallback(SpriteChangeCallback);
        }

        private void OnDisable()
        {
            SpriteRenderer.UnregisterSpriteChangeCallback(SpriteChangeCallback);
        }

        private void SpriteChangeCallback(SpriteRenderer spriteRenderer)
        {
            Debug.Log("SpriteChangeCallback");
            Refresh();
        }

        [ContextMenu("Refresh")]
        private void Refresh()
        {
            
        }
    }
}