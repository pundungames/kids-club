using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace nickeltin.InternalBridge.Editor
{
    public static class _PropertyHandler
    {
        /// <summary>
        /// Use this when need to get associated with property handler list drawer.
        /// Lists from property handlers is managed by unity, so we just borrowing them, without need to create their lifecycle system.
        /// Getting reorderable list directly allows to draw it without header.
        /// </summary>
        /// <param name="identifierProperty">Property used to identify list drawer in dictionary</param>
        /// <param name="listProperty">Actual list/array property being drawn</param>
        internal static ReorderableListWrapper GetReorderableListWrapper(SerializedProperty identifierProperty, SerializedProperty listProperty)
        {
            var propertyIdentifier = ReorderableListWrapper.GetPropertyIdentifier(identifierProperty);
            if (!PropertyHandler.s_reorderableLists.TryGetValue(propertyIdentifier, out var reorderableListWrapper))
            {
                reorderableListWrapper = new ReorderableListWrapper(listProperty, GUIContent.none);
                PropertyHandler.s_reorderableLists[propertyIdentifier] = reorderableListWrapper;
            }
            reorderableListWrapper.Property = listProperty;
            return reorderableListWrapper;
        }

        public static ReorderableList GetReorderableList(SerializedProperty identifierProperty,
            SerializedProperty listProperty)
        {
            return GetReorderableListWrapper(identifierProperty, listProperty).m_ReorderableList;
        }
    }
}