using System.Collections.Generic;
using System.Linq;
using nickeltin.Core.Editor;
using nickeltin.InternalBridge.Editor;
using nickeltin.SDF.Runtime;
using UnityEditor;

namespace nickeltin.SDF.Editor
{
    [CustomEditor(typeof(SettingsContainer))]
    internal partial class SettingsContainerEditor : UnityEditor.Editor
    {
        private SerializedProperty _settings;
        private SerializedObject _initialState;
        
        /// <summary>
        /// Is serialized object state different from original?
        /// </summary>
        public bool HasChanges => !_SerializedObject.DataEquals(serializedObject, _initialState);

        /// <summary>
        /// Will set initial state object to the current state
        /// </summary>
        public void UpdateInitialState()
        {
            _initialState?.Dispose();
            _initialState = new SerializedObject(targets);
            _initialState.Update();
        }
        
        public SerializedProperty Settings => _settings;

        /// <summary>
        /// Reverts serialized object to initial state
        /// </summary>
        public void RevertChanges()
        {
            _initialState.ApplyModifiedProperties();
        }
        
        private void OnEnable()
        {
            _settings = serializedObject.FindProperty(nameof(SettingsContainer.Settings));
          
            UpdateInitialState();
        }

        private void OnDisable()
        {
            _initialState.Dispose();
        }

        public override void OnInspectorGUI() => DrawGUIForImporter();
        

        public void DrawGUIForImporter()
        {
            serializedObject.Update();

            var isPreset = AnyTargetIsPreset();
            SDFGenerationSettingsDrawer.DrawLayoutForImporter(_settings, !isPreset, 
                GetImporters().ToArray(), GetImportSettings().ToArray());
            
            
            serializedObject.ApplyModifiedProperties();
        }
        
        public void ResetToDefaults()
        {
            Undo.RecordObjects(serializedObject.targetObjects, "Setting import settings to default");
            Settings.SetValues(new SDFGenerationSettings());
            serializedObject.ApplyModifiedProperties();
        }

        public IEnumerable<SettingsContainer> GetTargets() => targets.Cast<SettingsContainer>();

        public bool AnyTargetIsPreset()
        {
            return GetTargets().Any(container => container.IsPreset());
        }
        
        public IEnumerable<TextureImporter> GetImporters()
        {
            return GetTargets().Select(container => container.TextureImporter);
        }
        
        public IEnumerable<SDFGenerationSettings> GetImportSettings()
        {
            return GetTargets().Select(container => container.Settings);
        }
        
        public void Apply(bool reimport = true)
        {
            foreach (var container in GetTargets())
            {
                container.Apply(reimport);
            }
        }
    }
}