using UnityEngine;

public class MoneyItem : MonoBehaviour
{
    private CounterManager manager;

    // Manager bu parayý yaratýrken kendini buraya tanýtacak
    public void Initialize(CounterManager managerRef)
    {
        manager = managerRef;
    }

    void OnMouseDown()
    {
        if (manager != null)
        {
            // Manager'a haber ver: "Beni listeden düþ"
            manager.OnMoneyCollected(this);
        }

        // Efektli yok olma (Opsiyonel: Ses veya partikül eklenebilir)
        Destroy(gameObject);
    }
}