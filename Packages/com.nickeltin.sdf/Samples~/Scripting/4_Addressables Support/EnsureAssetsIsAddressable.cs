#if UNITY_EDITOR && ADDRESSABLES 
using UnityEditor;
using UnityEditor.AddressableAssets;

namespace nickeltin.SDF.Samples.Runtime.AddressablesSupport
{
    /// <summary>
    /// Utility that ensures that all used assets from samples is addressables in any project they are imported in.
    /// For course if Addressables package is presented.
    /// </summary>
    internal static class EnsureAssetsIsAddressable
    {
        private readonly struct Entry
        {
            public readonly string GUID;
            public readonly string AddressablePath;

            public Entry(string guid, string addressablePath)
            {
                GUID = guid;
                AddressablePath = addressablePath;
            }

            public readonly string GetFullAddressablePath() => PREF + AddressablePath;
        }

        private const string PREF = "com.nickeltin.sdf/Samples/";

        /// <summary>
        /// Just list of all used assets and their addressable postfix.
        /// If any new asset is added to sample define them here.
        /// </summary>
        private static readonly Entry[] ENTRIES = new[]
        {
            new Entry("aed7d43327a51144fb0a09d309f8e0b3", "space-vehicle"),
            new Entry("d5319229b8372904e8a05cd348ea42e2", "space_SDF"),
            new Entry("ce70b0d9ffb3b374a8203ff74665ebff", "ufo_SDF"),
        };

        public static readonly string FIRST_SDF_ASSET_PATH = ENTRIES[1].GetFullAddressablePath();
        
        [InitializeOnLoadMethod]
        private static void Init()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            foreach (var entry in ENTRIES)
            {
                // If asset is already addressable then whatever
                if (settings.FindAssetEntry(entry.GUID) != null)
                {
                    continue;
                }
                
                settings.CreateAssetReference(entry.GUID);
                var assetEntry = settings.FindAssetEntry(entry.GUID);
                assetEntry.address = entry.GetFullAddressablePath();
            }
        }
    }
}
#endif