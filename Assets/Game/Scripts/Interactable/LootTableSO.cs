using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LootTable", menuName = "Looting/Loot Table")]
public class LootTableSO : ScriptableObject
{
    [SerializeField] internal string objectName;

    [Tooltip("Bu tabloya ait düþme olasýlýklarý ve miktarlarý.")]
    public List<DropItemData> drops = new List<DropItemData>();
    [SerializeField] internal string hitVfx;
    [SerializeField] internal string hitSfx;
    [SerializeField] internal string cutSfx;
}