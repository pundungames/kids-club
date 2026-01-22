using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// Lock Button - Double Click Fix
    /// </summary>
    public class LockButton : MonoBehaviour
    {
        [Header("Purchase Info")]
        [SerializeField] private PurchaseType unlockType;
        [SerializeField] private int targetIndex;

        [Header("References")]
        [SerializeField] private Button button;

        [Inject] private UIManager uiManager;

        private bool isProcessing = false; // Çift tıklama önleme

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button != null)
            {
                // Listener'ları temizle
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnLockClicked);
            }
            else
            {
                Debug.LogError("[LockButton] Button component bulunamadı!");
            }
        }

        private void OnLockClicked()
        {
            // Çift tıklama önleme
            if (isProcessing)
            {
                Debug.LogWarning("[LockButton] ⚠️ Already processing!");
                return;
            }

            isProcessing = true;

            Debug.Log($"[LockButton] 🖱️ CLICKED! Type: {unlockType}, Index: {targetIndex}");

            if (uiManager == null)
            {
                Debug.LogError("[LockButton] UIManager null!");
                isProcessing = false;
                return;
            }

            // Panel aç
            switch (unlockType)
            {
                case PurchaseType.UnlockCow:
                    Debug.Log($"[LockButton] Opening Cow panel for index {targetIndex}");
                    uiManager.OpenCowPurchasePanel(targetIndex);
                    break;

                case PurchaseType.UnlockArea:
                    Debug.Log($"[LockButton] Opening Area panel for index {targetIndex}");
                    uiManager.OpenAreaPurchasePanel(targetIndex);
                    break;

                case PurchaseType.UnlockTrough:
                    Debug.Log($"[LockButton] Opening Trough panel for index {targetIndex}");
                    uiManager.OpenTroughPurchasePanel(targetIndex);
                    break;

                default:
                    Debug.LogWarning($"[LockButton] Unknown unlock type: {unlockType}");
                    break;
            }

            // İşlem bitti
            isProcessing = false;
        }

        [ContextMenu("Test Click")]
        private void TestClick()
        {
            OnLockClicked();
        }
    }
}