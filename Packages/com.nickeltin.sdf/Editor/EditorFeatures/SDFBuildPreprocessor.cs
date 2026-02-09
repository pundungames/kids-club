using System.Collections.Generic;
using System.Linq;
using nickeltin.InternalBridge.Editor;
using nickeltin.SDF.Runtime;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace nickeltin.SDF.Editor
{
    internal class SDFBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;
        
        public void OnPreprocessBuild(BuildReport report)
        {
            var fields = TypeCache.GetFieldsWithAttribute<ForceIncludeShaderInBuildAttribute>();
            var forceIncludeShaderNames = fields.Select(info => info.GetValue(null))
                .Where(o => o != null)
                .Select(o =>
                {
                    switch (o)
                    {
                        case string:
                            return o.ToString();
                        case ForceIncludeShaderInBuildAttribute.IShaderNameProvider provider:
                            return provider.ShouldIncludeShaderInBuild() ? provider.GetShaderName() : null;
                        default:
                            return null;
                    }
                })
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .ToList();

            var shaderNameToShader = forceIncludeShaderNames
                .ToDictionary(name => name, Shader.Find);

            var foundShaders = shaderNameToShader
                .Where(pair => pair.Value != null)
                .Select(pair => pair.Value)
                .ToList();

            var missingShaders = shaderNameToShader
                .Where(pair => pair.Value == null)
                .Select(pair => pair.Key)
                .ToList();

            var reportText = $"[SDFImage Shader Inclusion Report]\n" +
                             $"Requested: {forceIncludeShaderNames.Count}\n" +
                             $"Found: {foundShaders.Count}\n" +
                             $"Missing: {missingShaders.Count}\n";

            if (foundShaders.Count > 0)
            {
                reportText += "\nFound Shaders:\n" + string.Join("\n", foundShaders.Select(s => $" - {s.name}"));
            }

            if (missingShaders.Count > 0)
            {
                reportText += "\n\nMissing Shaders:\n" + string.Join("\n", missingShaders.Select(s => $" - {s}"));
            }

            Debug.Log(reportText);
            ModifyIncludeShaders(foundShaders, null);
        }



        private static IEnumerable<Object> EnumerateArrayObjects(SerializedProperty property)
        {
            for (var i = 0; i < property.arraySize; i++)
            {
                yield return property.GetArrayElementAtIndex(i).objectReferenceValue;
            }
        }
        
        private static void ModifyIncludeShaders(IEnumerable<Shader> add, IEnumerable<Shader> remove)
        {
            if (add == null && remove == null) return;

            var graphicsSettingsObj = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
            var serializedObject = new SerializedObject(graphicsSettingsObj);
            var arrayProp = serializedObject.FindProperty("m_AlwaysIncludedShaders");
            // ReSharper disable once CollectionNeverQueried.Local
            var shaders = EnumerateArrayObjects(arrayProp).Where(s => s != null).ToHashSet();
            var hasChange = false;
            
            if (add != null)
            {
                foreach (var shader in add)
                {
                    if (shaders.Add(shader)) hasChange = true;
                }
            }

            if (remove != null)
            {
                foreach (var shader in remove)
                {
                    if (shaders.Remove(shader)) hasChange = true;
                }
            }


            if (hasChange)
            {
                arrayProp.ClearArray();
                foreach (var shader in shaders) arrayProp.AppendFoldoutPPtrValue(shader);
                serializedObject.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                serializedObject.Dispose();
            }
        }
    }
}