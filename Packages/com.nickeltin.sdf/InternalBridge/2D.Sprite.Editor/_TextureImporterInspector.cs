using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.Assertions;

namespace nickeltin.SDF.InternalBridge.Editor
{
    internal abstract class _TextureImporterInspector : TextureImporterInspector
    {
        public enum _TextureInspectorGUIElement
        {
            None = 0,
            PowerOfTwo = 1,
            Readable = 2,
            AlphaHandling = 4,
            ColorSpace = 8,
            MipMaps = 16, // 0x00000010
            NormalMap = 32, // 0x00000020
            Sprite = 64, // 0x00000040
            Cookie = 128, // 0x00000080
            CubeMapConvolution = 256, // 0x00000100
            CubeMapping = 512, // 0x00000200
            StreamingMipmaps = 1024, // 0x00000400
            SingleChannelComponent = 2048, // 0x00000800
            PngGamma = 4096, // 0x00001000
            VTOnly = 8192, // 0x00002000
            ElementsAtlas = 16384, // 0x00004000
#if UNITY_2022_1_OR_NEWER
            Swizzle = 32768, // 0x00008000
#endif
        }

        public static bool IsInstance(UnityEditor.Editor editor) => editor is TextureImporterInspector;

        public delegate void GUIMethod(_TextureInspectorGUIElement guiElements);
        
        private static readonly FieldInfo m_GUIElementMethods_Field;
        private static readonly ConstructorInfo Native_GUIMethod_Ctor;
        private static readonly Type TextureInspectorGUIElement_EnumType;

        static _TextureImporterInspector()
        {
            m_GUIElementMethods_Field = typeof(TextureImporterInspector).GetField("m_GUIElementMethods",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m_GUIElementMethods_Field);

            var nativeGUIMethodDelegateType = typeof(TextureImporterInspector).GetNestedType("GUIMethod", BindingFlags.NonPublic);
            Assert.IsNotNull(nativeGUIMethodDelegateType);
            
            Native_GUIMethod_Ctor = nativeGUIMethodDelegateType.GetConstructors().FirstOrDefault();
            Assert.IsNotNull(Native_GUIMethod_Ctor);

            TextureInspectorGUIElement_EnumType = typeof(TextureImporterInspector).GetNestedType("TextureInspectorGUIElement", BindingFlags.NonPublic);
            Assert.IsNotNull(TextureInspectorGUIElement_EnumType);
        }
        
        private IDictionary m_GUIElementMethods;

        public IEnumerable<TextureImporter> GetImporters() => targets.Cast<TextureImporter>();

        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="append">If true will add method after previous methods, if false will prepend method before</param>
        public void RegisterGUIMethod(_TextureInspectorGUIElement type, GUIMethod method, bool append = true)
        {
            // This is quite edgy to convert enums this way but whatever
            var castedType = Enum.Parse(TextureInspectorGUIElement_EnumType, type.ToString());
            var previousMethod = (Delegate)m_GUIElementMethods[castedType];

            var methodToNative = ToNativeGUIMethod(method);
            
            Delegate combinedMethod = null;
            combinedMethod = append 
                ? Delegate.Combine(previousMethod, methodToNative) 
                : Delegate.Combine(methodToNative, previousMethod);
            
            m_GUIElementMethods[castedType] = combinedMethod; 
        }

        public static Delegate ToNativeGUIMethod(Delegate method)
        {
            return (Delegate)Native_GUIMethod_Ctor.Invoke(new[]
                { method.Target, method.Method.MethodHandle.GetFunctionPointer() });
        }
        
        public override void OnEnable()
        {
            base.OnEnable();
            m_GUIElementMethods = (IDictionary)m_GUIElementMethods_Field.GetValue(this);
        }
    }
}