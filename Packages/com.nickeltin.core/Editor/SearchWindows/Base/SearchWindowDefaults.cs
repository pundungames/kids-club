using UnityEditor;
using UnityEngine;

namespace nickeltin.Core.Editor
{
    internal static class SearchWindowDefaults
    {
        public static readonly Texture2D NoneIcon;
        public static readonly GUIContent NoneContent;
        public static readonly GUIContent NoItemsFoundContent;
            
        static SearchWindowDefaults()
        {
            NoItemsFoundContent = new GUIContent("List is empty");
            NoneIcon = (Texture2D)EditorGUIUtility.IconContent("Invalid").image;
            NoneContent = new GUIContent("<none>", NoneIcon);
        }
    }
}