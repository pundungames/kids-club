using UnityEngine;
using UnityEngine.Search;

namespace nickeltin.SDF.Runtime
{
    /// <summary>
    /// Base part of the class.
    /// There is second part that active only for Unity 2023.1 with sprite metadata related extension methods.
    /// Also, there is editor only version "nickeltin.SDF.Editor.SDFEditorUtil"
    /// </summary>
    public static partial class SDFUtil
    {
        /// <summary>
        /// Is new sprite sdf metadata pipeline is enabled.
        /// New pipeline is enabled for Unity 2023.1 and higher as it has scriptable objects for sprites API.
        /// Definition SDF_NEW_SPRITE_METADATA is managed by assembly definitions "nickeltin.SDF.Editor" and "nickeltin.SDF.Runtime"
        /// and used internally for thees assemblies, if you want to know is new sprite metadata pipeline used access this const
        /// (<see cref="IsNewSpriteMetadataEnabled"/>) or create editor script that will define SDF_NEW_SPRITE_METADATA project wide.
        /// </summary>
        public static bool IsNewSpriteMetadataEnabled =>
#if SDF_NEW_SPRITE_METADATA
            true;
#else
            false;
#endif
        
        /// <summary>
        /// To search sdf import artifacts <see cref="SDFSpriteMetadataAsset"/> this search provider is used,
        /// otherwise artifacts is hidden and regular search can't display them.
        /// Can be used on <see cref="SDFSpriteMetadataAsset"/> field together with <see cref="SearchContextAttribute"/>,
        /// but intended for public way is to use <see cref="SDFSpriteReference"/>.
        /// </summary>
        public const string ArtifactsSearchProviderID = "sdf-artifacts";

        /// <summary>
        /// Default sdf shader for the UI path. Get it with <see cref="Shader.Find"/>
        /// </summary>
        [ForceIncludeShaderInBuild]
        public const string SDFDisplayUIShaderName = "nickeltin/SDF/UI";
        
        /// <summary>
        /// Default pure sdf shader for the UI path. Get it with <see cref="Shader.Find"/>
        /// </summary>
        [ForceIncludeShaderInBuild]
        public const string SDFDisplayPureUIShaderName = "nickeltin/SDF/UIPure";
        

        static SDFUtil()
        {
            Init2023Support();         
            InitEditor();
        }

        static partial void Init2023Support();
        static partial void InitEditor();
    }
}