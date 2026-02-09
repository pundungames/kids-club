using nickeltin.SDF.Runtime;
using UnityEditor;
using UnityEditor.Sprites;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;
using SM = UnityEditor.SceneManagement.EditorSceneManager;

namespace nickeltin.SDF.Editor
{
    internal static class PreviewUtil
    {
        public const int RENDER_WIDTH = 1024;
        public const int RENDER_HEIGHT = 1024;
        
        public static readonly int ALPHA_TEX_ID = Shader.PropertyToID("_AlphaTex");
        public const string SDF_PREVIEW_KWRD = "SDF_PREVIEW";
        
        public static readonly Texture2D ClearTex;

        private class RenderingSetup
        {
            public readonly Canvas Canvas;
            public readonly Camera Camera;
            public readonly PreviewImageData PreviewImage;

            public RenderTexture RenderTex { get; private set; }

            public RenderingSetup(Canvas canvas, Camera camera, SDFImage sdfImage)
            {
                PreviewImage = new PreviewImageData(sdfImage);
                Canvas = canvas;
                Camera = camera;
            }
            
            public void VerifyRenderTexture(int width, int height, FilterMode filterMode)
            {
                var canvasRect = Canvas.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(width,height);
                var desc = new RenderTextureDescriptor(width, height, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.D32_SFloat_S8_UInt)
                {
                    sRGB = true,
                };
                if (RenderTex == null)
                {
                    RenderTex = RenderTexture.GetTemporary(desc);
                }
                else if (!RenderTex.descriptor.Equals(desc))
                {
                    DisposeRenderTexture();
                    RenderTex = RenderTexture.GetTemporary(desc);
                }
                
                RenderTex.filterMode = filterMode;

                Camera.targetTexture = RenderTex;
            }

            public void ClearRenderTex()
            {
                Graphics.Blit(ClearTex, RenderTex);
            }
            
            private void DisposeRenderTexture()
            {
                if (RenderTex != null)
                {
                    Camera.targetTexture = null;
                    RenderTexture.ReleaseTemporary(RenderTex);
                }
            }

            public void Dispose()
            {
                PreviewImage.SerializedObject.Dispose();
                DisposeRenderTexture();   
            }
        }

        public class PreviewImageData
        {
            public readonly SDFImage Image;
            public readonly SerializedObject SerializedObject;

            public PreviewImageData(SDFImage image)
            {
                Image = image;
                SerializedObject = new SerializedObject(Image);
            }
        }
        
        private static readonly Scene _bakingScene;
        private static RenderingSetup _renderingSetup;

        static PreviewUtil()
        {
            _bakingScene = SM.NewPreviewScene();

            ClearTex = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_UNorm, 0, TextureCreationFlags.None);
            ClearTex.SetPixel(0,0, Color.clear);
            ClearTex.Apply();

            CreateRenderingSetup();
            
            AssemblyReloadEvents.beforeAssemblyReload += BeforeRecompile;
        }

        public static PreviewImageData PreviewImage => _renderingSetup.PreviewImage;

        private static void BeforeRecompile()
        {
            //Disposing before compile if not disposed manually.
            _renderingSetup.Dispose();
            
            SM.ClosePreviewScene(_bakingScene);
        }

        private static void CreateRenderingSetup()
        {
            var renderRoot = AddGameObject(null, "SDFPreview");
            AddGameObject(renderRoot, "Canvas").AddComponent<Canvas>(out var canvas);
            AddGameObject(renderRoot, "Camera").AddComponent<Camera>(out var camera);
            AddGameObject(canvas.gameObject, "SDFImage").AddComponent<SDFImage>(out var sdfImage);

            sdfImage.PreserveAspect = true;
            var sdfImgRect = sdfImage.rectTransform;
            sdfImgRect.anchorMin = Vector2.zero;
            sdfImgRect.anchorMax = Vector2.one;
            sdfImgRect.sizeDelta = Vector2.zero;

            camera.clearFlags = CameraClearFlags.Depth;
            camera.orthographic = true;
            camera.scene = _bakingScene;

            canvas.worldCamera = camera;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.planeDistance = 1;

            _renderingSetup = new RenderingSetup(canvas, camera, sdfImage);
        }

