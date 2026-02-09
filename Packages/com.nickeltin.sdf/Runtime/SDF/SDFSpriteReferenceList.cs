using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace nickeltin.SDF.Runtime
{
    /// <summary>
    /// Container for <see cref="SDFSpriteMetadataAsset"/>, that uses <see cref="SDFSpriteReference"/> for elements.
    /// Exist to provide proper way to mass-reference a lot of sdf meta assets in editor, handles all possible DragAndDrop scenarios.
    /// Implements <see cref="IList"/>, also internal list can be accessed with <see cref="GetList"/>
    /// </summary>
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.DrawWithUnity, Sirenix.OdinInspector.DisableContextMenu()]
#endif
    [Serializable]
    public sealed class SDFSpriteReferenceList : IList<SDFSpriteReference>
    {
        /// <summary>
        /// Using nested list, since unity don't handle natively classes inherited from list
        /// </summary>
        [SerializeField] internal List<SDFSpriteReference> _list;
        
        public SDFSpriteReferenceList(IEnumerable<SDFSpriteReference> references)
        {
            _list = new List<SDFSpriteReference>(references);
        }

        public SDFSpriteReferenceList(IEnumerable<SDFSpriteMetadataAsset> metadataAssets) 
            : this(metadataAssets.Select(asset => new SDFSpriteReference(asset)))
        {
        }

        /// <summary>
        /// Gives access to internal list.
        /// </summary>
        /// <returns></returns>
        public List<SDFSpriteReference> GetList() => _list;

        public IEnumerator<SDFSpriteReference> GetEnumerator() => _list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(SDFSpriteReference item) => _list.Add(item);

        public void Clear() => _list.Clear();

        public bool Contains(SDFSpriteReference item) => _list.Contains(item);

        public void CopyTo(SDFSpriteReference[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

        public bool Remove(SDFSpriteReference item) => _list.Remove(item);
        
        /// <remarks>Null check for editor, otherwise throws error when host object first created</remarks>
        public int Count => _list?.Count ?? 0;

        public bool IsReadOnly => false;
        
        public int IndexOf(SDFSpriteReference item) => _list.IndexOf(item);

        public void Insert(int index, SDFSpriteReference item) => _list.Insert(index, item);

        public void RemoveAt(int index) => _list.RemoveAt(index);

        public SDFSpriteReference this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }
    }
}