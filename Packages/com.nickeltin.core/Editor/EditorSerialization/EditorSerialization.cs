using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using nickeltin.InternalBridge.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace nickeltin.Core.Editor
{
    public static class EditorSerialization
    {
        /// <summary>
        /// Loads all assets at path, returns first that matches the type.
        /// </summary>
        public static T Load<T>(string path) where T : Object
        {
            return InternalEditorUtility.LoadSerializedFileAndForget(path).OfType<T>().FirstOrDefault();
        }

        public static T LoadFromString<T>(string serializedObject) where T : Object
        {
            var tempPath = FileUtil.GetUniqueTempPathInProject();
            File.WriteAllText(tempPath, serializedObject);
            var obj = Load<T>(tempPath);
            File.Delete(tempPath);
            return obj;
        }
        
        /// <summary>
        /// Loads all assets at path, finds first that matches the type, copies loaded instance to provider instance, destroys loaded instance. 
        /// </summary>
        public static void LoadObject<T>(string path, T obj) where T : ScriptableObject
        {
            var instance = Load<T>(path);
            EditorUtility.CopySerialized(instance, obj);
            Object.DestroyImmediate(instance);
        }
        
        /// <summary>
        /// Serializes object, writes it to file at path.
        /// </summary>
        public static void SaveObject<T>(string path, T obj) where T : ScriptableObject
        {
            InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { obj }, path,
                EditorSettings.serializationMode != SerializationMode.ForceBinary);
        }
        
        /// <summary>
        /// Serializes object to file, reads its data, returns bytes, deletes file.
        /// </summary>
        public static byte[] SerializeObject<T>(T obj) where T : ScriptableObject
        {
            var tempPath = FileUtil.GetUniqueTempPathInProject();
            SaveObject(tempPath, obj);
            var bytes = File.ReadAllBytes(tempPath);
            File.Delete(tempPath);
            return bytes;
        }
        
        /// <summary>
        /// Serializes object to bytes, then encodes bytes to string.
        /// </summary>
        public static string SerializeObjectToString<T>(T obj) where T : ScriptableObject
        {
            return Encoding.UTF8.GetString(SerializeObject(obj));
        }
        
        /// <summary>
        /// Serialized to string empty instance of object, destroys it after.
        /// </summary>
        public static string SerializeObjectToString<T>() where T : ScriptableObject
        {
            var instance = ScriptableObject.CreateInstance<T>();
            var str = SerializeObjectToString(instance);
            Object.DestroyImmediate(instance);
            return str;
        }


        // [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property)]
        // public class ExternalObjectAttribute : Attribute
        // {
        // }

        /// <summary>
        /// Creates <see cref="ScriptableObject"/> from <see cref="Object"/> and goes over all of its properties,
        /// yields all referenced <see cref="Object"/>'s. Works for any depth of serialized structures, but not for filed of referenced objects.
        /// Can be used to save structure of interconnected assets in same file
        /// </summary>
        /// <param name="source"></param>
        /// <param name="skipScriptFile"></param>
        /// <returns></returns>
        public static IEnumerable<Object> GetAllReferencedObjects(Object source, bool skipScriptFile = true)
        {
            // Create a SerializedObject from the asset
            var serializedObject = new SerializedObject(source);
            var iterator = serializedObject.GetIterator();
            // var enterChildren = true;
            while (iterator.Next(true)) // Iterate over all properties
            {
                // enterChildren = true;
                if (skipScriptFile && iterator.name == "m_Script") continue;

                // if (skipExternalObjects)
                // {
                //     if (IsPropertyExternal(iterator))
                //     {
                //         // If filed or object marked with external object attribute will not enter it marked
                //         enterChildren = false;
                //         continue;
                //     }
                // }

                if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;
                var referencedObject = iterator.objectReferenceValue;
                if (referencedObject == null) continue;
                yield return referencedObject;
            }
        }


        // public static bool IsPropertyExternal(SerializedProperty property)
        // {
        //     // Field info is invalid for non C# side defined fields - unity natives
        //     var filedInfo = _ScriptAttributeUtility.GetFieldInfoFromProperty(property, out var type);
        //     // If field info defined we can grab its attribute or the attribute for filed's type
        //     return filedInfo?.GetCustomAttribute<ExternalObjectAttribute>() != null || type?.GetCustomAttribute<ExternalObjectAttribute>() != null;
        // }
    }
}