using System;
using nickeltin.SDF.Runtime;

namespace nickeltin.SDF.Editor
{
    /// <summary>
    /// Keeping it for naming compatability.
    /// Please use new version <see cref="SDFGenerationSettings"/> or <see cref="SDFGenerationSettingsBase"/>
    /// </summary>
    [Obsolete]
    [Serializable]
    public class SDFImportSettings : SDFGenerationSettings { }
}