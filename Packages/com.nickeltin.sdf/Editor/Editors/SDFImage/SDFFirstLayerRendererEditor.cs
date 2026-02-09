using nickeltin.SDF.Runtime;
using UnityEditor;
using UnityEngine;

namespace nickeltin.SDF.Editor
{
    [CustomEditor(typeof(SDFFirstLayerRenderer))]
    internal class SDFFirstLayerRendererEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            GUI.enabled = true;
            EditorGUILayout.HelpBox(
                "This is first layer renderer for SDF Image." +
                "Since uGUI don't have MaterialPropertyBlocks, or rather have some per-instance material properties injection " +
                "in for of CanvasRenderer, it still only allows to inject one Texture, and we need two textures Source and SDF." +
                "Because of that we using hidden, instantiated at runtime Graphic component, but its managed by SDF Image." +
                "Renders regular sprite, as would regular Image does.", 
                MessageType.Info);
        }
    }
}