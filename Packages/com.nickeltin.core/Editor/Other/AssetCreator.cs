using nickeltin.InternalBridge.Editor;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace nickeltin.Core.Editor
{
    public static class AssetCreator
    {
        public class CreateAssetWithContent : EndNameEditAction
        {
            public string FileContent;
            public ObjectCreatedDelegate ObjectCreated;
        
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = _ProjectWindowUtil.CreateScriptAssetWithContent(pathName, FileContent);
                ProjectWindowUtil.ShowCreatedAsset(instance);
                ObjectCreated?.Invoke(instance, pathName);
            }
        }
        
        public class CreateAssetFromObject: EndNameEditAction
        {
            public ObjectCreatedDelegate ObjectCreated;
            public ObjectCreatorDelegate ObjectCreator;
        
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = ObjectCreator();
                // ProjectWindowUtil.CreateAsset(instance, pathName);
                Debug.Log(pathName);
                InternalEditorUtility.SaveToSerializedFileAndForget(new [] {instance}, pathName, EditorSettings.serializationMode != SerializationMode.ForceBinary);
                AssetDatabase.ImportAsset(pathName);
                ProjectWindowUtil.ShowCreatedAsset(instance);
                ObjectCreated?.Invoke(instance, pathName);
            }
        }
        
        public delegate void ObjectCreatedDelegate(Object @object, string path); 
        public delegate Object ObjectCreatorDelegate(); 
        
        public static void CreateWithContent(string filename, string content, ObjectCreatedDelegate objectCreated)
        {
            var instance = ScriptableObject.CreateInstance<CreateAssetWithContent>();
            instance.FileContent = content;
            instance.ObjectCreated = objectCreated;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, instance, filename, null, null);
        }

        public static void CreateFromObject(string filename, ObjectCreatorDelegate objectCreator,
            ObjectCreatedDelegate objectCreated)
        {
            var instance = ScriptableObject.CreateInstance<CreateAssetFromObject>();
            instance.ObjectCreator = objectCreator;
            instance.ObjectCreated = objectCreated;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, instance, filename, null, null);
        }
    }
}