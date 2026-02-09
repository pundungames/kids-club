using System;
using System.IO;
using System.Linq;
using nickeltin.InternalBridge.Editor;
using nickeltin.SDF.Runtime.DecoupledPipeline;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.Experimental;

namespace nickeltin.SDF.Editor
{
    /// <summary>
    /// Represents runtime version of serialized data saved to VirtualArtifact file.
    /// Holds localId's of meta assets and corresponding source sprites to make search possible.
    /// LocalId for meta asset is calculated with <see cref="_AssetImporter.MakeInternalIDForCustomType"/>.
    /// LocalId for sprite is its guid hash.
    /// See more at <see cref="PostprocessSprites"/>
    /// </summary>
    [Serializable]
    internal struct SDFImportResult
    {
        public const string META_ARTIFACT_NAME = "sdf-import-result";

        public long[] MetaAssetsLocalIDs;
        public long[] SourceSpritesLocalIDs;
        public string SourceTextureGUID;
        
        public SDFImportResult(long[] metaAssetsLocalIDs, long[] sourceSpritesLocalIDs, string sourceTextureGuid)
        {
            MetaAssetsLocalIDs = metaAssetsLocalIDs;
            SourceSpritesLocalIDs = sourceSpritesLocalIDs;
            SourceTextureGUID = sourceTextureGuid;
        }
        
        public static SDFImportResult Default => new(Array.Empty<long>(), Array.Empty<long>(), "");
        
        public static void Save(AssetImportContext ctx, SDFImportResult result)
        {
            var json = EditorJsonUtility.ToJson(result, false);
            var path = ctx.GetOutputArtifactFilePath(META_ARTIFACT_NAME);
            File.WriteAllText(path, json);
        }
        
        
        /// <summary>
        /// Will calculate artifact path for particular asset, if file exist will deserialize it, else return default (empty) result.
        /// </summary>
        /// <param name="assetGUID">This might be texture or <see cref="SDFAsset"/></param>
        public static SDFImportResult Get(GUID assetGUID)
        {
            var artifactID = AssetDatabaseExperimental.LookupArtifact(new ArtifactKey(assetGUID));
            object result = Default;
            if (AssetDatabaseExperimental.GetArtifactPaths(artifactID, out var paths))
            {
                var path = paths.FirstOrDefault(p => p.EndsWith(META_ARTIFACT_NAME));
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    EditorJsonUtility.FromJsonOverwrite(json, result);
                }
            }

            return (SDFImportResult)result;
        }
    }
}