using UnityEditor;
using UnityEditor.U2D.Sprites;

namespace nickeltin.SDF.Editor
{
    internal static class SDFImportUtil
    {
        private static readonly SpriteDataProviderFactories Factory;

        static SDFImportUtil()
        {
            Factory = new SpriteDataProviderFactories();
            Factory.Init();
        }

        public static SpriteRect[] GetSpriteRects(this TextureImporter importer)
        {
            var dataProvider = Factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();
            return dataProvider.GetSpriteRects();
        }
        
        /// <summary>
        /// Internal id is basically a file id that is used alongside with guid to reference asset.
        /// In <see cref="SpriteDataExt"/> using GetHashCode() on sprite guid to get internalID,
        /// but all importers also have a map, and so internalID might be different from that.
        /// </summary>
        public static long GetInternalID(this SpriteRect spriteRect)
        {
            if (spriteRect is SpriteDataExt extendedData)
            {
                return extendedData.internalID;
            }
            
            return spriteRect.spriteID.GetHashCode();
        }
    }
}