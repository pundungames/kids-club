#if UNITY_2022_2_OR_NEWER

namespace nickeltin.SDF.Editor
{
    internal partial class TextureImporterEditor
    {
        public sealed class UnsavedChangesCheck : BoolChangeCheck
        {
            public UnsavedChangesCheck() : base(editor => editor.hasUnsavedChanges) { }
        }
    }
}

#endif