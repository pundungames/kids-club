using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// Stable Selection Panel - Event-driven refresh
    /// </summary>
    public class StableSelectionPanel : MonoBehaviour
    {
        [Inject] private StableManager stableManager;
        [Inject] private UIManager uiManager;

        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button closeButton;
        [SerializeField] private StableSlotUI[] stableSlots;

        [Header("Panels")]
        [SerializeField] private StableInfoPanel stableInfoPanel;

        private void OnEnable()
        {
            MilkFarmEvents.OnAreaUnlocked += HandleAreaUnlocked;
            MilkFarmEvents.OnCowUnlocked += HandleCowUnlocked; // ✅ EKLE
        }

        private void OnDisable()
        {
            MilkFarmEvents.OnAreaUnlocked -= HandleAreaUnlocked;
            MilkFarmEvents.OnCowUnlocked -= HandleCowUnlocked; // ✅ EKLE
        }

        private void HandleCowUnlocked(int cowIndex)
        {
            Debug.Log($"[StableSelectionPanel] 🔔 Cow {cowIndex} unlocked event!");
            RefreshAll(); // ✅ Tüm slot'ları güncelle (capacity text)
        }
        private void Start()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            // Setup slots
            for (int i = 0; i < stableSlots.Length; i++)
            {
                int index = i;
                stableSlots[i].Setup(index, stableManager);
                stableSlots[i].onSlotClicked += OnStableSlotClicked;
                stableSlots[i].onPurchaseClicked += OnPurchaseButtonClicked;
            }

            //Close();
        }

        public void Open()
        {
            if (panel != null) panel.SetActive(true);
            RefreshAll();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void RefreshAll()
        {
            foreach (var slot in stableSlots)
                slot.Refresh();
        }

        // ✅ EVENT HANDLER - Area unlocked
        private void HandleAreaUnlocked(int areaIndex)
        {
            Debug.Log($"[StableSelectionPanel] 🔔 Area {areaIndex} unlocked event!");
            RefreshAll(); // UI güncelle
        }

        private void OnStableSlotClicked(int stableIndex)
        {
            bool isUnlocked = stableManager.IsStableUnlocked(stableIndex);

            if (isUnlocked)
            {
                if (stableInfoPanel != null)
                    stableInfoPanel.Open(stableIndex);
            }
        }

        private void OnPurchaseButtonClicked(int stableIndex)
        {
            if (uiManager != null)
            {
                uiManager.OpenAreaPurchasePanel(stableIndex);
            }
        }
    }
}