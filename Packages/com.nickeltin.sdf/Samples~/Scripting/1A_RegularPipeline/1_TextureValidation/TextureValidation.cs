#if UNITY_EDITOR
using nickeltin.SDF.Editor;
using nickeltin.SDF.Samples.Runtime;
using nickeltin.SOCreateWindow.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace nickeltin.SDF.Samples.Editor.RegularPipeline
{
    [CreateAssetWindow]
    public class TextureValidation : SampleBaseAsset
    {
        [SerializeField] private Texture _importCandidate;
        
        [SampleButton]
        private void ValidateTexture()
        {
            Assert.IsNotNull(_importCandidate, "_importCandidate != null");
            var texturePath = AssetDatabase.GetAssetPath(_importCandidate);
            var shouldImportSDF = SDFEditorUtil.ShouldImportSDF(texturePath, 
                out var importer,
                out var settings);

            var jsonImportSettings = EditorJsonUtility.ToJson(settings, true);
            
            Debug.Log($"Texture {texturePath} Validation Report:\n" +
                      $"    Should import SDF: {shouldImportSDF}\n" +
                      $"    SDFImportSettings:\n {jsonImportSettings}");
        }
    }
}
#endif