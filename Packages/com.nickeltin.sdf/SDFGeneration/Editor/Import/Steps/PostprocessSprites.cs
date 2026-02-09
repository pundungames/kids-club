using System.Linq;
using nickeltin.InternalBridge.Editor;
using nickeltin.SDF.InternalBridge.Editor;
using nickeltin.SDF.Runtime;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.U2D;
using UnityEngine;
using static nickeltin.SDF.Runtime.SDFGenerationInternalUtil;

namespace nickeltin.SDF.Editor
{
    /// <summary>
    /// Third step for import.
    /// We generate packed texture and meta assets for sprites here
    /// </summary>
    internal class PostprocessSprites : ImportStep
    {
        protected HideFlags SDFSpriteMetadataHideFlags = HideFlags.HideInHierarchy;
        protected bool IsSDFSpriteMetadataDecoupled = false;
        /// <summary>
        /// If set to true will use sprite GUID (Legacy) way to bind to sprites.
        /// If false will use sprite InternalID which is more consistent.
        /// </summary>
        protected bool UseSpriteGUIDToBindAssets = true;

        protected virtual int GetPackedTextureMaxSize(SDFImportContext sdfCtx, AssetImportContext astCtx)
        {
            return sdfCtx.TextureImporter.maxTextureSize;
        }
        
        public sealed override ProcessResult Process(SDFImportContext sdfCtx, AssetImportContext astCtx)
        {
            // Performing some additional validation for provided sprite data
            var spriteNames = sdfCtx.Sprites.Select(sprite => sprite.name).ToList();
            var spriteRectNames = sdfCtx.TextureImporter.GetSpriteRects().Select(rect => rect.name).ToList();
            
            // Simple case where provided sprite count is different from import settings
            if (spriteNames.Count != spriteRectNames.Count)
            {
                Debug.LogError(
                    $"Can't import SDF, source sprites count ({spriteNames.Count}) not matching import settings sprite count ({spriteRectNames.Count}).",
                    sdfCtx.MainAssetImporter);
                return ProcessResult.End(false);
            }
            
            // Case where sprite order is not the same as their rects in import settings
            // This case was a pain to find in decoupled pipeline
            if (!spriteNames.SequenceEqual(spriteRectNames))
            {
                Debug.LogError($"Can't import SDF, source sprite sequence not matching import settings sprite rects.\n" +
                               $"Source sprites: {string.Join(", ", spriteNames)}\n" + 
                               $"Source sprite rects: {string.Join(", ", spriteRectNames)}", 
                    sdfCtx.MainAssetImporter);
                return ProcessResult.End(false);
            }
            
            Generate_Internal(ref sdfCtx, astCtx);
            return ProcessResult.End(true);
        }

        protected virtual void Generate_Internal(ref SDFImportContext sdfCtx, AssetImportContext astCtx)
        {
            Generate(ref sdfCtx, astCtx);
        }

        protected SDFSpritesGenerationResult Generate(ref SDFImportContext sdfCtx, AssetImportContext astCtx)
        {
            var spriteRects = sdfCtx.TextureImporter.GetSpriteRects();
            // Converting for unified format for runtime generation support
            var sourceSpritesData = spriteRects
                .Select(meta => new SourceSpriteData(meta.rect, meta.pivot))
                .ToArray();
            
            var maxSize = GetPackedTextureMaxSize(sdfCtx, astCtx);
            var result = GeneratePackedTextures(sdfCtx.TextureCopy, sdfCtx.AdjustedBorderOffset, 
                sdfCtx.ImportedTextureScale, maxSize, sdfCtx.Sprites, sourceSpritesData, 
                sdfCtx.BackendContext.Backend, sdfCtx.ImportSettings);
            
            sdfCtx.AddSubAsset("PackedSDFTex", result.PackedTexture);

            var sourceTextureGUID = AssetDatabase.AssetPathToGUID(sdfCtx.TextureImporter.assetPath);
            var importResult = new SDFImportResult(new long[sdfCtx.Sprites.Length], 
                new long[sdfCtx.Sprites.Length], sourceTextureGUID);
            
            var metadataAssets = new SDFSpriteMetadataAsset[sdfCtx.Sprites.Length];
            for (var i = 0; i < sdfCtx.Sprites.Length; i++)
            {
                var sdfSprite = result.PackedSprites[i];
                var sprite = sdfCtx.Sprites[i];
                var spriteRect = spriteRects[i];
                
                // Sprite ID is basically project-wide unique ID, just in case we're adding SDF postfix and using it as local identifier
                var sdfSpriteStrId = GetUniqueLocalID(spriteRect, "SDFSprite", UseSpriteGUIDToBindAssets);
                sdfSprite.SetSpriteID(new GUID(sdfSpriteStrId));
                
                sdfCtx.AddSubAsset(sdfSpriteStrId, sdfSprite);
                
                // Creating meta asset
                var metadataAsset = CreateSpriteMetadataAsset(sprite, sdfSprite, sdfCtx.AdjustedBorderOffset, sdfCtx.BackendContext.Backend.BaseData.Identifier);
                metadataAssets[i] = metadataAsset;
                var metaAssetStrId = GetUniqueLocalID(spriteRect, "SDFSpriteMetadata", UseSpriteGUIDToBindAssets);
                
                // Generating preview of meta asset
                var spriteSize = sprite.rect.size;
                var thumbnail = _SpriteUtility.RenderStaticPreview(sprite, Color.white, (int)spriteSize.x, (int)spriteSize.y);
                sdfCtx.AddSubAsset(metaAssetStrId, metadataAsset, thumbnail);
                
                // Calculating internalID for meta asset, custom type is MonoBehaviour 114 - any script defined object.
                importResult.MetaAssetsLocalIDs[i] = sdfCtx.MainAssetImporter.MakeInternalIDForCustomType(metaAssetStrId);
                // Sprite internalID is 21300000 for single mode, and saved id in sprite rect (or loaded from table in importer) for multiple.
                importResult.SourceSpritesLocalIDs[i] = GetSourceSpriteLocalID(sdfCtx, spriteRect);
            }
            
            sdfCtx.ResultArtifact = importResult;
            // result.MetadataAssets = metadataAssets;
            return new SDFSpritesGenerationResult(result, metadataAssets);
        }
        
