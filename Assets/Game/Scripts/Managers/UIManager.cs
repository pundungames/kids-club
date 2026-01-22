using UnityEngine;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// UI yöneticisi - Panelleri açar/kapatýr
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] internal PurchasePanelUI purchasePanel;

        [Header("ScriptableObject References")]
        [SerializeField] private PurchaseItemData[] cowPurchaseData;     // Ýnek satýn alma verileri
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
        /// Ýnek satýn alma panelini aç
        /// </summary>
        public void OpenCowPurchasePanel(int cowIndex)
        {
            if (cowPurchaseData == null || cowIndex >= cowPurchaseData.Length)
            {
                Debug.LogError($"[UIManager] Cow data yok! Index: {cowIndex}");
                return;
            }

            PurchaseItemData data = cowPurchaseData[cowIndex];
            if (data == null)
            {
                Debug.LogError($"[UIManager] Cow data null! Index: {cowIndex}");
                return;
            }

            OpenPurchasePanel(data);
        }

        /// <summary>
        /// Area unlock panelini aç
        /// </summary>
        public void OpenAreaPurchasePanel(int areaIndex)
        {
            if (areaPurchaseData == null || areaIndex >= areaPurchaseData.Length)
            {
                Debug.LogError($"[UIManager] Area data yok! Index: {areaIndex}");
                return;
            }

            PurchaseItemData data = areaPurchaseData[areaIndex];
            OpenPurchasePanel(data);
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