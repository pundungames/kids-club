using System.Collections;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace nickeltin.SDF.Editor
{
    internal static class NickeltinSDF
    {
        public const string NAME = "com.nickeltin.sdf";

        public static void OpenDocumentation()
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(OpenDocumentation(NAME));
        }

        public static IEnumerator OpenDocumentation(string packageName)
        {
            var request = Client.List(true); // true = include built-in
            while (!request.IsCompleted)
                yield return null;

            if (request.Status == StatusCode.Failure)
            {
                Debug.LogError("Failed to retrieve package list.");
                yield break;
            }
                
            // const string packageName = "com.nickeltin.sdf";
            var package = request.Result.FirstOrDefault(info => info.name == packageName);
            if (package == null)
            {
                Debug.LogError($"Package '{packageName}' not found.");
                yield break;
            }

            OpenDocumentation(package);
        }

        public static void OpenDocumentation(PackageInfo package)
        {
            Application.OpenURL(package.documentationUrl);
        }
    }
}