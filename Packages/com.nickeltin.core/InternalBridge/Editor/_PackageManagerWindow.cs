using UnityEditor.PackageManager.UI;

namespace nickeltin.InternalBridge.Editor
{
    public static class _PackageManagerWindow
    {
        public static void OpenAndSelectPackage(string packageToSelect, string pageId = null)
        {
#if UNITY_6000_0_OR_NEWER
            PackageManagerWindow.OpenAndSelectPackage(packageToSelect, pageId);
#else
            PackageManagerWindow.OpenPackageManager(packageToSelect);
#endif
        }
    }
}