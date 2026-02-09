using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.SceneTemplate;
using UnityEngine;

namespace nickeltin.InternalBridge.Editor
{
    public abstract class _SnapshotUtils
    {
        public delegate void OnTextureReady(Texture2D texture);

        public static Texture2D TakeCameraSnapshot([NotNull] Camera camera, bool compress = true)
        {
            return SnapshotUtils.TakeCameraSnapshot(camera, compress);
        }
        
        public static void TakeGameViewSnapshot([NotNull] EditorWindow gameView, OnTextureReady onTextureReadyCallback, bool compress = true)
        {
            SnapshotUtils.TakeGameViewSnapshot(gameView, new SnapshotUtils.OnTextureReady(onTextureReadyCallback), compress);
        }

        public static void TakeSceneViewSnapshot([NotNull] SceneView sceneView, OnTextureReady onTextureReadyCallback, bool compress = true)
        {
            SnapshotUtils.TakeSceneViewSnapshot(sceneView, new SnapshotUtils.OnTextureReady(onTextureReadyCallback), compress);
        }
    }
}