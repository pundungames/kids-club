using UnityEditor;

namespace nickeltin.InternalBridge.Editor
{
    public readonly struct _SavedInt
    {
        private readonly SavedInt _instance;

        public _SavedInt(string name, int value = 0)
        {
            _instance = new SavedInt(name, value);
        }

        public int Value
        {
            get => _instance.value;
            set => _instance.value = value;
        }

        public static implicit operator int(_SavedInt instance) => instance.Value;
    }
}