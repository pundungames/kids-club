using System;
using UnityEngine;

namespace nickeltin.Core.Runtime
{
    /// <summary>
    /// Custom drawer that fixes built-in drawer issue with mixed values not being supported
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class LazyLoadReferenceFixedDrawerAttribute : PropertyAttribute
    {
    }
}