using System.Collections.Generic;
using System.Linq;
using nickeltin.Core.Runtime;
using nickeltin.InternalBridge.Editor;
using nickeltin.SDF.Editor.DecoupledPipeline;
using nickeltin.SDF.Runtime;
using nickeltin.SDF.Runtime.DecoupledPipeline;
using UnityEditor;
using UnityEditor.Sprites;
using UnityEngine;
using UnityEngine.U2D;

namespace nickeltin.SDF.Editor
{
    /// <summary>
    /// Responsible for all objects that related to SDF pipeline and from which <see cref="SDFSpriteMetadataAsset"/>
    /// can be extracted.
    /// Also handles <see cref="SDFAsset"/> and <see cref="SDFSpriteMetadataAsset"/> drag and drop in scene hierarchy,
    /// that creates <see cref="SDFImage"/> from it.
    /// </summary>
    internal static class SDFDragAndDrop
    {
        // public delegate void DragAndDropHandler(SerializedProperty property, bool isDragging, bool isDropping);
        private static void LogDragAndDropMessage(object message, Object context, LogType logType = LogType.Log)
        {
            Debug.LogFormat(logType, LogOption.NoStacktrace, context, "<b>[SDF Drag&Drop]:</b> {0}", message);
        }

        #region Single
        
        /// <inheritdoc cref="HandleDragAndDrop(SerializedProperty, bool, bool)"/>
        public static void HandleDragAndDrop(SerializedProperty property, Rect rect)
        {
            var e = Event.current;
            var isDragging = e.type == EventType.DragUpdated && rect.Contains(e.mousePosition);
            var isDropping = e.type == EventType.DragPerform && rect.Contains(e.mousePosition);
            
            HandleDragAndDrop(property, isDragging, isDropping);
        }
        
        /// <summary>
        /// Handles drag and drop conversion if dragging regular texture, sprite or SDFAsset, tries to retrieve its <see cref="SDFSpriteMetadataAsset"/>.
        /// </summary>
        /// <param name="property">Property that has reference to <see cref="SDFSpriteMetadataAsset"/></param>
        /// <param name="isDragging">Is drag performing</param>
        /// <param name="isDropping">Is drag performed (object dropped over the field)</param>
        public static void HandleDragAndDrop(SerializedProperty property, bool isDragging, bool isDropping)
        {
            if (!isDragging && !isDropping)
                return;

            if (DragAndDrop.objectReferences.Length == 0 || DragAndDrop.objectReferences[0] == null ||
                DragAndDrop.paths.Length == 0)
                return;

            var dragObj = DragAndDrop.objectReferences.FirstOrDefault();
            var path = DragAndDrop.paths.FirstOrDefault();
            var target = property.serializedObject.targetObject;
            switch (dragObj)
            {
                case SDFAsset:
                    var sdfAssetImporter = SDFAssetImporter.Get(path);
                    if (!sdfAssetImporter.IsValid()) return;

                    if (!isDropping)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                        return;
                    }

                    var firstSDFSpriteMetaAsset = AssetDatabase.LoadAssetAtPath<SDFAsset>(path).SpriteMetadataAssets
                        .FirstOrDefault();
                    if (firstSDFSpriteMetaAsset == null)
                    {
                        LogDragAndDropMessage("No sprites is generated in SDFAsset", target, LogType.Error);
                        return;
                    }

                    property.objectReferenceValue = firstSDFSpriteMetaAsset;
                    property.serializedObject.ApplyModifiedProperties();
                    break;
                case Texture:
                    var importer = AssetImporter.GetAtPath(path);
                    if (importer is not TextureImporter textureImporter)
                        return;
                    if (textureImporter.textureType != TextureImporterType.Sprite)
                        return;

                    // If not dropping doesn't load the sprite but changes the visual mode.
                    if (!isDropping)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                        return;
                    }

                    var texSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (texSprite == null)
                    {
                        LogDragAndDropMessage("No sprites is generated from texture", target, LogType.Error);
                        return;
                    }

                    AcceptDrag(texSprite, importer.assetPath);
                    break;
                case Sprite sprite:
                    AcceptDrag(sprite, path);
                    break;
            }

