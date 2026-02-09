using System.Collections;
using System.Collections.Generic;
using System.Linq;
using nickeltin.SDF.InternalBridge.Editor;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UEditor = UnityEditor.Editor;

namespace nickeltin.SDF.Editor
{
    /// <summary>
    /// Header gui for displaying import sdf toggle, and sdf importer button.
    /// Fallback for when <see cref="TextureImporterEditor"/> for some reason not drawn.
    /// </summary>
    internal static class TextureImporterHeaderGUI
    {
        private static class Defaults
        {
            public static readonly GUIContent FallbackInfo;
            public static readonly GUIContent MultiSelectPresets;
            
            static Defaults()
            {
                FallbackInfo = new GUIContent(EditorGUIUtility.IconContent("console.infoicon.sml"))
                {
                    text = "",
                    tooltip = "SDF settings is drawn in header since TextureImporterInspector override failed to take control. " +
                              "Maybe you have another Editor assigned for TextureImporter in project?"
                };
            }
        }
        
        private readonly struct EditorDrawData : IEnumerable<TextureImporter>
        {
            public readonly AssetImporterEditor ImporterEditor;
            public readonly TextureImporter FirstImporter;
            public readonly bool FirstTextureValid;
            
            public IEnumerable<TextureImporter> Importers => ImporterEditor.targets.Cast<TextureImporter>();

            public EditorDrawData(AssetImporterEditor importerEditor)
            {
                ImporterEditor = importerEditor;
                FirstImporter = (TextureImporter)ImporterEditor.target;
                FirstTextureValid = SDFEditorUtil.IsValidTextureType(FirstImporter);
            }

            public int Count => ImporterEditor.targets.Length;
            
            public TextureImporter this[int i] => (TextureImporter)ImporterEditor.targets[i];
            public IEnumerator<TextureImporter> GetEnumerator() => Importers.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        
        private static bool _isDirty = true;
        private static readonly TextureImporterEditor.AllChangeChecks _changeCheck = new();
        private static readonly ImportSettingsState _state = new();
        
        [InitializeOnLoadMethod]
        private static void Init()
        {
            UEditor.finishedDefaultHeaderGUI += HeaderGUI;
            Selection.selectionChanged += SelectionChanged;
        }
        
        private static void SelectionChanged()
        {
            _isDirty = true;
        }

        private static void UpdateState(EditorDrawData drawData)
        {
            _state.Update(drawData.Importers.ToArray(), true, true);
            _isDirty = false;
        }
        
        
        private static void HeaderGUI(UEditor obj)
        {
            if (!_TextureImporterInspector.IsInstance(obj)) return;
            
            // Dealing with instance of our own editor, don't need to draw any additional settings.
            if (obj is TextureImporterEditor) return;
            
            if (obj.targets.Any(o => !AssetDatabase.IsOpenForEdit(o))) return;
            
            var drawData = new EditorDrawData((AssetImporterEditor)obj);
            
            if (_isDirty) UpdateState(drawData);
            
            using (new EditorGUI.DisabledScope(!drawData.FirstTextureValid))
            using (new EditorGUILayout.HorizontalScope())
            {
                var labelRect = EditorGUILayout.GetControlRect(GUILayout.Height(24), GUILayout.Width(16));
                GUI.Label(labelRect, Defaults.FallbackInfo);
                _state.ContainersEditor.DrawGUIForImporter();
            }

            // Check is sdf import settings has changed, if so apply it to importers 
            if (_state.ContainersEditor.HasChanges)
            {
                _state.ContainersEditor.UpdateInitialState();
                _state.ContainersEditor.Apply(false);
            }

            if (_changeCheck.SetNewDataAndIterate(drawData.ImporterEditor))
            {
                _isDirty = true;
            }
        }
    }
}