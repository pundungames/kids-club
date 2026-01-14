using UnityEngine;

public class MilkCrate : MonoBehaviour
{
    [Header("Kasa Ayarlarý")]
    public Transform[] milkSlots;
    private int currentMilkCount = 0;

    // --- YENÝ AYAR: ÞÝÞE BOYUTU ---
    // Eskiden 1.0'dý. Þimdi 0.8 yaptýk (0.2 küçüldü).
    // Editörden istediðin gibi deðiþtirebilirsin.
    public float bottleScale = 0.8f;

    public bool IsFull => currentMilkCount >= milkSlots.Length;

    public void AddMilkToCrate(GameObject milkPrefab, Vector3 startPos)
    {
        if (IsFull) return;

        Transform targetSlot = milkSlots[currentMilkCount];

        if (milkPrefab != null)
        {
            GameObject newMilk = Instantiate(milkPrefab, startPos, Quaternion.identity);

            FlyingItem flyer = newMilk.GetComponent<FlyingItem>();
            if (flyer == null) flyer = newMilk.AddComponent<FlyingItem>();

            flyer.FlyTo(targetSlot.position, () =>
            {
                newMilk.transform.SetParent(targetSlot);
                newMilk.transform.localPosition = Vector3.zero;
                newMilk.transform.localRotation = Quaternion.identity;

                // --- GÜNCELLEME BURADA ---
                // Þiþeyi senin belirlediðin boyuta getiriyoruz.
                newMilk.transform.localScale = Vector3.one * bottleScale;
            });
        }

        currentMilkCount++;
    }
}