using System;
using UnityEditor;
using Object = UnityEngine.Object;

namespace nickeltin.InternalBridge.Editor
{
    public static class _AssetImporter
    {
        public static long MakeLocalFileIDWithHash(int persistentTypeId, string name, long offset)
        {
            return AssetImporter.MakeLocalFileIDWithHash(persistentTypeId, name, offset);
        }

        public static long MakeInternalID(this AssetImporter importer, int persistentTypeId, string name)
        {
            return importer.MakeInternalID(persistentTypeId, name);
        }
        
        /// <summary>
        /// Generates file id for non-native unity type, for any scriptable object, basically.
        /// All unity types is mapped to ids, see more at https://docs.unity3d.com/Manual/ClassIDReference.html.
        /// User-defined type will always have id 114, which stands for MonoBehaviour
        /// </summary>
        public static long MakeInternalIDForCustomType(this AssetImporter importer, string name)
        {
            return importer.MakeInternalID(114, name);
        }

        /// <summary>
        /// Result is the localFileId that is saved to serialized file for each sub asset.
        /// This is useful when local id needed to be known before import process ends.
        /// </summary>
        public static long GetLocalFileIDForObject(this AssetImporter importer, Object obj, string identifier)
        {
            if (obj == null)
            {
                throw new Exception("Can't create local file id for null object");
            }

            // Trying to find unity type
            var unityType = UnityType.FindTypeByName(obj.GetType().Name);

            // 114 stands for built-in unity type MonoBehaviour, see more at https://docs.unity3d.com/Manual/ClassIDReference.html
            var persistentTypeId = 114;
            if (unityType != null)
            {
                persistentTypeId = unityType.persistentTypeID;
                
            }
            
            return importer.MakeInternalID(persistentTypeId, identifier);
        }
    }
}