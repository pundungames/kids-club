#if UNITY_EDITOR
using nickeltin.SDF.Editor;
using nickeltin.SDF.Editor.DecoupledPipeline;
using nickeltin.SDF.Samples.Runtime;
using nickeltin.SOCreateWindow.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace nickeltin.SDF.Samples.Editor
{
    [CreateAssetWindow]
    public class CreateSDFAsset : SampleBaseAsset
    {
        [SerializeField] private Texture _texture;
        [SerializeField] private SDFImportSettings _sdfImportSettings = new(); 

        [SampleButton]
        private void Create()
        {
            Assert.IsNotNull(_texture, "_texture != null");
            var texImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(_texture)) as TextureImporter;
            Assert.IsNotNull(texImporter, "texImporter != null");
            var createdPath = SDFAssetImporter.CreateForTexture(texImporter, _sdfImportSettings);
            Debug.Log($"SDFAsset for texture {texImporter.assetPath} is created at {createdPath}");
        }
    }
}
#endif