using UnityEngine;

[System.Serializable]
public struct DropItemData
{
    public GameObject itemPrefab;
    public int dropAmount;
    /*public int minAmount;
    public int maxAmount;
    [Tooltip("Düþürme olasýlýðý (0.0 = %0, 1.0 = %100).")]
    [Range(0f, 1f)]
    public float dropChance;*/

    [Header("XP Ayarý")]
    [Tooltip("Eðer düþen item XP ise, bu, birim XP miktarýný temsil eder.")]
    public float xpValuePerUnit;
}