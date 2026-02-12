using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public Button buyButton;

    public void SetItem(ShopItemSO item)
    {
        nameText.text = item.name;
        if (item.itemIcon)
            iconImage.sprite = item.itemIcon;
        priceText.text = item.price.ToString() + "$";

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => OnBuyClicked(item));
    }

    private void OnBuyClicked(ShopItemSO item)
    {
        Debug.Log(item.itemName + " satýn alýnýyor... Fiyat: " + item.price);
        // Burada parayý kontrol eden GameManager'a sinyal gönderebilirsin
    }
}