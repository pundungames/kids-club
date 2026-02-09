#if !UNITY_2022_2_OR_NEWER

using System.Collections.Generic;
using nickeltin.InternalBridge.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace nickeltin.SDF.Editor
{
    internal partial class TextureImporterEditor
    {
        /// <summary>
        /// Checks for asset hashes change, mainly this used to detect reset calls
        /// </summary>
        internal sealed class HashCheck : ChangeCheck
        {
            private readonly List<Hash128> _lastHashes = new List<Hash128>();
            private Hash128[] _temp;
            
            protected override void SetNewData_Impl(AssetImporterEditor editor)
            {
                _temp = editor.GetAssetHashes();
                Changed = !HashesEquals(_temp, _lastHashes);
            }

            protected override void IterateData_Impl()
            {
                _lastHashes.Clear();
                _lastHashes.AddRange(_temp);
                _temp = null;
                Changed = false;
            }

            private static bool HashesEquals(IReadOnlyList<Hash128> a, IReadOnlyList<Hash128> b)
            {
                if (a.Count != b.Count) return false;
                for (var i = 0; i < a.Count; i++)
                {
                    var hashA = a[i];
                    var hashB = b[i];
                    if (hashA != hashB) return false;
                }

                return true;
            }
        }
    }
}

#endif