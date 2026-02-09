using UnityEditor;
using UnityEditor.Build;

namespace nickeltin.InternalBridge.Editor
{
    public static class _NamedBuildTarget
    {
        public static NamedBuildTarget FromActiveSettings(BuildTarget target)
        {
            return NamedBuildTarget.FromActiveSettings(target);
        }
    }
}