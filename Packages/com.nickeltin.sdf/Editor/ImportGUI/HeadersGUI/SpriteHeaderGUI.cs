using nickeltin.SDF.InternalBridge.Editor;
using nickeltin.SDF.Runtime;
using UnityEditor;
using UnityEngine;

namespace nickeltin.SDF.Editor
{
    internal static class SpriteHeaderGUI
    {
        private static GUIContent _content;
        
        [InitializeOnLoadMethod]
        private static void Init()
        {
            _content = new GUIContent("SDF Metadata", "If this asset displayed it means SDF for this sprite is generated");
            UnityEditor.Editor.finishedDefaultHeaderGUI += EditorOnFinishedDefaultHeaderGUI;
        }

        private static void EditorOnFinishedDefaultHeaderGUI(UnityEditor.Editor obj)
        {
            if (!_SpriteInspector.IsInstance(obj)) return;

            if (obj.serializedObject.isEditingMultipleObjects) return;

            if (!EditorUtility.IsPersistent(obj.target)) return;

            // Its better not to search for sdf metadata asset in gui frames
            var sprite = (Sprite)obj.target;
            if (SDFEditorUtil.TryGetSpriteMetadataAsset(sprite, false, out var metadataAsset))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(_content, metadataAsset, typeof(SDFSpriteMetadataAsset), false);
                }
            }
        }
    }
}