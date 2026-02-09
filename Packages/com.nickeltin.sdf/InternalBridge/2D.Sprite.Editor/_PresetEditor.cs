using System;
using System.Reflection;
using UnityEditor.Presets;

namespace nickeltin.SDF.InternalBridge.Editor
{
    internal readonly struct _PresetEditor
    {
        private static readonly FieldInfo m_InternalEditor; 
        
        static _PresetEditor()
        {
            m_InternalEditor = typeof(PresetEditor).GetField("m_InternalEditor", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public static readonly Type NativeType = typeof(PresetEditor);
        
        internal PresetEditor instance { get; }
        
        internal _PresetEditor(PresetEditor instance) => this.instance = instance;

        public UnityEditor.Editor InternalEditor => (UnityEditor.Editor)m_InternalEditor.GetValue(instance);
        
        public static bool TryCast(UnityEditor.Editor editor, out _PresetEditor presetEditor)
        {
            if (editor is PresetEditor castedEditor) 
            {
                presetEditor = new _PresetEditor(castedEditor);
                return true;
            }

            presetEditor = new _PresetEditor();
            return false;
        }

        public void UpdateProperties()
        {
            for (var index = 0; index < InternalEditor.targets.Length; ++index)
                ((Preset) instance.targets[index]).UpdateProperties(InternalEditor.targets[index]);
        }
    }
}