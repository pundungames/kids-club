using Unity.Profiling;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

namespace nickeltin.SDF.Runtime
{
    public class SDFCPUBackend : SDFGenerationBackend
    {
        private static class ProfilerMarkers
        {
            public static readonly ProfilerMarker CpuJobsBackendMarker = new(nameof(SDFCPUBackend));
            public static readonly ProfilerMarker EdgeDetectionPassMarker = new(nameof(EdgeDetectionPass));
            public static readonly ProfilerMarker JumpFloodPassMarker = new(nameof(JumpFloodPass));
            public static readonly ProfilerMarker FinalSdfPassMarker = new(nameof(FinalSDFPass));
        }
        
        public const string ID = "cpu";

        internal SDFCPUBackend() { }

        public override BackendBaseData BaseData { get; } = new(
            ID,
            "Uses Jump Flood Algorithm to generate SDF on CPU with Jobs.\n" +
            "Not as fast as GPU, for cases when you don't have access to CPU.",
            nameof(SDFGenerationSettings.GradientSize));

        public override ProfilerMarker GetProfilerMarker() => ProfilerMarkers.CpuJobsBackendMarker;

        public override string GetDisplayName() => "CPU";

        public override void Generate(Texture texInOut, Settings settings)
        {
            Generate((Texture2D)texInOut, settings.GradientSize);
        }

        public override Texture CopyTexture(Texture2D source)
        {
            return SDFGenerationInternalUtil.CopyTextureCPU(source);
        }

        public override Texture CreateWorkingTexture(Texture source, RectInt area, Vector4 borderOffset)
        {
            return SDFGenerationInternalUtil.CreateWorkingTextureCPU(source, area, borderOffset);
        }
        
        public override Texture ResizeTexture(Texture source, int width, int height)
        {
            return SDFGenerationInternalUtil.ResizeTextureCPU((Texture2D)source, width, height);
        }
        
        public override Texture2D GetOutputTexture(Texture source)
        {
            return (Texture2D)source;
        }
        
        public static void Generate(Texture2D texInOut, float gradientSize)
        {
            var width = texInOut.width;
            var height = texInOut.height;
            var pixelCount = width * height;
            var colorData = texInOut.GetPixelData<Color32>(0);
            var pixels = new NativeArray<Color32>(colorData.Length, Allocator.TempJob);
            pixels.CopyFrom(colorData);

            var solidMask = new NativeArray<bool>(pixelCount, Allocator.TempJob);
            var edgePing = new NativeArray<float2>(pixelCount, Allocator.TempJob);
            var edgePong = new NativeArray<float2>(pixelCount, Allocator.TempJob);

            ProfilerMarkers.EdgeDetectionPassMarker.Begin();
            new EdgeDetectionPass
            {
                width = width,
                height = height,
                pixels = pixels,
                solidMask = solidMask,
                edgeOut = edgePing
            }.Schedule(pixelCount, 64).Complete();
            ProfilerMarkers.EdgeDetectionPassMarker.End();
            
            edgePing.CopyTo(edgePong);

            var maxSteps = (int)math.ceil(math.log2(math.max(width, height)));
            for (var i = maxSteps; i >= 0; i--)
            {
                var offset = 1 << i;
                ProfilerMarkers.JumpFloodPassMarker.Begin();
                new JumpFloodPass
                {
                    width = width,
                    height = height,
                    offset = offset,
                    inputEdge = edgePing,
                    outputEdge = edgePong
                }.Schedule(pixelCount, 64).Complete();
                ProfilerMarkers.JumpFloodPassMarker.End();

                (edgePing, edgePong) = (edgePong, edgePing);
            }

            var pixelGradientSize = gradientSize * math.max(width, height);

            ProfilerMarkers.FinalSdfPassMarker.Begin();
            new FinalSDFPass
            {
                width = width,
                gradientSize = pixelGradientSize,
                solidMask = solidMask,
                edge = edgePing,
                pixels = pixels
            }.Schedule(width * height, 64).Complete();
            ProfilerMarkers.FinalSdfPassMarker.End();

            texInOut.LoadRawTextureData(pixels);
            texInOut.Apply();
            
            solidMask.Dispose();
            edgePing.Dispose();
            edgePong.Dispose();
            pixels.Dispose(); //Is it need to be disposed?
            
        }

