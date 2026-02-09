using nickeltin.Core.Editor;
using nickeltin.SDF.Runtime;
using UnityEditor;
using UnityEngine;

namespace nickeltin.SDF.Editor
{
    [CustomPropertyDrawer(typeof(SDFRendererSettings))]
    internal class SDFRendererSettingsDrawer : PropertyDrawer
    {
        public static class Defaults
        {
            public static readonly GUIContent MainColor = new GUIContent("Main Color", 
                "Color applied for all layers, will get multiplied with corresponding shader color");
            
            public static readonly GUIContent Width = new GUIContent("Width",
                "This value is passed to shader, SDF width of current layer");

            public static readonly GUIContent RegularLayer = new GUIContent("Regular",
                "This is base sprite rendering layer, the same as would regular image render");

            public static readonly GUIContent OutlineLayer = new GUIContent("Outline",
                "First SDF layer, in material corresponds to Main and Outline sections");

            public static readonly GUIContent ShadowLayer = new GUIContent("Shadow", 
                "Second SDF layer, can be offseted");

            public static readonly GUIContent Color = new GUIContent("Color", 
                "Color of this layer, will get multiplied with corresponding shader color");
            
            public static readonly GUIContent Offset = new GUIContent("Offset",
                "Offset of shadow layer mesh");
        }
        
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            Draw(rect, property, label, true);
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return GetHeight(property, label, true);
        }

        public static void DrawLayout(SerializedProperty property)
        {
            var height = GetHeight(property, GUIContent.none, false);
            var rect = EditorGUILayout.GetControlRect(false, height);
            Draw(rect, property, GUIContent.none, false);
        }
        
        public static void Draw(Rect rect, SerializedProperty property, GUIContent label, bool expandable)
        {
            var drawHelper = new DrawHelper(rect);
            
            EditorGUI.BeginProperty(rect, label, property);
            
            if (expandable)
            {
                drawHelper.AddRect();
                property.isExpanded = EditorGUI.Foldout(drawHelper.CurrentRect, property.isExpanded, label, true);
            }

            if ((!expandable || !property.isExpanded) && expandable)
            {
                EditorGUI.EndProperty();
                return;
            }

            using (new EditorGUI.IndentLevelScope(expandable ? 1 : 0))
            {
                var mainColor = property.FindPropertyRelative(nameof(SDFRendererSettings.MainColor));

                var renderRegular = property.FindPropertyRelative(nameof(SDFRendererSettings.RenderRegular));
                var regularColor = property.FindPropertyRelative(nameof(SDFRendererSettings.RegularColor));

                var renderOutline = property.FindPropertyRelative(nameof(SDFRendererSettings.RenderOutline));
                var outlineColor = property.FindPropertyRelative(nameof(SDFRendererSettings.OutlineColor));
                var outlineMult = property.FindPropertyRelative(nameof(SDFRendererSettings.OutlineWidth));

                var renderShadow = property.FindPropertyRelative(nameof(SDFRendererSettings.RenderShadow));
                var shadowColor = property.FindPropertyRelative(nameof(SDFRendererSettings.ShadowColor));
                var shadowMult = property.FindPropertyRelative(nameof(SDFRendererSettings.ShadowWidth));
                var shadowOffset = property.FindPropertyRelative(nameof(SDFRendererSettings.ShadowOffset));


                drawHelper.DrawProperty(mainColor, Defaults.MainColor);
                drawHelper.DrawLeftToggle(renderRegular, Defaults.RegularLayer);
                using (new EditorGUI.IndentLevelScope())
                using (new EditorGUI.DisabledScope(!renderRegular.boolValue))
                {
                    drawHelper.DrawProperty(regularColor, Defaults.Color);
                }

                drawHelper.DrawLeftToggle(renderOutline, Defaults.OutlineLayer);
                using (new EditorGUI.IndentLevelScope())
                using (new EditorGUI.DisabledScope(!renderOutline.boolValue))
                {
                    drawHelper.DrawProperty(outlineColor, Defaults.Color);
                    drawHelper.DrawProperty(outlineMult, Defaults.Width);
                }

                drawHelper.DrawLeftToggle(renderShadow, Defaults.ShadowLayer);
                using (new EditorGUI.IndentLevelScope())
                using (new EditorGUI.DisabledScope(!renderShadow.boolValue))
                {
                    drawHelper.DrawProperty(shadowColor, Defaults.Color);
                    drawHelper.DrawProperty(shadowMult, Defaults.Width);
                    drawHelper.DrawProperty(shadowOffset, Defaults.Offset);
                }
            }

            EditorGUI.EndProperty();
        }

        public static float GetHeight(SerializedProperty property, GUIContent label, bool expandable)
        {
            var drawHelper = new DrawHelper(Rect.zero);
            if (expandable)
            {
                drawHelper.AddRect();
                if (!property.isExpanded)
                {
                    return drawHelper.TotalHeight;
                }
            }
            
            foreach (var child in property.GetVisibleChilds())
            {
                drawHelper.AddProperty(child);
            }
            
            return drawHelper.TotalHeight;
        }
    }
}