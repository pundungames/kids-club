#if UNITY_EDITOR
using nickeltin.SDF.Editor;
using nickeltin.SDF.Editor.DecoupledPipeline;
using nickeltin.SDF.Runtime.DecoupledPipeline;
using nickeltin.SDF.Samples.Runtime;
using nickeltin.SOCreateWindow.Runtime;
using UnityEditor;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace nickeltin.SDF.Samples.Editor
{
    [CreateAssetWindow]
    public class UpdateSDFAsset : SampleBaseAsset
    {
        [SerializeField] private SDFAsset _targetSDFAsset;
        
        [Header("SDFAsset settings")]
        [SerializeField] private Texture _texture;
        [SerializeField] private SDFImportSettings _sdfImportSettings = new();
        
        [SampleButton]
        private void Update()
        {
            Assert.IsNotNull(_targetSDFAsset, "_targetSDFAsset != null");
            var importer = SDFAssetImporter.Get(AssetDatabase.GetAssetPath(_targetSDFAsset));
            Assert.IsNotNull(importer, "importer != null");
            importer.Texture = _texture;
            importer.ImportSettings = _sdfImportSettings;
            importer.SaveAndReimport();
        }
    }
}
#endif