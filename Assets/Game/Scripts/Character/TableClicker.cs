using UnityEngine;
using MilkFarm;
using Zenject;

/// <summary>
/// Masaya týklama handler - PackageManager'a baðlanýr
/// Kasadaki ürünleri satar
/// </summary>
public class TableClicker : MonoBehaviour
{
    [Header("Manager Referanslarý")]
    [Inject] PackageManager packageManager;
    [Inject] AudioManager audioManager;
    void OnMouseDown()
    {
        if (packageManager != null)
        {
            Debug.Log("[TableClicker] Masaya týklandý! Satýþ deneniyor...");
            packageManager.OnStationClicked();
            audioManager.Play("Sell");
        }
        else
        {
            Debug.LogError("[TableClicker] PackageManager atanmamýþ!");
        }
    }

    void OnValidate()
    {
        // Collider kontrolü
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning("[TableClicker] Bu objeye Collider ekleyin! OnMouseDown çalýþmaz.");
        }
    }
}