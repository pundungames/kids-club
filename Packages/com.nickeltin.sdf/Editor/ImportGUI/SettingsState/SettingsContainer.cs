using nickeltin.SDF.Runtime;
using UnityEditor;
using UnityEngine;

namespace nickeltin.SDF.Editor
{
    internal class SettingsContainer : ScriptableObject
    {
        public SDFGenerationSettings Settings = new();
        public TextureImporter TextureImporter;

        public static SettingsContainer CreateForPreset(TextureImporter importer)
        {
            var container = CreateInstance<SettingsContainer>();
            container.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            container.TextureImporter = importer;
            container.LoadSettings();
            return container;
        }

        public void LoadSettings()
        {
            SDFEditorUtil.ShouldImportSDF(TextureImporter, out Settings);
        }

        /// <summary>
        /// Saves current settings state
        /// </summary>
        public void Apply(bool reimport = true)
        {
            SDFEditorUtil.SaveGenerationSettings(TextureImporter, Settings);
            // SDFGenerationSettings.Save(TextureImporter, Settings);

            // Texture importer edited in preset will be the importer with this path
            if (IsPreset()) return;

            if (!reimport) return;
            
            TextureImporter.SaveAndReimport();
        }

        public bool IsPreset()
        {
            return TextureImporter.assetPath == "temp/preset";
        }
    }
}