        [BurstCompile]
        private struct EdgeDetectionPass : IJobParallelFor
        {
            public int width, height;
            [ReadOnly] public NativeArray<Color32> pixels;
            public NativeArray<bool> solidMask;
            public NativeArray<float2> edgeOut;

            public void Execute(int index)
            {
                var x = index % width;
                var y = index / width;

                var a = pixels[index].a / 255f;
                var selfSolid = a >= 0.5f;
                solidMask[index] = selfSolid;

                var best = new float2(-1, -1);
                var minDist = float.MaxValue;

                for (var i = 0; i < 8; i++)
                {
                    var offset = NeighborOffsets[i];
                    var nx = x + offset.x;
                    var ny = y + offset.y;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;

                    var neighborIdx = nx + ny * width;
                    var na = pixels[neighborIdx].a / 255f;
                    var neighborSolid = na >= 0.5f;

                    if (neighborSolid != selfSolid)
                    {
                        var l = math.clamp((0.5f - a) / (na - a), 0f, 1f);
                        var edge = math.lerp(new float2(x, y), new float2(nx, ny), l);
                        var dist = math.length(edge - new float2(x, y));
                        if (dist < minDist)
                        {
                            minDist = dist;
                            best = edge;
                        }
                    }
                }

                edgeOut[index] = best;
            }
        }

        [BurstCompile]
        private struct JumpFloodPass : IJobParallelFor
        {
            public int width, height, offset;
            [ReadOnly] public NativeArray<float2> inputEdge;
            public NativeArray<float2> outputEdge;

            public void Execute(int index)
            {
                var x = index % width;
                var y = index / width;
                var current = new float2(x, y);

                var best = inputEdge[index];
                var bestValid = best.x >= 0 && best.y >= 0;
                var bestDist = bestValid ? math.lengthsq(current - best) : float.MaxValue;

                for (var i = 0; i < 8; i++)
                {
                    var offsetDir = NeighborOffsets[i] * offset;
                    var nx = x + offsetDir.x;
                    var ny = y + offsetDir.y;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;

                    var nIdx = nx + ny * width;
                    var candidate = inputEdge[nIdx];
                    if (candidate.x < 0 || candidate.y < 0) continue; // skip invalid

                    var dist = math.lengthsq(current - candidate);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = candidate;
                        bestValid = true;
                    }
                }

                // If no valid candidates were found and current best is invalid, keep as (-1, -1)
                outputEdge[index] = bestValid ? best : new float2(-1, -1);
            }
        }


        [BurstCompile]
        private struct FinalSDFPass : IJobParallelFor
        {
            public int width;
            public float gradientSize;
            [ReadOnly] public NativeArray<bool> solidMask;
            [ReadOnly] public NativeArray<float2> edge;
            public NativeArray<Color32> pixels;

            public void Execute(int index)
            {
                var x = index % width;
                var y = index / width;
                var current = new float2(x, y);
                var nearest = edge[index];
                var dist = math.length(current - nearest);

                var value = math.clamp(0.5f + dist * (solidMask[index] ? 1f : -1f) / gradientSize, 0f, 1f);
                var c = pixels[index];
                c.a = (byte)(value * 255f);
                pixels[index] = c;
            }
        }

        private static readonly int2[] NeighborOffsets = {
            new int2(-1, 0), new int2(1, 0), new int2(0, -1), new int2(0, 1),
            new int2(-1, -1), new int2(-1, 1), new int2(1, -1), new int2(1, 1)
        };
    }
}
