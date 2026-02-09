using UnityEditor;

namespace nickeltin.InternalBridge.Editor
{
    public static class _BuildPlayerWindow
    {
        public static void CallBuildMethods(bool askForBuildLocation, BuildOptions defaultBuildOptions)
        {
            BuildPlayerWindow.CallBuildMethods(askForBuildLocation, defaultBuildOptions);
        }
    }
}