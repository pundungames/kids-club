using System;
using UnityEngine;

namespace nickeltin.SDF.Runtime
{
    [Serializable]
    public struct SDFSpriteMetadata
    {
        public Sprite SourceSprite;
        public Sprite SDFSprite;
        public Vector4 BorderOffset;
        // public Vector2 SDFSpriteScaleInAtlas;

        internal SDFSpriteMetadata(Sprite sourceSprite, Sprite sdfSprite, Vector4 borderOffset)
        {
            SourceSprite = sourceSprite;
            SDFSprite = sdfSprite;
            BorderOffset = borderOffset;
        }

        public override string ToString()
        {
            return $"SourceSprite {SourceSprite}, SDFSprite {SDFSprite}, BorderOffset {BorderOffset}";
        }
    }
}