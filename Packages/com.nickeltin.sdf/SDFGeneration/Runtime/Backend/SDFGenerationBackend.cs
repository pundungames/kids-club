using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace nickeltin.SDF.Runtime
{
    /// <summary>
    /// SDF might be generated in multiple way, inherit this class to create a new type of sdf generation.
    /// </summary>
    public abstract class SDFGenerationBackend
    {
        /// <summary>
        /// Some data associated with backend
        /// </summary>
        public struct BackendBaseData
        {
            /// <summary>
            /// This identifier used to save the current backend.
            /// </summary>
            public string Identifier;
            
            public string Description;

            public int InspectorSortOrder;
            
            /// <summary>
            /// Return property names form <see cref="SDFGenerationSettings"/> that is used by this backend.
            /// </summary>
            public string[] UsedProperties;

            public bool HideFromInspector;

            public bool Obsolete;

            public BackendBaseData(string identifier, string description, params string[] usedProperties)
            {
                Identifier = identifier;
                Description = description;
                UsedProperties = usedProperties;
                HideFromInspector = false;
                Obsolete = false;
                InspectorSortOrder = 0;
            }
        }
        
        /// <summary>
        /// Intermediate between <see cref="SDFGenerationSettings"/> and their adjusted version used directly in backend.
        /// </summary>
        public struct Settings
        {
            public SDFGenerationSettingsBase OriginalSettings;
            public float ResolutionScale;
            public Vector4 BorderOffset;
            public float GradientSize;

            public Settings(SDFGenerationSettingsBase originalSettings) : this(originalSettings, originalSettings.ResolutionScale, 
                Vector4.one * originalSettings.BorderOffset, originalSettings.GradientSize)
            {
            }

            public Settings(SDFGenerationSettingsBase originalSettings, float resolutionScale, Vector4 borderOffset, float gradientSize)
            {
                OriginalSettings = originalSettings;
                ResolutionScale = resolutionScale;
                BorderOffset = borderOffset;
                GradientSize = gradientSize;
            }
        }
        
        public Settings GetSettings(SDFGenerationSettingsBase settings, Vector4 borderOffset, 
            int width, int height, float textureScale)
        {
            var gradientSize = SDFGenerationInternalUtil.GetAdjustedGradientSize(settings.GradientSize, 
                width, height, borderOffset) * textureScale;

            var settings2 = new Settings(settings)
            {
                GradientSize = gradientSize,
                BorderOffset = borderOffset,
            };

            return settings2;
        }
        
        internal SDFGenerationBackend() { }

        public abstract BackendBaseData BaseData { get; }

        /// <summary>
        /// Return source asset GUIDs that this importer is depended on. This will trigger re-import if any of source asset is changed.
        /// For example if shader used for generation then yield its guid saved in a meta file here.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<string> GetArtifactDependencies()
        {
            yield break;
        }

        public abstract ProfilerMarker GetProfilerMarker();
        
        public virtual string GetDisplayName() => BaseData.Identifier;

        /// <summary>
        /// Implement backend SDF generation logic here.
        /// </summary>
        /// <param name="inOutTexture"></param>
        /// <param name="settings"></param>
        public abstract void Generate(Texture inOutTexture, Settings settings);

        public abstract Texture CopyTexture(Texture2D source);
        public abstract Texture CreateWorkingTexture(Texture source, RectInt area, Vector4 offset);
        public abstract Texture ResizeTexture(Texture source, int width, int height);
        public abstract Texture2D GetOutputTexture(Texture source);
    }
}