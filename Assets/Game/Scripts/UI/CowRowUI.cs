using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// CowRowUI - CLEAN VERSION (GameConfig only)
    /// </summary>
    public class CowRowUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image cowIcon;
        [SerializeField] private TextMeshProUGUI levelText; // "Lv 1"
        [SerializeField] private TextMeshProUGUI productionTimeText; // "30s"
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TextMeshProUGUI upgradeCostText;
        [SerializeField] private GameObject maxText;
        [SerializeField] private Button plusButton;
        [SerializeField] private GameObject[] purchasedOpenObjects;

        private AnimalData animal;
        private IAnimalManager animalManager;
        private IAPManager iapManager;

        [Inject] private GameConfig config; // ✅ Just GameConfig!

        public System.Action onCowChanged;
        public System.Action<int> onPurchaseClicked;

        public void Setup(AnimalData animalData, IAnimalManager manager, IAPManager iap)
        {
            animal = animalData;
            animalManager = manager;
            iapManager = iap;

            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveAllListeners();
                upgradeButton.onClick.AddListener(OnUpgradeClicked);
            }

            if (plusButton != null)
                plusButton.onClick.AddListener(() => onPurchaseClicked?.Invoke(animal.index));

            Refresh();
        }

        private void Refresh()
        {
            if (animal == null) return;

            // Icon
            if (cowIcon != null)
            {
                Sprite sprite = animalManager.GetAnimalSprite(animal.level);
                if (sprite != null) cowIcon.sprite = sprite;
                cowIcon.color = animal.isUnlocked ? Color.white : new Color(0.3f, 0.3f, 0.3f, 1f);
            }

            // Level text
            if (levelText != null)
                levelText.text = animal.isUnlocked ? $"Lv {animal.level}" : "Locked";

            // Production time
            if (productionTimeText != null)
            {
                float pt = animalManager.GetProductionTime(animal.level);
                productionTimeText.text = $"{pt:F0}s";
            }

            // Lock icon
            if (lockIcon != null) lockIcon.SetActive(!animal.isUnlocked);

            // Upgrade button
            if (animal.isUnlocked && animal.level < 3)
            {
                if (upgradeButton != null) upgradeButton.gameObject.SetActive(true);
                if (maxText != null) maxText.SetActive(false);
                int cost = animalManager.GetUpgradeCostGems(animal.level);
                if (upgradeCostText != null) upgradeCostText.text = $"{cost}";
            }
            else if (animal.isUnlocked && animal.level >= 3)
            {
                if (upgradeButton != null) upgradeButton.gameObject.SetActive(false);
                if (maxText != null) maxText.SetActive(true);
            }
            else
            {
                if (upgradeButton != null) upgradeButton.gameObject.SetActive(false);
                if (maxText != null) maxText.SetActive(false);
            }

            // Plus button (purchase)
            if (plusButton != null) plusButton.gameObject.SetActive(!animal.isUnlocked);

            // Purchased objects
            if (purchasedOpenObjects != null)
                foreach (var obj in purchasedOpenObjects)
                    if (obj != null) obj.SetActive(animal.isUnlocked);
        }
        private void OnUpgradeClicked()
        {
            if (animal == null || !animal.isUnlocked) return;
            if (animalManager.UpgradeAnimal(animal.index, iapManager))
            {
                animal.level++; // local data güncelle
                Refresh();
                onCowChanged?.Invoke();
            }
        }
    }
}