using UnityEditor;

namespace nickeltin.InternalBridge.Editor
{
    public readonly struct _ScriptingMenuItem
    {
        internal ScriptingMenuItem instance { get; }

        internal _ScriptingMenuItem(ScriptingMenuItem instance)
        {
            this.instance = instance;
        }
        
        public string path => instance.path;

        public bool isSeparator => instance.isSeparator;

        public int priority => instance.priority;

        public override string ToString()
        {
            return $"path {path} | isSeparator {isSeparator} | priority {priority}";
        }
    }
}