using System.Collections.Generic;
using nickeltin.OdinSupport.Runtime;
using nickeltin.SDF.Runtime;
using UnityEngine;


[ExcludeEditorFromOdin(true)]
[CreateAssetMenu(menuName = "nickeltin/SDF/Samples/SDF List Asset")]
public class SDFListSampleAsset : ScriptableObject
{
    [Header("Proper mass-reference")]
    public SDFSpriteReferenceList ProperSDFList;
    
    [Header("Other")]
    public List<SDFSpriteReference> NotFriendlySDFList;
    public SDFSpriteReference[] NotFriendlySDFArr;
    public SDFSpriteReference SingleReference;
}
