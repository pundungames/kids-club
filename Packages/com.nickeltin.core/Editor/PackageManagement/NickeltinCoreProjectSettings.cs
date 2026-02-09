using System.Collections.Generic;
using nickeltin.SOCreateWindow.Editor;
using nickeltin.SOCreateWindow.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace nickeltin.Core.Editor
{
    [FilePath("ProjectSettings/NickeltinCoreSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class NickeltinCoreProjectSettings : ScriptableSingleton<NickeltinCoreProjectSettings>
    {
        [SerializeField, Tooltip("Context click in project view can open up Window where scriptable objects or other assets can be defined. " +
                                 "Use " + nameof(CreateAssetWindowAttribute) + " or " + nameof(CustomCreateAssetWindowAttribute))]
        internal bool _showCreateWindow = false;
        
        public static bool ShowCreateWindow
        {
            get => instance._showCreateWindow;
            set => SetProperty(ref instance._showCreateWindow, value);
        }

        private void OnEnable()
        {
            // Removing flag to make object editable in editor
            this.hideFlags &= ~HideFlags.NotEditable;
        }

        private void OnDisable() => Save();

        private static bool SetProperty<T>(ref T prop, T newValue)
        {
            if (EqualityComparer<T>.Default.Equals(prop, newValue)) return false;

            prop = newValue;
            instance.Save();
            return true;
        }
        
        public void Save()
        {
            Save(true);
        }

        protected override void Save(bool saveAsText)
        {
            ScriptableObjectCreator.VerifyCreateMenu();
            base.Save(saveAsText);
        }

        internal SerializedObject GetSerializedObject() => new(this);
    }

    internal class NickeltinCoreSettingsProvider : SettingsProvider
    {
        private readonly SerializedObject _serializedObject;
        private SerializedProperty _showCreateWindow;


        public NickeltinCoreSettingsProvider(string path, SettingsScope scopes, SerializedObject serializedObject)
            : base(path, scopes, GetSearchKeywordsFromSerializedObject(serializedObject))
        {
            _serializedObject = serializedObject;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            NickeltinCoreProjectSettings.instance.Save();
            
            _showCreateWindow = _serializedObject.FindProperty(nameof(NickeltinCoreProjectSettings._showCreateWindow));
        }

        public override void OnGUI(string searchContext)
        {
            _serializedObject.Update();
            
            EditorGUILayout.PropertyField(_showCreateWindow);
            
            if (_serializedObject.ApplyModifiedProperties())
            {
                NickeltinCoreProjectSettings.instance.Save();
            }
        }

      
        
        [SettingsProvider]
        public static SettingsProvider CreateSettingProvider()
        {
            var provider = new NickeltinCoreSettingsProvider("Project/Nickeltin/Core", SettingsScope.Project, 
                NickeltinCoreProjectSettings.instance.GetSerializedObject());
            return provider;
        }
    }

}