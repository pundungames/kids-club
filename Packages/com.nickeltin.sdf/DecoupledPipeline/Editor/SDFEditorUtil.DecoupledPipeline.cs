using System.Collections.Generic;
using nickeltin.SDF.Runtime;
using nickeltin.SDF.Runtime.DecoupledPipeline;
using UnityEditor;
using UnityEngine;

namespace nickeltin.SDF.Editor
{
    public partial class SDFEditorUtil
    {
        /// <summary>
        /// Is this sdf sprite (or other nested asset) generated as part of <see cref="SDFAsset"/>
        /// </summary>
        [SDFPipelineCompatible(SDFPipelineFlags.DecoupledSDFSprite | SDFPipelineFlags.Unity2023)]
        public static bool IsPartOfDecoupledPipelineAsset(Object asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            return IsDecoupledPipelineAsset(path);
        }

        [SDFPipelineCompatible(SDFPipelineFlags.DecoupledSDFSprite | SDFPipelineFlags.Unity2023)]
        private static bool IsDecoupledPipelineAsset(string path)
        {
            return typeof(SDFAsset).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(path));
        }
        
        /// <summary>
        /// Finds all <see cref="SDFAsset"/>'s
        /// </summary>
        /// <returns>GUID's</returns>
        [SDFPipelineCompatible(SDFPipelineFlags.DecoupledAnd2023)]
        public static IEnumerable<string> FindAllSDFAssets()
        {
            return AssetDatabase.FindAssets($"t:{typeof(SDFAsset)}");
        }
        
        /// <summary>
        /// Finds all <see cref="SDFAsset"/>'s that uses particular texture.
        /// Makes lookup by searching all SDFAsset's in a project, then loads its import results artifacts and returns
        /// those SDFAssets that match provided texture guids. 
        /// </summary>
        /// <returns>SDFAsset GUID's</returns>
        [SDFPipelineCompatible(SDFPipelineFlags.DecoupledAnd2023)]
        public static IEnumerable<string> FindSDFAssetsThatUsesTexture(string textureGUID)
        {
            var guids = FindAllSDFAssets();
            foreach (var guid in guids)
            {
                var importResult = SDFImportResult.Get(new GUID(guid));
                if (importResult.SourceTextureGUID == textureGUID)
                {
                    yield return guid;
                }
            }
        }
    }
}