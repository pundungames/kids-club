using nickeltin.SDF.Runtime;
using UnityEditor;
using UnityEngine;

namespace nickeltin.SDF.Editor
{
    internal static class FLRSceneVisibilityManager
    {
        private static bool _changing;
        private static readonly int _isSceneViewCameraKeyword = Shader.PropertyToID("_IsSceneViewCamera");
        
        [InitializeOnLoadMethod]
        private static void Init()
        {
            SceneVisibilityManager.visibilityChanged += RefreshVisibilityAndPicking;
            SDFFirstLayerRenderer.activeRenderersChanged += RefreshVisibilityAndPicking;
            Camera.onPreRender += OnCameraPreRender;
            Camera.onPostRender += OnCameraPostRender;
            RefreshVisibilityAndPicking();
        }
        
        private static void OnCameraPreRender(Camera cam)
        {
            if (cam.cameraType == CameraType.SceneView)
            {
                Shader.SetGlobalFloat(_isSceneViewCameraKeyword, 1f);
            }
        }

        private static void OnCameraPostRender(Camera cam)
        {
            if (cam.cameraType == CameraType.SceneView)
            {
                Shader.SetGlobalFloat(_isSceneViewCameraKeyword, 0f);
            }
        }

        private static void RefreshVisibilityAndPicking()
        {
            // To prevent stack overflow
            if (_changing)
                return;
            
            _changing = true;
            
            
            foreach (var reference in SDFFirstLayerRenderer.activeRenderers)
            {
                if (!reference.TryGetTarget(out var flr)) continue;
                if (flr.Owner == null) continue;
                
                var isHidden = SceneVisibilityManager.instance.IsHidden(flr.Owner.gameObject);
                flr._isHiddenInSceneView = isHidden;
                flr.Owner.SetMaterialDirty();
                flr.SetMaterialDirty();
            }

            _changing = false;
        }
    }
}