using nickeltin.SDF.Runtime;
using nickeltin.SOCreateWindow.Runtime;
using UnityEngine;


namespace nickeltin.SDF.Samples.Runtime
{
    [CreateAssetWindow]
    internal class Unity2023RuntimeExamples : SampleBaseAsset
    {
        [Header("Referencing")]
        [SerializeField] private SDFSpriteReference _spriteReference = new SDFSpriteReference();
        [Header("Direct reference (2023)")]
        [SerializeField] private Sprite _sprite = null;
        
        [Header("Comparing sprites (2023)")] 
        [SerializeField] private Sprite _spriteA;
        [SerializeField] private Sprite _spriteB;
        
        [SampleButton]
        public void PrintMetadataAsset_FromReference()
        {
            Debug.Log($"Sprite {_spriteReference.SourceSprite}, metadata: {_spriteReference.Metadata}");
        } 
        
        [SampleButton]
        public void PrintMetadataAsset_FromSprite()
        {
#if UNITY_2023_1_OR_NEWER
            var found = _sprite.TryGetSpriteMetadata(out var metadata);
            Debug.Log($"Sprite {_sprite}, metadata found: {found}, metadata: {metadata}");
#else
            Debug.LogError($"Unity before 2023 can't get sdf metadata directly from sprite");
#endif
        }
        
        
        [SampleButton]
        public void PrintSpritesComparison_FromSprite()
        {
#if UNITY_2023_1_OR_NEWER
            var isASdf = _spriteA.IsGeneratedSDFSprite();
            var isASource = _spriteA.IsSourceSDFSprite();
            
            var isBSdf = _spriteB.IsGeneratedSDFSprite();
            var isBSource = _spriteB.IsSourceSDFSprite();

            var isPair = _spriteA.IsSDFPair(_spriteB);
            
            Debug.Log($"Sprite: {_spriteA}\n    is SDF: {isASdf}\n    is SDF source: {isASource}\n" +
                      $"Sprite: {_spriteB}\n    is SDF: {isBSdf}\n    is SDF source: {isBSource}\n" +
                      $"Is sprite an SDF pair: {isPair}");
#else
            Debug.LogError($"Unity before 2023 can't get sdf metadata directly from sprite, therefore can't compare sprites");
#endif
        }

        [SampleButton]
        public void Print2023Support()
        {
            // New pipeline active in unity 2023 so define can be used to determine is new pipeline active
            // Internally com.nickeltin.sdf uses SDF_NEW_SPRITE_METADATA defined in *.asmdef files
#if UNITY_2023_1_OR_NEWER
#endif
            // Or just use property from SDFUtil
            var isNewPipeline = SDFUtil.IsNewSpriteMetadataEnabled;
            Debug.Log($"Is 2023 SDF metadata pipeline active: {isNewPipeline}");
        }
    }
}