using System;
using System.Collections.Generic;
using System.Linq;
using nickeltin.Core.Editor;
using nickeltin.Core.Runtime;
using nickeltin.SDF.Editor.DecoupledPipeline;
using nickeltin.SDF.Runtime;
using nickeltin.SDF.Runtime.DecoupledPipeline;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace nickeltin.SDF.Editor
{
    /// <summary>
    /// Some editor specific utils to work with sdf sprites.
    /// Unifies sprite metadata workflow between versions because in editor we can get metadata reliably from sprite.
    /// </summary>
    public static partial class SDFEditorUtil
    {
        public const int SDF_POSTPROCESSOR_ORDER = 0;
        
        public delegate void TextureProcessedDelegate(string path);
        
        /// <summary>
        /// Invoked whenever texture with proper sdf settings or <see cref="SDFAsset"/> is imported.
        /// This does not mean that sdf was actually generated.
        /// </summary>
        public static event TextureProcessedDelegate TextureProcessed;

        internal static void SubmitImportedSDF(string atPath)
        {
            TextureProcessed?.Invoke(atPath);
        }

        #region Validation
        
        
        // NOTE: For now only targeting sprite textures
        [SDFPipelineCompatible(SDFPipelineFlags.RegularAnd2023)]
        public static bool IsValidTextureType(TextureImporter importer)
        {
            return
                // IsValidDefaultTexture() || 
                   IsValidSpriteTexture();

            // bool IsValidDefaultTexture()
            // {
            //     return importer.textureShape == TextureImporterShape.Texture2D &&
            //            importer.textureType == TextureImporterType.Default;
            // }

            bool IsValidSpriteTexture()
            {
                return importer.textureType == TextureImporterType.Sprite && (importer.spriteImportMode == SpriteImportMode.Single || importer.spriteImportMode == SpriteImportMode.Multiple);
            }
        }
        
        private const string GENERATION_SETTINGS_SAVE_KEY = "SDFImportSettings";
        
        public static SDFGenerationSettings LoadGenerationSettings(AssetImporter importer)
        {
            var data = ImportSettingsUserData.Load(importer);
            var settings = data.Read(GENERATION_SETTINGS_SAVE_KEY, new SDFGenerationSettings());
            SDFGenerationSettingsBase.Validate(settings);
            return settings;
        }

        public static void SaveGenerationSettings(AssetImporter importer, SDFGenerationSettings settings)
        {
            var data = ImportSettingsUserData.Load(importer);
            data.Write(GENERATION_SETTINGS_SAVE_KEY, settings);
            data.Save();
        }

        /// <summary>
        /// Checks <see cref="IsValidTextureType"/> if so then loads sdf import settings and checks <see cref="SDFGenerationSettings.GenerateSDF"/>.
        /// If returned true that means texture should have sdf generated unless something goes wrong in import process.
        /// </summary>
        [SDFPipelineCompatible(SDFPipelineFlags.RegularAnd2023)]
        public static bool ShouldImportSDF(TextureImporter importer, out SDFGenerationSettings settings)
        {
            settings = new SDFGenerationSettings();
            if (importer == null) return false;

            var isTextureValid = IsValidTextureType(importer);
            
            if (!isTextureValid) return false;

            settings = LoadGenerationSettings(importer);
            return settings.GenerateSDF;
        }

        /// <inheritdoc cref="ShouldImportSDF(UnityEditor.TextureImporter,out SDFGenerationSettings)"/>
        /// <remarks>
        ///     Checks is main asset is texture, and loads texture importer.
        /// </remarks>
        [SDFPipelineCompatible(SDFPipelineFlags.RegularAnd2023)]
        public static bool ShouldImportSDF(string texturePath, out TextureImporter importer, out SDFGenerationSettings settings)
        {
            settings = new SDFGenerationSettings();
            importer = null;
            
            var isTexture = typeof(Texture).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(texturePath));
            if (!isTexture)
            {
                return false;
            }

            importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            return ShouldImportSDF(importer, out settings);
        }
        
        /// <summary>
        /// Returns true for Source and SDF sprites generated from regular pipeline
        /// as part of Texture
        /// </summary>
        [SDFPipelineCompatible(SDFPipelineFlags.RegularAnd2023)]
        public static bool IsPartOfRegularPipeline(Sprite sprite)
        {
            return ShouldImportSDF(AssetDatabase.GetAssetPath(sprite), out _, out _);
        }

        
        #endregion

        #region Import settings
        
        /// <summary>
        /// Attempts to fix textures so that sdf will be generated for them.
        /// Textures should be in sprite mode beforehand.
        /// </summary>
        [SDFPipelineCompatible(SDFPipelineFlags.RegularAnd2023)]
        public static void FixTextures(Texture[] textures)
        {
            var state = ImportSettingsState.CreateStaticState(textures);
            var defaultSettings = new SDFGenerationSettings();
            foreach (var container in state)
            {
                var settings = container.Settings;
                
                settings.GenerateSDF = true;
                
                if (settings.GradientSize <= 0)
                {
                    settings.GradientSize = defaultSettings.GradientSize;
                }
                    
                if (settings.ResolutionScale <= 0)
                {
                    settings.ResolutionScale = defaultSettings.ResolutionScale;
                }

                settings.BorderOffset = Mathf.Max(0, settings.BorderOffset);

                container.Settings = settings;
            }
            state.Apply();
            state.Dispose();
        }
        
        /// <summary>
        /// Will set particular sdf import settings to all textures and re-import them.
        /// Textures need to be pre-configured as sprites before.
        /// </summary>
        [SDFPipelineCompatible(SDFPipelineFlags.RegularAnd2023)]
        public static void SetImportSettings(Texture[] textures, SDFGenerationSettings settings)
        {
            var state = ImportSettingsState.CreateStaticState(textures);
            foreach (var container in state)
            {
                container.Settings = settings;
            }
            state.Apply();
            state.Dispose();
        }
        
        #endregion

        #region Regular pipeline direct loading

        /// <summary>
        /// Before directly loading the assets you might want to use <see cref="ShouldImportSDF(UnityEditor.TextureImporter,out SDFGenerationSettings)"/>
        /// Will load all assets at path and then return only assets of type <see cref="SDFSpriteMetadataAsset"/>
        /// All assets needed to be loaded because metadata asset is hidden.
        ///
        /// <see cref="SDFPipelineFlags.Everywhere"/> is used since <see cref="SDFAsset"/> contents can also be accessed this way.
        /// </summary>
        [SDFPipelineCompatible(SDFPipelineFlags.Everywhere)]
        public static IEnumerable<SDFSpriteMetadataAsset> LoadSpriteMetadataAssets(string sdfHostAssetPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(sdfHostAssetPath).OfType<SDFSpriteMetadataAsset>();
        }
        
        [SDFPipelineCompatible(SDFPipelineFlags.Everywhere)]
        public static IEnumerable<SDFSpriteMetadataAsset> LoadSpriteMetadataAssets(Object sdfHostAsset)
        {
            Assert.IsNotNull(sdfHostAsset);
            Assert.IsTrue(EditorUtility.IsPersistent(sdfHostAsset));
            return LoadSpriteMetadataAssets(AssetDatabase.GetAssetPath(sdfHostAsset));
        }

        /// <inheritdoc cref="LoadSpriteMetadataAsset(UnityEngine.Sprite)"/>
        /// <remarks>
        /// Slightly faster version for when you already know the path of the sprite.
        /// </remarks>
        [SDFPipelineCompatible(SDFPipelineFlags.RegularAnd2023)]
        private static SDFSpriteMetadataAsset LoadSpriteMetadataAsset(string path, Sprite sprite)
        {
            return LoadSpriteMetadataAssets(path).FirstOrDefault(asset =>
                asset._metadata.SourceSprite == sprite || asset._metadata.SDFSprite == sprite);
        }

        /// <summary>
        /// Will load SDF metadata asset for either sdf or source sprite.
        /// Note: All assets at path will be loaded
        /// </summary>
        [SDFPipelineCompatible(SDFPipelineFlags.RegularAnd2023)]
        public static SDFSpriteMetadataAsset LoadSpriteMetadataAsset(Sprite sprite)
        {
            return LoadSpriteMetadataAsset(AssetDatabase.GetAssetPath(sprite), sprite);
        }
        
        [SDFPipelineCompatible(SDFPipelineFlags.RegularAnd2023)]
        public static IEnumerable<SDFSpriteMetadataAsset> LoadSpriteMetadataAssets(IEnumerable<Sprite> sprites)
        {
            return sprites.Select(LoadSpriteMetadataAsset).Where(asset => asset != null);
        }
        

        #endregion

        /// <summary>
        /// Returns guids of persistent root textures assets that has sdf generated for it.
        /// Works only for a regular pipeline, to search for decoupled pipeline use <see cref="FindAllSDFAssets"/>. 
        /// </summary>
        /// <returns></returns>
        [SDFPipelineCompatible(SDFPipelineFlags.RegularAnd2023)]
        public static IEnumerable<string> FindAllTexturesWithGeneratedSDF()
        {
            var textures = AssetDatabase.FindAssets("t:Texture2D");
            foreach (var textureGUID in textures)
            {
                var texturePath = AssetDatabase.GUIDToAssetPath(textureGUID);
                if (ShouldImportSDF(texturePath, out _, out _))
                {
                    yield return textureGUID;
                }
            }
        }
        

        /// <summary>
        /// Will fill all required <see cref="SDFPipelineFlags"/> for sprite.
        /// Flag <see cref="SDFPipelineFlags.DecoupledSourceSprite"/> can be true for any
        /// sprite imported by unity in TextureImporter, but if this sprite was found as part of
        /// <see cref="SDFPipelineFlags.Regular"/> additional search for this flag even if
        /// <paramref name="searchForSDFAsset"/> is true will not be executed. 
        /// </summary>
        [SDFPipelineCompatible(SDFPipelineFlags.Everywhere)]
        public static SDFPipelineFlags GetPipelineFlags(Object asset, bool searchForSDFAsset, 
            out string path, out string firstSDFAssetGUID)
        {
            firstSDFAssetGUID = "";
#pragma warning disable CS0618 // Type or member is obsolete
            var flags = SDFPipelineFlags.Unknown;
#pragma warning restore CS0618 // Type or member is obsolete

            path = AssetDatabase.GetAssetPath(asset);
            // Sprite is either sdf or source sprite from a regular pipeline (part of texture)
            if (ShouldImportSDF(path, out _, out _))
            {
                flags |= SDFPipelineFlags.Regular;
            }
            else
            {
                // Sprite is sdf sprite from a decoupled pipeline (part of SDFAsset)
                if (IsDecoupledPipelineAsset(path))
                {
                    flags |= SDFPipelineFlags.DecoupledSDFSprite;
                }

                // If search enabled checks that maybe sprite has any SDFAsset using it
                if (searchForSDFAsset)
                {
                    var textureGUID = AssetDatabase.GUIDFromAssetPath(path);
                    firstSDFAssetGUID = FindSDFAssetsThatUsesTexture(textureGUID.ToString()).FirstOrDefault(); 
                    if (!string.IsNullOrEmpty(firstSDFAssetGUID))
                    {
                        flags |= SDFPipelineFlags.DecoupledSourceSprite;
                    }
                }
            }
            
#if SDF_NEW_SPRITE_METADATA
            flags |= SDFPipelineFlags.Unity2023;
#endif
            return flags;
        }
        
        /// <summary>
        /// This is a very tricky method, and its return result might not be consistent.
        /// It works relatively straightforward for <see cref="SDFPipelineFlags.Regular"/> pipeline,
        /// since all required data resides inside in the main texture asset.
        /// Also, it works fine if <paramref name="sprite"/> is part of <see cref="SDFPipelineFlags.DecoupledSDFSprite"/>,
        /// since metadata can be extracted easily due to sdf sprite and its meta-asset is also inside one asset.
        ///
        /// But when it comes for <paramref name="sprite"/> that is part of <see cref="SDFPipelineFlags.DecoupledSourceSprite"/>
        /// everything became non-deterministic and slow.
        /// If <paramref name="searchForSDFAsset"/> is ture then last step of search if others not yielded any results
        /// will be look up of all <see cref="SDFAsset"/>'s in a project and using first that uses <paramref name="sprite"/> texture
        /// as source texture (<see cref="SDFAssetImporter.Texture"/>).
        /// Lookup does not load all assets so its relativity quick, however, its result is not guaranteed
        /// since multiple <see cref="SDFAsset"/>'s can use one source texture, therefore, any of those assets can be
        /// used to retrieve <see cref="SDFSpriteMetadataAsset"/>.
        /// </summary>
        /// <param name="sprite">Sprite from any pipeline</param>
        /// <param name="metadataAsset">Output meta-asset generated either from a regular or decoupled pipeline</param>
        /// <param name="searchForSDFAsset">
        ///     If sprite is not part of regular pipeline (can be checked with <see cref="IsPartOfRegularPipeline"/>, <see cref="SDFPipelineFlags.Regular"/>)
        ///     And not sdf sprite from a decoupled pipeline (<see cref="SDFPipelineFlags.DecoupledSDFSprite"/>)
        ///     Then if <paramref name="searchForSDFAsset"/> is ture will execute project-wide
        ///     search for first <see cref="SDFAsset"/> that uses sprite texture as source texture,
        ///     and will extract <see cref="SDFSpriteMetadataAsset"/> from it.
        /// </param>
        /// <returns></returns>
        [SDFPipelineCompatible(SDFPipelineFlags.Everywhere)]
        public static bool TryGetSpriteMetadataAsset(Sprite sprite, bool searchForSDFAsset, out SDFSpriteMetadataAsset metadataAsset)
        {
            return TryGetSpriteMetadataAsset(sprite, searchForSDFAsset, out metadataAsset, out _, out _);
        }
        
        /// <inheritdoc cref="TryGetSpriteMetadataAsset"/>
        [SDFPipelineCompatible(SDFPipelineFlags.Everywhere)]
        internal static bool TryGetSpriteMetadataAsset(Sprite sprite, bool searchForSDFAsset, 
            out SDFSpriteMetadataAsset metadataAsset, out SDFPipelineFlags flags, out string firstSDFAssetGUID)
        {
            metadataAsset = null;
            if (sprite == null)
            {
                throw new ArgumentNullException(nameof(sprite));
            }

            if (!EditorUtility.IsPersistent(sprite))
            {
                throw new Exception("Sprite is not persistent");
            }

            flags = GetPipelineFlags(sprite, searchForSDFAsset, out var path,
                out firstSDFAssetGUID);
            
            // At this point sprite can either be: Source sprite or SDF sprite from a regular pipeline,
            // or any sprite from a decoupled pipeline.
            // Assuming we're dealing with sprite (sdf or source) from a regular pipeline,
            // Then check is texture type and import settings are valid to import sdf (therefore, sdf should be imported)
            if (flags.HasFlag(SDFPipelineFlags.Regular))
            {
#if SDF_NEW_SPRITE_METADATA
                // For unity 2023 can extract sprite directly from both sdf and source sprite
                return sprite.TryGetSpriteMetadataAsset(out metadataAsset);
#else
                // For older version need to load all meta-assets and find the one that
                // matches one of the sprites (sdf or source)
                metadataAsset = LoadSpriteMetadataAsset(path, sprite);
                return metadataAsset != null;
#endif
            }
            
            // If we're dealing with sdf sprite from a decoupled pipeline, then it's easy.
            if (flags.HasFlag(SDFPipelineFlags.DecoupledSDFSprite))
            {
#if SDF_NEW_SPRITE_METADATA
                // For unity 2023 just extract sprite metadata
                return sprite.TryGetSpriteMetadataAsset(out metadataAsset);
#else
                // For the older version we need to load SDFAsset and then look up for a meta-asset
                // that matches sdf sprite. Meta-asset for regular sprite also can be located this way.
                var sdfAsset = AssetDatabase.LoadAssetAtPath<SDFAsset>(AssetDatabase.GetAssetPath(sprite));
                return sdfAsset.TryGetSpriteMetadataAsset(sprite, out metadataAsset);
#endif
            }

            // At this point we know that import settings of sprite texture not valid to generate sdf from it,
            // and sprite is not sdf sprite from SDFAsset.
            // Therefore, the only way this sprite is anyhow involved in any sdf pipeline is by being source sprite for
            // any SDFAsset in a project, so all we can do here is try to search for the first sdf asset that has this
            // sprite texture used as a source.
            if (searchForSDFAsset && flags.HasFlag(SDFPipelineFlags.DecoupledSourceSprite))
            {
                var sdfAsset = AssetDatabase.LoadAssetAtPath<SDFAsset>(AssetDatabase.GUIDToAssetPath(firstSDFAssetGUID));
                return sdfAsset.TryGetSpriteMetadataAsset(sprite, out metadataAsset);
            }
            
            return false;
        }
        
        
        /// <summary>
        /// Is persistent sprite a product of sdf import?
        /// </summary>
        [SDFPipelineCompatible(SDFPipelineFlags.Everywhere)]
        public static bool IsSDFSprite(Sprite sprite)
        {
            // Don't need to search for sdf asset since sdf sprite is either sdf sprite from a regular pipeline
            // or sdf sprite from SDFAsset, in both cases we have the main asset for metadata.
            if (TryGetSpriteMetadataAsset(sprite, false, out var metadata))
            {
                return metadata.Metadata.SDFSprite == sprite;
            }

            return false;
        }

        /// <summary>
        /// Is a persistent sprite product of Unity sprite import and has sdf sprite generated from it?
        /// </summary>
        [SDFPipelineCompatible(SDFPipelineFlags.Everywhere)]
        public static bool IsSourceSprite(Sprite sprite, bool searchForSDFAsset)
        {
            if (TryGetSpriteMetadataAsset(sprite, searchForSDFAsset, out var metadata))
            {
                return metadata.Metadata.SourceSprite == sprite;
            }

            return false;
        }
        
        [SDFPipelineCompatible(SDFPipelineFlags.RegularAnd2023)]
        public static bool IsSourceSpriteFromRegularPipeline(Sprite sprite)
        {
            return IsSourceSprite(sprite, false);
        }
        
        
        /// <summary>
        /// Is two sprites part of sdf import?
        /// One of them is sdf sprite, and the other is source sprite that sdf was generated from.
        /// </summary>
        [SDFPipelineCompatible(SDFPipelineFlags.Everywhere)]
        public static bool IsSDFPair(Sprite a, Sprite b)
        {
            // Don't need to search for SDFAsset since in sdf pair at least one sprite is sdf sprite,
            // and metadata can be extracted from it without SDFAssets project lookup.
            if (TryGetSpriteMetadataAsset(a, false, out var metadataAsset) 
                || TryGetSpriteMetadataAsset(b, false, out metadataAsset))
            {
                var metadata = metadataAsset.Metadata;
                return (metadata.SDFSprite == a && metadata.SourceSprite == b) ||
                       (metadata.SDFSprite == b && metadata.SourceSprite == a);
            }

            return false;
        }
        
        #region Serialization
        
        /// <inheritdoc cref="TryRefreshSDFSpriteReferenceFields(string, Object, IEnumerable{string})"/>
        internal static void TryRefreshSDFSpriteReferenceFields(string path, SDFGraphic graphic)
        {
            TryRefreshSDFSpriteReferenceFields(path, graphic, graphic.GetUsedSpritesPropertyPaths());
        }

        
        /// <summary>
        /// Call this after re-import of sdf asset <see cref="SDFEditorUtil.TextureProcessed"/> that can be called from this property.
        /// to fix SDF assets that might still remain null due to weird unity object lifecycle.
        ///
        /// Will create <see cref="SerializedObject"/> out of target, go over all properties from <paramref name="propertyPaths"/>
        /// and will check is old value path is the same as newly imported path. If true, that means we using that asset and need to rest values.
        ///
        /// For some unknown for me reason right after re-import imported object, generated by importer (at least) is null,
        /// and its value is not straightaway restored, its similar to value being 'Missing' in object field.
        /// To overcome this setting object value of property to null, and then setting old value again.
        /// </summary>
        /// <param name="path">Imported asset path</param>
        /// <param name="target">Object that possibly using imported asset</param>
        /// <param name="propertyPaths">Paths to <see cref="SDFSpriteReference"/>'s that target is using</param>
        internal static void TryRefreshSDFSpriteReferenceFields(string path, Object target, IEnumerable<string> propertyPaths)
        {
            var serializedObject = new SerializedObject(target);
            foreach (var spriteReferencePath in propertyPaths)
            {
                var prop = serializedObject.FindProperty(
                    spriteReferencePath + "." + 
                    nameof(SDFSpriteReference._metadataAsset));
                
                // Getting old value, It's still valid even if object is null, since its C++ side still exist (apparently)
                var metaAsset = prop.objectReferenceValue;
                var lPath = AssetDatabase.GetAssetPath(metaAsset);
                // Checking is we working with same path of used asset
                if (lPath == path)
                {
                    // Important part were we resetting value to null, to make 'Missing' value clear its state.
                    prop.objectReferenceValue = null;
                    // Re assigning value
                    prop.objectReferenceValue = metaAsset;
                }
            }
            
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Dispose();
        }
        
        
        private static void LogSerializationMessage(object message, Object context, LogType logType = LogType.Log)
        {
            Debug.LogFormat(logType, LogOption.NoStacktrace, context, "<b>[SDFImage Serialization]:</b> {0}", message);
        }
        
        /// <summary>
        /// In case of versions porting checks for legacy sprite (how sprite was serialized for previous version)
        /// loads metadata and sprite to new fields.
        /// </summary>
        [SDFPipelineCompatible(SDFPipelineFlags.Everywhere)]
        public static void TryRestoreSerialization(SDFImage image)
        {
            using var so = new SerializedObject(image);
            var legacySprite = so.FindProperty(nameof(SDFImage._legacySprite));
            var spriteReference = so.FindProperty(nameof(SDFImage._sdfSpriteReference));
            var refMetadata = spriteReference.FindPropertyRelative(nameof(SDFSpriteReference._metadataAsset));
            so.Update();
            
            // Restoring legacy sprite
            if (legacySprite.objectReferenceValue != null && refMetadata.objectReferenceValue == null)
            {
                TryGetSpriteMetadataAsset((Sprite)legacySprite.objectReferenceValue, true, out var metadataAsset);
                refMetadata.objectReferenceValue = metadataAsset;
                legacySprite.objectReferenceValue = null;
                LogSerializationMessage($"Restored metadata \"{refMetadata.objectReferenceValue}\" from legacy sprite \"{legacySprite.objectReferenceValue}\"", image);
            }

            so.ApplyModifiedProperties();
        }
         
        #endregion


        #region SDF meta assets macros

        /// <summary>
        /// Will load sprites that is used in atlas.
        /// In atlas <see cref="SpriteAtlasExtensions.GetPackables"/> array can be three types:
        ///    - Sprite
        ///    - Texture
        ///    - Folder
        /// For first two its simple, add sprite or load all sprites.
        /// For folder, it requires recursive search for all sprites in folders.
        ///
        /// NOTE:
        ///    If sprites in atlas are from a regular sdf pipeline, it will return basically sdf-source sprite pairs.
        ///    So if that sprite was used in conversion to <see cref="SDFSpriteMetadataAsset"/> filter them first.
        /// </summary>
        public static IEnumerable<Sprite> LoadSpritesFromAtlas(SpriteAtlas atlas)
        {
            var sprites = new HashSet<Sprite>();
            var packables = atlas.GetPackables();
            var spriteHostAssetPaths = new HashSet<string>();

            // Sprites - if sprite encountered, simply add it to the list
            sprites.AddRange(packables
                .OfType<Sprite>());

            // Textures - fill textures paths to load sprites later
            spriteHostAssetPaths.AddRange(packables
                .OfType<Texture>()
                .Select(AssetDatabase.GetAssetPath));


            // Folders - check is the asset folder, add it paths to load textures from it later
            var folders = packables
                .Select(AssetDatabase.GetAssetPath)
                .Where(AssetDatabase.IsValidFolder)
                .ToArray();

            spriteHostAssetPaths.AddRange(AssetDatabase.FindAssets("t:Sprite", folders)
                .Select(AssetDatabase.GUIDToAssetPath));

            foreach (var path in spriteHostAssetPaths)
                sprites.AddRange(AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<Sprite>());

            // Verify that sprite is bound to this atlas
            return sprites.Where(atlas.CanBindTo);
        }

        public static IEnumerable<Sprite> GetSourceSprites(this IEnumerable<SDFSpriteMetadataAsset> metadataAssets)
        {
            return metadataAssets.Select(asset => asset.Metadata.SourceSprite);
        }

        public static IEnumerable<Sprite> GetSDFSprites(this IEnumerable<SDFSpriteMetadataAsset> metadataAssets)
        {
            return metadataAssets.Select(asset => asset.Metadata.SDFSprite);
        }

        #endregion
    }
}