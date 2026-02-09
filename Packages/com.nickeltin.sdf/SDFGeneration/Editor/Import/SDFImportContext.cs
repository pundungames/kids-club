using System.Collections.Generic;
using nickeltin.SDF.Editor.DecoupledPipeline;
using nickeltin.SDF.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using static nickeltin.SDF.Editor.SDFGenerationEditorUtil;

namespace nickeltin.SDF.Editor
{
    /// <summary>
    /// DTO for texture import pipeline.
    /// Import done in three steps:
    ///     - texture pre-processing
    ///     - texture post-processing
    ///     - sprite-post-processing
    /// </summary>
    internal class SDFImportContext
    {
        public readonly struct SubAsset
        {
            public readonly string ID;
            public readonly Object Asset;
            public readonly Texture2D Thumbnail;

            public SubAsset(string id, Object asset, Texture2D thumbnail = null)
            {
                ID = id;
                Asset = asset;
                Thumbnail = thumbnail;
            }
        }
        
        public SDFImportContext(TextureImporter textureImporter, SDFGenerationSettings importSettings, AssetImporter mainAssetImporter)
        {
            TextureImporter = textureImporter;
            ImportSettings = importSettings;
            MainAssetImporter = mainAssetImporter;
        }
        
        /// <summary>
        /// For regular pipeline <see cref="TextureImporter"/> and <see cref="MainAssetImporter"/> will be the same.
        /// For decoupled pipeline main importer is <see cref="SDFAssetImporter"/>
        /// </summary>
        public AssetImporter MainAssetImporter { get; }
        public TextureImporter TextureImporter { get; }
        public Texture2D Texture { get; set; }
        public Sprite[] Sprites { get; set; }

        public SDFGenerationSettings ImportSettings { get; set; }
        
        public BackendContext BackendContext { get; set; }

        /// <summary>
        /// Copy of original texture with <see cref="GraphicsFormat.R8G8B8A8_UNorm"/> format.
        /// Persist over all import process, released at end.
        /// </summary>
        public Texture TextureCopy { get; set; }

        /// <summary>
        /// How imported texture is smaller compared to original texture.
        /// Texture might get smaller due to max size setting.
        /// </summary>
        public float ImportedTextureScale { get; set; }

        public Vector4 AdjustedBorderOffset { get; set; }

        public SDFImportResult ResultArtifact { get; set; }
        
        private readonly List<SubAsset> _subAssets = new();
        private readonly Queue<SubAsset> _subAssetsQueue = new();
        
        /// <summary>
        /// Sub assets definition that will be added at the end of import, or destroyed if import not successful
        /// </summary>
        public IEnumerable<SubAsset> SubAssets => _subAssets;

        public void AddSubAsset(string id, Object asset, Texture2D thumbnail = null)
        {
            var subAsset = new SubAsset(id, asset, thumbnail);
            _subAssets.Add(subAsset);
            _subAssetsQueue.Enqueue(subAsset);
        }

        /// <summary>
        /// Try dequeue sub assets that's still needs to be added.
        /// </summary>
        public bool TryDequeueSubAsset(out SubAsset subAsset)
        {
            return _subAssetsQueue.TryDequeue(out subAsset);
        }
    }
}