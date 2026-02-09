using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using nickeltin.InternalBridge.Editor;
using UnityEditor;
using UnityEngine;

namespace nickeltin.Core.Editor
{
    public static class SerializedPropertyExtensions
    {
        #region Other

        public static IEnumerable<SerializedProperty> GetVisibleChilds(this SerializedProperty property)
        {
            var currentProperty = property.Copy();
            var nextSiblingProperty = property.Copy();
            nextSiblingProperty.NextVisible(false);

            if (currentProperty.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(currentProperty, nextSiblingProperty))
                        break;

                    yield return currentProperty;
                }
                while (currentProperty.NextVisible(false));
            }
        }

        public static void DrawVisibleChildren(this SerializedProperty property)
        {
            foreach (var visibleChild in property.GetVisibleChilds())
            {
                EditorGUILayout.PropertyField(visibleChild);
            }
        }
        
        /// <summary>
        /// Will traverse up in property hierarchy, returning eiter direct parent or any ascending.
        /// <remarks>
        /// TODO: not tested with collections
        /// </remarks>
        /// </summary>
        /// <param name="property"></param>
        /// <param name="offset">Default is 1 to get direct parent, 0 will return property itself</param>
        /// <returns></returns>
        public static SerializedProperty GetParent(this SerializedProperty property, int offset = 1)
        {
            var path = string.Join(".", property.propertyPath.Split(".").SkipLast(offset));
            return property.serializedObject.FindProperty(path);
        }
        

        #endregion

        #region Property Type Access
        
        /// <summary>
        /// Unity built-in internal function.
        /// Static type don't include <see cref="SerializeReference"/> fields, and will return their base types.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static FieldInfo GetFieldInfoAndStaticTypeFromProperty(this SerializedProperty property, out Type type)
        {
            return _ScriptAttributeUtility.GetFieldInfoAndStaticTypeFromProperty(property, out type);
        }

        /// <summary>
        /// Same as <see cref="GetFieldInfoAndElementStaticTypeFromProperty"/> but in case of type being <see cref="Array"/> or <see cref="List{T}"/>
        /// will return element type instead.
        /// </summary>
        public static FieldInfo GetFieldInfoAndElementStaticTypeFromProperty(this SerializedProperty property, out Type type)
        {
            var fieldInfo = property.GetFieldInfoAndStaticTypeFromProperty(out type);
            type = GetElementType(type);
            return fieldInfo;
        }

        /// <summary>
        /// Unity built-in internal function.
        /// Will include <see cref="SerializeReference"/> types.
        /// </summary>
        public static FieldInfo GetFieldInfoAndTypeFromProperty(this SerializedProperty property, out Type type)
        {
            return _ScriptAttributeUtility.GetFieldInfoFromProperty(property, out type);
        }
        
        /// <summary>
        /// Same as <see cref="GetFieldInfoAndTypeFromProperty"/> but in case of type being <see cref="Array"/> or <see cref="List{T}"/>
        /// will return element type instead.
        /// </summary>
        public static FieldInfo GetFieldInfoAndElementTypeFromProperty(this SerializedProperty property, out Type type)
        {
            var fieldInfo = property.GetFieldInfoAndTypeFromProperty(out type);
            type = GetElementType(type);
            return fieldInfo;
        }

        /// <summary>
        /// If type <see cref="Array"/> or <see cref="List{T}"/> will return element type, else 
        /// </summary>
        private static Type GetElementType(Type type)
        {
            if (type == null) return null;
            
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                var genericArgument = type.GetGenericArguments().FirstOrDefault();
                return genericArgument ?? typeof(object);
            }

            return type;
        }


        #endregion

        #region Property Value Access

        /// <summary>
        /// Gets boxed value of property with support of Nested Types, Arrays, Lists, SerializedReference
        /// </summary>
        /// <remarks>
        /// If property has multiple values this will only return value of first object.
        /// </remarks>
        /// <param name="offset">Allows to get parent values of nested property, if 1 that will return direct parent of final value, 2 parent of that parent.</param>
        /// <returns></returns>
        // ReSharper disable once InvalidXmlDocComment
        public static object GetValue(this SerializedProperty property, int offset = 0)
        {
            var obj = new object[] { property.serializedObject.targetObject };
            return property.GetValues(obj, offset, out _).First();
        }
        
        /// <inheritdoc cref="GetValue"/>
        /// <remarks>
        /// Version of <see cref="GetValue"/> that supports multi-editing.
        /// </remarks>
        public static object[] GetValues(this SerializedProperty property, int offset = 0)
        {
            return property.GetValues(property.GetTargetObjects(), offset, out _);
        }

        private static object[] GetTargetObjects(this SerializedProperty property)
        {
            return Array.ConvertAll(property.serializedObject.targetObjects, obj => (object)obj);
        }
        
        private static object[] GetValues(this SerializedProperty property, object[] targetObjects, int offset, out string[] fieldStructure)
        {
            if (offset < 0)
            {
                throw new Exception("Offset can't be lower then 0");
            }
            
            var path = property.propertyPath.Replace(".Array.data", "");
            fieldStructure = path.Split('.');
            for (var i = 0; i < fieldStructure.Length - offset; i++)
            {
                for (var j = 0; j < targetObjects.Length; j++)
                {
                    var val = targetObjects.GetValue(j);
                    if (fieldStructure[i].Contains("["))
                    {
                        var index = Convert.ToInt32(new string(fieldStructure[i].Where(char.IsDigit).ToArray()));
                        val = GetFieldValueWithIndex(RGX.Replace(fieldStructure[i], ""), val, index);
                    }
                    else
                    {
                        val = GetFieldValue(fieldStructure[i], val);
                    }
                    targetObjects.SetValue(val, j);
                }
            }

            return targetObjects;
        }

        /// <summary>
        /// Sets boxed value of property with reflections, supports Nesting, Arrays, Lists, SerializedReference
        /// </summary>
        public static void SetValue(this SerializedProperty property, object value)
        {
            var fieldOwners = property.GetValues(new object[] {property.serializedObject.targetObject}, 
                1, out var fieldStructure);
            SetValues(fieldOwners, fieldStructure.Last(), i => value);
        }

        /// <inheritdoc cref="SetValue"/>
        /// <remarks>
        /// Supports multi-editing, with single object value for all targets
        /// </remarks>
        public static void SetValues(this SerializedProperty property, object value)
        {
            var fieldOwners = property.GetValues(property.GetTargetObjects(), 1, out var fieldStructure);
            SetValues(fieldOwners, fieldStructure.Last(), i => value);
        }
        
        /// <inheritdoc cref="SetValue"/>
        /// <remarks>
        /// Supports multi-editing, with specified object values
        /// </remarks>
        public static void SetValues(this SerializedProperty property, object[] values)
        {
            if (values.Length != property.serializedObject.targetObjects.Length)
            {
                throw new Exception($"Values array don't match, target length {property.serializedObject.targetObjects.Length}, provided length {values.Length}");
            }
            
            var fieldOwners = property.GetValues(property.GetTargetObjects(), 1, out var fieldStructure);
            SetValues(fieldOwners, fieldStructure.Last(), i => values[i]);
        }


        /// <summary>
        /// Private values setter, more flexible
        /// </summary>
        /// <param name="fieldOwners">Objects that holds field</param>
        /// <param name="fieldName">Field to be setted</param>
        /// <param name="valuesGetter">Object getter that takes index and returns object</param>
        private static void SetValues(object[] fieldOwners, string fieldName, Func<int, object> valuesGetter)
        {
            for (var i = 0; i < fieldOwners.Length; i++)
            {
                var obj = fieldOwners.GetValue(i);
                var val = valuesGetter(i);
                if (fieldName.Contains("["))
                {
                    var index = Convert.ToInt32(new string(fieldName.Where(char.IsDigit).ToArray()));
                    SetFieldValueWithIndex(RGX.Replace(fieldName, ""), obj, index, val);
                }
        
                SetFieldValue(fieldName, obj, val);
            }
        }
        
        private static readonly Regex RGX = new(@"\[\d+\]", RegexOptions.Compiled);

        private const BindingFlags DEFAULT_BINDING_FLAGS =
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
            BindingFlags.NonPublic;

        private static object GetFieldValue(string fieldName, object obj, BindingFlags bindings = DEFAULT_BINDING_FLAGS)
        {
            var field = obj.GetType().GetField(fieldName, bindings);
            return field != null ? field.GetValue(obj) : default;
        }

        private static object GetFieldValueWithIndex(string fieldName, object obj, int index, BindingFlags bindings = DEFAULT_BINDING_FLAGS)
        {
            var field = obj.GetType().GetField(fieldName, bindings);
            if (field == null) return default;
            var list = field.GetValue(obj);
            if (list.GetType().IsArray)
            {
                return ((Array)list).GetValue(index);
            }
                
            if (list is IList castedList)
            {
                return castedList[index];
            }

            return default;
        }

        private static void SetFieldValue(string fieldName, object obj, object value, BindingFlags bindings = DEFAULT_BINDING_FLAGS)
        {
            var field = obj.GetType().GetField(fieldName, bindings);
            field!.SetValue(obj, value);

        }

        private static void SetFieldValueWithIndex(string fieldName, object obj, int index, object value, BindingFlags bindings = DEFAULT_BINDING_FLAGS)
        {
            var field = obj.GetType().GetField(fieldName, bindings);
            var list = field!.GetValue(obj);
            if (list.GetType().IsArray)
            {
                ((object[])list)[index] = value;
            }
            else if (list is IList castedList)
            {
                castedList[index] = value;
            }
            else
            {
                throw new Exception($"Can't set field value for {fieldName} at index {index}");
            }
        }
        
        #endregion
    }
}