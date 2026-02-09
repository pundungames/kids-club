using System;
using System.Collections.Generic;
using nickeltin.SDF.Runtime;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using static nickeltin.SDF.Runtime.SDFGenerationUtil;

namespace nickeltin.SDF.Editor
{
    public static class SDFGenerationEditorUtil
    {
        /// <summary>
        /// Automatic backend selection dummy class, basically used just for gui.
        /// None of the methods should be called directly
        /// </summary>
        private class SDFAutoBackend : SDFGenerationBackend
        {
            public override BackendBaseData BaseData { get; } = new(AUTO_EDITOR_BACKEND_ID,
                "Uses GPU backend if machine have it, else uses CPU backend", 
                nameof(SDFGenerationSettingsBase.GradientSize));
            
            public override ProfilerMarker GetProfilerMarker() => default;

            public override string GetDisplayName() => "Automatic";
            public override void Generate(Texture inOutTexture, Settings settings) => throw new NotSupportedException();

            public override Texture CopyTexture(Texture2D source) => throw new NotSupportedException();

            public override Texture CreateWorkingTexture(Texture source, RectInt area, Vector4 offset) => throw new NotSupportedException();

            public override Texture ResizeTexture(Texture source, int width, int height) => throw new NotSupportedException();

            public override Texture2D GetOutputTexture(Texture source) => throw new NotSupportedException();
        }
        
        /// <summary>
        /// Wrapper to make interaction with the backend easier
        /// </summary>
        public readonly struct BackendContext
        {
            private readonly HashSet<string> _usedProperties;
            private readonly HashSet<string> _artifactDependencies;

            public BackendContext(SDFGenerationBackend backend)
            {
                Backend = backend;
                _usedProperties = new HashSet<string>();
                _artifactDependencies = new HashSet<string>();
                foreach (var prop in backend.BaseData.UsedProperties)
                {
                    _usedProperties.Add(prop);
                }

                foreach (var guid in backend.GetArtifactDependencies())
                {
                    _artifactDependencies.Add(guid);
                }
            }

            public SDFGenerationBackend Backend { get; }

            public int DisplayOrder
            {
                get
                {
                    if (IsDefault)
                        return -1000;
                    
                    return Backend.BaseData.InspectorSortOrder;
                }
            }

            public bool IsDefault => Backend.BaseData.Identifier == AUTO_EDITOR_BACKEND_ID;

            public string DisplayName
            {
                get
                {
                    var result = Backend.GetDisplayName();
                    if (IsDefault) result += " (Default)";
                    return result;
                }
            }

            public IEnumerable<string> ArtifactDependencies => _artifactDependencies;

            public bool IsPropertyUsed(string propName) => _usedProperties.Contains(propName);
        }
        
        /// <summary>
        /// Access backends here. Creates all backend instances upon first access.
        /// </summary>
        public static class BackendProvider
        {
            private static Dictionary<string, BackendContext> _backends;

            static BackendProvider() => Init();

            private static void Init()
            {
                _backends = new Dictionary<string, BackendContext>();

                // AutoRegisterBackends();
                RegisterBackend(new SDFAutoBackend());
                RegisterBackend(DefaultGPUBackend);
                RegisterBackend(DefaultCPUBackend);
            }

            private static void AutoRegisterBackends()
            {
                var backendsTypes = TypeCache.GetTypesDerivedFrom<SDFGenerationBackend>();
                foreach (var type in backendsTypes)
                {
                    if (type.IsAbstract || type.IsGenericType) continue;
                
                    var instance = (SDFGenerationBackend)Activator.CreateInstance(type);
                    RegisterBackend(instance);
                }
            }

            private static void RegisterBackend(SDFGenerationBackend backend)
            {
                var id = backend.BaseData.Identifier;
                var context = new BackendContext(backend);
                if (!_backends.TryAdd(id, context))
                {
                    Debug.LogError($"Multiple {nameof(SDFGenerationBackend)}'s with identifier: {id} registered");
                }
            }
            
            public static IEnumerable<BackendContext> GetBackends() => _backends.Values;
            
            public static BackendContext GetBackendForGUI(string backendId)
            {
                return backendId switch
                {
                    // We either have defined gpu or cpu id's or just match depending on the current gpu availability
                    SDFGPUBackend.ID => _backends[SDFGPUBackend.ID],
                    SDFCPUBackend.ID => _backends[SDFCPUBackend.ID],
                    _ => _backends[AUTO_EDITOR_BACKEND_ID]
                };
            }

            public static BackendContext GetBackendForGeneration(string backendId)
            {
                // We either have defined gpu or cpu id's or just match depending on the current gpu availability
                var gpu = _backends[SDFGPUBackend.ID];
                if (backendId == SDFGPUBackend.ID) return gpu;

                var cpu = _backends[SDFCPUBackend.ID];
                if (backendId == SDFCPUBackend.ID) return cpu;
                
                return IsGPUAvailable() ? gpu : cpu;
            }
        }
        
        public static float GetImportedTextureScale(int sourceTexW, int importedTexW)
        {
            return (float)importedTexW / sourceTexW;
        }
        
        public static float GetImportedTextureScale(TextureImporter importer, Texture importedTexture)
        {
            importer.GetSourceTextureWidthAndHeight(out var sourceW, out _);
            return GetImportedTextureScale(sourceW, importedTexture.width);
        }
        
        /// <summary>
        /// Border offset needs to be adjusted for imported texture. Border offset is configured for the source texture size, and the
        ///  imported texture size might be smaller. 
        /// </summary>
        public static Vector4 GetAdjustedBorderOffset(TextureImporter importer, Texture tex, int borderOffset)
        {
            return SDFGenerationInternalUtil.GetBorderOffset(GetImportedTextureScale(importer, tex), borderOffset);
        }

        public static void MakeReadable(TextureImporter importer)
        {
            importer.isReadable = true;
            EditorUtility.SetDirty(importer);
            AssetDatabase.WriteImportSettingsIfDirty(importer.assetPath);
            AssetDatabase.ImportAsset(importer.assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            // importer.SaveAndReimport();
        }
    }
}