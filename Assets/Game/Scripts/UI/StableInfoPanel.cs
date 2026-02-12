using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// Stable Info Panel - Event-driven refresh
    /// </summary>
    public class StableInfoPanel : MonoBehaviour
    {
        [Inject] private StableManager stableManager;
        [Inject] private CowManager cowManager;
        [Inject] private IAPManager iapManager; // ✅ Changed from MoneyManager
        [Inject] private UIManager uiManager;

        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Image stableImage;
        [SerializeField] private TextMeshProUGUI capacityText;

        [Header("Cow List")]
        [SerializeField] private Transform cowListContent;
        [SerializeField] private CowRowUI cowRowPrefab;

        private int currentStableIndex = -1;
        private List<CowRowUI> cowRows = new List<CowRowUI>();

        // ✅ EVENT SUBSCRIPTION
        private void OnEnable()
        {
            MilkFarmEvents.OnCowUnlocked += HandleCowUnlocked;
            MilkFarmEvents.OnCowUpgraded += HandleCowUpgraded;
        }

        private void OnDisable()
        {
            MilkFarmEvents.OnCowUnlocked -= HandleCowUnlocked;
            MilkFarmEvents.OnCowUpgraded -= HandleCowUpgraded;
        }

        private void Start()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

           // Close();
        }

        public void Open(int stableIndex)
        {
            currentStableIndex = stableIndex;
            if (panel != null) panel.SetActive(true);
            Refresh();
            RefreshCowList();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            currentStableIndex = -1;
        }

        private void Refresh()
        {
            if (currentStableIndex < 0) return;

            var (current, max) = stableManager.GetStableCapacity(currentStableIndex);

            if (capacityText != null)
                capacityText.text = $"{current}/{max}";
        }

        private void RefreshCowList()
        {
            if (currentStableIndex < 0) return;

            // Clear existing rows
            foreach (var row in cowRows)
                if (row != null) Destroy(row.gameObject);
            cowRows.Clear();

            // Get cows in this stable
            var cows = stableManager.GetCowsInStable(currentStableIndex);

            // Spawn rows
            foreach (var cow in cows)
            {
                CowRowUI row = Instantiate(cowRowPrefab, cowListContent);
                row.Setup(cow, cowManager, iapManager);
                row.onCowChanged += RefreshCowList;
                row.onPurchaseClicked += OnCowPurchaseButtonClicked;
                cowRows.Add(row);
            }
        }
        private void HandleCowUnlocked(int cowIndex)
        {
            // This stable'ın ineği mi?
            int stableIndex = cowIndex / 3; // 3 cows per stable

            if (stableIndex != currentStableIndex) return;

            Debug.Log($"[StableInfoPanel] 🔔 Cow {cowIndex} unlocked event!");

            Refresh(); // ✅ Capacity güncelle (2/3 → 3/3)
            RefreshCowList(); // ✅ Cow list güncelle
        }

        // ✅ EVENT HANDLER - Cow upgraded
        private void HandleCowUpgraded(int cowIndex, int newLevel)
        {
            int stableIndex = cowIndex / 3;

            if (stableIndex != currentStableIndex) return;

            Debug.Log($"[StableInfoPanel] 🔔 Cow {cowIndex} upgraded to Lv{newLevel}!");
            RefreshCowList();
        }

        private void OnCowPurchaseButtonClicked(int cowIndex)
        {
            if (uiManager != null)
            {
                uiManager.OpenCowPurchasePanel(cowIndex);
            }
        }
    }
}