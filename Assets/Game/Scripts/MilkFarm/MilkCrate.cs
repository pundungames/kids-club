using UnityEngine;
using System;
using System.Collections.Generic;

public class MilkCrate : MonoBehaviour
{
    [Header("Kasa Ayarları")]
    public Transform[] milkSlots; // 6 slot

    private List<GameObject> spawnedBottles = new List<GameObject>();

    public int landedMilkCount { get; private set; } = 0; // Public getter
    public int targetMilkCount { get; private set; } = 0;  // Public getter

    public float bottleScale = 0.8f;

    public bool IsPhysicallyFull => landedMilkCount >= milkSlots.Length;
    public bool HasSpace => targetMilkCount < milkSlots.Length;
    public int CurrentBottleCount => landedMilkCount;

    void Start()
    {
        if (landedMilkCount == 0) ToggleVisuals(false);
    }

    /// <summary>
    /// Runtime'da şişe ekle (flying animation ile)
    /// </summary>
    public void AddMilkToCrate(GameObject milkPrefab, Vector3 startPos, Action onFull)
    {
        if (!HasSpace) return;

        if (targetMilkCount == 0) ToggleVisuals(true);

        Transform targetSlot = milkSlots[targetMilkCount];
        targetMilkCount++;

        if (milkPrefab != null)
        {
            GameObject newMilk = Instantiate(milkPrefab, startPos, Quaternion.identity);

            FlyingItem flyer = newMilk.GetComponent<FlyingItem>();
            if (flyer == null) flyer = newMilk.AddComponent<FlyingItem>();

            flyer.FlyTo(targetSlot.position, () =>
            {
                if (newMilk != null)
                {
                    newMilk.transform.SetParent(targetSlot);
                    newMilk.transform.localPosition = Vector3.zero;
                    newMilk.transform.localRotation = Quaternion.identity;
                    newMilk.transform.localScale = Vector3.one * bottleScale;

                    spawnedBottles.Add(newMilk);
                }

                landedMilkCount++;

                if (IsPhysicallyFull)
                {
                    onFull?.Invoke();
                }
            });
        }
    }

    /// <summary>
    /// Load için instant şişe ekle (animation YOK)
    /// </summary>
    public void AddBottleInstant(GameObject milkPrefab)
    {
        if (landedMilkCount >= milkSlots.Length)
        {
            Debug.LogWarning("[MilkCrate] Kasa FULL!");
            return;
        }

        if (landedMilkCount == 0) ToggleVisuals(true);

        Transform targetSlot = milkSlots[landedMilkCount];

        if (milkPrefab != null)
        {
            GameObject bottle = Instantiate(milkPrefab, targetSlot.position, Quaternion.identity);
            bottle.transform.SetParent(targetSlot);
            bottle.transform.localPosition = Vector3.zero;
            bottle.transform.localRotation = Quaternion.identity;
            bottle.transform.localScale = Vector3.one * bottleScale;

            spawnedBottles.Add(bottle);
        }

        landedMilkCount++;
        targetMilkCount++;
    }

    /// <summary>
    /// Kasadan bir şişe çıkar (görsel olarak)
    /// </summary>
    public bool RemoveOneBottle()
    {
        if (spawnedBottles.Count == 0)
        {
            Debug.LogWarning("[MilkCrate] Kasada şişe yok!");
            return false;
        }

        // Son şişeyi al (LIFO - Last In First Out)
        GameObject lastBottle = spawnedBottles[spawnedBottles.Count - 1];
        spawnedBottles.RemoveAt(spawnedBottles.Count - 1);

        // Yok et
        if (lastBottle != null)
        {
            Destroy(lastBottle);
        }

        landedMilkCount--;
        targetMilkCount--;

        Debug.Log($"[MilkCrate] Şişe çıkarıldı. Kalan: {landedMilkCount}");

        // Kasa boşaldıysa gizle
        if (landedMilkCount <= 0)
        {
            ToggleVisuals(false);
        }

        return true;
    }

    /// <summary>
    /// Kasadaki tüm şişeleri temizle
    /// </summary>
    public void ClearAllBottles()
    {
        foreach (var bottle in spawnedBottles)
        {
            if (bottle != null)
            {
                Destroy(bottle);
            }
        }

        spawnedBottles.Clear();
        landedMilkCount = 0;
        targetMilkCount = 0;
        ToggleVisuals(false);
    }

    public void ToggleVisuals(bool isActive)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = isActive;
        }
    }
}