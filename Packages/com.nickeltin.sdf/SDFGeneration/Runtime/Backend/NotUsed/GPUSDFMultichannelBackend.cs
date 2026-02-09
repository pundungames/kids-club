using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

namespace nickeltin.SDF.Runtime
{
    /// <summary>
    /// Idea was to generate some kind of sdf-like map for all the channels to draw them then smoothly, like they were a vector image.
    /// But that turned up much more complicated...
    ///
    /// Currently, don't do anything more than <see cref="SDFGPUBackend"/>
    /// </summary>
    internal class GPUSDFMultichannelBackend : SDFGPUBackend
    {
        public const bool ENABLED = false; 
        
        private static readonly ProfilerMarker _profilerMarker = new(nameof(GPUSDFMultichannelBackend));

        [ForceIncludeShaderInBuild]
        public static readonly ShaderReference RBGGenerationShader = new(
            "5e01f6c7e0e7429a8aada9389d9fc8e3", 
            "Hidden/nickeltin/SDF/RGBSDFGenerator")
        {
            IncludeShaderInBuild = ENABLED
        };
        
        [ForceIncludeShaderInBuild]
        public static readonly ShaderReference CopyChannelShader = new( 
            "60ceaa0450ac4bffb22ce450a45d778a",
            "Hidden/nickeltin/SDF/CopyChannel")
        {
            IncludeShaderInBuild = ENABLED
        };
        
        protected static readonly int SRC_CHANNEL_ID = Shader.PropertyToID("_SourceChannel");
        protected static readonly int DEST_CHANNEL_ID = Shader.PropertyToID("_DestChannel");
        protected static readonly int DEST_TEX_ID = Shader.PropertyToID("_DestTex");
        
        public override BackendBaseData BaseData
        {
            get
            {
                var result = base.BaseData;
                result.Identifier = "legacy-multichannel";
                result.Description = "Uses Jump Flood Algorithm to generate SDF on GPU with far all RGBA channels";
                result.HideFromInspector = !ENABLED;
                result.InspectorSortOrder++;
                return result;
            }
        }

        public override ProfilerMarker GetProfilerMarker() => _profilerMarker;

        public override void Generate(Texture texInOut, Settings settings)
        {
            VerifyGPUAvailability(texInOut);
            GenerateARGBChannels((RenderTexture)texInOut, settings.GradientSize);
        }

        public override IEnumerable<string> GetArtifactDependencies()
        {
            return base.GetArtifactDependencies().Append(CopyChannelShader.GUID);
        }

        public override string GetDisplayName() => "GPU Multichannel";
        
        /// <summary>
        /// TODO: finish
        ///
        /// So the theory is that other channels can be filled with difference map (or sdf as well) to use this
        /// difference data to detect color islands and how one island if flown into the other to draw crisp edges where needed.
        /// The bigger the difference in that channel - higher crispness 
        /// 
        /// </summary>
        /// <param name="rtInOut"></param>
        /// <param name="gradientSize"></param>
        public static void GenerateARGBChannels(RenderTexture rtInOut, float gradientSize)
        {
            Debug.Log("Generating ARGB Channels");
            var resultTarget = RenderTexture.GetTemporary(rtInOut.descriptor);
            resultTarget.wrapMode = TextureWrapMode.Clamp;
            
            var stepOut = RenderTexture.GetTemporary(rtInOut.descriptor);
            stepOut.wrapMode = TextureWrapMode.Clamp;
            
            // var stepOut2 = RenderTexture.GetTemporary(rtInOut.descriptor);
            // stepOut2.wrapMode = TextureWrapMode.Clamp;
            
            // Only alpha channel
            var genMat = GenerationShader.CreateMaterial();
            genMat.SetInt(CHANNEL_ID, 3);
            GenerateChannel(rtInOut, stepOut, genMat, gradientSize, true);
            CopyChannel(stepOut, resultTarget, 3);
            Object.DestroyImmediate(genMat);
            
            // Red channel
            // var rgbGenMat = RBGGenerationShader.CreateMaterial();
            // rgbGenMat.SetTexture(DEST_TEX_ID, rtInOut);
            // rgbGenMat.SetInt(CHANNEL_ID, 0);
            // rgbGenMat.SetFloat(SPREAD_ID, 1);
            // Graphics.Blit(rtInOut, stepOut, rgbGenMat);
            // // CopyChannel(stepOut, resultTarget, 0);
            // Object.DestroyImmediate(rgbGenMat);
            //
            // genMat = GenerationShader.CreateMaterial();
            // genMat.SetInt(CHANNEL_ID, 0);
            // GenerateChannel(stepOut, stepOut2, genMat, gradientSize, true);
            // CopyChannel(stepOut2, resultTarget, 0);
            // Object.DestroyImmediate(genMat);
            
            
            
            Graphics.CopyTexture(resultTarget, rtInOut);
            RenderTexture.ReleaseTemporary(stepOut);
            // RenderTexture.ReleaseTemporary(stepOut2);
            RenderTexture.ReleaseTemporary(resultTarget);
        }
        
        
        public static void CopyChannel(Texture src, RenderTexture dest, int channel)
        {
            CopyChannel(src, channel, dest, channel);
        }
        public static void CopyChannel(Texture src, int srcChannel, RenderTexture dest, int destChannel)
        {
            var tempInput = RenderTexture.GetTemporary(dest.descriptor);
            Graphics.Blit(dest, tempInput);
            
            var copyMat = CopyChannelShader.CreateMaterial();
            copyMat.SetTexture(DEST_TEX_ID, tempInput);
            copyMat.SetInt(SRC_CHANNEL_ID, srcChannel);
            copyMat.SetInt(DEST_CHANNEL_ID, destChannel);
            
            Graphics.Blit(src, dest, copyMat);
            RenderTexture.ReleaseTemporary(tempInput);
            Object.DestroyImmediate(copyMat);
        }

    }
}