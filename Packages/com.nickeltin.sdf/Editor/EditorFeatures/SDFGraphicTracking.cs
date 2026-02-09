using System.Collections.Generic;
using nickeltin.SDF.Runtime;
using UnityEditor;

namespace nickeltin.SDF.Editor
{
    /// <summary>
    /// Handles sdf graphic refresh when sdf assets they're using are re-imported.
    /// Covers a case when sdf graphic in a scene is not immediately updated after sdf re-imported, for some reason...
    /// </summary>
    internal static class SDFGraphicTracking
    {
        private static readonly HashSet<SDFGraphic> _graphics = new();

        [InitializeOnLoadMethod]
        private static void Init()
        {
            SDFGraphic.SDFGraphicOnEnable += SDFGraphicOnEnable;
            SDFGraphic.SDFGraphicOnDisable += SDFGraphicOnDisable;
            SDFEditorUtil.TextureProcessed += OnSDFTextureProcessed;
        }

        private static void OnSDFTextureProcessed(string path)
        {
            foreach (var graphic in _graphics)
            {
                SDFEditorUtil.TryRefreshSDFSpriteReferenceFields(path, graphic);
            }
        }

        private static void SDFGraphicOnEnable(SDFGraphic obj)
        {
            _graphics.Add(obj);
        }
        
        private static void SDFGraphicOnDisable(SDFGraphic obj)
        {
            _graphics.Remove(obj);
        }
    }
}