using nickeltin.SDF.Runtime;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace nickeltin.SDF.Editor.DecoupledPipeline
{
    /// <summary>
    /// Overrides some functionality of default <see cref="PostprocessSprites"/> to make it work with decoupled pipeline
    /// and <see cref="SDFAssetImporter"/>
    /// </summary>
    internal sealed class PostprocessSpritesOverride : PostprocessSprites
    {
        public SDFSpritesGenerationResult Result { get; private set; }

        public PostprocessSpritesOverride()
        {
            SDFSpriteMetadataHideFlags = HideFlags.None;
            IsSDFSpriteMetadataDecoupled = true;
            // For decoupled pipeline not using guid's, rather internalID
            // Regular pipeline continues to use older approach since we don't want references to be lost
            UseSpriteGUIDToBindAssets = false;
        }
        
        protected override void Generate_Internal(ref SDFImportContext sdfCtx, AssetImportContext astCtx)
        {
            Result = Generate(ref sdfCtx, astCtx);
        }

        protected override void AssignNewPipelineSpriteMetadata(Sprite sourceSprite, Sprite sdfSprite, SDFSpriteMetadataAsset asset)
        {
            // Assigning only metadata asset to sdf sprite since it's our import product and can be modified.
            // Source sprites is already imported and can't be modified.
#if SDF_NEW_SPRITE_METADATA
            sdfSprite.AddScriptableObject(asset);
#endif
        }
    }
}