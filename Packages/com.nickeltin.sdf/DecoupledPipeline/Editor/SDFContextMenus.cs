using System.Linq;
using nickeltin.Core.Editor;
using nickeltin.SDF.Editor.DecoupledPipeline;
using nickeltin.SDF.Runtime.DecoupledPipeline;
using UnityEditor;
using UnityEngine;

namespace nickeltin.SDF.Editor
{
    internal static partial class SDFContextMenus
    {
        [MenuItem("Assets/Create/2D/SDF Asset")]
        private static void CreateSDFAsset()
        {
            var serializedAsset = EditorSerialization.SerializeObjectToString<SDFAssetSourceAsset>();
            AssetCreator.CreateWithContent($"New SDF Asset.{SDFAssetImporter.EXT}", serializedAsset, null);
        }
        
        [MenuItem(MenuPaths.TOOLBAR + "Find all textures with generated SDF")]
        private static void FindAllTextureWithGeneratedSDF()
        {
            var texturePaths = SDFEditorUtil.FindAllTexturesWithGeneratedSDF()
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var textures = texturePaths
                .Select(AssetDatabase.LoadAssetAtPath<Object>)
                .ToArray();
            
            if (textures.Length == 0)
            {
                Debug.Log($"Textures with generated SDF were not found");
            }
            else
            {
                Debug.Log($"Found {textures.Length} textures with generated SDF, " +
                          $"list:\n{string.Join("\n", texturePaths)}");
                Selection.objects = textures;
            }
        }
        
        [MenuItem("CONTEXT/TextureImporter/Find SDFAssets that uses this texture")]
        private static void FindSDFAssetsForTexture(MenuCommand command)
        {
            var texImporter = (TextureImporter)command.context;
            var texGUID = AssetDatabase.GUIDFromAssetPath(texImporter.assetPath).ToString();
            var sdfAssetsGUIDs = SDFEditorUtil.FindSDFAssetsThatUsesTexture(texGUID);
            var sdfAssetsPaths = sdfAssetsGUIDs
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();
            var sdfAssets = sdfAssetsPaths
                .Select(AssetDatabase.LoadAssetAtPath<SDFAsset>)
                .ToArray();

            if (sdfAssets.Length == 0)
            {
                Debug.Log($"SDFAsset's that uses texture {texImporter.assetPath} were not found", command.context);
            }
            else
            {
                Debug.Log($"Found {sdfAssets.Length} SDFAsset's that uses texture {texImporter.assetPath}, " +
                          $"list: \n{string.Join("\n", sdfAssetsPaths)}", command.context);
                Selection.objects = sdfAssets;
            }
        }
    }
}