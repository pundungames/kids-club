using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MilkFarm
{
    /// <summary>
    /// StableSlotUI - + button opens PurchasePanel
    /// </summary>
    public class StableSlotUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button slotButton; // Unlocked slot click → StableInfoPanel
        [SerializeField] private Image stableImage;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private TextMeshProUGUI capacityText; // "3/3"
        [SerializeField] private Button plusButton; // + button (locked slot)

        private int stableIndex;
        private StableManager stableManager;

        public System.Action<int> onSlotClicked;
        public System.Action<int> onPurchaseClicked; // New: purchase callback

        public void Setup(int index, StableManager manager)
        {
            stableIndex = index;
            stableManager = manager;

            if (slotButton != null)
                slotButton.onClick.AddListener(() => onSlotClicked?.Invoke(stableIndex));

            // ✅ + button for purchase
            if (plusButton != null)
                plusButton.onClick.AddListener(() => onPurchaseClicked?.Invoke(stableIndex));

            Refresh();
        }

        public void Refresh()
        {
            if (stableManager == null) stableManager = FindObjectOfType<StableManager>();

            bool isUnlocked = false;

            if (stableManager.isChicken)
                isUnlocked = stableManager.IsChickenStableUnlocked(stableIndex);
            else isUnlocked = stableManager.IsStableUnlocked(stableIndex);
            // Lock icon
            if (lockIcon != null)
                lockIcon.SetActive(!isUnlocked);

            // + button (show only if locked)
            if (plusButton != null)
                plusButton.gameObject.SetActive(!isUnlocked);

            if (isUnlocked)
            {
                // Show capacity
                var (current, max) = stableManager.GetStableCapacity(stableIndex);

                if (capacityText != null)
                {
                    capacityText.gameObject.SetActive(true);
                    capacityText.text = $"{current}/{max}";
                }
            }
            else
            {
                // Hide capacity
                if (capacityText != null)
                    capacityText.gameObject.SetActive(false);
            }
        }
    }
}