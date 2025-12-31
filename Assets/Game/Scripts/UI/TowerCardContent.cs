using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Zenject;
using System.Collections.Generic;
using System;

public class TowerCardContent : MonoBehaviour
{
    // UI Elementleri
    [SerializeField] TextMeshProUGUI towerName, towerInfo;
    [SerializeField] TextMeshProUGUI priceText;
    [SerializeField] Image towerImage;
    [SerializeField] GameObject focus;
    [SerializeField] GameObject sold;
    [SerializeField] Image border;
    [SerializeField] internal Button button;
    // Kart Verisi
   // private TowerStatSo _towerStat; // Kartýn Scriptable Object verisi
    private Vector3 _initialLocalPosition; // Kartýn baþlangýç konumu

    // Görsel Efekt Ayarlarý 
    private readonly float _selectedYOffset = 50f; // Seçilince ne kadar yukarý hareket edecek

    // Ana Yönetici Referansý
    private TowerCardManager _manager;
    [SerializeField] List<CardRarityContent> cardRarity;

    public void SetContent(/*TowerStatSo towerStat, */TowerCardManager manager, bool shop)
    {
      /*  _towerStat = towerStat;
        _manager = manager;

        towerName.text = towerStat.towerName;
        towerInfo.text = towerStat.towerInfo;
        if (shop)
        {
            priceText.text = _towerStat.price.ToString();
            priceText.transform.parent.gameObject.SetActive(true);
        }
        else
            priceText.transform.parent.gameObject.SetActive(false);
        Sprite sprite = towerStat.towerSprite;
        if (sprite != null)
            towerImage.sprite = sprite;


        _initialLocalPosition = transform.localPosition;

        for (int i = 0; i < cardRarity.Count; i++)
        {
            foreach (var item in cardRarity[i].visuals)
            {
                if (i == ((int)towerStat.rarityType))
                {
                    item.SetActive(true);
                    border.sprite = cardRarity[i].border;
                }
                else item.SetActive(false);
            }
        }
      */
        ResetCardVisuals();
    }
    public void CheckCurrency()
    {
        if (!sold.activeSelf)
        {
           /* if (!_resourceManager.HasResource(ResourceType.Gold, _towerStat.price))
                button.interactable = false;*/
        }
    }
    public void SelectTower()
    {
        if (sold.activeSelf) return;
        if (priceText.transform.parent.gameObject.activeSelf)
        {
          /*  if (!_resourceManager.HasResource(ResourceType.Gold, _towerStat.price))
                return;*/
        }
        transform.DOKill(true);
        if (_manager.CanSelectCard())
        {
            focus.SetActive(true);
            transform.DOLocalMoveY(_initialLocalPosition.y + _selectedYOffset, .3f).SetUpdate(true);
            towerImage.enabled = false;

           // _manager.CardSelected(this, _towerPrefab);
        }
    }
    public void Placed()
    {
        sold.SetActive(true);
        focus.SetActive(false);
        transform.localPosition = _initialLocalPosition;
        towerImage.enabled = true;
    }
    // --- Kartý Sýfýrlama Fonksiyonu ---
    public void ResetCardVisuals()
    {
        sold.SetActive(false);
        focus.SetActive(false);
        transform.localPosition = _initialLocalPosition;
        towerImage.enabled = true;
    }

}

[Serializable]
public class CardRarityContent
{
    [SerializeField] internal List<GameObject> visuals;
    [SerializeField] internal Sprite mask, border;
}