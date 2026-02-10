using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

namespace MilkFarm
{
    public class StableSelectionPanel : MonoBehaviour
    {
        [Inject] private StableManager stableManager;

        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button closeButton;
        [SerializeField] private StableSlotUI[] stableSlots;

        [Header("Panels")]
        [SerializeField] private StableInfoPanel stableInfoPanel;

        private void Start()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            for (int i = 0; i < stableSlots.Length; i++)
            {
                int index = i;
                stableSlots[i].Setup(index, stableManager);
                stableSlots[i].onSlotClicked += OnStableSlotClicked;
            }

            Close();
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

        private void OnStableSlotClicked(int stableIndex)
        {
            bool isUnlocked = stableManager.IsStableUnlocked(stableIndex);

            if (isUnlocked)
            {
                if (stableInfoPanel != null)
                    stableInfoPanel.Open(stableIndex);
            }
            else
            {
                TryUnlockStable(stableIndex);
            }
        }

        private void TryUnlockStable(int stableIndex)
        {
            if (stableManager.UnlockStable(stableIndex))
                RefreshAll();
        }
    }
}