#if UNITY_EDITOR
using System.Linq;
using nickeltin.SDF.Editor;
using nickeltin.SDF.Runtime;
using nickeltin.SDF.Runtime.DecoupledPipeline;
using nickeltin.SDF.Samples.Runtime;
using nickeltin.SOCreateWindow.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace nickeltin.SDF.Samples.Editor
{
    [CreateAssetWindow]
    public class FindSDFAssets : SampleBaseAsset
    {
        [SerializeField] private Texture _searchTarget;
        
        [Header("Searching for sprite metadata")]
        [SerializeField] private Sprite _metadataSearchTarget;
        [SerializeField] private bool _searchForSDFAssets = true;
            
        [SampleButton]
        private void FindAllSDFAssets()
        {
            var sdfAssets = SDFEditorUtil.FindAllSDFAssets().ToArray();
            if (sdfAssets.Length == 0)
            {
                Debug.Log($"SDFAsset's were not found");
            }
            else
            {
                Debug.Log($"Found {sdfAssets.Length} SDFAsset's");
                Selection.objects = sdfAssets.Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Object>).ToArray();
            }
        }

        [SampleButton]
        private void FindSDFAssetsThatUsesTexture()
        {
            Assert.IsNotNull(_searchTarget, "_searchTarget != null");
            
            var texImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(_searchTarget)) as TextureImporter;
            Assert.IsNotNull(texImporter, "texImporter != null");
            var texGUID = AssetDatabase.GUIDFromAssetPath(texImporter.assetPath).ToString();
            var sdfAssetsGUIDs = SDFEditorUtil.FindSDFAssetsThatUsesTexture(texGUID);
            var sdfAssets = sdfAssetsGUIDs.Select(guid =>
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                return AssetDatabase.LoadAssetAtPath<SDFAsset>(path);
            }).ToArray();

            if (sdfAssets.Length == 0)
            {
                Debug.Log($"SDFAsset's that uses texture {texImporter.assetPath} were not found", texImporter);
            }
            else
            {
                Debug.Log($"Found {sdfAssets.Length} SDFAsset's that uses texture {texImporter.assetPath}", texImporter);
                Selection.objects = sdfAssets;
            }
        }

        /// <summary>
        /// Will try to locate <see cref="SDFSpriteMetadataAsset"/>. If its regular pipeline then its easy,
        /// if this is decoupled pipeline will do project wide search to find find first <see cref="SDFAsset"/>
        /// that using this sprites texture.
        /// </summary>
        [SampleButton]
        private void FindSpriteMetadataAsset()
        {
            Assert.IsNotNull(_metadataSearchTarget, "_metadataSearchTarget != null");
            
            var found = SDFEditorUtil.TryGetSpriteMetadataAsset(_metadataSearchTarget, 
                _searchForSDFAssets, out var metadataAsset);
            
            Debug.Log($"Metadata search report for sprite {_metadataSearchTarget}:\n" +
                      $"    Search for SDFAsset?: {_searchForSDFAssets}\n" +
                      $"    Meta asset found: {found}\n" +
                      $"    Meta asset {metadataAsset} at ({AssetDatabase.GetAssetPath(metadataAsset)})");

            if (found)
            {
                Selection.activeObject = metadataAsset;
            }
        }
    }
}
#endif