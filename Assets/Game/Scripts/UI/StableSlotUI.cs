using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MilkFarm
{
    public class StableSlotUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button slotButton;
        [SerializeField] private Image stableImage; // Single sprite (doesn't change)
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private TextMeshProUGUI capacityText; // "3/3"
        [SerializeField] private TextMeshProUGUI unlockCostText; // "💎 500"

        private int stableIndex;
        private StableManager stableManager;

        public System.Action<int> onSlotClicked;

        public void Setup(int index, StableManager manager)
        {
            stableIndex = index;
            stableManager = manager;

            if (slotButton != null)
                slotButton.onClick.AddListener(() => onSlotClicked?.Invoke(stableIndex));

            Refresh();
        }

        public void Refresh()
        {
            bool isUnlocked = stableManager.IsStableUnlocked(stableIndex);

            // Lock icon
            if (lockIcon != null)
                lockIcon.SetActive(!isUnlocked);

            if (isUnlocked)
            {
                // Show capacity
                var (current, max) = stableManager.GetStableCapacity(stableIndex);

                if (capacityText != null)
                {
                    capacityText.gameObject.SetActive(true);
                    capacityText.text = $"{current}/{max}";
                }

                if (unlockCostText != null)
                    unlockCostText.transform.parent.gameObject.SetActive(false);
            }
            else
            {
                // Show unlock cost
                if (capacityText != null)
                    capacityText.gameObject.SetActive(false);

                if (unlockCostText != null)
                {
                    unlockCostText.transform.parent.gameObject.SetActive(true);
                    int cost = stableManager.GetStableUnlockCost(stableIndex);
                    unlockCostText.text = $"💎 {cost}";
                }
            }
        }
    }
}