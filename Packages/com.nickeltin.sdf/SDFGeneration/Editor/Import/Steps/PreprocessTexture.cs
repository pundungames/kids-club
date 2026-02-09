using nickeltin.SDF.Runtime;
using UnityEditor.AssetImporters;

namespace nickeltin.SDF.Editor
{
    /// <summary>
    /// First step of import.
    /// We define is texture has valid type and is its <see cref="SDFGenerationSettings"/> allows to import sdf.
    /// </summary>
    internal class PreprocessTexture : ImportStep
    {
        /// <summary>
        /// At this point due to new CPU pipeline we need to check is texture readable...
        /// However, asset postprocessor is special case where texture is compressed, but not yet made not-readable.
        /// Different story for Decoupled Pipeline tho... 
        /// </summary>
        public override ProcessResult Process(SDFImportContext sdfCtx, AssetImportContext astCtx)
        {
            var shouldImportSDF = SDFEditorUtil.ShouldImportSDF(sdfCtx.TextureImporter, out var importSettings);
            sdfCtx.ImportSettings = importSettings;
            
            return shouldImportSDF 
                ? ProcessResult.Continue() 
                : ProcessResult.End(false);
        }
    }
}