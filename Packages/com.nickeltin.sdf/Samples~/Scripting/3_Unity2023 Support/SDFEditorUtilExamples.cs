#if UNITY_EDITOR
using nickeltin.SDF.Editor;
using nickeltin.SDF.Samples.Runtime;
using nickeltin.SOCreateWindow.Runtime;
using UnityEngine;


namespace nickeltin.SDF.Samples.Editor
{
    [CreateAssetWindow]
    internal class SDFEditorUtilExamples : SampleBaseAsset
    {
        [Header("Referencing")]
        [SerializeField] private Sprite _sprite = null;
        
        [Header("Comparing")] 
        [SerializeField] private Sprite _spriteA;
        [SerializeField] private Sprite _spriteB;

        [SampleButton]
        public void PrintMetadataAsset()
        {
            var found = SDFEditorUtil.TryGetSpriteMetadataAsset(_sprite, false, out var metadata);
            Debug.Log($"Sprite {_sprite}, metadata found: {found}, metadata: {metadata}");
        } 
        
        [SampleButton]
        public void PrintSpritesComparison()
        {
            var isASdf = SDFEditorUtil.IsSDFSprite(_spriteA);
            var isASource = SDFEditorUtil.IsSourceSprite(_spriteA, false);
            
            var isBSdf = SDFEditorUtil.IsSDFSprite(_spriteB);
            var isBSource = SDFEditorUtil.IsSourceSprite(_spriteB, false);

            var isPair = SDFEditorUtil.IsSDFPair(_spriteA, _spriteB);
            
            Debug.Log($"Sprite: {_spriteA}\n    is SDF: {isASdf}\n    is SDF source: {isASource}\n" +
                      $"Sprite: {_spriteB}\n    is SDF: {isBSdf}\n    is SDF source: {isBSource}\n" +
                      $"Is sprite an SDF pair: {isPair}");
            
        }
    }
}
#endif

