using System.Linq;
using nickeltin.SDF.InternalBridge.Editor;
using UnityEditor;

namespace nickeltin.SDF.Editor
{
    /// <summary>
    /// Override for default <see cref="TextureImporterInspector"/> that displays sdf import settings.
    /// If editor fails to display gui for sdf import settings fallbacks to <see cref="TextureImporterHeaderGUI"/>
    /// </summary>
    [CustomEditor(typeof(TextureImporter)), CanEditMultipleObjects]
    internal partial class TextureImporterEditor : _TextureImporterInspector
    {
        private ImportSettingsState _state;
        private readonly AllChangeChecks _changeCheck = new();
        
        public override void OnEnable()
        {
            base.OnEnable();
            _state = new ImportSettingsState();
            UpdateImportState();
            
            RegisterGUIMethod(_TextureInspectorGUIElement.Sprite, AfterSpriteGUI);
        }

        public override void OnDisable()
        {
            base.OnDisable();
            _state?.Dispose();
        }

        private void UpdateImportState()
        {
            _state.Update(GetImporters().ToArray(), true, true);
            Repaint();
        }

        private void AfterSpriteGUI(_TextureInspectorGUIElement elementType)
        {
            if (!SDFEditorUtil.IsValidTextureType((TextureImporter)target)) return;
            
            using (new EditorGUI.IndentLevelScope())
            {
                _state.ContainersEditor.DrawGUIForImporter();
            }
            
            if (_state.ContainersEditor.HasChanges)
            {
                _state.ContainersEditor.UpdateInitialState();
                _state.Apply(false);
            }
            
            if (_changeCheck.SetNewDataAndIterate(this))
            {
                UpdateImportState();
            }
        }
    }
}