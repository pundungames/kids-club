using UnityEditor;

namespace nickeltin.InternalBridge.Editor
{
    public readonly struct _SavedBool
    {
        private readonly SavedBool _instance;
        
        public _SavedBool(string name, bool value = false)
        {
            _instance = new SavedBool(name, value);
        }

        public bool Value
        {
            get => _instance.value;
            set => _instance.value = value;
        }
        
        public static implicit operator bool(_SavedBool instance) => instance.Value;
    }
}