        private static GameObject AddComponent<T>(this GameObject go, out T component) where T : Component
        {
            component = ObjectFactory.AddComponent<T>(go);
            return go;
        }
        
        private static GameObject AddGameObject(GameObject parent, string name = "TempGO")
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, _bakingScene);
            if (parent != null)
            {
                go.transform.SetParent(parent.transform);
            }
            return go;
        }

        /// <summary>
        /// Setups SDF on sdf image, get it with <see cref="PreviewImage"/>
        /// Will disable material <see cref="SDF_PREVIEW_KWRD"/>
        /// </summary>
        /// <returns></returns>
        public static RenderTexture Render(SDFSpriteMetadataAsset metadataAsset, Material material, int width = RENDER_WIDTH, int height = RENDER_HEIGHT)
        {
            if (metadataAsset == null || metadataAsset._metadata.SourceSprite == null)
            {
                _renderingSetup.ClearRenderTex();
                return _renderingSetup.RenderTex;
            }
            
            var sdfImg = PreviewImage.Image;
            sdfImg.SDFSpriteReference = new SDFSpriteReference(metadataAsset);
            sdfImg.material = material;
            
            // TODO: use border offset instead
            // sdfImg.rectTransform.localScale = (1 / sdfImg.MeshScale) * Vector3.one;

            material.DisableKeyword(SDF_PREVIEW_KWRD);

            var tex = SpriteUtility.GetSpriteTexture(sdfImg.ActiveSprite, false);
            _renderingSetup.VerifyRenderTexture(width, height, tex.filterMode);
            _renderingSetup.ClearRenderTex();
            _renderingSetup.Camera.gameObject.SetActive(true);
            _renderingSetup.Camera.Render();
            
            return _renderingSetup.RenderTex;
        }

        /// <inheritdoc cref="Render(UnityEngine.Texture,UnityEngine.Material,int,int)"/>
        /// <remarks>
        /// Will use <paramref name="sdfTexture"/> for width and height, if you want to render in different resolution use overload.
        /// </remarks>
        public static RenderTexture Render(Texture sdfTexture, Material material)
        {
            return Render(sdfTexture, material, sdfTexture.width, sdfTexture.height);
        }


        /// /// <summary>
        /// Render whole sdf texture.
        /// Will enable material <see cref="SDF_PREVIEW_KWRD"/>.
        /// If <paramref name="sdfTexture"/> filter type is pointer (means its pixel art) shader property <see cref="GRADIENT_SCALE_ID"/> will be forced to be 0.
        /// </summary>
        /// <param name="sdfTexture">Will be injected to the material as alpha tex</param>
        /// <param name="material"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <remarks>
        /// Render resolution can be changed 
        /// </remarks>
        public static RenderTexture Render(Texture sdfTexture, Material material, int width, int height)
        {
            if (sdfTexture == null)
            {
                _renderingSetup.ClearRenderTex();
                return _renderingSetup.RenderTex;
            }
            
            material.EnableKeyword(SDF_PREVIEW_KWRD);
            material.SetTexture(ALPHA_TEX_ID, sdfTexture);


            // var pointFilter = sdfTexture.filterMode == FilterMode.Point;
            // var gradientSize = 0f;
            // if (pointFilter)
            // {
            //     gradientSize = material.GetFloat(GRADIENT_SCALE_ID);
            //     material.SetFloat(GRADIENT_SCALE_ID, 0);
            // }

            _renderingSetup.VerifyRenderTexture(width, height, sdfTexture.filterMode);
            _renderingSetup.Camera.gameObject.SetActive(false);
            _renderingSetup.ClearRenderTex();
            
            Graphics.Blit(null, _renderingSetup.RenderTex, material);

            // if (pointFilter)
            // {
            //     material.SetFloat(GRADIENT_SCALE_ID, gradientSize);
            // }
            
            return _renderingSetup.RenderTex;
        }

        /// <summary>
        /// Reads texture from GPU to CPU
        /// </summary>
        /// <param name="renderTexture"></param>
        /// <returns></returns>
        public static Texture2D Bake(RenderTexture renderTexture)
        {
            RenderTexture.active = renderTexture;

            var result = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
            
            // Copy to CPU
            result.ReadPixels(new Rect(0, 0, result.width, result.height), 0, 0);
            result.Apply();

            RenderTexture.active = null;

            return result;
        }
    }
}