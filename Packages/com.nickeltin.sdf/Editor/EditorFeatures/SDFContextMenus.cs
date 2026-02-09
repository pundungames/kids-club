using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using nickeltin.InternalBridge.Editor;
using nickeltin.SDF.Editor.DecoupledPipeline;
using nickeltin.SDF.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace nickeltin.SDF.Editor
{
    internal static partial class SDFContextMenus
    {
        #region Property Binding
        
        private readonly struct PropertyBinding
        {
            public readonly string From;
            public readonly string To;

            public readonly Func<object, object> FromToConverter;
            public readonly Func<object, object> ToFromConverter;
            

            public PropertyBinding(string from, string to) : this(from, to, DefaultConverter, DefaultConverter)
            {
                From = from;
                To = to;
            }

            public PropertyBinding(string from, string to, Func<object, object> fromToConverter, Func<object, object> toFromConverter)
            {
                From = from;
                To = to;
                FromToConverter = fromToConverter;
                ToFromConverter = toFromConverter;
            }

            public void Transfer(object fromTarget, object toTarget, bool inverted)
            {
                if (inverted)
                {
                    (fromTarget, toTarget) = (toTarget, fromTarget);
                }
                
                var fromType = fromTarget.GetType();
                var toType = toTarget.GetType();

                if (inverted)
                {
                    SetValue(GetMemberInfo(fromType, From), 
                        fromTarget, 
                        ToFromConverter(
                            GetValue(
                                GetMemberInfo(toType, To), toTarget)));
                }
                else
                {
                    SetValue(GetMemberInfo(toType, To), 
                        toTarget, 
                        FromToConverter(
                            GetValue(
                                GetMemberInfo(fromType, From), fromTarget)));
                }
            }

            private static MemberInfo GetMemberInfo(Type type, string member)
            {
                var prop = type.GetProperty(member,
                    BindingFlags.Public | 
                    BindingFlags.Instance | 
                    BindingFlags.GetProperty | 
                    BindingFlags.SetProperty);

                if (prop != null) return prop;
                
                var field = type.GetField(member,
                    BindingFlags.Public | 
                    BindingFlags.Instance | 
                    BindingFlags.GetField | 
                    BindingFlags.SetField);

                if (field == null)
                {
                    Debug.LogError($"Can't find member {member} in type {type}");    
                }
                
                return field;
            }

            private static void SetValue(MemberInfo memberInfo, object target, object value)
            {
                try
                {
                    switch (memberInfo)
                    {
                        case PropertyInfo propertyInfo:
                            propertyInfo.SetValue(target, value);
                            break;
                        case FieldInfo fieldInfo:
                            fieldInfo.SetValue(target, value);
                            break;
                    }
                }
                finally { }
            }
            
            private static object GetValue(MemberInfo memberInfo, object target)
            {
                try
                {
                    switch (memberInfo)
                    {
                        case PropertyInfo propertyInfo:
                            return propertyInfo.GetValue(target);
                        case FieldInfo fieldInfo:
                            return fieldInfo.GetValue(target);
                    }
                }
                finally { }

                return null;
            }
            
            public static implicit operator PropertyBinding (string path)
            {
                return new PropertyBinding(path, path);
            }

            private static object DefaultConverter(object value) => value;
        }
        
        private static readonly PropertyBinding SPRITE_TO_SDF_REF = new(nameof(Image.sprite), nameof(SDFImage.SDFSpriteReference),
            o =>
            {
                var sprite = o as Sprite;
                if (sprite != null)
                {
                    if (SDFEditorUtil.TryGetSpriteMetadataAsset(sprite, true, out var metaAsset)) 
                        return new SDFSpriteReference(metaAsset);

                    Debug.Log($"SDF Metadata for sprite {sprite} not found");
                }

                return default;
            }, o =>
            {
                var reference = (SDFSpriteReference)o;
                return reference.SourceSprite;
            });
        
        private static readonly HashSet<PropertyBinding> GRAPHIC_PROPS = new()
        {
            nameof(MaskableGraphic.raycastPadding),
            nameof(MaskableGraphic.color),
            nameof(MaskableGraphic.maskable),
            nameof(MaskableGraphic.raycastTarget),
            nameof(MaskableGraphic.material),
        };
        
        private static readonly HashSet<PropertyBinding> IMG_PROPS = new HashSet<PropertyBinding>(GRAPHIC_PROPS)
        {
            new(nameof(Image.type), nameof(SDFImage.ImageType)),
            new(nameof(Image.fillAmount), nameof(SDFImage.FillAmount)),
            new(nameof(Image.fillCenter), nameof(SDFImage.FillCenter)),
            new(nameof(Image.fillClockwise), nameof(SDFImage.FillClockwise)),
            new(nameof(Image.fillMethod), nameof(SDFImage.FillMethod)),
            new(nameof(Image.fillOrigin), nameof(SDFImage.FillOrigin)),
            new(nameof(Image.useSpriteMesh), nameof(SDFImage.UseSpriteMesh)),
            new(nameof(Image.preserveAspect), nameof(SDFImage.PreserveAspect)),
            new(nameof(Image.pixelsPerUnitMultiplier), nameof(SDFImage.PixelsPerUnitMultiplier)),
            new(nameof(Image.color), nameof(SDFImage.MainColor)),
            SPRITE_TO_SDF_REF,
        };

        
        private static TNewComponent ReplaceComponent<TNewComponent>(Component oldComponent, 
            IEnumerable<PropertyBinding> properties, bool inverted) 
            where TNewComponent : Component
        {
            var oldComponentType = oldComponent.GetType();
            var go = oldComponent.gameObject;
            var oldComponentCopy = EditorUtility.CreateGameObjectWithHideFlags("SDFReplacementTemp", 
                HideFlags.HideAndDontSave, oldComponentType).GetComponent(oldComponentType);
            EditorUtility.CopySerializedManagedFieldsOnly(oldComponent, oldComponentCopy);
            
            Undo.DestroyObjectImmediate(oldComponent);
            
            var newComponent = ObjectFactory.AddComponent<TNewComponent>(go);
            
            foreach (var property in properties)
            {
                property.Transfer(oldComponentCopy, newComponent, inverted);
            }
            
            Object.DestroyImmediate(oldComponentCopy.gameObject);

            return newComponent;
        }
        
        private static void CreateGraphic<GraphicType>(MenuCommand command)
        {
            var go = new GameObject(typeof(GraphicType).Name, typeof(RectTransform), typeof(GraphicType));
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100f, 100f);
            _MenuOptions.PlaceUIElementRoot(go, command);
        }

        #endregion
        
        #region Sprite
        
        private const string OPEN_SDF_META_CONTEXT = "CONTEXT/Sprite/Find SDF Metadata";
        
        
        [MenuItem(OPEN_SDF_META_CONTEXT)]
        private static void OpenSDFMetadata_Context(MenuCommand command)
        {
            var sprite = command.context as Sprite;
            if (SDFEditorUtil.TryGetSpriteMetadataAsset(sprite, true, out var asset))
            {
                ProjectWindowUtil.ShowCreatedAsset(asset);
            }
        }
        
        [MenuItem(OPEN_SDF_META_CONTEXT, true)]
        private static bool OpenSDFMetadata_Validator(MenuCommand command)
        {
            return IsValidSDFAsset(command, out _);
        }

        #endregion
        
        #region Texture
        
        public const string SELECT_SDF_META = "Select SDFSpriteMetadataAsset's";
        private const string SELECT_SDF_META_T_CONTEXT = "CONTEXT/TextureImporter/" + SELECT_SDF_META;
        
        [MenuItem(SELECT_SDF_META_T_CONTEXT)]
        public static void SelectSDFMetaAssets_Context(MenuCommand command)
        {
            Selection.objects = SDFEditorUtil.LoadSpriteMetadataAssets(command.context).ToArray();
        }
        
        [MenuItem(SELECT_SDF_META_T_CONTEXT, true)]
        public static bool SelectSDFSprites_Validator(MenuCommand command)
        {
            return IsValidSDFMainAsset(command, out _);
        }

        [MenuItem("CONTEXT/Texture/Extract")]
        private static void ExtractTexture(MenuCommand command)
        {
            if (command.context is Texture2D texture)
            {
                // Try get default path
                var defaultPath = "Assets";
                if (_ProjectWindowUtil.TryGetActiveFolderPath(out var activeFolder))
                    defaultPath = activeFolder;

                // Open save dialog
                var filePath = EditorUtility.SaveFilePanel(
                    "Save Texture As PNG",
                    defaultPath,
                    texture.name + ".png",
                    "png");

                if (string.IsNullOrEmpty(filePath))
                    return;

                // Make texture readable
                var tmpRT = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(texture, tmpRT);

                var previous = RenderTexture.active;
                RenderTexture.active = tmpRT;

                var readableTex = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                readableTex.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                readableTex.Apply();

                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(tmpRT);

                // Save PNG
                var pngData = readableTex.EncodeToPNG();
                File.WriteAllBytes(filePath, pngData);
                Object.DestroyImmediate(readableTex);

                AssetDatabase.Refresh();

                // If saved under Assets or Packages, try import
                if (filePath.StartsWith(Application.dataPath) ||
                    filePath.StartsWith(Path.Combine(Directory.GetCurrentDirectory(), "Packages")))
                {
                    var projectPath = Directory.GetCurrentDirectory().Replace('\\', '/');
                    var relativePath = filePath.Replace(projectPath + "/", "");

                    AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);

                    if (texture.format == TextureFormat.Alpha8)
                    {
                        var importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                        if (importer != null)
                        {
                            var settings = importer.GetDefaultPlatformTextureSettings();
                            settings.format = TextureImporterFormat.Alpha8;
                            importer.SetPlatformTextureSettings(settings);

                            EditorUtility.SetDirty(importer);
                            importer.SaveAndReimport();

                            Debug.Log($"Texture saved and imported with Alpha8 settings: {relativePath}");
                        }
                    }
                }
                else
                {
                    Debug.Log($"Texture saved outside project: {filePath}");
                }
            }
            else
            {
                Debug.LogError("Context is not a Texture2D.");
            }
        }
        
        #endregion


        [MenuItem(MenuPaths.TOOLBAR + "Welcome Window", priority = 1)]
        private static void OpenLandingWindow() => SDFLandingWindow.ShowWindow();

        [MenuItem(MenuPaths.TOOLBAR + "Open Documentation", priority = 2)]
        private static void OpenDocumentation() => NickeltinSDF.OpenDocumentation();

        private static bool IsValidSDFAsset(MenuCommand command, out SDFPipelineFlags flags)
        {
            flags = SDFEditorUtil.GetPipelineFlags(command.context, true, out _, out _);
            return flags.HasFlag(SDFPipelineFlags.Regular) 
                   || flags.HasFlag(SDFPipelineFlags.DecoupledSDFSprite) 
                   || flags.HasFlag(SDFPipelineFlags.DecoupledSourceSprite);
        }

        private static bool IsValidSDFMainAsset(MenuCommand command, out SDFPipelineFlags flags)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            flags = SDFPipelineFlags.Unknown;
