using UnityEditor.AssetImporters;

namespace nickeltin.InternalBridge.Editor
{
    /// <summary>
    /// Scripted importer editor with exposed internal members
    /// </summary>
    public abstract class _ScriptedImporterEditor : ScriptedImporterEditor
    {
        internal override void OnForceReloadInspector()
        {
            base.OnForceReloadInspector();
            _OnForceReloadInspector();
        }

        /// <summary>
        /// Recreation of internal <see cref="OnForceReloadInspector"/>
        /// Used for scripted importer after reimport, useful when need to re-initialed something based upon imported artifact.
        /// </summary>
        protected virtual void _OnForceReloadInspector()
        {
            
        }
    }
}