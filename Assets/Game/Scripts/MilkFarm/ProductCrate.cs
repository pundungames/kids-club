using UnityEngine;
using System;
using System.Collections.Generic;
using Dreamteck.Splines;
using Zenject;

/// <summary>
/// ProductCrate - Generic kasa. Slot sayısı prefab'dan gelir (milk=6, egg=4)
/// Backward compat: AddMilkToCrate, AddBottleInstant, RemoveOneBottle hala çalışır
/// </summary>
public class ProductCrate : MonoBehaviour
{
    [Header("Kasa Ayarları")]
    public Transform[] productSlots;
    [Inject] AudioManager audioManager;
    private List<GameObject> spawnedProducts = new List<GameObject>();
    public int landedProductCount { get; private set; } = 0;
    public int targetProductCount { get; private set; } = 0;
    public float productScale = 0.8f;

    public bool IsPhysicallyFull => landedProductCount >= productSlots.Length;
    public bool HasSpace => targetProductCount < productSlots.Length;
    public int CurrentProductCount => landedProductCount;
    public int MaxCapacity => productSlots != null ? productSlots.Length : 0;

    // Backward compat aliases
    public int landedMilkCount => landedProductCount;
    public int targetMilkCount => targetProductCount;
    public int CurrentBottleCount => landedProductCount;

    void Start() { if (landedProductCount == 0) ToggleVisuals(false); }

    public void AddProductToCrate(GameObject prefab, Vector3 startPos, SplineComputer animalSpline, Action onFull)
    {
        if (!HasSpace) return;
        if (targetProductCount == 0) ToggleVisuals(true);

        Transform slot = productSlots[targetProductCount];
        targetProductCount++;

        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, startPos, Quaternion.identity);

            // SplineController kontrolü
            if (obj.TryGetComponent<SplineController>(out var splineCtrl) && animalSpline != null)
            {
                // Hayvandan gelen özel spline yolunu gönderiyoruz
                splineCtrl.Init(animalSpline, slot.position, () =>
                {
                    HandleProductArrival(obj, slot, onFull);
                });
            }
            else
            {
                // Spline yoksa veya gönderilmemişse eski usul uçuş
                FlyingItem flyer = obj.GetComponent<FlyingItem>() ?? obj.AddComponent<FlyingItem>();
                flyer.FlyTo(slot.position, () =>
                {
                    HandleProductArrival(obj, slot, onFull);
                });
            }
        }
    }
    // Kod tekrarını önlemek için varış mantığını bir metoda ayırdık
    private void HandleProductArrival(GameObject obj, Transform slot, Action onFull)
    {
        if (obj != null)
        {
            audioManager.Play("Tap");
            obj.transform.SetParent(slot);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one * productScale;
            spawnedProducts.Add(obj);
        }
        landedProductCount++;
        if (IsPhysicallyFull) onFull?.Invoke();
    }

    public void AddMilkToCrate(GameObject p, Vector3 s, Action f, SplineComputer spline = null) => AddProductToCrate(p, s,animalSpline: spline, f);

    public void AddProductInstant(GameObject prefab)
    {
        if (landedProductCount >= productSlots.Length) return;
        if (landedProductCount == 0) ToggleVisuals(true);
        Transform slot = productSlots[landedProductCount];
        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, slot.position, Quaternion.identity);
            obj.transform.SetParent(slot); obj.transform.localPosition = Vector3.zero; obj.transform.localRotation = Quaternion.identity; obj.transform.localScale = Vector3.one * productScale;
            spawnedProducts.Add(obj);
        }
        landedProductCount++; targetProductCount++;
    }
    public void AddBottleInstant(GameObject p) => AddProductInstant(p);

    public bool RemoveOneProduct()
    {
        if (spawnedProducts.Count == 0) return false;
        GameObject last = spawnedProducts[spawnedProducts.Count - 1]; spawnedProducts.RemoveAt(spawnedProducts.Count - 1);
        if (last != null) Destroy(last);
        landedProductCount--; targetProductCount--;
        if (landedProductCount <= 0) ToggleVisuals(false);
        return true;
    }
    public bool RemoveOneBottle() => RemoveOneProduct();

    public void ClearAllProducts()
    {
        foreach (var p in spawnedProducts) if (p != null) Destroy(p);
        spawnedProducts.Clear(); landedProductCount = 0; targetProductCount = 0; ToggleVisuals(false);
    }
    public void ClearAllBottles() => ClearAllProducts();

    public void ToggleVisuals(bool active)
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = active;
    }
}