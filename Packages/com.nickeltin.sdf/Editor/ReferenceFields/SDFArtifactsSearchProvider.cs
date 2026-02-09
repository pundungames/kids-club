using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using nickeltin.InternalBridge.Editor;
using nickeltin.SDF.Editor.DecoupledPipeline;
using nickeltin.SDF.Runtime;
using nickeltin.SDF.Runtime.DecoupledPipeline;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
// ReSharper disable StringLiteralTypo


namespace nickeltin.SDF.Editor
{
    /// <summary>
    /// Search provider that allows to browse SDF import artifacts, otherwise hidden assets.
    /// SDF artifacts is scriptable objects with <see cref="HideFlags.HideInHierarchy"/> assigned at import time,
    /// this is done to prevent them from becoming main assets instead of texture in <see cref="TextureImporter"/> pipeline.
    /// This search provider performs regular search with AssetProvider(id = "asset") for textures,
    /// checks is this textures valid for sdf import with <see cref="SDFEditorUtil.ShouldImportSDF(string, out TextureImporter, out SDFGenerationSettings)"/>,
    /// then loads it <see cref="SDFImportResult"/> as artifact file.
    /// For its contents creates <see cref="GlobalObjectId"/> for meta asset and source sprite.
    /// 
    /// Main goal is to browse trough this assets as they were regular sprites.
    ///
    /// After adding decoupled pipeline this provider will also search for <see cref="SDFSpriteMetadataAsset"/> as regular assets
    /// </summary>
    internal static class SDFArtifactsSearchProvider
    {
        private static readonly Texture2D _sdfMetaAssetIcon;
        
        static SDFArtifactsSearchProvider()
        {
            // For some reason unity can't fetch icon from type only, so creating temp instance to get it
            var instance = ScriptableObject.CreateInstance<SDFSpriteMetadataAsset>();
            _sdfMetaAssetIcon = AssetPreview.GetMiniThumbnail(instance);
            Object.DestroyImmediate(instance);
        }

        private readonly struct AssetMetaInfo
        {
            public readonly GlobalObjectId MetaAssetGID;
            public readonly GlobalObjectId SourceSpriteGID;
            public readonly TextureImporter SourceTextureImporter;
            public readonly int SpriteIndex;
            /// <summary>
            /// Set this to true if referencing <see cref="SDFSpriteMetadataAsset"/> that is visible in project.
            /// Then it will be pinged instead of source sprite.
            /// </summary>
            public readonly bool VisibleAssetReference;
            
            public readonly string Label;
            public readonly string Description;
            
            public AssetMetaInfo(GlobalObjectId metaAssetGid, TextureImporter sourceTextureImporter, GlobalObjectId sourceSpriteGid, int spriteIndex, 
                bool visibleAssetReference)
            {
                MetaAssetGID = metaAssetGid;
                SourceTextureImporter = sourceTextureImporter;
                SourceSpriteGID = sourceSpriteGid;
                SpriteIndex = spriteIndex;
                VisibleAssetReference = visibleAssetReference;
                
                var texName = Path.GetFileName(SourceTextureImporter.assetPath);
                if (SourceTextureImporter.spriteImportMode == SpriteImportMode.Multiple)
                {
                    var rects = SourceTextureImporter.GetSpriteRects();
                    Label = $"{texName}/{rects[SpriteIndex].name} (SDF Sprite)";
                }
                else
                {
                    Label = $"{texName} (SDF Sprite)";
                }
                
                Description = "";
                Description = $"Path: {GetAssetPath()} | " +
                              $"Sprite Id: {(long)SourceSpriteGID.targetObjectId} | " +
                              $"MetaAsset Id: {(long)MetaAssetGID.targetObjectId}";
            }

            public string GetAssetPath()
            {
                var path = VisibleAssetReference 
                    ? AssetDatabase.GUIDToAssetPath(MetaAssetGID.assetGUID) 
                    : SourceTextureImporter.assetPath;
                return path;
            }
        }
        
        private static string ID => SDFUtil.ArtifactsSearchProviderID;

        public const string FILTER_ID = "sdfa:";

        #region SearchItem ext
        
        private static AssetMetaInfo GetInfo(this SearchItem item)
        {
            return (AssetMetaInfo)item.data;
        }

