#if SDF_NEW_SPRITE_METADATA

using System.Buffers;
using System.Linq;
using UnityEngine;

namespace nickeltin.SDF.Runtime
{
    /// <summary>
    /// Part that handles accessing metadata of sprite for 2023 version with scriptable objects.
    /// </summary>
    public static partial class SDFUtil
    {
        private static ArrayPool<ScriptableObject> _pool;
        
        static partial void Init2023Support()
        {
            _pool = ArrayPool<ScriptableObject>.Create(); 
        }

        [SDFPipelineCompatible(SDFPipelineFlags.RegularAnd2023 | SDFPipelineFlags.DecoupledSDFSprite)]
        public static bool TryGetSpriteMetadata(this Sprite sprite, out SDFSpriteMetadata metadata)
        {
            if (sprite.TryGetSpriteMetadataAsset(out var metadataAsset))
            {
                metadata = metadataAsset.Metadata;
                return true;
            }

            metadata = default;
            return false;
        }
        
        /// <summary>
        /// Will try to get sprite <see cref="SDFSpriteMetadataAsset"/> from sprite.
        /// in unity 2023 it will have meta asset assigned in <see cref="Sprite.GetScriptableObjects"/> array.
        /// </summary>
        /// <remarks>
        ///     Returns true (and outputs meta asset) if sprite imported from regular pipeline (not decoupled) and has proper sdf import settings.
        ///     For decoupled pipeline only works if called for SDF sprite.
        /// </remarks>
        [SDFPipelineCompatible(SDFPipelineFlags.RegularAnd2023 | SDFPipelineFlags.DecoupledSDFSprite)]
        public static bool TryGetSpriteMetadataAsset(this Sprite sprite, out SDFSpriteMetadataAsset metadataAsset)
        {
            metadataAsset = null;
            
            if (sprite == null)
            {
                return false;
            }
            
            var count = sprite.GetScriptableObjectsCount();
            if (count > 0)
            {
                // Renting pool to fit all sprite scriptable objects
                var array = _pool.Rent((int)count);
                sprite.GetScriptableObjects(array);
                metadataAsset = array.OfType<SDFSpriteMetadataAsset>().FirstOrDefault();
                _pool.Return(array, true);
                return metadataAsset != null;
            }
            
            return false;
        }
        
        /// <summary>
        /// Is persistent sprite is product of sdf import?
        /// </summary>
        /// <remarks>
        ///     Works for both decoupled and regular pipeline since SDF sprite will always have metadata asset reference,
        ///     therefore it can be extracted from either sprite.
        /// </remarks>
        [SDFPipelineCompatible(SDFPipelineFlags.Everywhere)]
        public static bool IsGeneratedSDFSprite(this Sprite sprite)
        {
            if (sprite.TryGetSpriteMetadata(out var metadata))
            {
                return metadata.SDFSprite == sprite;
            }

            return false;
        }

        /// <summary>
        /// Is persistent sprite product of Unity sprite import and has sdf sprite generated from it?
        /// </summary>
        /// <remarks>
        ///     Works only for regular pipeline (not decoupled) since source sprites gets its meta assets
        ///     assigned only from importer (postprocessor)
        /// </remarks>
        [SDFPipelineCompatible(SDFPipelineFlags.RegularAnd2023)]
        public static bool IsSourceSDFSprite(this Sprite sprite)
        {
            if (sprite.TryGetSpriteMetadata(out var metadata))
            {
                return metadata.SourceSprite == sprite;
            }

            return false;
        }

        /// <summary>
        /// Is two sprites part of sdf import?
        /// One of them is sdf sprite and other is source sprite that sdf was generated from.
        /// </summary>
        /// <remarks>
        ///     Works for both decoupled and regular pipeline since SDF sprite will always have metadata asset reference,
        ///     therefore it can be extracted from either sprite.
        /// </remarks>
        [SDFPipelineCompatible(SDFPipelineFlags.Everywhere)]
        public static bool IsSDFPair(this Sprite a, Sprite b)
        {
            if (a.TryGetSpriteMetadata(out var metadata) || b.TryGetSpriteMetadata(out metadata))
            {
                return (metadata.SDFSprite == a && metadata.SourceSprite == b) ||
                       (metadata.SDFSprite == b && metadata.SourceSprite == a);
            }

            return false;
        }
    }
}

#endif