#if !UNITY_2022_2_OR_NEWER

using System.Reflection;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Assertions;

namespace nickeltin.InternalBridge.Editor
{
    public static partial class _AssetImporterEditor
    {
        private static FieldInfo m_AssetHashes;

        static partial void InitBefore2022()
        {
            m_AssetHashes = typeof(AssetImporterEditor).GetField("m_AssetHashes", 
                BindingFlags.Instance | BindingFlags.NonPublic);
            
            Assert.IsNotNull(m_AssetHashes);
        }
        
        public static Hash128[] GetAssetHashes(this AssetImporterEditor editor)
        {
            return (Hash128[])m_AssetHashes.GetValue(editor);
        }
    }
}

#endif