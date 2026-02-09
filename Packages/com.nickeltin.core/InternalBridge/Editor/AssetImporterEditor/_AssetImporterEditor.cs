using UnityEditor.AssetImporters;
using Object = UnityEngine.Object;

namespace nickeltin.InternalBridge.Editor
{
    public static partial class _AssetImporterEditor
    {
        static _AssetImporterEditor()
        {
            InitBefore2022();
        }

        static partial void InitBefore2022();
        
        public static Object[] GetAssetTargets(this AssetImporterEditor editor)
        {
            return editor.assetTargets;
        }
    }
}