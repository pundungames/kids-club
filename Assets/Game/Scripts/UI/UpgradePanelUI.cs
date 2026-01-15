using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

namespace MilkFarm.UI
{
    /// <summary>
    /// Upgrade panelini yöneten UI controller
    /// İnek level up ve depo kapasitesi upgrade'lerini gösterir
    /// </summary>
    public class UpgradePanelUI : MonoBehaviour
    {
        [Inject] private UpgradeManager upgradeManager;
        [Inject] private CowManager cowManager;

        [Header("İnek Upgrade")]
        [SerializeField] private GameObject cowUpgradeItemPrefab;
        [SerializeField] private Transform cowUpgradeContainer;

        [Header("Depo Upgrade")]
        [SerializeField] private Button depoUpgradeButton;
        [SerializeField] private TextMeshProUGUI depoUpgradeCostText;
        [SerializeField] private TextMeshProUGUI depoLevelText;

        [Header("Para Display")]
        [SerializeField] private TextMeshProUGUI currentMoneyText;

        private void Start()
        {
            SetupCowUpgradeItems();
            SetupDepoUpgrade();
            
            // Event dinleyicileri
            MilkFarmEvents.OnMoneySpent += OnMoneyChanged;
            MilkFarmEvents.OnMoneyCollected += OnMoneyChanged;
            MilkFarmEvents.OnCowLevelUp += OnCowLevelUp;
            MilkFarmEvents.OnUpgradePurchased += OnUpgradePurchased;

            UpdateAllUI();
        }

        private void OnDestroy()
        {
            MilkFarmEvents.OnMoneySpent -= OnMoneyChanged;
            MilkFarmEvents.OnMoneyCollected -= OnMoneyChanged;
            MilkFarmEvents.OnCowLevelUp -= OnCowLevelUp;
            MilkFarmEvents.OnUpgradePurchased -= OnUpgradePurchased;
        }

        /// <summary>
        /// İnek upgrade itemlarını oluştur
        /// </summary>
        private void SetupCowUpgradeItems()
        {
            if (cowUpgradeItemPrefab == null || cowUpgradeContainer == null) return;

            var cows = cowManager.GetAllCows();
            
            for (int i = 0; i < cows.Count; i++)
            {
                if (!cows[i].isUnlocked) continue;

                int cowIndex = i; // Closure için
                GameObject itemObj = Instantiate(cowUpgradeItemPrefab, cowUpgradeContainer);
                
                var upgradeItem = itemObj.GetComponent<CowUpgradeItem>();
                if (upgradeItem != null)
                {
                    upgradeItem.Setup(cowIndex, upgradeManager, cowManager);
                }
            }
        }

        /// <summary>
        /// Depo upgrade button'ını ayarla
        /// </summary>
        private void SetupDepoUpgrade()
        {
            if (depoUpgradeButton != null)
            {
                depoUpgradeButton.onClick.AddListener(OnDepoUpgradeClicked);
            }
        }

        private void OnDepoUpgradeClicked()
        {
            if (upgradeManager.UpgradePackageCapacity())
            {
                Debug.Log("[UpgradePanelUI] Depo kapasitesi artırıldı!");
                UpdateAllUI();
            }
            else
            {
                Debug.LogWarning("[UpgradePanelUI] Depo upgrade için yeterli para yok!");
            }
        }

        private void OnMoneyChanged(float amount)
        {
            UpdateAllUI();
        }

        private void OnCowLevelUp(int cowIndex, int newLevel)
        {
            UpdateAllUI();
        }

        private void OnUpgradePurchased(string upgradeType, int level, float cost)
        {
            UpdateAllUI();
        }

        private void UpdateAllUI()
        {
            UpdateMoneyDisplay();
            UpdateDepoUpgradeUI();
        }

        private void UpdateMoneyDisplay()
        {
            if (currentMoneyText != null)
            {
                float money = upgradeManager.GetCurrentMoney();
                currentMoneyText.text = $"Para: {money:F0}";
            }
        }

        private void UpdateDepoUpgradeUI()
        {
            if (depoUpgradeCostText != null)
            {
                float cost = upgradeManager.GetPackageCapacityUpgradeCost();
                bool canAfford = upgradeManager.CanUpgradePackageCapacity();
                
                depoUpgradeCostText.text = $"Maliyet: {cost:F0}";
                depoUpgradeCostText.color = canAfford ? Color.green : Color.red;
            }

            if (depoUpgradeButton != null)
            {
                depoUpgradeButton.interactable = upgradeManager.CanUpgradePackageCapacity();
            }
        }
    }

    /// <summary>
    /// Tek bir inek upgrade item'ı
    /// </summary>
    public class CowUpgradeItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI cowInfoText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Button upgradeButton;

        private int cowIndex;
        private UpgradeManager upgradeManager;
        private CowManager cowManager;

        public void Setup(int index, UpgradeManager upgradeMgr, CowManager cowMgr)
        {
            cowIndex = index;
            upgradeManager = upgradeMgr;
            cowManager = cowMgr;

            if (upgradeButton != null)
            {
                upgradeButton.onClick.AddListener(OnUpgradeClicked);
            }

            UpdateUI();
        }

        private void OnUpgradeClicked()
        {
            if (upgradeManager.UpgradeCowLevel(cowIndex))
            {
                Debug.Log($"[CowUpgradeItem] İnek {cowIndex} upgrade edildi!");
                UpdateUI();
            }
        }

        private void Update()
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            var cow = cowManager.GetCow(cowIndex);
            if (cow == null) return;

            if (cowInfoText != null)
            {
                cowInfoText.text = $"İnek {cowIndex + 1}\nLevel {cow.level}";
            }

            float cost = upgradeManager.GetCowUpgradeCost(cowIndex);
            bool canAfford = upgradeManager.CanUpgradeCow(cowIndex);

            if (costText != null)
            {
                costText.text = $"{cost:F0}";
                costText.color = canAfford ? Color.green : Color.red;
            }

            if (upgradeButton != null)
            {
                upgradeButton.interactable = canAfford;
            }
        }
    }
}
