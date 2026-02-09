using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using nickeltin.Core.Editor;
using nickeltin.InternalBridge.Editor;
using nickeltin.SDF.Runtime;
using nickeltin.SDF.Runtime.DecoupledPipeline;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace nickeltin.SDF.Editor.DecoupledPipeline
{
    [ScriptedImporter(VERSION, EXT)]
    public class SDFAssetImporter : ScriptedImporter
    {
        public const string EXT = "sdfasset";
        public const int LOCAL_VERSION = 3;
        
        /// <summary>
        ///  Multiplying a version by two for a new pipeline
        /// </summary>
        public const int VERSION =
#if SDF_NEW_SPRITE_METADATA
            (LOCAL_VERSION + SDFImporter.VERSION) * 2;
#else
            LOCAL_VERSION + SDFImporter.VERSION;
#endif
        
        [SerializeField] internal SDFGenerationSettings _importSettings;
        [SerializeField] internal LazyLoadReference<Texture> _texture = null;

        public SDFGenerationSettings ImportSettings
        {
            get => _importSettings;
            set
            {
                _importSettings = value;
                EditorUtility.SetDirty(this);
            }
        }

        public Texture Texture
        {
            get => _texture.asset;
            set
            {
                _texture.asset = value;
                EditorUtility.SetDirty(this);
            }
        }

        public bool IsValid() => !_texture.isBroken;

        public static SDFAssetImporter Get(string path)
        {
            return (SDFAssetImporter)GetAtPath(path);
        }
        
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var postprocesSpriteStep = new PostprocessSpritesOverride();
            TryImportTexture(_texture,this, ctx, ImportSettings, new ImportStep[]
            {
                new PostprocessTexture(), postprocesSpriteStep
            });
            
            var result = postprocesSpriteStep.Result;
            var asset = ScriptableObject.CreateInstance<SDFAsset>();
            asset.name = "SDFAsset";
            if (result.MetadataAssets != null) asset._spriteMetadataAssets = result.MetadataAssets.ToList();
            ctx.AddObjectToAsset("ImportObject", asset, result.PackedTexture);
            ctx.SetMainObject(asset);
        }
        
        private static void TryImportTexture(LazyLoadReference<Texture> textureRef, AssetImporter mainImporter,
            AssetImportContext ctx, SDFGenerationSettings settings, ImportStep[] importSteps)
        {
            if (textureRef.isBroken) return;

            var texture = textureRef.asset;
            if (texture == null) return;
            
            // Adding texture as dependency before any other validation since texture validity can change later on
            // so it's better to ensure dependency 
            var path = AssetDatabase.GetAssetPath(texture);
            ctx.DependsOnArtifact(path);
            
            var texImporter = (TextureImporter)GetAtPath(path);

            if (!SDFEditorUtil.IsValidTextureType(texImporter))
            {
                return;
            }

            // When GPU not available using make sure that texture is readable for the CPU copy
            if (!texImporter.isReadable)
            {
                SDFGenerationEditorUtil.MakeReadable(texImporter);
                return;
            }
            
            var importer = SDFImporter.CreateDecoupled(texImporter, ctx, mainImporter, settings, importSteps);
            importer.SetTexture((Texture2D)texture);
            importer.SetSprites(LoadSpritesInCorrectOrder(texImporter).ToArray());
            while (importer.Step()) { }
        }

        /// <summary>
        /// In order to work properly sprites need to be sorted in same order as sprite rects in importer.
        /// Loads sprites and then sorts them to match sequence in importer.
        /// </summary>
        private static IEnumerable<Sprite> LoadSpritesInCorrectOrder(TextureImporter texImporter)
        {
            var rects = texImporter.GetSpriteRects();
            var nameToSprite = AssetDatabase.LoadAllAssetsAtPath(texImporter.assetPath)
                .Where(a => a is Sprite s && !SDFEditorUtil.IsSDFSprite(s)).Cast<Sprite>()
                .ToDictionary(sprite => sprite.name);

            foreach (var rect in rects)
            {
                if (nameToSprite.TryGetValue(rect.name, out var sprite))
                    yield return sprite;
            }
        }

        private readonly struct RepeatedEnumerator<T> : IEnumerable<T>, IEnumerator<T>
        {
            public RepeatedEnumerator(T current) => Current = current;
            public bool MoveNext() => true;
            public void Reset() { }
            public T Current { get; }
            object IEnumerator.Current => Current;
            public void Dispose() { }
            public IEnumerator<T> GetEnumerator() => this;
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        
        public static List<string> CreateForTextures(IEnumerable<TextureImporter> importers, SDFGenerationSettings settings)
        {
            return CreateForTextures(importers, new RepeatedEnumerator<SDFGenerationSettings>(settings));
        }
        
        public static List<string> CreateForTextures(IEnumerable<TextureImporter> importers,
            IEnumerable<SDFGenerationSettings> settings)
        {
            using var e1 = importers.GetEnumerator();
            using var e2 = settings.GetEnumerator();
            var result = new List<string>();
            while (e1.MoveNext() && e2.MoveNext()) result.Add(CreateForTexture(e1.Current, e2.Current));
            return result;
        }
        
        /// <summary>
        /// Call this to generate sdf asset for particular texture
        /// </summary>
        /// <returns>
        /// Returns path of newly created asset
        /// </returns>
        public static string CreateForTexture(TextureImporter textureImporter, SDFGenerationSettings settings)
        {
            var fileName = Path.GetFileNameWithoutExtension(textureImporter.assetPath);
            var path = Path.Combine(Path.GetDirectoryName(textureImporter.assetPath)!, fileName + "_SDF." + EXT);
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            var serializedAsset = EditorSerialization.SerializeObjectToString<SDFAssetSourceAsset>();
            _ProjectWindowUtil.CreateScriptAssetWithContent(path, serializedAsset);
            var importer = (SDFAssetImporter)GetAtPath(path);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(textureImporter.assetPath);
            importer._texture = new LazyLoadReference<Texture>(texture);
            importer._importSettings = settings;
            // For some reason otherwise importer settings is not written to meta file
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            return path;
        }
    }
}