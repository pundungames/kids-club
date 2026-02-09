#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using nickeltin.SDF.Samples.Runtime;
using UnityEditor;
using UnityEngine;


namespace nickeltin.SDF.Samples.Editor
{
    public abstract class SampleBaseEditor : UnityEditor.Editor
    {
        private static class Defaults
        {
            public static readonly GUIContent Error = new(EditorGUIUtility.IconContent("console.erroricon"));
            public static readonly GUIContent TempContent = new();
        }
        
        private readonly struct ButtonEntry
        {
            public readonly SampleButtonAttribute Attribute;
            public readonly MethodInfo Method;
            public readonly string DisplayName;
            public readonly bool IsValid;
            public readonly string InvalidMessage;

            private ButtonEntry(SampleButtonAttribute attribute, MethodInfo method, string invalidMessage)
            {
                Attribute = attribute;
                Method = method;
                InvalidMessage = invalidMessage;
                IsValid = string.IsNullOrEmpty(invalidMessage);
                DisplayName = string.IsNullOrEmpty(Attribute.Name)
                    ? ObjectNames.NicifyVariableName(method.Name)
                    : attribute.Name;
            }

            public readonly void Invoke(object[] targets)
            {
                foreach (var target in targets)
                {
                    Method.Invoke(target, null);
                }
            }
            
            public static bool TryCreate(MethodInfo info, out ButtonEntry buttonEntry)
            {
                if (!info.IsAbstract)
                {
                    var attr = info.GetCustomAttribute<SampleButtonAttribute>();
                    if (attr != null)
                    {
                        var invalidMessage = new List<string>();
                        if (info.GetParameters().Length > 0)
                        {
                            invalidMessage.Add($"Method can't have parameters");
                        }

                        if (info.ReturnType != typeof(void))
                        {
                            invalidMessage.Add($"Method should have void return");
                        }
                        buttonEntry = new ButtonEntry(attr, info, string.Join("\n", invalidMessage));
                        return true;
                    }
                }

                buttonEntry = new ButtonEntry();
                return false;
            }

            public static int FillAllEntries(Type fromType, List<ButtonEntry> entries)
            {
                var i = 0;
                foreach (var method in fromType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (TryCreate(method, out var entry))
                    {
                        entries.Add(entry);
                        i++;
                    }
                }

                return i;
            }
        }
        
        private readonly struct ReadonlyPropertyEntry
        {
            public readonly PropertyInfo Property;
            public readonly string DisplayName;
            public readonly bool IsValid;
            public readonly string InvalidMessage;

            private ReadonlyPropertyEntry(PropertyInfo property, string invalidMessage)
            {
                Property = property;
                InvalidMessage = invalidMessage;
                IsValid = string.IsNullOrEmpty(invalidMessage);
                DisplayName = ObjectNames.NicifyVariableName(property.Name);
            }

            public readonly object GetValue(object target)
            {
                return Property.GetValue(target);
            }
            
            public static bool TryCreate(PropertyInfo info, out ReadonlyPropertyEntry propertyEntry)
            {
                var attr = info.GetCustomAttribute<SampleReadonlyAttribute>();
                if (attr != null)
                {
                    var invalidMessage = new List<string>();
                    
                    if (!info.CanRead)
                    {
                        invalidMessage.Add("Property must be readable");
                    }
                    
                    propertyEntry = new ReadonlyPropertyEntry(info, string.Join("\n", invalidMessage));
                    return true;
                }

                propertyEntry = new ReadonlyPropertyEntry();
                return false;
            }

            public static int FillAllEntries(Type fromType, List<ReadonlyPropertyEntry> entries)
            {
                var i = 0;
                foreach (var property in fromType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (TryCreate(property, out var entry))
                    {
                        entries.Add(entry);
                        i++;
                    }
                }

                return i;
            }
        }
        
        private List<ButtonEntry> _buttons;
        private List<ReadonlyPropertyEntry> _readonlyProperties;
        
        private void OnEnable()
        {
            _buttons = new List<ButtonEntry>();
            _readonlyProperties = new List<ReadonlyPropertyEntry>();
            ButtonEntry.FillAllEntries(target.GetType(), _buttons);
            ReadonlyPropertyEntry.FillAllEntries(target.GetType(), _readonlyProperties);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            // Display readonly properties
            if (_readonlyProperties.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Read-only Properties", EditorStyles.boldLabel);

                foreach (var property in _readonlyProperties)
                {
                    using (new EditorGUI.DisabledScope(!property.IsValid))
                    {
                        var content = Defaults.TempContent;
                        content.image = property.IsValid ? null : Defaults.Error.image;
                        content.text = property.DisplayName;
                        content.tooltip = property.IsValid ? "" : property.InvalidMessage;

                        if (property.IsValid)
                        {
                            var value = property.GetValue(target);

                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(content, GUILayout.Width(EditorGUIUtility.labelWidth - 4));

                            // If the property type is UnityEngine.Object or a subclass, use ObjectField
                            var propertyType = property.Property.PropertyType;
                            if (typeof(UnityEngine.Object).IsAssignableFrom(propertyType))
                            {
                                var unityObj = value as UnityEngine.Object;
                                EditorGUILayout.ObjectField(unityObj, propertyType, true, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                            }
                            else
                            {
                                var valueString = value?.ToString() ?? "null";
                                EditorGUILayout.SelectableLabel(valueString, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                            }

                            EditorGUILayout.EndHorizontal();
                        }
                        else
                        {
                            EditorGUILayout.LabelField(content);
                        }
                    }
                }
            }
            
            // Display buttons
            if (_buttons.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
                
                foreach (var button in _buttons)
                {
                    using (new EditorGUI.DisabledScope(!button.IsValid))
                    {
                        var content = Defaults.TempContent;
                        content.image = button.IsValid ? null : Defaults.Error.image;
                        content.text = button.DisplayName;
                        content.tooltip = button.IsValid ? "" : button.InvalidMessage;
                        if (GUILayout.Button(content))
                        {
                            button.Invoke(targets);
                        }
                    }
                }
            }
        }
    }
}


#endif