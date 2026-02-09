using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AssetImporters;

namespace nickeltin.SDF.Editor
{
    internal partial class TextureImporterEditor
    {
        /// <summary>
        /// Collection of all defined change checks
        /// </summary>
        public sealed class AllChangeChecks : CombinedChangeCheck
        {
            public AllChangeChecks() : base(new ModifiedCheck()
#if !UNITY_2022_2_OR_NEWER
                ,new HashCheck()
#endif
#if UNITY_2022_2_OR_NEWER
                ,new UnsavedChangesCheck()
#endif
            ) { }
        }
        
        public abstract class ChangeCheck
        {
            public virtual bool Changed { get; protected set; }

            protected bool HasDataToIterate;

            public bool SetNewDataAndIterate(AssetImporterEditor editor)
            {
                SetNewData(editor);
                var changed = Changed;
                IterateData();
                return changed;
            }

            public void SetNewData(AssetImporterEditor editor)
            {
                if (HasDataToIterate)
                {
                    throw new Exception("Call IterateData after each SetNewData call");
                }

                HasDataToIterate = true;

                SetNewData_Impl(editor);
            }

            public void IterateData()
            {
                if (!HasDataToIterate)
                {
                    throw new Exception("Call SetNewData before each IterateData call");
                }

                HasDataToIterate = false;

                IterateData_Impl();
            }

            protected abstract void SetNewData_Impl(AssetImporterEditor editor);

            protected abstract void IterateData_Impl();
        }

        public class CombinedChangeCheck : ChangeCheck
        {
            private readonly List<ChangeCheck> _checks;

            public override bool Changed => _checks.Any(check => check.Changed);

            public CombinedChangeCheck(params ChangeCheck[] checks)
            {
                _checks = new List<ChangeCheck>(checks);
            }

            protected override void SetNewData_Impl(AssetImporterEditor editor)
            {
                foreach (var check in _checks) check.SetNewData(editor);
            }

            protected override void IterateData_Impl()
            {
                foreach (var check in _checks) check.IterateData();
            }
        }
        
        public class BoolChangeCheck : ChangeCheck
        {
            private bool _lastModified;
            private bool _temp;
            private Func<AssetImporterEditor, bool> _getter;

            public BoolChangeCheck(Func<AssetImporterEditor, bool> getter) => _getter = getter;

            protected override void SetNewData_Impl(AssetImporterEditor editor)
            {
                _temp = editor.HasModified();
                Changed = _lastModified != _temp;
            }

            protected override void IterateData_Impl()
            {
                _lastModified = _temp;
                Changed = false;
            }
        }

        /// <summary>
        /// Checks for revert or external properties applied, like from preset.
        /// </summary>
        public sealed class ModifiedCheck : BoolChangeCheck
        {
            public ModifiedCheck() : base(editor => editor.HasModified()) { }
        }
    }
}