using System;

namespace nickeltin.SDF.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    internal class ForceIncludeShaderInBuildAttribute : Attribute
    {
        public interface IShaderNameProvider
        {
            string GetShaderName();
            
            bool ShouldIncludeShaderInBuild();
        }
    }
}