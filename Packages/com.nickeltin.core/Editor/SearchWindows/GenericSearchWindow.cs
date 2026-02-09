using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace nickeltin.Core.Editor
{
    public class GenericSearchWindow : SearchWindowBase<GenericSearchWindow>
    {
        public class Builder
        {
            private readonly List<Entry> _entries = new();

            private void Add(Entry entry) => _entries.Add(entry);

            public void Add(string path, Action action) => Add(path, path, null, action);
            public void Add(string path, string alias, Action action) => Add(path, alias, null, action);
            public void Add(string path, Texture icon, Action action) => Add(path, path, icon, action);
            public void Add(string path, string alias, Texture icon, Action action) => Add(new Entry(path, alias, (Texture2D)icon, action));
            

            public void Open(Vector2? position = null, 
                Vector2? size = null, 
                string topLabel = "Select")
            {
                GenericSearchWindow.Open(_entries.Cast<ISearchWindowEntry>(), entry =>
                {
                    var e = (Entry)entry.GetData();
                    e.OnClick?.Invoke();
                }, false, true, position, size, null, topLabel);
            }
        }
        
        private readonly struct Entry : ISearchWindowEntry
        {
            public readonly Texture2D Icon;
            public readonly Action OnClick;
            
            private readonly string[] _path;
            private readonly string[] _alias;

            public Entry(string path, string alias, Texture2D icon, Action onClick)
            {
                OnClick = onClick;
                _path = path.Split("/");
                _alias = alias.Split("/");
                Icon = icon;
            }
            
            public object GetData() => this;

            public string[] GetPathAlias() => _alias;

            public string[] GetPath() => _path;
        }
        
        protected override Texture2D GetIconForEntry(ISearchWindowEntry entry)
        {
            return ((Entry)entry.GetData()).Icon;
        }
        
        public static Builder Build() => new();
    }
}