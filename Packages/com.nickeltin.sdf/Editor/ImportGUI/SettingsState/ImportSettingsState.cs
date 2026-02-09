using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using USelection = UnityEditor.Selection;
using UEditor = UnityEditor.Editor;

namespace nickeltin.SDF.Editor
{
    internal class ImportSettingsState : IDisposable, IReadOnlyCollection<SettingsContainer>
    {
        private readonly List<SettingsContainer> _containers;
        private UEditor _cachedEditor;
        private readonly TextureImporter _mainImporter;

        public event Action Changed;
        
        public bool SyncWithSelection { get; set; }
        
        
        public SettingsContainerEditor ContainersEditor => (SettingsContainerEditor)_cachedEditor;
        
        public ImportSettingsState(TextureImporter mainImporter, bool syncWithSelection)
        {
            SyncWithSelection = syncWithSelection;
            _mainImporter = mainImporter;
            _containers = new List<SettingsContainer>();
            USelection.selectionChanged += DoSyncWithSelection;
            DoSyncWithSelection();
        }

        public ImportSettingsState() : this(null, false)
        {
        }

        private void DoSyncWithSelection()
        {
            if (!SyncWithSelection)
            {
                return;
            }

            var newSelection = GetTextureImporters(
                USelection.GetFiltered<Texture>(SelectionMode.Assets | SelectionMode.TopLevel));
            
            if (_mainImporter != null)
            {
                newSelection.Add(_mainImporter);
            }

            if (newSelection.Count == 0)
            {
                return;
            }
            
            Update(newSelection, true, false);
        }

        public static HashSet<TextureImporter> GetTextureImporters(IEnumerable<Texture> textures)
        {
            return textures
                .Where(texture =>
                {
                    if (EditorUtility.IsPersistent(texture)) return true;
                    Debug.LogError($"{texture} is not persistent and importer can't be extracted");
                    return false;
                })
                .Select(AssetDatabase.GetAssetPath)
                .Select(AssetImporter.GetAtPath)
                .Cast<TextureImporter>()
                .ToHashSet();
        }
        
        private bool ImportersEquals(IEnumerable<TextureImporter> importers)
        {
            return _containers.Select(container => container.TextureImporter).SequenceEqual(importers);
        }
        
        private void UpdateEditor()
        {
            var entriesObjects = _containers.ToArray();
            UEditor.CreateCachedEditor(entriesObjects, typeof(SettingsContainerEditor), ref _cachedEditor);
        }
        
        public void Update(ICollection<TextureImporter> importers, bool updateEditor, bool forceUpdate)
        {
            if (!forceUpdate && ImportersEquals(importers))
            {
                return;
            }
            
            DisposeContainers();
            foreach (var importer in importers) _containers.Add(SettingsContainer.CreateForPreset(importer));
            if (updateEditor) UpdateEditor();
            Changed?.Invoke();
        }
        
        /// <summary>
        /// Will apply current write current state of import settings for each asset and reimport them in batch.
        /// </summary>
        public void Apply(bool reimport = true)
        {
            if (reimport)
            {
                try
                {
                    AssetDatabase.StartAssetEditing();
                    foreach (var sdfSettingsContainer in _containers) sdfSettingsContainer.Apply();
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
            }
            else
            {
                foreach (var sdfSettingsContainer in _containers) sdfSettingsContainer.Apply(false);
            }
        }

        /// <summary>
        /// Will reset serialized state of all import settings to initial state.
        /// </summary>
        public void Revert()
        {
            ContainersEditor.RevertChanges();
        }

        private void DisposeContainers()
        {
            foreach (var container in _containers)
            {
                Object.DestroyImmediate(container);
            }
            
            _containers.Clear();
            
            if (_cachedEditor != null)
            {
                Object.DestroyImmediate(_cachedEditor);
            }
        }
        
        private void ReleaseUnmanagedResources()
        {
            USelection.selectionChanged -= DoSyncWithSelection;
            DisposeContainers();
        }
        
        public int Count => _containers.Count;

        public IEnumerator<SettingsContainer> GetEnumerator() => _containers.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        ~ImportSettingsState() => ReleaseUnmanagedResources();

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Will capture state for of sdf import settings for textures, will not change with selection, will not update editor.
        /// </summary>
        public static ImportSettingsState CreateStaticState(IEnumerable<Texture> textures)
        {
            var instance = new ImportSettingsState();
            instance.Update(GetTextureImporters(textures), false, false);
            return instance;
        }
    }
}