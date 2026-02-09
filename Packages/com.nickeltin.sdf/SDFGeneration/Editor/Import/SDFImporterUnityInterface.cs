using System.IO;
using nickeltin.SDF.Editor.DecoupledPipeline;
using nickeltin.SDF.Runtime;
using UnityEditor;
using UnityEngine;

namespace nickeltin.SDF.Editor
{
    /// <summary>
    /// Interface to receive callbacks from unity import pipeline
    /// </summary>
    internal class SDFImporterUnityInterface : AssetPostprocessor
    {
        private const uint INTERFACE_VERSION = 16;
        private SDFImporter _sdfImporter;


        public override int GetPostprocessOrder()
        {
            return SDFEditorUtil.SDF_POSTPROCESSOR_ORDER;
        }

        /// <summary>
        /// Incrementing version of importer will cause all textures to re-import
        /// </summary>
        public override uint GetVersion()
        {
            var result = INTERFACE_VERSION + SDFImporter.VERSION;

            // Multiplying by two to ensure unique importer version to reimport if changing unity versions
            if (SDFUtil.IsNewSpriteMetadataEnabled)
            {
                result *= 2;
            }
            
            return result;
        }
        
        private void OnPreprocessTexture()
        {
            var texImporter = assetImporter as TextureImporter;
            if (texImporter != null)
            {
                // if (!texImporter.isReadable)
                // {
                //     Debug.Log($"{texImporter.assetPath} is not readable, making readable, and re-importing it.");
                //     SDFGenerationEditorUtil.MakeReadable(texImporter);
                //     return;
                // }
                
                _sdfImporter = SDFImporter.CreateRegular(texImporter, context);
                _sdfImporter.Step();
            }
        }
        
        public void OnPostprocessTexture(Texture2D texture)
        {
            if (_sdfImporter != null)
            {
                _sdfImporter.SetTexture(texture);
                _sdfImporter.Step();
            }
        }
        
       
        private void OnPostprocessSprites(Texture2D texture, Sprite[] sprites)
        {
            if (_sdfImporter != null)
            {
                _sdfImporter.SetSprites(sprites);
                _sdfImporter.Step();
            }

            _sdfImporter = null;
        }
        
       

        /// <summary>
        /// Working with postprocessor to invoke processing event from main thread after all assets was imported.
        /// </summary>
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var importedAsset in importedAssets)
            {
                // We're dealing with sdf asset, its easy, just invoke the event
                // Adding "." since returned extension is not cleared from it
                if (Path.GetExtension(importedAsset) == "." + SDFAssetImporter.EXT)
                {
                    SDFEditorUtil.SubmitImportedSDF(importedAsset);
                }
                // Else check for asset to be a texture and to have valid SDF import settings
                else if (SDFEditorUtil.ShouldImportSDF(importedAsset, out _, out _))
                {
                    SDFEditorUtil.SubmitImportedSDF(importedAsset);
                }
            }
        }
    }
}