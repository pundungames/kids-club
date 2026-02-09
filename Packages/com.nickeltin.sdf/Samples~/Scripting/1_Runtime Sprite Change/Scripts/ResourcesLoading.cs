using nickeltin.SDF.Runtime;
using UnityEngine;

namespace nickeltin.SDF.Samples.Runtime
{
    public class ResourcesLoading : SDFImageModifier
    {
        [SerializeField] private string _decoupledMetadataAssetPath;

        [Header("Unity 2023, regular pipeline")] 
        [SerializeField] private string _regularSpritePath;

        [SampleButton]
        private void LoadDecoupledSDFMetaAsset()
        {
            // For decoupled pipeline can load directly
            // For regular pipeline resources loading is not possible unless in unity 2023
            // due to SDFSpriteMetadataAsset's is hidden
            var metaAsset = Resources.Load<SDFSpriteMetadataAsset>(_decoupledMetadataAssetPath);
            SDFImage.SDFSpriteReference = metaAsset;
            Debug.Log(metaAsset);
        }

        [SampleButton]
        private void Load2023RegularSprite()
        {
            var sprite = Resources.Load<Sprite>(_regularSpritePath);
            Debug.Log(sprite);
#if UNITY_2023_1_OR_NEWER
            var found = sprite.TryGetSpriteMetadataAsset(out var metadata);
            Debug.Log($"Sprite {sprite}, metadata asset found: {found}, metadata: {metadata}");
            SDFImage.SDFSpriteReference = metadata;
#else
            Debug.LogError($"Unity before 2023 can't get sdf metadata directly from sprite");
#endif
        }
    }
}