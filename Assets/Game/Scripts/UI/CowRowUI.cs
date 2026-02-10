using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MilkFarm
{
    /// <summary>
    /// CowRowUI - LockButton sistemi için
    /// Purchase button YOK, sadece info + upgrade
    /// </summary>
    public class CowRowUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image cowIcon;
        [SerializeField] private TextMeshProUGUI levelText; // "Lv 1", "Lv 2", "Lv 3"
        [SerializeField] private GameObject lockIcon; // 🔒 icon (not owned)
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TextMeshProUGUI upgradeCostText;
        [SerializeField] private TextMeshProUGUI maxText;
        [SerializeField] GameObject[] openedObjects;
        private Cow cow;
        private CowManager cowManager;
        private MoneyManager moneyManager;

        public System.Action onCowChanged;

        public void Setup(Cow cowData, CowManager manager, MoneyManager money)
        {
            cow = cowData;
            cowManager = manager;
            moneyManager = money;

            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(OnUpgradeClicked);

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

            // Lock icon (show if not owned)
            if (lockIcon != null)
                lockIcon.SetActive(!cow.isUnlocked);

            // Level text (show if owned)
            if (levelText != null)
            {
                levelText.text = $"Lv {cow.level}";
                levelText.gameObject.SetActive(cow.isUnlocked);
            }

            if (cow.isUnlocked)
            {
                // Owned cow - show upgrade/max
                bool isMaxLevel = cow.level >= 3;

                if (upgradeButton != null) upgradeButton.gameObject.SetActive(!isMaxLevel);
                if (maxText != null) maxText.gameObject.SetActive(isMaxLevel);

                if (!isMaxLevel && upgradeCostText != null)
                {
                    float cost = cowManager.GetUpgradeCost(cow.level);
                    upgradeCostText.text = $"${cost:F0}";
                }
                foreach (var item in openedObjects)
                    item.SetActive(true);
            }
            else
            {
                // Not owned - hide upgrade UI
                if (upgradeButton != null) upgradeButton.gameObject.SetActive(false);
                if (maxText != null) maxText.gameObject.SetActive(false);
                foreach (var item in openedObjects)
                    item.SetActive(false);
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