using System;
using UnityEditor;

namespace nickeltin.InternalBridge.Editor
{
    public static class _SerializedObject
    {
        /// <summary>
        /// Iterates over copy of all serialized properties for both objects and compares their data.
        /// Make sure to pass serialized objects with same targets.
        /// </summary>
        public static bool DataEquals(SerializedObject x, SerializedObject y)
        {
            var xp = x.GetIterator().Copy();
            var yp = y.GetIterator().Copy();
            
            while (xp.Next(true) && yp.Next(true))
            {
                if (!SerializedProperty.DataEquals(xp, yp))
                {
                    return false;
                }    
            }

            return true;
        }
        
        public static bool VersionEquals(SerializedObject x, SerializedObject y)
        {
            return SerializedObject.VersionEquals(x, y);
        }

        public static uint GetObjectVersion(this SerializedObject so)
        {
            // so.CopyFromSerializedProperty();
            return so.objectVersion;
        }

        public static IntPtr GetObjectPtr(this SerializedObject so)
        {
            return so.m_NativeObjectPtr;
        }

        public static bool IsValid(this SerializedObject so)
        {
            return so.GetObjectPtr() != IntPtr.Zero;
        }
    }
}