        private static int GetMetaAssetInstanceID(this SearchItem item)
        {
            return GlobalObjectId.GlobalObjectIdentifierToInstanceIDSlow(item.GetInfo().MetaAssetGID);
        }

        private static int GetSourceSpriteInstanceID(this SearchItem item)
        {
            return GlobalObjectId.GlobalObjectIdentifierToInstanceIDSlow(item.GetInfo().SourceSpriteGID);
        }
        
        private static string GetAssetPath(this SearchItem item)
        {
            return item.GetInfo().GetAssetPath();
        }

        private static Object GetObject(this SearchItem item)
        {
            return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(item.GetInfo().MetaAssetGID);
        }
        #endregion
        
        [SearchItemProvider]
        private static SearchProvider CreateProvider()
        {
            var instance =  new SearchProvider(ID, "SDF Artifacts")
            {
                priority = 1000,
                filterId = FILTER_ID,
                active = false,
                isExplicitProvider = true,
                showDetails = true,
                showDetailsOptions = ShowDetailsOptions.Default | ShowDetailsOptions.Inspector | ShowDetailsOptions.DefaultGroup,
                fetchItems = SearchAllAssets,
                fetchThumbnail = FetchThumbnail,
                fetchPreview = FetchPreview,
                fetchLabel = FetchLabel,
                fetchDescription = FetchDescription,
                toObject = ToObject,
                trackSelection = TrackSelection,
                startDrag = StartDrag,
            };
            
            // To type is always meta asset type, for now
            instance.SetToType((_, _) => typeof(SDFSpriteMetadataAsset));
            return instance;
        }

        #region Provider Callbacks
      
        private static void TrackSelection(SearchItem item, SearchContext context)
        {
            var info = item.GetInfo();
            var assetToPing = info.VisibleAssetReference
                ? item.GetMetaAssetInstanceID()
                : item.GetSourceSpriteInstanceID();
            EditorGUIUtility.PingObject(assetToPing);
        }

        private static Texture2D FetchThumbnail(SearchItem item, SearchContext context)
        {
            return _sdfMetaAssetIcon;
        }
        
        private static Texture2D FetchPreview(SearchItem item, SearchContext context, Vector2 size, FetchPreviewOptions options)
        {
            // We can get icon without loading object with its instance id and internal AssetPreview method
            return _AssetPreview.GetAssetPreview(item.GetSourceSpriteInstanceID());
        }
        
        private static string FetchLabel(SearchItem item, SearchContext context)
        {
            return item.GetInfo().Label;
        }
        
        private static string FetchDescription(SearchItem item, SearchContext context)
        {
            return item.GetInfo().Description;
        }
        
        private static Object ToObject(SearchItem item, Type type)
        {
            return item.GetObject();
        }
        
        private static void StartDrag(SearchItem item, SearchContext context)
        {
            var set = new HashSet<SearchItem> { item };
            foreach (var searchItem in context.selection) set.Add(searchItem);
            if (set.Count == 0) return;

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = set.Select(i => i.GetObject()).ToArray();
            DragAndDrop.paths = set.Select(i => i.GetAssetPath()).ToArray();
            DragAndDrop.StartDrag(item.GetLabel(context, true));
        }

        private static IEnumerable<SearchItem> SearchAllAssets(SearchContext context, List<SearchItem> items, SearchProvider provider)
        {
            foreach (var item in SearchLegacyPipelineAssets(context, items, provider))
                yield return item;
            
            foreach (var item in SearchDecoupledPipeline(context, items, provider))
                yield return item;
            
        }
        
