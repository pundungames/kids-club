using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MilkFarm
{
    /// <summary>
    /// CowRowUI - + button opens PurchasePanel
    /// </summary>
    public class CowRowUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image cowIcon;
        [SerializeField] private TextMeshProUGUI levelText; // "Lv 1", "Lv 2", "Lv 3"
        [SerializeField] private GameObject lockIcon; // 🔒 (not owned)
        [SerializeField] private Button upgradeButton; // Upgrade (owned)
        [SerializeField] private TextMeshProUGUI upgradeCostText; // "$500"
        [SerializeField] private TextMeshProUGUI maxText; // "MAX"
        [SerializeField] private Button plusButton; // + button (not owned)

        private Cow cow;
        private CowManager cowManager;
        private MoneyManager moneyManager;

        public System.Action onCowChanged;
        public System.Action<int> onPurchaseClicked; // New: purchase callback

        public void Setup(Cow cowData, CowManager manager, MoneyManager money)
        {
            cow = cowData;
            cowManager = manager;
            moneyManager = money;

            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(OnUpgradeClicked);

            // ✅ + button for purchase
            if (plusButton != null)
                plusButton.onClick.AddListener(() => onPurchaseClicked?.Invoke(cow.index));

            Refresh();
        }

        private void Refresh()
        {
            if (cow == null) return;

            // Cow icon
            if (cowIcon != null)
            {
                Sprite sprite = cowManager.GetCowSprite(cow.level);
                if (sprite != null) cowIcon.sprite = sprite;
                cowIcon.color = cow.isUnlocked ? Color.white : Color.gray;
            }

            // Lock icon
            if (lockIcon != null)
                lockIcon.SetActive(!cow.isUnlocked);

            // + button (show only if not owned)
            if (plusButton != null)
                plusButton.gameObject.SetActive(!cow.isUnlocked);

            // Level text (show if owned)
            if (levelText != null)
            {
                levelText.text = $"Lv {cow.level}";
                levelText.gameObject.SetActive(cow.isUnlocked);
            }

            if (cow.isUnlocked)
            {
                // Owned - show upgrade/max
                bool isMaxLevel = cow.level >= 3;

                if (upgradeButton != null) upgradeButton.gameObject.SetActive(!isMaxLevel);
                if (maxText != null) maxText.gameObject.SetActive(isMaxLevel);

                if (!isMaxLevel && upgradeCostText != null)
                {
                    float cost = cowManager.GetUpgradeCost(cow.level);
                    upgradeCostText.text = $"${cost:F0}";
                }
            }
            else
            {
                // Not owned - hide upgrade UI
                if (upgradeButton != null) upgradeButton.gameObject.SetActive(false);
                if (maxText != null) maxText.gameObject.SetActive(false);
            }
        }

        private void OnUpgradeClicked()
        {
            if (cowManager.UpgradeCow(cow.index, moneyManager))
            {
                Refresh();
                onCowChanged?.Invoke();
            }
        }
    }
}