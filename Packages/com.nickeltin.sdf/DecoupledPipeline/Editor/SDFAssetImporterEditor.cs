using nickeltin.SDF.Runtime;
using UnityEditor;
using UnityEditor.AssetImporters;

namespace nickeltin.SDF.Editor.DecoupledPipeline
{
    [CustomEditor(typeof(SDFAssetImporter)), CanEditMultipleObjects]
    internal class SDFAssetImporterEditor : ScriptedImporterEditor
    {
        public override bool showImportedObject => false;

        private SerializedProperty _texture;
        private SerializedProperty _importSettings;
        
        public override void OnEnable()
        {
            base.OnEnable();
            _texture = serializedObject.FindProperty(nameof(SDFAssetImporter._texture));
            _importSettings = serializedObject.FindProperty(nameof(SDFAssetImporter._importSettings));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                $"SDFAsset is part of \"Decoupled pipeline\". " + 
                $"It was made to expose {nameof(SDFSpriteMetadataAsset)} in editor since in regular pipeline they can't be visible." + 
                $"It is useful when you want to load sprites to SDFImage with resources or addressables." + 
                $"In unity 2023 this can be done with source sprites itself, since they carry all required metadata themselves", 
                MessageType.Info);
            
            serializedObject.Update();

            var hasMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = _texture.hasMultipleDifferentValues;
            EditorGUILayout.PropertyField(_texture);
            EditorGUI.showMixedValue = hasMixedValue;
            
            SDFGenerationSettingsDrawer.DrawLayout(_importSettings, false);
            
            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }
    }
}