using UnityEngine;
using System;

public class MilkCrate : MonoBehaviour
{
    [Header("Kasa Ayarlarý")]
    public Transform[] milkSlots;

    // Ulaþan (Görünen) Süt Sayýsý
    private int landedMilkCount = 0;

    // Havada Olan + Ulaþan Toplam Süt (Mantýksal Sayý)
    private int targetMilkCount = 0;

    public float bottleScale = 0.8f;

    // Kasa tamamen doldu mu? (Hareket için buna bakacaðýz)
    public bool IsPhysicallyFull => landedMilkCount >= milkSlots.Length;

    // Süt gelecek yer kaldý mý? (Ýnek týklamasý için buna bakacaðýz)
    public bool HasSpace => targetMilkCount < milkSlots.Length;

    void Start()
    {
        // Baþlangýçta boþsa gizle (Hayalet Modu)
        if (landedMilkCount == 0) ToggleVisuals(false);
    }

    // --- SÜT EKLEME ---
    // Action onFull: Kasa fiziksel olarak dolduðunda Manager'a haber verecek
    public void AddMilkToCrate(GameObject milkPrefab, Vector3 startPos, Action onFull)
    {
        // Eðer yer yoksa iþlem yapma
        if (!HasSpace) return;

        // 1. Hemen görünür yap (Ýlk süt yola çýktý)
        if (targetMilkCount == 0) ToggleVisuals(true);

        // 2. Hedef slotu belirle
        Transform targetSlot = milkSlots[targetMilkCount];

        // 3. Mantýksal sayýyý artýr (Artýk buraya baþka süt gelemez)
        targetMilkCount++;

        if (milkPrefab != null)
        {
            GameObject newMilk = Instantiate(milkPrefab, startPos, Quaternion.identity);

            FlyingItem flyer = newMilk.GetComponent<FlyingItem>();
            if (flyer == null) flyer = newMilk.AddComponent<FlyingItem>();

            // Uçuþ Hýzý (Daha hýzlý varsýn ki bekletmesin)
            // FlyingItem scriptinde hýzý public yapmadýysan burasý standart çalýþýr.

            flyer.FlyTo(targetSlot.position, () =>
            {
                // --- SÜT YERÝNE VARDIÐINDA ---
                if (newMilk != null)
                {
                    newMilk.transform.SetParent(targetSlot);
                    newMilk.transform.localPosition = Vector3.zero;
                    newMilk.transform.localRotation = Quaternion.identity;
                    newMilk.transform.localScale = Vector3.one * bottleScale;
                }

                // Fiziksel sayacý artýr
                landedMilkCount++;

                // Eðer son süt de indiyse ve kasa tamamen dolduysa
                if (IsPhysicallyFull)
                {
                    // Manager'a haber ver: "Ben hazýrým, beni uçur!"
                    onFull?.Invoke();
                }
            });
        }
    }

    public void ToggleVisuals(bool isActive)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = isActive;
    }
}