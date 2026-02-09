using nickeltin.SDF.Runtime;
using UnityEngine;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif

namespace nickeltin.SDF.Samples.Runtime.AddressablesSupport
{
    public class AddressablesLoading : SDFImageModifier
    {
#if ADDRESSABLES
        [SerializeField] private string _addressablePath = EnsureAssetsIsAddressable.FIRST_SDF_ASSET_PATH;
        [SerializeField] private AssetReferenceT<SDFSpriteMetadataAsset> _addressableRef;
        
        [Header("Unity 2023, regular pipeline")]
        [SerializeField] private AssetReferenceT<Sprite> _spriteAddressableRef;

        [SampleButton]
        private void LoadAddressableAtPath()
        {
            var sdf = Addressables.LoadAssetAsync<SDFSpriteMetadataAsset>(_addressablePath);
            sdf.WaitForCompletion();
            Debug.Log($"Addressable at {_addressablePath} loaded, status: {sdf.Status}, object: {sdf.Result}");
            SDFImage.SDFSpriteReference = new SDFSpriteReference(sdf.Result);
        }
        
        [SampleButton]
        private void LoadAddressableReference()
        {
            var sdf =_addressableRef.LoadAssetAsync();
            sdf.WaitForCompletion();
            Debug.Log($"Addressable with key {_addressableRef.RuntimeKey} loaded, status: {sdf.Status}, object: {sdf.Result}");
            SDFImage.SDFSpriteReference = new SDFSpriteReference(sdf.Result);
            _addressableRef.ReleaseAsset();
        }
        
        [SampleButton]
        private void LoadAddressableSpriteReference2023()
        {
#if UNITY_2023_1_OR_NEWER
            
            var sdf =_spriteAddressableRef.LoadAssetAsync();
            sdf.WaitForCompletion();
            Debug.Log($"Addressable with key {_spriteAddressableRef.RuntimeKey} loaded, status: {sdf.Status}, object: {sdf.Result}");
            
            if (sdf.Result != null && sdf.Result.TryGetSpriteMetadataAsset(out var metadataAsset))
            {
                SDFImage.SDFSpriteReference = new SDFSpriteReference(metadataAsset);
            }
            else
            {
                SDFImage.SDFSpriteReference = new SDFSpriteReference();
            }
            
            _spriteAddressableRef.ReleaseAsset();
#else
            Debug.LogError($"Unity before 2023 can't get sdf metadata directly from sprite");
#endif
        }

#else
        private const string NO_ADDRESSABLES = "No addressables installed";

        [TextArea] public string _message = NO_ADDRESSABLES;
        private void OnValidate()
        {
            _message = NO_ADDRESSABLES;
        }
#endif
    }
}