#pragma warning restore CS0618 // Type or member is obsolete
            return (command.context is TextureImporter || command.context is SDFAssetImporter) && IsValidSDFAsset(command, out flags);
        }
        
        #region SDF Image

        private const string SDF_IMG_CONTEXT = "CONTEXT/" + nameof(SDFImage) + "/";
        private const string IMAGE_TO_SDF = "CONTEXT/" + nameof(Image) + "/Convert to SDF Image";
        private const string SDF_TO_IMAGE = SDF_IMG_CONTEXT + "Convert to Image";
        private const string CREATE_MENU = "GameObject/UI/" + nameof(SDFImage);
        private const string RESTORE_SERIALIZATION = SDF_IMG_CONTEXT + "Restore Serialization";
        
        
        [MenuItem(RESTORE_SERIALIZATION)]
        private static void TryRestoreSerialization_Context(MenuCommand command)
        {
            SDFEditorUtil.TryRestoreSerialization(command.context as SDFImage);
        }

        
        [MenuItem(CREATE_MENU)]
        private static void CreateSDFImage(MenuCommand command)
        {
            CreateGraphic<SDFImage>(command);
        }

        
        [MenuItem(IMAGE_TO_SDF)]
        private static void ImageToSDF(MenuCommand command)
        {
            // var sprite = ((Image)command.context).sprite;
            var sdfImage = ReplaceComponent<SDFImage>((Image)command.context, IMG_PROPS, false);
            // sdfImage.color = 
            
        }
        
        [MenuItem(IMAGE_TO_SDF, true)]
        private static bool ConvertToSDFImage_Validator(MenuCommand command)
        {
           return command.context is Image && command.context is not SDFImage;
        }
        
        
        [MenuItem(SDF_TO_IMAGE)]
        private static void SDFToImage(MenuCommand command)
        {
            ReplaceComponent<Image>((SDFImage)command.context, IMG_PROPS, true);
        }
        
        
        [MenuItem(SDF_TO_IMAGE, true)]
        private static bool ConvertToImage_Validator(MenuCommand command)
        {
            return command.context is SDFImage;
        }

        #endregion
        
        #region Pure SDF Image
        
        private const string P_CREATE_MENU = "GameObject/UI/" + nameof(PureSDFImage);
        private const string P_RS_CREATE_MENU = "GameObject/UI/" + nameof(PureSDFImage) + " [Multilayer]";
        
        [MenuItem(P_CREATE_MENU)]
        private static void CreatePureSDFImage(MenuCommand command)
        {
            CreateGraphic<PureSDFImage>(command);
        }

        [MenuItem(P_RS_CREATE_MENU)]
        private static void CreatePureSDFImageRenderStack(MenuCommand command)
        {
            var root = new GameObject("Pure SDF Image Rendering Stack", typeof(RectTransform));
            var shadow = new GameObject("Shadow", typeof(RectTransform)).AddComponent<PureSDFImage>();
            var outline = new GameObject("Outline", typeof(RectTransform)).AddComponent<PureSDFImage>();
            var face = new GameObject("Face", typeof(RectTransform)).AddComponent<PureSDFImage>();
            
            shadow.transform.SetParent(root.transform);
            outline.transform.SetParent(root.transform);
            face.transform.SetParent(root.transform);

            shadow.Offset = new Vector2(10, -10);
            shadow.color = new Color(0f, 0f, 0f, 0.5f);
            shadow.Width = 0.6f;

            outline.Width = 0.6f;
            outline.color = Color.black;
            
            root.AddComponent<PureSDFImageRenderingStack>();
            _MenuOptions.PlaceUIElementRoot(root, command);
        }
        
        #endregion
    }
}