            void AcceptDrag(Sprite sprite, string lPath)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                if (!isDropping) return;
                if (SDFEditorUtil.TryGetSpriteMetadataAsset(sprite, true,
                        out var metadataAsset, out var flags, out var sdfAssetGUID))
                {
                    if (flags.HasFlag(SDFPipelineFlags.DecoupledSourceSprite))
                    {
                        LogDragAndDropMessage($"SDF for sprite {sprite} ({lPath}) is found in SDFAsset at " +
                                              $"{AssetDatabase.GUIDToAssetPath(sdfAssetGUID)}, using it.", target);
                    }

                    property.objectReferenceValue = metadataAsset;
                    property.serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    LogDragAndDropMessage($"SDF for sprite {sprite} ({lPath}) can't be found. Generating it", target);

                    // Setting null to clear a value that might be missing
                    property.objectReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();

                    SDFEditorUtil.FixTextures(new[] { SpriteUtility.GetSpriteTexture(sprite, false) });
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                    // Just in case making delay upon looking for sdf metadata
                    EditorApplication.delayCall += () =>
                    {
                        // Don't need to search for sdf asset since we generating sdf texture in regular pipeline
                        if (!SDFEditorUtil.TryGetSpriteMetadataAsset(sprite, false, out metadataAsset))
                        {
                            LogDragAndDropMessage("SDF for some reason were not generated or can't be found",
                                target, LogType.Error);
                        }

                        property.objectReferenceValue = metadataAsset;
                        property.serializedObject.ApplyModifiedProperties();
                    };
                }
            }
        }

        #endregion

        #region List
   
        public static bool HandleListDragAndDrop(SerializedProperty property, Rect rect, bool searchForSDFAsset = true)
        {
            var e = Event.current;
            var isDragging = e.type == EventType.DragUpdated && rect.Contains(e.mousePosition);
            var isDropping = e.type == EventType.DragPerform && rect.Contains(e.mousePosition);
            
            return HandleListDragAndDrop(property, isDragging, isDropping, searchForSDFAsset);
        }
        
        /// <summary>
        /// This is a highly unoptimized version to handle dragNDrop for all possible cases for collection of objects.
        /// Supports dragging of:
        ///     <see cref="Texture"/> (Will load all sprites from it, then try to find corresponding meta assets
        ///     <see cref="SDFAsset"/> (Directly load all meta-assets from it)
        ///     <see cref="Sprite"/> (Any sprite from selection, trying to find corresponding meta-assets)
        ///     <see cref="SpriteAtlas"/> (Extract all sprites affected by atlas, then try to find corresponding meta-assets)
        /// </summary>
        public static bool HandleListDragAndDrop(SerializedProperty property, bool isDragging, bool isDropping, bool searchForSDFAsset)
        {
            if (!isDragging && !isDropping)
                return false;

            if (DragAndDrop.objectReferences.Length == 0 || DragAndDrop.objectReferences[0] == null ||
                DragAndDrop.paths.Length == 0)
                return false;

            var dragObjects = DragAndDrop.objectReferences;

            // Adding meta-assets right away
            var sdfSpriteMetaAssets = new HashSet<SDFSpriteMetadataAsset>(dragObjects.OfType<SDFSpriteMetadataAsset>());
            
            #region Cases
            
            // For regular sprites just try to get their meta-asset for both regular or decoupled pipeline
            AddSprites(dragObjects.OfType<Sprite>());

            // Meta-assets can be easily extracted from SDFAsset, so no search just adds them directly
            foreach (var sdfAsset in dragObjects.OfType<SDFAsset>())
            {
                sdfSpriteMetaAssets.AddRange(sdfAsset.SpriteMetadataAssets);
            }
            
            // For textures load all sprites inside of it, then trying to get their meta-asset for both regular or decoupled pipeline
            foreach (var texture in dragObjects.OfType<Texture>())
            {
                var lSprites = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(texture)).OfType<Sprite>();
                AddSprites(lSprites);
            }
            
            // Extracting all sprites from atlases, then trying to get their meta-asset for both regular or decoupled pipeline
            foreach (var atlas in dragObjects.OfType<SpriteAtlas>())
            {
                var lSprites = SDFEditorUtil.LoadSpritesFromAtlas(atlas);
                AddSprites(lSprites);
            }
            
            #endregion
            
            if (sdfSpriteMetaAssets.Count == 0) return false;
            
            if (!isDropping)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                return true;
            }
            
            foreach (var metaAsset in sdfSpriteMetaAssets)
            {
                property.arraySize++;
                var sdfRef = property.GetArrayElementAtIndex(property.arraySize - 1);
                var objRef = SDFSpriteReferenceDrawer.GetMetadataAssetProperty(sdfRef);
                objRef.objectReferenceValue = metaAsset;
            }

            property.serializedObject.ApplyModifiedProperties();

            return true;

            void AddSprites(IEnumerable<Sprite> lSprites)
            {
                foreach (var sprite in lSprites)
                {
                    if (SDFEditorUtil.TryGetSpriteMetadataAsset(sprite, searchForSDFAsset, out var metaAsset))
                    {
                        sdfSpriteMetaAssets.Add(metaAsset);
                    }
                }
            }
        }
        
        #endregion

        #region Scene Drag$Drop
       
        [InitializeOnLoadMethod]
        private static void Init()
        {
            DragAndDrop.AddDropHandler(SDFAssetHandler);
            DragAndDrop.AddDropHandler(SDFSpriteMetaAssetHandler);
        }

        private static DragAndDropVisualMode DragAndDropHandler(int parentInstanceID, Transform parent, bool perform,
            SDFSpriteMetadataAsset[] assets)
        {
            var mode = DragAndDropVisualMode.None;

            if (assets.Length <= 0) return mode;

            var lParent = parent != null 
                ? parent.gameObject 
                : (GameObject)EditorUtility.InstanceIDToObject(parentInstanceID);
            
            if (perform)
            {
                foreach (var sdfAsset in assets)
                {
                    var go = new GameObject(nameof(SDFImage), typeof(RectTransform), typeof(SDFImage));
                    var rect = go.GetComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(100f, 100f);
                    var sdfImage = go.GetComponent<SDFImage>();
                    sdfImage.SDFSpriteReference = sdfAsset;
                    _MenuOptions.PlaceUIElementRoot(go, new MenuCommand(lParent));
                }
            }
            mode = DragAndDropVisualMode.Move;
            return mode;
        }
        
        private static DragAndDropVisualMode SDFSpriteMetaAssetHandler(int parentInstanceID, HierarchyDropFlags dropMode, Transform parent, bool perform)
        {
            var draggedSDFAssets = DragAndDrop.objectReferences
                .OfType<SDFSpriteMetadataAsset>()
                .ToArray();

            return DragAndDropHandler(parentInstanceID, parent, perform, draggedSDFAssets);
        }
        
        private static DragAndDropVisualMode SDFAssetHandler(int parentInstanceID, HierarchyDropFlags dropMode, Transform parent, bool perform)
        {
            var draggedSDFAssets =  DragAndDrop.objectReferences
                .OfType<SDFAsset>()
                .SelectMany(asset => asset.SpriteMetadataAssets)
                .ToArray();

            return DragAndDropHandler(parentInstanceID, parent, perform, draggedSDFAssets);
        }
        
        #endregion
    }
}