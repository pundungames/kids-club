using UnityEngine;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// UI yöneticisi - Panelleri açar/kapatır
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Inject] private CowManager cowManager;
        [Inject] private StableManager stableManager;

        [Header("Panels")]
        [SerializeField] internal PurchasePanelUI purchasePanel;

        [Header("ScriptableObject References")]
        [SerializeField] private PurchaseItemData[] cowPurchaseData;     // İnek satın alma verileri
        [SerializeField] private PurchaseItemData[] areaPurchaseData;    // Area unlock verileri
        [SerializeField] private PurchaseItemData[] troughPurchaseData;  // Trough unlock verileri
        [SerializeField] private PurchaseItemData[] slotPurchaseData;    // Slot unlock verileri

        private void Start()
        {
            // Panel'leri kapat
            if (purchasePanel != null)
            {
                purchasePanel.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// İnek satın alma panelini aç
        /// </summary>
        public void OpenCowPurchasePanel(int cowIndex)
        {
            if (purchasePanel == null)
            {
                Debug.LogError("[UIManager] PurchasePanel null!");
                return;
            }

            if (cowManager == null)
            {
                Debug.LogError("[UIManager] CowManager null!");
                return;
            }

            // Create dynamic data
            PurchaseItemData data = ScriptableObject.CreateInstance<PurchaseItemData>();
            data.titleText = "UNLOCK COW";
            data.productName = $"Cow #{cowIndex + 1}";
            data.benefitDescription = "Unlock this cow to produce milk!";

            int gemCost = cowManager.GetCowPurchaseCost(cowIndex);
            data.gemCost = gemCost;
            data.priceText = $"{gemCost} 💎";
            data.isRealMoney = false;

            data.type = PurchaseType.UnlockCow;
            data.targetIndex = cowIndex;

            purchasePanel.OpenPanel(data);
        }


        /// <summary>
        /// Area unlock panelini aç
        /// </summary>
        public void OpenAreaPurchasePanel(int areaIndex)
        {
            if (purchasePanel == null)
            {
                Debug.LogError("[UIManager] PurchasePanel null!");
                return;
            }

            if (stableManager == null)
            {
                Debug.LogError("[UIManager] StableManager null!");
                return;
            }

            // Create dynamic data
            PurchaseItemData data = ScriptableObject.CreateInstance<PurchaseItemData>();
            data.titleText = "UNLOCK STABLE";
            data.productName = $"Stable {areaIndex + 1}";
            data.benefitDescription = "Unlock this stable to house 3 more cows!";

            int gemCost = stableManager.GetStableUnlockCost(areaIndex);
            data.gemCost = gemCost;
            data.priceText = $"{gemCost}";
            data.isRealMoney = false;

            data.type = PurchaseType.UnlockArea;
            data.targetIndex = areaIndex;

            purchasePanel.OpenPanel(data);
        }

        /// <summary>
        /// Trough unlock panelini aç
        /// </summary>
        public void OpenTroughPurchasePanel(int troughIndex)
        {
            if (troughPurchaseData == null || troughIndex >= troughPurchaseData.Length)
            {
                Debug.LogError($"[UIManager] Trough data yok! Index: {troughIndex}");
                return;
            }

            PurchaseItemData data = troughPurchaseData[troughIndex];
            OpenPurchasePanel(data);
        }

        /// <summary>
        /// Slot unlock panelini aç
        /// </summary>
        public void OpenSlotPurchasePanel(int slotIndex)
        {
            if (slotPurchaseData == null || slotIndex >= slotPurchaseData.Length)
            {
                Debug.LogError($"[UIManager] Slot data yok! Index: {slotIndex}");
                return;
            }

            PurchaseItemData data = slotPurchaseData[slotIndex];
            OpenPurchasePanel(data);
        }

        /// <summary>
        /// Genel purchase panel aç
        /// </summary>
        public void OpenPurchasePanel(PurchaseItemData data)
        {
            if (purchasePanel == null)
            {
                Debug.LogError("[UIManager] Purchase panel null!");
                return;
            }

            purchasePanel.OpenPanel(data);
        }

        /// <summary>
        /// Tüm panelleri kapat
        /// </summary>
        public void CloseAllPanels()
        {
            if (purchasePanel != null)
            {
                purchasePanel.ClosePanel();
            }
        }
    }
}