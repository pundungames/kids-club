using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// UI Manager - ScriptableObject based purchase system
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] internal PurchasePanelUI purchasePanel;

        [Header("ScriptableObject References")]
        [SerializeField] private PurchaseItemData[] cowPurchaseData;     // 12 cow unlock data
        [SerializeField] private PurchaseItemData[] chickenPurchaseData;     // 12 cow unlock data

        [SerializeField] private PurchaseItemData[] areaPurchaseData;    // 4 stable unlock data
        [SerializeField] private PurchaseItemData[] chickenAreaPurchaseData;    // 4 stable unlock data

        [SerializeField] private PurchaseItemData[] troughPurchaseData;  // Trough unlock data
        [SerializeField] private PurchaseItemData[] slotPurchaseData;    // Slot unlock data

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
        public void OpenChickenPurchasePanel(int cowIndex)
        {
            if (chickenPurchaseData == null || cowIndex >= chickenPurchaseData.Length)
            {
                Debug.LogError($"[UIManager] Cow data yok! Index: {cowIndex}");
                return;
            }

            PurchaseItemData data = chickenPurchaseData[cowIndex];
            if (data == null)
            {
                Debug.LogError($"[UIManager] Cow data null! Index: {cowIndex}");
                return;
            }

            OpenPurchasePanel(data);
        }
        public void OpenChickenAreaPurchasePanel(int areaIndex)
        {
            if (chickenAreaPurchaseData == null || areaIndex >= chickenAreaPurchaseData.Length)
            {
                Debug.LogError($"[UIManager] Area data yok! Index: {areaIndex}");
                return;
            }

            PurchaseItemData data = chickenAreaPurchaseData[areaIndex];
            if (data == null)
            {
                Debug.LogError($"[UIManager] Area data null! Index: {areaIndex}");
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
            if (data == null)
            {
                Debug.LogError($"[UIManager] Area data null! Index: {areaIndex}");
                return;
            }

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
            if (data == null)
            {
                Debug.LogError($"[UIManager] Trough data null! Index: {troughIndex}");
                return;
            }

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
            if (data == null)
            {
                Debug.LogError($"[UIManager] Slot data null! Index: {slotIndex}");
                return;
            }

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

            if (data == null)
            {
                Debug.LogError("[UIManager] Purchase data null!");
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
        public void Home()
        {
            SceneManager.LoadScene(1);
        }
    }
}