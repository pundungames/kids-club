using nickeltin.SDF.Runtime;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using static nickeltin.SDF.Editor.SDFGenerationEditorUtil;
using static nickeltin.SDF.Runtime.SDFGenerationInternalUtil;

namespace nickeltin.SDF.Editor
{
    /// <summary>
    /// Second step of import.
    /// We get generation backend from import settings here, and ensure its dependencies.
    /// If dependencies is not yet loaded import stops.
    /// Otherwise if texture is not sprite the regular SDFTex is generated, for plain texture.
    /// TODO: in future maybe return non-sprite textures support
    /// </summary>
    internal class PostprocessTexture : ImportStep
    {
        public override ProcessResult Process(SDFImportContext sdfCtx, AssetImportContext astCtx)
        {
            if (sdfCtx.Texture == null)
            {
                return ProcessResult.End(false);
            }
            
            sdfCtx.BackendContext = BackendProvider.GetBackendForGeneration(sdfCtx.ImportSettings.SDFGenerationBackend);
            
            // Adding dependencies to texture.
            // If any dependency is not yet imported aborting import.
            // Texture will be re-imported after dependencies is imported.
            if (!EnsureDependencies(sdfCtx.BackendContext, astCtx))
            {
                return ProcessResult.End(false);
            }

            // Creating texture and blitting original to work with unified format texture.
            sdfCtx.TextureCopy = sdfCtx.BackendContext.Backend.CopyTexture(sdfCtx.Texture);
            
            
            // Calculating imported texture scale. Texture might be clamped by max size, we need to know for how much.
            sdfCtx.ImportedTextureScale = GetImportedTextureScale(sdfCtx.TextureImporter, sdfCtx.Texture);
            
            
            // Border offset needs to be adjusted because texture size might be different
            var borderOffset = GetAdjustedBorderOffset(sdfCtx.TextureImporter, sdfCtx.Texture, sdfCtx.ImportSettings.BorderOffset);
            sdfCtx.AdjustedBorderOffset = borderOffset;

            // var texContainer = CreateTextureContainer(sdfCtx.Texture, borderOffset);
            // sdfCtx.TextureContainer = texContainer;
            // astCtx.AddObjectToAsset("SDFTextureContainer", texContainer);
            
            
            // TODO: currently not supporting plain texture, only sprites
            // If plain texture then import process ends here
            // if (sdfCtx.Importer.textureType != TextureImporterType.Sprite)
            // {
            //     var sdfTex = GenerateTexture(sdfCtx.Importer, sdfCtx.TextureCopy, sdfCtx.AdjustedBorderOffset, 
            //         sdfCtx.ImportedTextureScale, sdfCtx.Backend, sdfCtx.ImportSettings);
            //     sdfTex.name = sdfCtx.Texture.name;
            //     // Settings generated texture, if texture in sprite mode it will be setted in PostprocessSprites step.
            //     // texContainer._generatedTexture = sdfTex;
            //     astCtx.AddObjectToAsset("SDFTex", sdfTex);
            //     return ProcessResult.End(true);
            // }
            
            return ProcessResult.Continue();
        }

        // protected virtual Texture CopyTexture(Texture2D texture)
        // {
        //     return SDFGenerationInternalUtil.CopyTexture(texture, true);
        // }
        
        // private static SDFTextureContainer CreateTextureContainer(Texture sourceTex, Vector4 borderOffset)
        // {
        //     var texContainer = ScriptableObject.CreateInstance<SDFTextureContainer>();
        //     texContainer.hideFlags = HideFlags.HideInHierarchy;
        //     texContainer._sourceTexture = sourceTex;
        //     texContainer._borderOffset = borderOffset;
        //     texContainer.name = sourceTex.name;
        //     return texContainer;
        // }
        
        
        /// <summary>
        /// Generates SDFTex for plain texture, not a sprite texture.
        /// </summary>
        private static Texture2D GenerateTexture(TextureImporter importer, Texture tex, Vector4 borderOffset, float textureScale, 
            BackendContext backend, SDFGenerationSettings settings)
        {
            importer.GetSourceTextureWidthAndHeight(out var sourceW, out var sourceH);
            textureScale *= GetGradientSizeAdjustment(sourceW, sourceH);
            var backendSettings = backend.Backend.GetSettings(settings, borderOffset, tex.width, tex.height, textureScale);
            
            var sdfTex = GenerateSDF(tex,
                new RectInt(0, 0, tex.width, tex.height), backendSettings, backend.Backend);
            
            sdfTex.filterMode = tex.filterMode;
            sdfTex.wrapMode = tex.wrapMode;
            
            return sdfTex;
        }
        
        /// <summary>
        /// Ensures all SDF backend dependencies by adding them to <paramref name="importContext"/> with <see cref="AssetImportContext.DependsOnArtifact(GUID)"/>.
        /// </summary>
        /// <param name="backend"></param>
        /// <param name="importContext"></param>
        /// <returns>Returns true is all dependencies is already imported</returns>
        private static bool EnsureDependencies(BackendContext backend, AssetImportContext importContext)
        {
            var allDependenciesImported = true;
            foreach (var guid in backend.ArtifactDependencies)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                importContext.DependsOnArtifact(path);
                if (allDependenciesImported)
                {
                    var dependency = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
                    if (dependency == null)
                    {
                        allDependenciesImported = false;
                    }
                }
            }

            return allDependenciesImported;
        }
    }
}