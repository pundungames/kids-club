using UnityEngine;
using MilkFarm;

/// <summary>
/// Production area'ya týklama handler
/// Stack'teki case'leri sales'e taþýr
/// </summary>
public class ProductionAreaClicker : MonoBehaviour
{
    [Header("Manager Referansý")]
    [SerializeField] private PackageManager packageManager;

    void OnMouseDown()
    {
        if (packageManager != null)
        {
            Debug.Log("[ProductionAreaClicker] Production area'ya týklandý!");
            packageManager.OnProductionAreaClicked();
        }
        else
        {
            Debug.LogError("[ProductionAreaClicker] PackageManager atanmamýþ!");
        }
    }

    void OnValidate()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning("[ProductionAreaClicker] Collider ekleyin!");
        }
    }
}