        /// <summary>
        /// Searches for <see cref="SDFSpriteMetadataAsset"/> by finding all textures, checking their sdf import settings,
        /// loading their <see cref="SDFImportResult"/> artifact files, which contains local id's for source sprites and meta assets.
        /// This id's can be used with <see cref="GlobalObjectId"/> to locate particular asset without loading it.
        /// </summary>
        private static IEnumerable<SearchItem> SearchLegacyPipelineAssets(SearchContext context, List<SearchItem> items, SearchProvider provider)
        {
            var innerContext = SearchService.CreateContext("asset", $"t:Texture2D is:main {context.searchQuery}", context.options);
            var searchList = SearchService.Request(innerContext);
            foreach (var searchItem in searchList.Fetch())
            {
                if (searchItem == null)
                {
                    yield return null;
                    continue;
                }

                GlobalObjectId.TryParse(searchItem.id, out var id);
                
                var assetPath = AssetDatabase.GUIDToAssetPath(id.assetGUID);
                
                // Checking is texture import settings valid to import sdf without loading it.
                if (!SDFEditorUtil.ShouldImportSDF(assetPath, out var textureImporter, out _))
                {
                    yield return null;
                    continue;
                }
                
                // We found compatible sdf texture, now try to load its sdf import result
                // now we can form search item from local ids from sprite and sd meta asset
                var searchIndex = SDFImportResult.Get(id.assetGUID);
                for (var i = 0; i < searchIndex.MetaAssetsLocalIDs.Length; i++)
                {
                    var metaAssetLocalId = searchIndex.MetaAssetsLocalIDs[i];
                    var sourceSpriteLocalId = searchIndex.SourceSpritesLocalIDs[i];
                    
                    // Creating two GIDs for meta asset and sprite. Sprite needed to get previews from it. 
                    var metaAssetGid = CreateGID(id.assetGUID, metaAssetLocalId);
                    var sourceSpriteGid = CreateGID(id.assetGUID, sourceSpriteLocalId);

                    var itemData = new AssetMetaInfo(metaAssetGid, textureImporter, sourceSpriteGid, i, false);
                    yield return provider.CreateItem(innerContext, metaAssetGid.ToString(), null, null, null,
                        itemData);
                }
            }
        }
        
        
        /// <summary>
        /// Decoupled pipeline contains <see cref="SDFSpriteMetadataAsset"/> not inside of texture and they are visible,
        /// however preview for them is still messed-up for some reason.
        /// So we still going trough the same process of finding main assets, <see cref="SDFAsset"/> in this case,
        /// then loading their <see cref="SDFImportResult"/>'s, and creating same search items from this data.
        ///
        /// Difference being that this items is pointing to the original <see cref="SDFSpriteMetadataAsset"/> that is visible, rather then source sprite.
        /// </summary>
        private static IEnumerable<SearchItem> SearchDecoupledPipeline(SearchContext context, List<SearchItem> items, SearchProvider provider)
        {
            var innerContext = SearchService.CreateContext("asset", $"t:{typeof(SDFAsset).FullName} {context.searchQuery}", context.options);
            var searchList = SearchService.Request(innerContext);
            foreach (var searchItem in searchList.Fetch())
            {
                if (searchItem == null)
                {
                    yield return null;
                    continue;
                }
            
                GlobalObjectId.TryParse(searchItem.id, out var id);
                
                var assetPath = AssetDatabase.GUIDToAssetPath(id.assetGUID);
            
                var sdfAssetImporter = SDFAssetImporter.Get(assetPath);
                
                // Reference to texture is broken
                if (sdfAssetImporter == null || sdfAssetImporter._texture.isBroken)
                {
                    yield return null;
                    continue;
                }
            
                var texturePath = AssetDatabase.GetAssetPath(sdfAssetImporter._texture.instanceID);
                var textureGUID = AssetDatabase.GUIDFromAssetPath(texturePath);
                var textureImporter = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                
                var searchIndex = SDFImportResult.Get(id.assetGUID);
                for (var i = 0; i < searchIndex.MetaAssetsLocalIDs.Length; i++)
                {
                    var metaAssetLocalId = searchIndex.MetaAssetsLocalIDs[i];
                    var sourceSpriteLocalId = searchIndex.SourceSpritesLocalIDs[i];
                    
                    // Creating two GIDs for meta asset and sprite. Sprite needed to get previews from it. 
                    var metaAssetGid = CreateGID(id.assetGUID, metaAssetLocalId);
                    // Source sprite using guid for sprite guid
                    var sourceSpriteGid = CreateGID(textureGUID, sourceSpriteLocalId);
            
                    var itemData = new AssetMetaInfo(metaAssetGid, textureImporter, sourceSpriteGid, i, true);
                    yield return provider.CreateItem(innerContext, metaAssetGid.ToString(), null, null, null,
                        itemData);
                }
            }
        }


