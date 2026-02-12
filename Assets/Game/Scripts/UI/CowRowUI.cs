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

        private Cow cow;
        private CowManager cowManager;
        private IAPManager iapManager;

        [Inject] private GameConfig config; // ✅ Just GameConfig!

        public System.Action onCowChanged;
        public System.Action<int> onPurchaseClicked;

        public void Setup(Cow cowData, CowManager manager, IAPManager iap)
        {
            cow = cowData;
            cowManager = manager;
            iapManager = iap;

            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveAllListeners();
                upgradeButton.onClick.AddListener(OnUpgradeClicked);
            }

            if (plusButton != null)
                plusButton.onClick.AddListener(() => onPurchaseClicked?.Invoke(cow.index));

            Refresh();
        }

        private void Refresh()
        {
            if (cow == null) return;

            // Cow icon (from GameConfig)
            if (cowIcon != null)
            {
                Sprite sprite = cowManager.GetCowSprite(cow.level);
                if (sprite != null) cowIcon.sprite = sprite;
                cowIcon.color = cow.isUnlocked ? Color.white : Color.gray;
            }

            // Lock icon
            if (lockIcon != null)
                lockIcon.SetActive(!cow.isUnlocked);

            // + button
            if (plusButton != null)
                plusButton.gameObject.SetActive(!cow.isUnlocked);

            // Level text
            if (levelText != null)
            {
                levelText.text = $"Lv {cow.level}";
                levelText.gameObject.SetActive(cow.isUnlocked);
            }

            // ✅ Production time (from GameConfig)
            if (productionTimeText != null)
            {
                if (cow.isUnlocked && config != null)
                {
                    float time = config.GetProductionTime(cow.level);
                    productionTimeText.text = $"{time:F0}s";
                    productionTimeText.gameObject.SetActive(true);
                }
                else
                {
                    productionTimeText.gameObject.SetActive(false);
                }
            }

            if (cow.isUnlocked)
            {
                bool isMaxLevel = cow.level >= 3;

                if (upgradeButton != null) upgradeButton.gameObject.SetActive(!isMaxLevel);
                if (maxText != null) maxText.gameObject.SetActive(isMaxLevel);

                if (!isMaxLevel && upgradeCostText != null)
                {
                    int gemCost = cowManager.GetUpgradeCostGems(cow.level);
                    upgradeCostText.text = $"{gemCost} 💎";
                }

                foreach (var item in purchasedOpenObjects)
                    item.SetActive(true);
            }
            else
            {
                if (upgradeButton != null) upgradeButton.gameObject.SetActive(false);
                if (maxText != null) maxText.gameObject.SetActive(false);

                foreach (var item in purchasedOpenObjects)
                    item.SetActive(false);
            }
        }

        private void OnUpgradeClicked()
        {
            if (cowManager.UpgradeCow(cow.index, iapManager))
            {
                Refresh(); // ✅ Updates production time automatically
                onCowChanged?.Invoke();
            }
        }
    }
}