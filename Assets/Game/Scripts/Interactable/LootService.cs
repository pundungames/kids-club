using UnityEngine;
using DG.Tweening; // DoTween kullanmak için gerekli

public static class LootService
{
    // DoTween Parametreleri
    private const float JUMP_POWER = 4f;   // Zýplama yüksekliði
    private const float JUMP_DURATION = 0.8f; // Zýplama süresi
    private const float FLOAT_DURATION = 1.5f; // Süzülme (yükselme/alçalma) süresi
    private const float FLOAT_Y_OFFSET = 1f; // Yüksekte süzülme miktarý (sizin isteðiniz)
    private const float SCATTER_RADIUS = 4f; // Düþme çevresi (radius)

    private const float DROP_POSITION_Y_OFFSET = 0.5f; // Spawn pozisyonu Y ofseti (sizin isteðiniz)


    /// <summary>
    /// Verilen düþürme tablosuna göre, belirlenen pozisyondan ödül düþürür ve DoTween ile animasyon uygular.
    /// </summary>
    public static void DropLoot(LootTableSO dropTable, Vector3 position)
    {
        if (dropTable == null)
        {
            Debug.LogError("DropLoot çaðrýldý ancak LootTableSO boþ (Null).");
            return;
        }

        Vector3 spawnPosition = position + Vector3.up * DROP_POSITION_Y_OFFSET;

        foreach (var drop in dropTable.drops)
        {
            /*  // 1. Drop Olasýlýðý Kontrolü
              if (Random.value > drop.dropChance)
              {
                  continue;
              }

              // 2. Düþürülecek miktarý hesapla
              // DropItemData'yý sizin min/maxAmount alanlarýnýza göre güncelledik.
              int amountToDrop = Random.Range(drop.minAmount, drop.maxAmount + 1);

              if (amountToDrop <= 0 || drop.itemPrefab == null)
              {
                  continue;
              }
            */
            // 3. Her birimi ayrý ayrý düþür
            for (int i = 0; i < drop.dropAmount; i++)
            {
                GameObject droppedItem = GameObject.Instantiate(drop.itemPrefab, spawnPosition, Quaternion.identity);

                if (droppedItem != null)
                {
                    // Item'ýn Rigidbody'si olmamalý.
                    if (droppedItem.GetComponent<Rigidbody>() != null)
                    {
                        Debug.LogWarning($"DÝKKAT: '{droppedItem.name}' üzerinde Rigidbody bulundu! DoTween animasyonu ile çakýþabilir. Kaldýrýlmasý önerilir.");
                        // Rigidbody varsa, fizik ile çakýþmamasý için kapatýlabilir.
                        // droppedItem.GetComponent<Rigidbody>().isKinematic = true;
                    }

                    ApplyDropAnimation(droppedItem.transform, spawnPosition);

                    // Eðer düþen þey XP ise, deðerini atayalým
                 /*   XPPickup xpPickup = droppedItem.GetComponent<XPPickup>();
                    if (xpPickup != null)
                    {
                        xpPickup.SetXPValue(drop.xpValuePerUnit);
                    }
                 */
                    droppedItem.SetActive(true); // Pasif prefab'leri aktif et
                }
            }
        }
    }

    // --- YENÝ ANÝMASYON METODU ---
    private static void ApplyDropAnimation(Transform itemTransform, Vector3 startPosition)
    {
        // 1. Hedef Pozisyonu Hesapla
        Vector2 randomCircle = Random.insideUnitCircle * SCATTER_RADIUS;
        Vector3 endPosition = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);

        // 2. Sequence Oluþtur
        Sequence sequence = DOTween.Sequence();

        // --- 1. ADIM: Zýplama ---
        sequence.Append(
            itemTransform.DOJump(
                endPosition,
                JUMP_POWER,
                1,
                JUMP_DURATION
            ).SetEase(Ease.OutQuad)
        );

        // --- ARA ADIM: Zýplama biter bitmez bu kod çalýþýr ---
        sequence.AppendCallback(() =>
        {
            // Null check yapmak güvenlidir, obje yok olmuþ olabilir
            if (itemTransform != null)
            {
               /* var item = itemTransform.GetComponent<CollectableItem>();
                if (item != null) item.moveable = true;*/
            }
        });

        // --- 2. ADIM: Sonsuz Süzülme ---
        // Burasý sonsuz döngü olduðu için sequence asla "Complete" olmaz.
        sequence.Append(
            itemTransform.DOMoveY(endPosition.y + FLOAT_Y_OFFSET, FLOAT_DURATION)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
        );

        sequence.Play();
    }
}