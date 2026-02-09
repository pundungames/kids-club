using System;
using System.Diagnostics;

namespace nickeltin.SDF.Runtime
{
    /// <summary>
    /// This is just a marker attribute that make easier to understand which method supports which pipeline/environment.
    /// Runtime methods can't support all pipelines since there is a lot of data that exist only in editor.
    /// On the other hand high-level (publicly exposed) editor methods should provide full support to get/process all data.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Method)]
    public class SDFPipelineCompatibleAttribute : Attribute
    {
        public SDFPipelineFlags Flags;

        public SDFPipelineCompatibleAttribute(SDFPipelineFlags flags) => Flags = flags;
    }
}