        /// <summary>
        /// Will search for <see cref="SDFSpriteMetadataAsset"/> with regular provider and return regular items, without any functionality override.
        /// </summary>
        private static IEnumerable<SearchItem> RegularSDFMetaAssetsSearch(SearchContext context, List<SearchItem> items, SearchProvider provider)
        {
            var innerContext = SearchService.CreateContext("adb", $"t:{typeof(SDFSpriteMetadataAsset).FullName} {context.searchQuery}", context.options);
            var searchList = SearchService.Request(innerContext);
            foreach (var searchItem in searchList.Fetch())
            {
                yield return searchItem;
            }
        }

        #endregion
        
        /// <summary>
        /// Creates gid for imported object, not scene asset or source asset
        /// See more at https://docs.unity.cn/2021.3/Documentation/ScriptReference/GlobalObjectId.html
        /// </summary>
        private static GlobalObjectId CreateGID(GUID guid, long localId)
        {
            // 1 = imported asset, asset guid, local id, 0 = prefab instance id.
            var gidStr = $"GlobalObjectId_V1-{1}-{guid}-{(ulong)localId}-{0}";
            GlobalObjectId.TryParse(gidStr, out var gid);
            return gid;
        }
     

        #region Actions
        
        /// <summary>
        /// Basically copy-paste from AssetProvider
        /// </summary>
        /// <returns></returns>
        [SearchActionsProvider]
        internal static IEnumerable<SearchAction> CreateActionHandlers()
        {
            return new SearchAction[]
            {
                new(ID, "select", tooltip: "Select")
                {
                    handler = item => SearchUtils.SelectMultipleItems(new [] { item }, true),
                    execute = items => SearchUtils.SelectMultipleItems(items, true)
                },
                new(ID, "open", null, "Open")
                {
                    handler = OpenItem,
                    enabled = IsSingleItem
                },
                new(ID, "reimport", null, "Reimport")
                {
                    execute = ReimportAssets,
                    enabled = IsSingleItem
                },
                new(ID, "reveal", null, "Show in Explorer")
                {
                    handler = item => EditorUtility.RevealInFinder(item.GetAssetPath()),
                    execute = items =>
                    {
                        foreach (var item in items)
                        {
                            EditorUtility.RevealInFinder(item.GetAssetPath());
                        }
                    }
                },
                new(ID, "copy_path", tooltip: "Copy Path")
                {
                    enabled = IsSingleItem,
                    handler = CopyPath
                },
                new(ID, "copy_guid", tooltip: "Copy GUID")
                {
                    enabled = IsSingleItem,
                    handler = CopyGUID
                },
                new(ID, "properties", null, "Properties")
                {
                    execute = OpenPropertyEditorsOnSelection
                }
            };
        }

        private static bool IsSingleItem(IReadOnlyCollection<SearchItem> items)
        {
            return items.Count == 1;
        }
        
        private static void CopyGUID(SearchItem item)
        {
            var assetPath = GetAssetPath(item);
            if (assetPath == null) return;
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            EditorGUIUtility.systemCopyBuffer = guid;
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, item.ToObject(), guid);
        }

        private static void CopyPath(SearchItem item)
        {
            var assetPath = item.GetAssetPath();
            if (assetPath == null) return;
            EditorGUIUtility.systemCopyBuffer = assetPath;
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, item.ToObject(), assetPath);
        }
        
        private static void OpenItem(SearchItem item)
        {
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<Object>(item.GetAssetPath()));
        }
        
        private static void ReimportAssets(IEnumerable<SearchItem> items)
        {
            foreach (var searchItem in items)
            {
                // Value is not clear but this is copy paste from "asset" provider
                AssetDatabase.ImportAsset(searchItem.GetAssetPath(), (ImportAssetOptions) 8577);
            }
        }

        private static void OpenPropertyEditorsOnSelection(IEnumerable<SearchItem> items)
        {
            var objs = items.Select(i => i.ToObject()).Where(o=> o != null).ToArray();
            if (objs.Length == 0)
                return;

            foreach (var obj in objs)
            {
                EditorUtility.OpenPropertyEditor(obj);
            }
        }
        
        #endregion
    }
}