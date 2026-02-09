using System.Linq;
using nickeltin.SDF.Runtime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace nickeltin.SDF.Editor
{
    /// <summary>
    /// This window exist only observe current state of hidden renderers of SDF Image.
    /// </summary>
    internal partial class SDFFLRDebugWindow : EditorWindow, IHasCustomMenu
    {
        private static class Defaults
        {
            public static readonly GUIContent Select =
                new GUIContent(EditorGUIUtility.IconContent("d_scenepicking_pickable_hover"))
                {
                    tooltip = "Select"
                };
            
            public static readonly GUIContent Delete =
                new GUIContent(EditorGUIUtility.IconContent("d_CollabDeleted Icon"))
                {
                    tooltip = "Delete"
                };
        }
        
        [SerializeField] private Vector2 _scrollPos;
        
        [MenuItem(MenuPaths.TOOLBAR + "First Layer Renderer Debug")]
        private static void ShowWindow()
        {
            var window = GetWindow<SDFFLRDebugWindow>();
            window.Focus();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("SDF FLR Debug");
            SDFFirstLayerRenderer.activeRenderersChanged += SortRenderers;
            SortRenderers();
        }

        private void OnDisable()
        {
            SDFFirstLayerRenderer.activeRenderersChanged -= SortRenderers;
        }
        
        private void SortRenderers() => _sortedRenderers.Refresh();
        
        private readonly SortedRenderersCollection _sortedRenderers = new SortedRenderersCollection();

        
        private void OnGUI()
        {
            _sortedRenderers.ConsumeRefresh();
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            foreach (var sceneRenderers in _sortedRenderers)
            {
                EditorGUILayout.BeginVertical();
                var sceneName = $"{sceneRenderers.Scene.name} ({sceneRenderers.Renderers.Count})";
                if (!string.IsNullOrEmpty(sceneRenderers.Scene.path))
                {
                    sceneName += $" ({sceneRenderers.Scene.path})";
                }
                else
                {
                    sceneName += " (Temp)";
                }
                sceneRenderers.Expanded = EditorGUILayout.Foldout(sceneRenderers.Expanded, sceneName);
                if (sceneRenderers.Expanded)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        foreach (var renderer in sceneRenderers)
                        {
                            EditorGUILayout.BeginHorizontal();
                            if (renderer.TryGet(out var rendererInstance))
                            {
                                using (new EditorGUI.DisabledScope(true))
                                {
                                    EditorGUILayout.ObjectField(rendererInstance, typeof(SDFFirstLayerRenderer), false);
                                }
                                if (GUILayout.Button(Defaults.Select, EditorStyles.iconButton))
                                {
                                    var img = rendererInstance.GetComponentInParent<SDFImage>();
                                    Selection.activeObject = img;
                                    EditorGUIUtility.PingObject(img);
                                }

                                if (GUILayout.Button(Defaults.Delete, EditorStyles.iconButton))
                                {
                                    DestroyImmediate(rendererInstance.gameObject);
                                    GUIUtility.ExitGUI();
                                    return;
                                }
                                
                                DrawHideFlagsDropdown(rendererInstance);
                            }
                            else
                            {
                                EditorGUILayout.HelpBox($"FLR at index {renderer.Index} is has lost reference", MessageType.Error);
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }

        public static void DrawHideFlagsDropdown(SDFFirstLayerRenderer rendererInstance, GUIContent content = null)
        {
            var hideFlagsValue = rendererInstance.gameObject.hideFlags;
            var newHideFlagsValue =
                content == null 
                    ?
                (HideFlags)EditorGUILayout.EnumFlagsField(hideFlagsValue, GUILayout.ExpandWidth(false))
                    :
                (HideFlags)EditorGUILayout.EnumFlagsField(content, hideFlagsValue)
                    ;
            
            if (!Equals(newHideFlagsValue, hideFlagsValue))
            {
                // Changing hide flags for all components and game object itself
                foreach (var component in rendererInstance.gameObject.GetComponents<Component>()
                             .Cast<Object>()
                             .Prepend(rendererInstance.gameObject))
                {
                    var so = new SerializedObject(component);
                    var hideFlagsProp = so.FindProperty("m_ObjectHideFlags");
                    hideFlagsProp.enumValueFlag = (int)newHideFlagsValue;
                    so.ApplyModifiedProperties();
                    so.Dispose();
                }
                                    
                EditorUtility.SetDirty(rendererInstance.gameObject);
            }
        }
        
        private static bool shouldRepaint => (EditorApplication.isPlaying && !EditorApplication.isPaused) || !EditorApplication.isPlaying;

        private void OnInspectorUpdate()
        {
            if (shouldRepaint && _sortedRenderers.Refreshed)
            {
                Repaint();
            }
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Sort Renderers"), false, SortRenderers);
        }
    }
}