using System.Linq;
using nickeltin.InternalBridge.Editor;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace nickeltin.SDF.Editor
{
    internal class SDFLandingWindow : EditorWindow
    {
        private const string STORE_URL = "https://assetstore.unity.com/packages/tools/gui/sdf-image-quality-ui-outlines-and-shadow-244942";
        private const string PACKAGE_NAME = NickeltinSDF.NAME;

        [SerializeField] private Texture2D _frogIcon;

        private static GUIContent _frogContent;

        [InitializeOnLoadMethod]
        private static void Init()
        {
            Events.registeredPackages += OnRegisteredPackages;
        }

        private static void OnRegisteredPackages(PackageRegistrationEventArgs args)
        {
            if (args.added.Any(p => p.name == PACKAGE_NAME))
            {
                Debug.Log("SDF Package was installed");
                ShowWindow();
            }
        }

       
        public static void ShowWindow()
        {
            var window = GetWindow<SDFLandingWindow>(true, "Welcome to SDF Image", true);
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        private void OnGUI()
        {
            if (_frogIcon != null && _frogContent == null)
                _frogContent = new GUIContent(_frogIcon, "Ribbit 🐸");

            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            const float iconSize = 48f;
            if (_frogContent != null)
            {
                GUILayout.Label(_frogContent, GUILayout.Width(iconSize), GUILayout.Height(iconSize));
                GUILayout.Space(8);
            }

            GUILayout.Label("Welcome to SDF Image!", Defaults.TitleStyle, GUILayout.Height(iconSize));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.Label(
                "Thank you for installing <b>SDF Image</b>!\n\nThis tool helps you render pixel-perfect signed distance field graphics in Unity with ease and flexibility.",
                Defaults.TextStyle);
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("Start by creating a new SDF Image object via GameObject > UI > SDF Image.", MessageType.Info);

            GUILayout.FlexibleSpace();

            DrawCenteredButton(Defaults.DocsButtonContent, NickeltinSDF.OpenDocumentation);
            GUILayout.Space(10);
            DrawCenteredButton(Defaults.ReviewButtonContent, () => Application.OpenURL(STORE_URL));
            GUILayout.Space(10);
            DrawCenteredButton(Defaults.SamplesButtonContent, () => _PackageManagerWindow.OpenAndSelectPackage(PACKAGE_NAME));

            GUILayout.Space(20);
        }

        private static void DrawCenteredButton(GUIContent content, System.Action onClick)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(content, Defaults.ButtonStyle, GUILayout.Width(200)))
                onClick?.Invoke();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static class Defaults
        {
            public static readonly GUIStyle TitleStyle;
            public static readonly GUIStyle TextStyle;
            public static readonly GUIStyle ButtonStyle;

            public static readonly GUIContent DocsButtonContent;
            public static readonly GUIContent ReviewButtonContent;
            public static readonly GUIContent SamplesButtonContent;

            static Defaults()
            {
                TitleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 24,
                    wordWrap = true
                };

                TextStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 13,
                    wordWrap = true,
                    richText = true
                };

                ButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 14,
                    fixedHeight = 40
                };

                DocsButtonContent = new GUIContent(" Documentation", EditorGUIUtility.IconContent("_Help").image);
                ReviewButtonContent = new GUIContent(" Leave a Review", EditorGUIUtility.IconContent("Favorite").image);
                SamplesButtonContent = new GUIContent(" See Samples", EditorGUIUtility.IconContent("d_Project").image);
            }
        }
    }
}
