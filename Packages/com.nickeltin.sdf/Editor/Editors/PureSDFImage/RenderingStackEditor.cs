using nickeltin.SDF.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace nickeltin.SDF.Editor
{
    [CustomEditor(typeof(PureSDFImageRenderingStack)), CanEditMultipleObjects]
    internal class RenderingStackEditor : UnityEditor.Editor
    {
        private SerializedProperty _stack;
        private SerializedProperty _lerpLayers;
        private SerializedProperty _layersLerp;
        private SerializedProperty _spriteReferenceA;
        private SerializedProperty _spriteReferenceB;
        private ReorderableList _stackList;
        
        private void OnEnable()
        {
            _stack = serializedObject.FindProperty(nameof(PureSDFImageRenderingStack._stack));
            _lerpLayers = serializedObject.FindProperty(nameof(PureSDFImageRenderingStack._lerpLayers));
            _layersLerp = serializedObject.FindProperty(nameof(PureSDFImageRenderingStack._layersLerp));
            _spriteReferenceA = serializedObject.FindProperty(nameof(PureSDFImageRenderingStack._spriteReferenceA));
            _spriteReferenceB = serializedObject.FindProperty(nameof(PureSDFImageRenderingStack._spriteReferenceB));

            _stackList = new ReorderableList(serializedObject, _stack, true, false, 
                true, true)
            {
                drawElementCallback = DrawElementCallback,
            };
        }
        
        private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            var imageProp = _stack.GetArrayElementAtIndex(index);
            var image = imageProp.objectReferenceValue as PureSDFImage;
            if (image != null)
            {
                var newEnabled = EditorGUI.ToggleLeft(new Rect(rect)
                {
                    width = 16
                }, GUIContent.none, image.enabled);
                rect.width -= 17;
                rect.x += 17;
                if (newEnabled != image.enabled)
                {
                    Undo.RecordObject(image, $"{image}.SetActive({newEnabled})");
                    image.enabled = newEnabled;
                }
            }

            rect.height -= 2;
            rect.y += 1;
            EditorGUI.PropertyField(rect, imageProp, GUIContent.none);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            _stackList.DoLayoutList();
            
            EditorGUILayout.PropertyField(_lerpLayers);
            using (new EditorGUI.DisabledScope(!_lerpLayers.boolValue))
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(_layersLerp);
            }

            EditorGUILayout.PropertyField(_spriteReferenceA);

            using (new EditorGUI.DisabledScope(!_lerpLayers.boolValue))
            {
                EditorGUILayout.PropertyField(_spriteReferenceB);
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}