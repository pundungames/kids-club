using UnityEngine;

[CreateAssetMenu(fileName = "NewShopItem", menuName = "Shop/Item")]
public class ShopItemSO : ScriptableObject
{
    public string itemName;
    public Sprite itemIcon;
    public int price;
    // Ýleride gerçek para için: public string storeID;
}