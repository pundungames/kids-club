using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// Purchase Panel UI - FORCE ACTIVE
    /// SetActive çalışmıyor sorunu için force fix
    /// </summary>
    public class PurchasePanelUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI productNameTMP;
        [SerializeField] private TextMeshProUGUI descriptionTMP;
        [SerializeField] private TextMeshProUGUI priceTMP;

        [Header("Gem Cost Panel")]
        [SerializeField] private GameObject gemCostPanel;
        [SerializeField] private TextMeshProUGUI currentGemsTMP;
        [SerializeField] private TextMeshProUGUI requiredGemsTMP;

        [Header("Buttons")]
        [SerializeField] private Button buyButton;
        [SerializeField] private Button closeButton;

        [Inject] private IAPManager iapManager;

        private PurchaseItemData currentData;
        private CanvasGroup canvasGroup;
        private Canvas canvas;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            canvas = GetComponent<Canvas>();

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(ClosePanel);
            }

            // Başlangıçta kapat
            gameObject.SetActive(false);
        }

        public void OpenPanel(PurchaseItemData data)
        {
            if (data == null)
            {
                Debug.LogError("[PurchasePanelUI] Data null!");
                return;
            }

            Debug.Log($"[PurchasePanelUI] 🔓 OpenPanel: {data.productName}");
            Debug.Log($"[DEBUG] ÖNCE - Active: {gameObject.activeSelf}");

            currentData = data;

            // UI doldur
            if (titleTMP != null) titleTMP.text = data.titleText;
            if (iconImage != null) iconImage.sprite = data.icon;
            if (productNameTMP != null) productNameTMP.text = data.productName;
            if (descriptionTMP != null) descriptionTMP.text = data.benefitDescription;
            if (priceTMP != null) priceTMP.text = data.priceText;

            if (gemCostPanel != null)
            {
                bool isGem = !data.isRealMoney && data.gemCost > 0;
                gemCostPanel.SetActive(isGem);

                if (isGem && iapManager != null)
                {
                    int currentGems = iapManager.GetCurrentGems();
                    if (currentGemsTMP != null) currentGemsTMP.text = $"{currentGems} 💎";
                    if (requiredGemsTMP != null) requiredGemsTMP.text = $"{data.gemCost} 💎";
                }
            }

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(OnBuyClicked);
            }

            // ✅ FORCE ACTIVE - Multiple methods
            ForceActivatePanel();

            Debug.Log($"[DEBUG] SONRA - Active: {gameObject.activeSelf}");
            Debug.Log($"[PurchasePanelUI] ✅ Panel açıldı!");

            // Next frame kontrol
            StartCoroutine(CheckActiveNextFrame());
        }

        /// <summary>
        /// Force activate - Her yolu dene
        /// </summary>
        private void ForceActivatePanel()
        {
            // 1. GameObject active
            gameObject.SetActive(true);

            // 2. Transform'dan da
            transform.gameObject.SetActive(true);

            // 3. Parent kontrol
            if (transform.parent != null && !transform.parent.gameObject.activeSelf)
            {
                Debug.LogWarning("[PurchasePanelUI] Parent kapalı, açıyorum!");
                transform.parent.gameObject.SetActive(true);
            }

            // 4. Canvas enable
            if (canvas != null)
            {
                canvas.enabled = true;
            }

            // 5. CanvasGroup
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            // 6. Force update
            Canvas.ForceUpdateCanvases();
        }

        /// <summary>
        /// 1 frame sonra kontrol - Başka script kapatıyor mu?
        /// </summary>
        private IEnumerator CheckActiveNextFrame()
        {
            yield return null; // 1 frame bekle

            Debug.Log($"[DEBUG] 1 FRAME SONRA - Active: {gameObject.activeSelf}");

            if (!gameObject.activeSelf)
            {
                Debug.LogError("❌ PANEL 1 FRAME İÇİNDE KAPANDI! Başka bir script kapatıyor!");
                Debug.LogError("UIManager veya başka bir component kontrol et!");

                // Force tekrar aç
                gameObject.SetActive(true);
            }
            else
            {
                Debug.Log("✅ Panel 1 frame sonra hala açık!");
            }
        }

        private void OnBuyClicked()
        {
            Debug.Log($"[PurchasePanelUI] 🛒 Buy: {currentData?.productName}");

            if (currentData == null || iapManager == null) return;

            iapManager.ProcessPurchase(currentData);
            ClosePanel();
        }

        public void ClosePanel()
        {
            Debug.Log("[PurchasePanelUI] 🔒 Kapatılıyor");

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
            currentData = null;
        }

        [ContextMenu("Debug: Force Open")]
        private void DebugForceOpen()
        {
            PurchaseItemData dummy = ScriptableObject.CreateInstance<PurchaseItemData>();
            dummy.titleText = "TEST";
            dummy.productName = "Test Product";
            dummy.priceText = "100 💎";
            OpenPanel(dummy);
        }
    }
}