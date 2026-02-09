using System;

namespace nickeltin.SDF.Samples.Runtime
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SampleButtonAttribute : Attribute
    {
        public string Name { get; set; }
    }
}