using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class TowerCardManager : MonoBehaviour
{
    [Inject] UIManager UIManager;
    [Inject] DiContainer container;
    [SerializeField] internal bool shop;

    [Header("Kart Ayarları")]
  //  [SerializeField] private List<TowerStatSo> allTowerStats;
    [SerializeField] private Button cancelButton;
    [SerializeField] private GameObject shopButton;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private GameObject ribbon;

    [Header("Spawn Ayarları")]
    [SerializeField] private List<TowerCardContent> _activeCards = new List<TowerCardContent>();

    private TowerCardContent _selectedCard = null;
    [SerializeField] bool _hasCardBeenChosen = false;

    [Header("Reroll Ayarları")]
    [SerializeField] TextMeshProUGUI rerollPriceText;
    [SerializeField] private Button rerollButton;
    //[SerializeField] private int maxRerolls = 1;
    private int currentRerolls = 0;

    [Header("Rarity Drop Weights")]
    [SerializeField] private int commonWeight = 70;
    [SerializeField] private int uncommonWeight = 25;
    [SerializeField] private int rareWeight = 5;

    [Header("Context Weight Modifiers")]
    [SerializeField] private int synergyBonusWeight = 60; // TowerContext'ten gelen sinerji için büyük bonus
    [SerializeField] private float damageTypeMultiplier = 1.5f; // Ice/Fire gibi hasar tipi eşleşmesi (x1.5)

    [Header("Stack Limits")]
    [SerializeField] private int commonStackLimit = 3;
    [SerializeField] private int rareStackLimit = 2;
    [SerializeField] private int supportCategoryLimit = 2;
    [SerializeField] private int stackPenaltyWeight = 1; // Limit aşılırsa düşülecek minimum ağırlık

    private void OnEnable()
    {
       // EventManager.toGame += OnPlacementConfirmed;
    }
    private void OnDisable()
    {
       // EventManager.toGame -= OnPlacementConfirmed;
    }
    public void Open()
    {
        currentRerolls = 0;

        cancelButton.gameObject.SetActive(false);
        continueButton.SetActive(false);

        if (shop)
        {
            continueButton.SetActive(true);
            shopButton.SetActive(false);
        }

        OpenNewCardSelection(shop);
        ribbon.SetActive(true);
        if (PlayerPrefs.GetInt("_level", 0) == 0) rerollButton.gameObject.SetActive(false);
        if (rerollButton != null)
        {
            rerollPriceText.text = ((10 - PlayerPrefs.GetInt("_castleReroll", 0)) * (currentRerolls + 1)).ToString();
            rerollButton.gameObject.SetActive(true);
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(OnRerollClick);
        }
        CheckCurrency();
    }
    public void CheckCurrency()
    {
        foreach (var item in _activeCards)
        {
            item.CheckCurrency();
        }
        int cost = (10 - PlayerPrefs.GetInt("_castleReroll", 0)) * (currentRerolls + 1);
        // rerollButton.interactable = resourceManager.HasResource(ResourceType.Gold, cost);
    }

    public void OnCancelButtonClick()
    {
        Taptic.Light();

        if (_selectedCard != null)
        {
            _selectedCard.ResetCardVisuals();
            _selectedCard = null;
        }
        ribbon.SetActive(true);
        rerollButton.gameObject.SetActive(true);
        _hasCardBeenChosen = false;
        cancelButton.gameObject.SetActive(false);
        SetAllCardsInteractable(true);
    }

    public void OnPlacementConfirmed()
    {
        continueButton.SetActive(false);
        OnCancelButtonClick();
        rerollButton.gameObject.SetActive(false);
        this.gameObject.SetActive(false);
    }

    public bool CanSelectCard() => !_hasCardBeenChosen;

    public void CardSelected(TowerCardContent card)
    {
        if (_hasCardBeenChosen) return;
        ribbon.SetActive(false);
        _selectedCard = card;
        _hasCardBeenChosen = true;
        Taptic.Medium();
        cancelButton.gameObject.SetActive(true);
        continueButton.SetActive(false);

        SetAllCardsInteractable(false);
        rerollButton.gameObject.SetActive(false);
        _activeCards.ForEach(c => c.button.interactable = false);
    }

    private void SetAllCardsInteractable(bool isInteractable)
    {
        foreach (var cardContent in _activeCards)
        {
            if (/*_selectedCard != cardContent && */cardContent != null)
            {
                cardContent.button.interactable = isInteractable;
                cardContent.gameObject.SetActive(isInteractable);
            }
        }
    }

    // --- 🔮 Weighted Random Kart Seçimi ---
    public void OpenNewCardSelection(bool shop)
    {
        /* List<TowerStatSo> selectedStats = GetWeightedRandomTowerStats(_activeCards.Count);

         for (int i = 0; i < _activeCards.Count; i++)
         {
             TowerCardContent cardContent = _activeCards[i];
             cardContent.button.interactable = true;

             if (i < selectedStats.Count)
             {
                 cardContent.SetContent(selectedStats[i], this, shop);
                 cardContent.gameObject.SetActive(true);
             }
             else
             {
                 cardContent.gameObject.SetActive(false);
             }
         }*/

        _selectedCard = null;
        _hasCardBeenChosen = false;
        cancelButton.gameObject.SetActive(false);
        SetAllCardsInteractable(true);
        this.gameObject.SetActive(true);
    }


    private void OnRerollClick()
    {
        /*if (currentRerolls >= maxRerolls)
        {
            Debug.Log("Reroll hakkın bitti!");
            rerollButton.interactable = false;
            return;
        }*/
        Taptic.Light();

        int cost = (10 - PlayerPrefs.GetInt("_castleReroll", 0)) * (currentRerolls + 1);
        //  if (!resourceManager.HasResource(ResourceType.Gold, cost)) return;

        // resourceManager.PaymentResource(ResourceType.Gold, cost);
        CheckCurrency();
        currentRerolls++;
        // EventManager.OnReroll();
        foreach (var card in _activeCards)
        {
            card.ResetCardVisuals();
            card.transform.DOScale(Vector3.one * 1.1f, .1f)
                .SetUpdate(true)
                .OnComplete(() => card.transform.DOScale(Vector3.one, .1f).SetUpdate(true));
        }

        OpenNewCardSelection(shop);
        rerollPriceText.text = ((10 - PlayerPrefs.GetInt("_castleReroll", 0)) * (currentRerolls + 1)).ToString();
    }

    public void Continue()
    {
        Taptic.Light();

        // UIManager.NextWaveIndicator();

        //  EventManager.OnToGame();
    }

    #region Weighted
    /*  private List<TowerStatSo> GetWeightedRandomTowerStats(int count)
      {
          List<TowerStatSo> result = new List<TowerStatSo>();

          // --- DEĞİŞİKLİK BURADA BAŞLIYOR ---

          // 1. Adım: Sadece PlayerPrefs'te kaydı olan (Unlocked) kuleleri havuza al.
          // allTowerStats listesindeki her bir 't' için Key kontrolü yapıyoruz.
          List<TowerStatSo> tempPool = allTowerStats
              .Where(t => PlayerPrefs.HasKey(t.towerName))
              .ToList();

          // GÜVENLİK ÖNLEMİ: Eğer oyunun başında hiç save yoksa veya debug yapıyorsan
          // liste boş kalıp hata vermesin diye, boşsa ana listeyi yedek olarak kullan.
          if (tempPool.Count == 0)
          {
              Debug.LogWarning("Hiçbir kulenin save kaydı bulunamadı! Varsayılan olarak hepsi açılıyor.");
              tempPool = new List<TowerStatSo>(allTowerStats);
          }

          // --- DEĞİŞİKLİK BURADA BİTİYOR ---

          for (int i = 0; i < count && tempPool.Count > 0; i++)
          {
              TowerStatSo chosen = GetWeightedRandom(tempPool);

              // Seçilen null değilse listeye ekle
              if (chosen != null)
              {
                  result.Add(chosen);
                  tempPool.Remove(chosen);
              }
          }

          return result;
      }
      private TowerStatSo GetWeightedRandom(List<TowerStatSo> towers)
      {
          List<TowerBase> placedTowers = castle.GetPlacedTowers();
          Dictionary<TowerStatSo, int> towerCounts = new Dictionary<TowerStatSo, int>();
          int supportCount = 0;

          // Context Analizi: Mevcut kuleleri say
          foreach (var tower in placedTowers)
          {
              // Kule sayısını kaydet (Stack takibi)
              towerCounts[tower.towerStat] = towerCounts.GetValueOrDefault(tower.towerStat, 0) + 1;

              // Support kule sayısını kaydet (Limit takibi)
              if (tower.towerStat.category == TowerCategory.Support)
              {
                  supportCount++;
              }
          }

          int totalWeight = 0;
          Dictionary<TowerStatSo, int> weights = new Dictionary<TowerStatSo, int>();

          foreach (var t in towers)
          {
              // 1. Temel Rarity Ağırlığı
              int currentWeight = t.rarityType switch
              {
                  RarityType.common => commonWeight,
                  RarityType.uncommon => uncommonWeight,
                  RarityType.rare => rareWeight,
                  _ => 1
              };

              // --- 2. Sinerji (Bonus Ağırlık) Uygulaması ---

              // 2a. DamageType Multiplier (Ice/Fire x1.5)
              // Eğer kalede bu DamageType'a sahip bir kule varsa, ağırlığı 1.5 katına çıkar.
              bool hasMatchingDamageType = placedTowers.Any(p => p.towerStat.damageType == t.damageType);
              if (hasMatchingDamageType)
              {
                  currentWeight = Mathf.RoundToInt(currentWeight * damageTypeMultiplier); // x1.5
              }

              // 2b. TowerContext Sinerji Bonus
              // Eğer kaledeki herhangi bir kule bu adayı context'inde gösteriyorsa, büyük bir bonus ver.
              bool isContextSynergy = placedTowers.Any(p => p.towerStat.towerContext.Contains(t));
              if (isContextSynergy)
              {
                  currentWeight += synergyBonusWeight; // Sabit +60 puan
              }

              // --- 3. Kısıtlama (Ceza Ağırlığı) Uygulaması ---

              int stack = towerCounts.GetValueOrDefault(t, 0);

              // 3a. Rare Kule Stack Limiti (Max 1)
              if (t.rarityType == RarityType.rare && stack >= rareStackLimit)
              {
                  currentWeight = stackPenaltyWeight; // Çok düşük ağırlık
              }
              // 3b. Common Kule Stack Limiti (Max 3)
              else if (t.rarityType == RarityType.common && stack >= commonStackLimit)
              {
                  currentWeight = stackPenaltyWeight; // Çok düşük ağırlık
              }

              // Negatif ağırlık olmaması için kontrol
              currentWeight = Mathf.Max(1, currentWeight);

              weights.Add(t, currentWeight);
              totalWeight += currentWeight;
          }

          // 4. Random Seçim
          int randomValue = Random.Range(0, totalWeight);
          foreach (var kvp in weights)
          {
              randomValue -= kvp.Value;
              if (randomValue <= 0)
                  return kvp.Key;
          }

          return towers.Count > 0 ? towers[0] : null;
      }*/
    #endregion
}
