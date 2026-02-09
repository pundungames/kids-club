using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace nickeltin.SDF.Runtime
{
    /// <summary>
    /// Main sdf generation backend.
    /// For provided Render Texture will generate sdf textures for each channel (rgba) with one material
    /// and then combine them all channels to a single texture with a second material.
    /// In order for backend to work <see cref="GenerationShader"/> shader should be in project
    /// with corresponding GUIDs defined in <see cref="GetArtifactDependencies"/>
    /// </summary>
    public class SDFGPUBackend : SDFGenerationBackend
    {
        public class ShaderReference : ForceIncludeShaderInBuildAttribute.IShaderNameProvider
        {
            private Shader _shader;
            
            public readonly string GUID;
            public readonly string Name;
            public bool IncludeShaderInBuild = true;

            public ShaderReference(string guid, string name)
            {
                GUID = guid;
                Name = name;
            }

            public Shader Shader
            {
                get
                {
                    if (_shader == null)
                    {
                        // For some reason shader with Shader.Find might return null, so manually loading shader as asset.
                        // Note: not sure that this will always work due to the shader might not be compiled at that point...
                        
#if UNITY_EDITOR
                        _shader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(UnityEditor.AssetDatabase.GUIDToAssetPath(GUID));
#else
                        _shader = Shader.Find(Name);
#endif
                    }
            
                    return _shader;
                }
            }

            public Material CreateMaterial() => new(Shader);
            public string GetShaderName() => Name;
            public bool ShouldIncludeShaderInBuild() => IncludeShaderInBuild;
        }
        
        public const string ID = "gpu";
        
        private static readonly ProfilerMarker _profilerMarker = new(nameof(SDFGPUBackend));

        [ForceIncludeShaderInBuild]
        public static readonly ShaderReference GenerationShader = new(
            "662d200f77a9a0b40af3f32a96f5bc3e",
            "Hidden/nickeltin/SDF/SDFGenerator");

        internal SDFGPUBackend() { }

        public override BackendBaseData BaseData { get; } = new(
            ID,
            "Fastest SDF Generation, but requires GPU to work.\nUses Jump Flood Algorithm to generate SDF on GPU",
            nameof(SDFGenerationSettings.GradientSize));
        
        public override ProfilerMarker GetProfilerMarker() => _profilerMarker;

        public override string GetDisplayName() => "GPU";


        protected static readonly int CHANNEL_ID = Shader.PropertyToID("_Channel");
        protected static readonly int FEATHER_ID = Shader.PropertyToID("_Feather");
        protected static readonly int SPREAD_ID = Shader.PropertyToID("_Spread");
        protected static readonly int SOURCE_TEX_ID = Shader.PropertyToID("_SourceTex");
        // protected static readonly int SOLID_THRESHOLD_ID = Shader.PropertyToID("_SolidThreshold");
        
        public override IEnumerable<string> GetArtifactDependencies()
        {
            // This is SDFGenerator shader GUID, it needed to be added as a dependency because texture can be imported before shader.
            // If shader is a dependency, it will trigger reimport on texture when shader is imported. 
            yield return GenerationShader.GUID;
        }

        protected static void VerifyGPUAvailability(Texture texInOut)
        {
            if (!SDFGenerationUtil.IsGPUAvailable())
            {
                Debug.LogError($"{nameof(SDFGPUBackend)} is requires GPU to function properly. SDF Generation for texture {texInOut.name} will be unsuccessful. " +
                               $"Consider using {nameof(SDFCPUBackend)} if your machine don't have access to GPU.");
            }
        }
        
        public override void Generate(Texture texInOut, Settings settings)
        {
            VerifyGPUAvailability(texInOut);
            GenerateSingleChannel((RenderTexture)texInOut, settings.GradientSize);   
        }
        
        public override Texture CopyTexture(Texture2D source)
        {
            return SDFGenerationInternalUtil.CopyTextureGPU(source);
        }

        public override Texture CreateWorkingTexture(Texture source, RectInt area, Vector4 borderOffset)
        {
            return SDFGenerationInternalUtil.CreateWorkingTextureGPU(source, area, borderOffset);
        }
        
        public override Texture ResizeTexture(Texture source, int width, int height)
        {
            return SDFGenerationInternalUtil.ResizeTextureGPU((RenderTexture)source, width, height);
        }
        
        public override Texture2D GetOutputTexture(Texture source)
        {
            return SDFGenerationInternalUtil.GetOutputTextureGPU((RenderTexture)source);
        }

        /// <inheritdoc cref="Generate"/>
        /// <remarks>
        ///     Uses only an alpha channel
        /// </remarks>
        public static void GenerateSingleChannel(RenderTexture rtInOut, float gradientSize)
        {
            var genMat = GenerationShader.CreateMaterial();
            genMat.SetInt(CHANNEL_ID, 3);
            // genMat.SetFloat(SOLID_THRESHOLD_ID, 0.5f);
            GenerateChannel(rtInOut, rtInOut, genMat, gradientSize, true);
            Object.DestroyImmediate(genMat);
        }
        
        public static void GenerateChannel(RenderTexture rtIn, RenderTexture rtOut, Material material, float gradientSize, bool sRGB)
        {
            material.SetFloat(FEATHER_ID, gradientSize);
            // Allocate some temporary buffers
            var stepFormat = new RenderTextureDescriptor(rtIn.width, rtIn.height, 
                GraphicsFormat.R16G16B16A16_UNorm, 0, 0);
            stepFormat.sRGB = false;

            var target1 = RenderTexture.GetTemporary(stepFormat);
            var target2 = RenderTexture.GetTemporary(stepFormat);
            
            
            target1.filterMode = FilterMode.Point;
            target2.filterMode = FilterMode.Point;
            target1.wrapMode = TextureWrapMode.Clamp;
            target2.wrapMode = TextureWrapMode.Clamp;

            const int firstPass = 0;
            var finalPass = material.FindPass("FinalPass");

            // Detect edges of an image
            material.EnableKeyword("FIRSTPASS");
            material.SetFloat(SPREAD_ID, 1);
            Graphics.Blit(rtIn, target1, material, firstPass);
            material.DisableKeyword("FIRSTPASS");
            
            Swap(ref target1, ref target2);
            
            // Gather the nearest edges with varying spread values
            for (var i = 11; i >= 0; i--)
            {
                material.SetFloat(SPREAD_ID, Mathf.Pow(2, i));
                Graphics.Blit(target2, target1, material, firstPass);
                Swap(ref target1, ref target2);
            }

            var resultFormat = new RenderTextureDescriptor(rtIn.width, rtIn.height, 
                GraphicsFormat.R8G8B8A8_UNorm, 0, 0);
            resultFormat.sRGB = sRGB;


            var resultTarget = RenderTexture.GetTemporary(resultFormat);
            resultTarget.wrapMode = TextureWrapMode.Clamp;

            // Compute the final distance from the nearest edge value
            material.SetTexture(SOURCE_TEX_ID, rtIn);
            Graphics.Blit(target2, resultTarget, material, finalPass);
            
            Graphics.CopyTexture(resultTarget, rtOut);
            
            // Clean up
            RenderTexture.ReleaseTemporary(resultTarget);
            RenderTexture.ReleaseTemporary(target2);
            RenderTexture.ReleaseTemporary(target1);
        }
        
        private static void Swap<T>(ref T v1, ref T v2) => (v1, v2) = (v2, v1);
    }
}