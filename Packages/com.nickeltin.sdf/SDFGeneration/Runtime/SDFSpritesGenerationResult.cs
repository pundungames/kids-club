using System;
using UnityEngine;

namespace nickeltin.SDF.Runtime
{
    /// <summary>
    /// Result of SDF generation for sprites
    /// </summary>
    public readonly struct SDFSpritesGenerationResult
    {
        /// <summary>
        /// Packed texture for sdf's, all sprites are present here
        /// </summary>
        public readonly Texture2D PackedTexture;

        public readonly Sprite[] PackedSprites;
            
        /// <summary>
        /// How sprites are scaled from their original size to fit into texture atlas
        /// </summary>
        public readonly float[] SpritesScale;

        public readonly SDFSpriteMetadataAsset[] MetadataAssets;

        public SDFSpritesGenerationResult(Texture2D packedTexture, Sprite[] packedSprites, 
            float[] spritesScale, SDFSpriteMetadataAsset[] metadataAssets)
        {
            PackedTexture = packedTexture;
            PackedSprites = packedSprites;
            SpritesScale = spritesScale;
            MetadataAssets = metadataAssets ?? Array.Empty<SDFSpriteMetadataAsset>();
        }

        public SDFSpritesGenerationResult(SDFSpritesGenerationResult source) : this(source, source.MetadataAssets)
        {
        }
        
        public SDFSpritesGenerationResult(SDFSpritesGenerationResult source, SDFSpriteMetadataAsset[] metadataAssets) 
            : this(source.PackedTexture, source.PackedSprites, source.SpritesScale, metadataAssets)
        {
        }
    }
}