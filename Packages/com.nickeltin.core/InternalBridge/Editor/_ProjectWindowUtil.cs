using UnityEditor;
using UnityEngine;

namespace nickeltin.InternalBridge.Editor
{
    public static class _ProjectWindowUtil
    {
        public static bool TryGetActiveFolderPath(out string path)
        {
            return ProjectWindowUtil.TryGetActiveFolderPath(out path);
        }
        
        public static Object CreateScriptAssetWithContent(string pathName, string templateContent)
        {
            return ProjectWindowUtil.CreateScriptAssetWithContent(pathName, templateContent);
        }
    }
}