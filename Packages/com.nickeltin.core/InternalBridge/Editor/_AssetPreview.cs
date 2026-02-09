using System;
using UnityEditor;
using UnityEngine;

namespace nickeltin.InternalBridge.Editor
{
    public static class _AssetPreview
    {
        public static Texture2D GetAssetPreview(int instanceID)
        {
            return AssetPreview.GetAssetPreview(instanceID);
        }

        public static Texture2D GetMiniTypeThumbnailFromType(Type managedType)
        {
            return AssetPreview.GetMiniTypeThumbnailFromType(managedType);
        }
    }
}