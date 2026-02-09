#if UNITY_EDITOR
using nickeltin.SDF.Editor;
using nickeltin.SDF.Samples.Runtime;
using nickeltin.SOCreateWindow.Runtime;
using UnityEngine;
using UnityEngine.Assertions;

namespace nickeltin.SDF.Samples.Editor.RegularPipeline
{
    [CreateAssetWindow]
    public class ImportSDF : SampleBaseAsset
    {
        [Header("Import SDF & Change settings")]
        [SerializeField] private Texture _texture;
        [SerializeField] private SDFImportSettings _sdfImportSettings = new();
        
        [SampleButton(Name = "Fix texture (Ensure that SDF is imported)")]
        private void FixTexture()
        {
            Assert.IsNotNull(_texture, "_texture != null");
            SDFEditorUtil.FixTextures(new [] { _texture });
        }

        [SampleButton]
        private void SetSDFImportSettings()
        {
            Assert.IsNotNull(_texture, "_texture != null");
            SDFEditorUtil.SetImportSettings(new [] { _texture }, _sdfImportSettings);
        }
    }
}
#endif