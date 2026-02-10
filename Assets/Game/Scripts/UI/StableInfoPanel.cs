using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// Stable Info Panel - LockButton sistemi için
    /// Cow rows always visible, purchase via scene LockButton
    /// </summary>
    public class StableInfoPanel : MonoBehaviour
    {
        [Inject] private StableManager stableManager;
        [Inject] private CowManager cowManager;
        [Inject] private MoneyManager moneyManager;

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

        private void Start()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            Close();
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
        }

        private void Refresh()
        {
            var (current, max) = stableManager.GetStableCapacity(currentStableIndex);

            if (capacityText != null)
                capacityText.text = $"{current}/{max}";
        }

        private void RefreshCowList()
        {
            // Clear existing rows
            foreach (var row in cowRows)
                if (row != null) Destroy(row.gameObject);
            cowRows.Clear();

            // Get cows in this stable
            var cows = stableManager.GetCowsInStable(currentStableIndex);

            // Spawn rows (simplified Setup - no IAPManager)
            foreach (var cow in cows)
            {
                CowRowUI row = Instantiate(cowRowPrefab, cowListContent);
                row.Setup(cow, cowManager, moneyManager);
                row.onCowChanged += RefreshCowList;
                cowRows.Add(row);
            }
        }
    }
}