        protected virtual long GetSourceSpriteLocalID(SDFImportContext sdfCtx, SpriteRect spriteRect)
        {
            if (sdfCtx.TextureImporter.spriteImportMode == SpriteImportMode.Multiple)
            {
                // For multiple sprites hash code of their guid is used
                return spriteRect.GetInternalID();
            }

            // 213 stands for sprite type, see more at: https://docs.unity3d.com/Manual/ClassIDReference.html
            // If single sprite mode sprite file id will always be 21300000 (don't know why zeros are added)
            return 21300000;
        }
        
        /// <summary>
        /// Will use base sprite id/guid and then append some offset/postfix to it.
        /// <paramref name="useSpriteGUID"/> determines is using old way with <see cref="SpriteEditorExtension.GetSpriteID"/>
        /// use it for assets imported from older version.
        /// 
        /// Sprite guid is bad since its unique project-wide and is reassigned upon sprite rect creation,
        /// meaning if you delete sprite rect and the creation identical one guid will be different.
        /// However <see cref="AssetImporter"/> writes to its meta file 'internalIDToNameTable',
        /// to make 'internalID' consistent over different sprite rects.
        /// Use internalID instead of spriteGUID, this is preferable to keep references intact.
        /// </summary>
        private static string GetUniqueLocalID(SpriteRect sprite, string addition, bool useSpriteGUID = true)
        {
            var hash = new Hash128();
            if (useSpriteGUID)
            {
                // ReSharper disable once SuggestVarOrType_SimpleTypes
                // Using defined type to use exact Append overload
                GUID guid = sprite.spriteID;
                hash.Append(ref guid);
            }
            else
            {
                // ReSharper disable once SuggestVarOrType_BuiltInTypes
                // Using defined type to use exact Append overload
                long internalID = sprite.GetInternalID();
                hash.Append(ref internalID);
            }
            hash.Append(addition);
            return hash.ToString();
        }
        
        private SDFSpriteMetadataAsset CreateSpriteMetadataAsset(Sprite sourceSprite, Sprite sdfSprite, Vector4 borderOffset, string generationBackend)
        {
            var asset = ScriptableObject.CreateInstance<SDFSpriteMetadataAsset>();
            asset._isImportedDecoupled = IsSDFSpriteMetadataDecoupled;
            // Settings hide flags to prevent visible scriptable object from becoming the main asset.
            // This is some of the stupid unit behaviour...
            // AssetImporter chooses himself what main asset is, you can't control this in AssetPostprocessor
            asset.hideFlags = SDFSpriteMetadataHideFlags;
            asset._metadata = new SDFSpriteMetadata(sourceSprite, sdfSprite, borderOffset);
            asset._generationBackend = generationBackend;
            
            // Previously name of the meta asset was == to sourceSprite.name but addressable for some reason referencing
            // sub-assets by parent asset GUID and sub-asset name, so unique names is required.
            // This is done for testing since addressables probably don't even support hidden assets
            asset.name = sourceSprite.name + SDF_SPRITE_META_ASSET_POSTFIX;
            
            AssignNewPipelineSpriteMetadata(sourceSprite, sdfSprite, asset);
            return asset;
        }

        protected virtual void AssignNewPipelineSpriteMetadata(Sprite sourceSprite, Sprite sdfSprite, SDFSpriteMetadataAsset asset)
        {
#if SDF_NEW_SPRITE_METADATA
            sourceSprite.AddScriptableObject(asset);
            sdfSprite.AddScriptableObject(asset);
#endif
        }
    }
}