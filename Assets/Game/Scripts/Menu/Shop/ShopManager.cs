using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Sýralama yapmak için bu kütüphane þart

public class ShopManager : MonoBehaviour
{
    public List<ShopItemSO> allItems; // 20 tane SO
    public List<ShopItemUI> uiSlots;  // Sahnedeki 20 UI objesi

    void Start()
    {
        InitializeSortedShop();
    }

    void InitializeSortedShop()
    {
        // 1. Listeyi fiyata göre (Price) azdan çoka sýralýyoruz
        List<ShopItemSO> sortedItems = allItems.OrderBy(item => item.price).ToList();

        // 2. Sýralanmýþ listeyi slotlara daðýtýyoruz
        for (int i = 0; i < sortedItems.Count; i++)
        {
            if (i < uiSlots.Count)
            {
                uiSlots[i].gameObject.SetActive(true);
                uiSlots[i].SetItem(sortedItems[i]);
            }
        }

        // 3. Eðer slot sayýsý itemdan fazlaysa boþ kalanlarý gizle
        for (int i = sortedItems.Count; i < uiSlots.Count; i++)
        {
            uiSlots[i].gameObject.SetActive(false);
        }
    }
}