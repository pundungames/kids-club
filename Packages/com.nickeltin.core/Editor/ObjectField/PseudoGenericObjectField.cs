using System.Collections.Generic;
using System.Text;
using nickeltin.InternalBridge.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace nickeltin.Core.Editor
{
    /// <summary>
    /// Some static functions to mimic Unity Object field, for custom referencing methods (not through object picker)
    /// </summary>
    public static class PseudoGenericObjectField
    {
        internal const string k_FieldControlName = "PseudoGenericObjectField";
        
        public delegate bool ObjectValidator(Object obj);
        public delegate void OpenObjectPicker(Rect fieldPosition);
        public delegate void PingObject();
        public delegate void OpenObject();
        public delegate IEnumerable<ISearchWindowEntry> ValidObjectsGetter();
        public delegate void StringConstructor(StringBuilder stringBuilder);

        public static class Defaults
        {
            public static readonly GUIStyle ButtonStyle;
            public static readonly Texture2D InvalidIcon;
            public static readonly GUIContent LabelContent;
            public static readonly GUIStyle ObjectField;
            public static readonly Texture2D NoneIcon;

            static Defaults()
            {
                ButtonStyle = _EditorStyles.objectFieldButton;
                InvalidIcon = (Texture2D)EditorGUIUtility.IconContent("Invalid").image;
                LabelContent = new GUIContent(GUIContent.none);
                ObjectField = EditorStyles.objectField;
                NoneIcon = (Texture2D)EditorGUIUtility.IconContent("DefaultAsset Icon").image;
            }
        }

        private static readonly StringBuilder _stringBuilder = new StringBuilder();

        public static void DrawObjectField(Rect rect,
            GUIContent label,
            SerializedProperty mainProperty,
            SerializedProperty objectReferenceSource,
            bool isValid,
            ObjectValidator objectValidator,
            StringConstructor objectName,
            string objectTooltip,
            OpenObjectPicker openObjectPicker,
            PingObject pingObject,
            OpenObject openObject,
            bool handleDragAndDrop = true)
        {
            EditorGUI.BeginProperty(rect, label, mainProperty);
            rect.height = Mathf.Min(rect.height, EditorGUIUtility.singleLineHeight);
            var objRef = objectReferenceSource.objectReferenceValue;
            var hasObjRef = objRef != null;
            rect = EditorGUI.PrefixLabel(rect, label);
            _stringBuilder.Clear();
            _stringBuilder.Append(hasObjRef ? objRef.name : "None");
            if (!isValid)
            {
                _stringBuilder.Append("<invalid>");
            }

            objectName ??= DefaultStringConstructorMethod;
            objectName(_stringBuilder);

            
            var content = Defaults.LabelContent;
            content.text = _stringBuilder.ToString();
            content.tooltip = objectTooltip;
            const float pickerWidth = 20f;
            var pickerRect = rect;
            pickerRect.width = pickerWidth;
            pickerRect.x = rect.xMax - pickerWidth;

            var e = Event.current;
            var isPickerPressed = e.type == EventType.MouseDown && e.button == 0 && pickerRect.Contains(e.mousePosition);
            var isEnterKeyPressed = e.type == EventType.KeyDown && e.isKey && (e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Return);

            var asset = objectReferenceSource.objectReferenceValue;
            var iconHeight = EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing * 3;
            var iconSize = EditorGUIUtility.GetIconSize();
            EditorGUIUtility.SetIconSize(new Vector2(iconHeight, iconHeight));
            if (asset != null)
            {
                var icon = !isValid 
                    ? Defaults.InvalidIcon 
                    : EditorGUIUtility.ObjectContent(objRef, null).image;
                
                content.image = icon;
                
                GUI.SetNextControlName(k_FieldControlName);
                if (EditorGUI.DropdownButton(rect, content, FocusType.Keyboard, Defaults.ObjectField) 
                    && !pickerRect.Contains(e.mousePosition))
                {
                    if (e.clickCount == 1)
                    {
                        GUI.FocusControl(k_FieldControlName);
                        pingObject();
                    }
                    if (e.clickCount == 2)
                    {
                        openObject();
                        GUIUtility.ExitGUI();
                    }
                }
            }
            else
            {
                content.image = Defaults.NoneIcon;
                GUI.SetNextControlName(k_FieldControlName);
                if (EditorGUI.DropdownButton(rect, content, FocusType.Keyboard, Defaults.ObjectField))
                { 
                    GUI.FocusControl(k_FieldControlName);
                }
            }
            
            EditorGUIUtility.SetIconSize(iconSize);

            var localPosition = pickerRect;
            localPosition.height -= 2;
            localPosition.y += 1;
            localPosition.x += 1;
            localPosition.width -= 2;
            GUI.Label(localPosition, GUIContent.none, Defaults.ButtonStyle);

            var enterKeyRequestsPopup = isEnterKeyPressed && (k_FieldControlName == GUI.GetNameOfFocusedControl());
            if (isPickerPressed || enterKeyRequestsPopup)
            {
                openObjectPicker(rect);
            }

            if (handleDragAndDrop)
            {
                HandleDragAndDrop(rect, objectValidator, objectReferenceSource);
            }
            
            EditorGUI.EndProperty();
        }

         public static void DrawObjectField(Rect rect,
            GUIContent label,
            SerializedProperty mainProperty,
            SerializedProperty objectReferenceSource,
            bool isValid,
            ValidObjectsGetter getValidObjects,
            ObjectValidator objectValidator,
            StringConstructor objectName,
            string objectTooltip)
        {
            DrawObjectField(rect, 
                label, 
                mainProperty, 
                objectReferenceSource, 
                isValid, 
                objectValidator, 
                objectName, 
                objectTooltip,
                (fieldPosition) =>
                {
                    var fieldRect = ObjectSearchWindow.CalculateFieldRect(fieldPosition);
                    var objSource = objectReferenceSource;
                    ObjectSearchWindow.Open(getValidObjects(), entry =>
                    {
                        var obj = entry.GetData() as Object;
                        objSource.objectReferenceValue = obj;
                        objSource.serializedObject.ApplyModifiedProperties();
                    }, position: fieldRect.position, size: fieldRect.size);
                }, 
                () =>
                {
                    EditorGUIUtility.PingObject(objectReferenceSource.objectReferenceValue);
                }, 
                () =>
                {
                    AssetDatabase.OpenAsset(objectReferenceSource.objectReferenceValue);
                });
        }
        
        public static void DrawObjectField(Rect rect, 
            GUIContent label, 
            SerializedProperty mainProperty, 
            SerializedProperty objectReferenceSource, 
            bool isValid, 
            ValidObjectsGetter getValidObjects, 
            ObjectValidator objectValidator,
            string objectTypeString,
            string genericTypeString,
            string objectTooltip)
        {
            DrawObjectField(rect, label, mainProperty, objectReferenceSource, isValid, getValidObjects, objectValidator, 
                builder =>
                {
                    builder.Append(" (");
                    builder.Append(objectTypeString);
                    builder.Append("<");
                    builder.Append(genericTypeString);
                    builder.Append(">)");
                }, objectTooltip);
        }
        
        private static void HandleDragAndDrop(Rect fieldPosition, ObjectValidator objectValidator, SerializedProperty objectReferenceSource)
        {
            var e = Event.current;
            if (!fieldPosition.Contains(e.mousePosition))
            {
                return;
            }

            if (DragAndDrop.objectReferences.Length == 0 || DragAndDrop.objectReferences[0] == null)
            {
                return;
            }

            var obj = DragAndDrop.objectReferences[0];
            
            if (objectValidator(obj))
            {
                if (e.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                    e.Use();
                }   
                else if (e.type == EventType.DragPerform)
                {
                    e.Use();

                    objectReferenceSource.objectReferenceValue = obj;
                    objectReferenceSource.serializedObject.ApplyModifiedProperties();
                }
            }
        }
        
        private static void DefaultStringConstructorMethod(StringBuilder builder) { }
    }
}