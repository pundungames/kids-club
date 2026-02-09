using nickeltin.SDF.Runtime;
using UnityEngine;
using UnityEngine.Search;

namespace nickeltin.SDF.Samples.Runtime
{
    public class DirectLoading : SDFImageModifier
    {
        [SerializeField] private SDFSpriteReference _spriteReference;
        [SerializeField, SearchContext("",SDFUtil.ArtifactsSearchProviderID)] 
        private SDFSpriteMetadataAsset _metadataAsset;

        [Header("2023 only, for regular pipeline")] 
        [SerializeField] private Sprite _sprite;

        [SampleButton]
        private void SetSpriteReference()
        {
            SDFImage.SDFSpriteReference = _spriteReference;
        }

        [SampleButton]
        private void SetMetadataReference()
        {
            // Metadata asset is interchangeable
            SDFImage.SDFSpriteReference = _metadataAsset;
        }

        [SampleButton]
        private void TrySet2023RegularPipelineSprite()
        {
#if UNITY_2023_1_OR_NEWER
            var found = _sprite.TryGetSpriteMetadataAsset(out var metadata);
            Debug.Log($"Sprite {_sprite}, metadata asset found: {found}, metadata: {metadata}");
            SDFImage.SDFSpriteReference = metadata;
#else
            Debug.LogError($"Unity before 2023 can't get sdf metadata directly from sprite");
#endif
        }
    }
}