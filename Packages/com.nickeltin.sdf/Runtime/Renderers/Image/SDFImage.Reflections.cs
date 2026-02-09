using System;
using System.Reflection;
using UnityEngine.UI;
using Color = UnityEngine.Color;

namespace nickeltin.SDF.Runtime
{
    public sealed partial class SDFImage
    {
        private static class ReflectionsCache
        {
            private const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.NonPublic;

            public delegate void BaseDelegate(SDFImage image);
            
            public static readonly FieldInfo MaterialDirty;
            public static readonly FieldInfo VertsDirty;
            public static readonly FieldInfo Color;
            public static readonly BaseDelegate SetRaycastDirty;

            private static void ValidateReflectedMember(MemberInfo memberInfo, string name, Type type)
            {
                if (memberInfo == null)
                {
                    throw new Exception($"Can't reflect {name} member in {type}, unity might have changed it");
                }
            }
            
            static ReflectionsCache()
            {
                MaterialDirty = CacheField<Graphic>("m_MaterialDirty");
                VertsDirty = CacheField<Graphic>("m_VertsDirty");
                Color = CacheField<Graphic>("m_Color");
                
                // This is special case, unity 2021.3.12f1 don't have this method,
                // but unity 2021.3.33f1 does, its unclear when it was introduced,
                // so it's easier to handle it with reflection and just invoke if is its found
                var setRaycastMethodInfo = typeof(Graphic).GetMethod("SetRaycastDirty", BindingFlags.Instance | BindingFlags.Public);
                if (setRaycastMethodInfo != null)
                {
                    SetRaycastDirty = (BaseDelegate)Delegate.CreateDelegate(typeof(BaseDelegate), setRaycastMethodInfo);
                }
            }

            private static TDelegate CacheMethodDelegate<TDelegate>(string methodName, Type[] types = null, 
                BindingFlags flags = FLAGS) where TDelegate : Delegate
            {
                return CacheMethodDelegate<TDelegate, Image>(methodName, types, flags);
            }
            
            private static TDelegate CacheMethodDelegate<TDelegate, TBaseType>(string methodName, Type[] types = null, 
                BindingFlags flags = FLAGS) where TDelegate : Delegate
            {
                var t = typeof(TBaseType);

                var methodInfo = types == null
                    ? t.GetMethod(methodName, flags)
                    : t.GetMethod(methodName, flags, null, types, null);
                ValidateReflectedMember(methodInfo, methodName, t);
                return (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), methodInfo!);
            }

            private static FieldInfo CacheField<TBaseType>(string fieldName, BindingFlags flags = FLAGS)
            {
                var t = typeof(TBaseType);

                var fieldInfo = t.GetField(fieldName, flags);
                ValidateReflectedMember(fieldInfo, fieldName, t);
                return fieldInfo;
            }
        }
        

        /// <summary>
        /// Reflections wrapper for private variable <see cref="Graphic.m_MaterialDirty"/>
        /// </summary>
        private bool m_MaterialDirty
        {
            get => (bool)ReflectionsCache.MaterialDirty.GetValue(this);
            set => ReflectionsCache.MaterialDirty.SetValue(this, value);
        }

        /// <summary>
        /// Reflections wrapper for private variable <see cref="Graphic.m_VertsDirty"/>
        /// </summary>
        private bool m_VertsDirty
        {
            get => (bool)ReflectionsCache.VertsDirty.GetValue(this);
            set => ReflectionsCache.VertsDirty.SetValue(this, value);
        }

        private Color m_Color
        {
            set => ReflectionsCache.Color.SetValue(this, value);
        }

        /// <summary>
        /// Version safe reflection wrapper around unity built-in method from <see cref="Graphic"/>.
        /// </summary>
        private void SetRaycastDirty_Reflected()
        {
            if (ReflectionsCache.SetRaycastDirty != null)
            {
                ReflectionsCache.SetRaycastDirty.Invoke(this);
            }
        }
    }
}