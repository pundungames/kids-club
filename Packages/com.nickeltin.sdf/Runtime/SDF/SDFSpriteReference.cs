using System;
using UnityEngine;
using UnityEngine.Search;

namespace nickeltin.SDF.Runtime
{
    /// <summary>
    /// Just a wrapper around <see cref="SDFSpriteMetadataAsset"/> that holds all data required to render sdf.
    /// </summary>
    /// <remarks>
    ///     In old version 1.1.x this also held reference to Source sprite, which allowed just assigning sprite in the editor
    ///     and at least display it even if sdf metadata isn't generated.
    ///     In version 1.2.x this ability was removed with an introduction of a decoupled pipeline.
    /// </remarks>
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.DrawWithUnity, Sirenix.OdinInspector.DisableContextMenu()]
#endif
    [Serializable]
    public struct SDFSpriteReference
    {
        /// <summary>
        /// For versions before 2023 metadata needs to be referenced directly, since there are no scriptable objects for sprites api.
        /// </summary>
        [SerializeField, SearchContext("", SDFUtil.ArtifactsSearchProviderID)]
        internal SDFSpriteMetadataAsset _metadataAsset;


        public SDFSpriteReference(SDFSpriteMetadataAsset metadata)
        {
            _metadataAsset = metadata;
        }

        public SDFSpriteReference(SDFSpriteReference reference) : this(reference.MetadataAsset)
        {
        }

        public SDFSpriteMetadata Metadata => MetadataAsset?._metadata ?? default;

        public SDFSpriteMetadataAsset MetadataAsset => _metadataAsset;


        /// <summary>
        /// Source sprite imported by unity.
        /// </summary>
        public Sprite SourceSprite => Metadata.SourceSprite;

        public Sprite SDFSprite => Metadata.SDFSprite;

        public static implicit operator SDFSpriteReference(SDFSpriteMetadataAsset asset)
        {
            return new SDFSpriteReference(asset);
        }

        public static implicit operator SDFSpriteMetadataAsset(SDFSpriteReference reference)
        {
            return reference.MetadataAsset;
        }

        public static implicit operator SDFSpriteMetadata(SDFSpriteReference reference)
        {
            return reference.Metadata;
        }
    }
}