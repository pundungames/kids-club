using System;
using UnityEditor.Search;

namespace nickeltin.InternalBridge.Editor
{
    public static class _SearchProvider
    {
        public static void SetToType(this SearchProvider provider, Func<SearchItem, Type, Type> toType)
        {
            provider.toType = toType;
        }

        public static void SetToKey(this SearchProvider provider, Func<SearchItem, ulong> toKey)
        {
            provider.toKey = toKey;
        }
    }
}