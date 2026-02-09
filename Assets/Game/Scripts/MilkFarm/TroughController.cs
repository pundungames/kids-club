using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// Trough Controller - Minimal Patch
    /// Senin koduna sadece event fire eklendi
    /// </summary>
    public class TroughController : MonoBehaviour
    {
        [Header("Görsel Ayarlar")]
        [SerializeField] private Transform fillMesh;
        [SerializeField] private float loweredYOffset = -0.5f;

        [Header("UI")]
        [SerializeField] private GameObject emptyIcon;
        [SerializeField] private Image fillBar;
        [SerializeField] private TextMeshProUGUI fillText;

        [Header("Tür")]
        [SerializeField] private bool isFeedTrough = true;

        [Header("Durum")]
        [Range(0f, 1f)]
        [SerializeField] private float currentFill = 1f;

        private Vector3 initialLocalPos;
        private Vector3 initialScale;
        private Vector3 loweredPos;

        private GameConfig config;
        private IAPManager iapManager;
        private int stationIndex = -1;
        private int activeCowCount = 0;
        private int producingCowCount = 0;

        public bool HasResource => currentFill > 0f;

        public void Initialize(int station, bool isFeed, GameConfig gameConfig, IAPManager iap)
        {
            stationIndex = station;
            isFeedTrough = isFeed;
            config = gameConfig;
            iapManager = iap;

            if (fillMesh != null)
            {
                initialLocalPos = fillMesh.localPosition;
                initialScale = fillMesh.localScale;
                loweredPos = initialLocalPos + new Vector3(0, loweredYOffset, 0);
                UpdateVisuals();
            }
        }

        void Start()
        {
            if (fillMesh != null)
            {
                initialLocalPos = fillMesh.localPosition;
                initialScale = fillMesh.localScale;
                loweredPos = initialLocalPos + new Vector3(0, loweredYOffset, 0);
                UpdateVisuals();
            }

            UpdateUI();
        }
        private void OnEnable()
        {
            MilkFarmEvents.OnSaveRequested += HandleSaveRequested;
        }

        private void OnDisable()
        {
            MilkFarmEvents.OnSaveRequested -= HandleSaveRequested;
        }

        void Update()
        {
            // Auto Feeder IAP
            if (iapManager != null && iapManager.HasAutoFeeder())
            {
                if (currentFill < 1f)
                {
                    currentFill = 1f;
                    UpdateVisuals();
                }
                return;
            }

            // Üretim yapan inek yoksa tüketim yok
            if (producingCowCount <= 0 || currentFill <= 0f)
            {
                return;
            }

            // Üretim bazlı tüketim
            float drainInterval = isFeedTrough ? config.feedingInterval : config.wateringInterval;
            float consumptionPerSecond = (1f / drainInterval) * producingCowCount;

            currentFill -= consumptionPerSecond * Time.deltaTime;
            currentFill = Mathf.Max(0f, currentFill);

            UpdateVisuals();

            if (currentFill <= 0f)
            {
                OnDepleted();
            }
        }

        private bool wasDepleted = false;

        private void OnDepleted()
        {
            if (wasDepleted) return;

            wasDepleted = true;

            if (isFeedTrough)
                MilkFarmEvents.StationFoodDepleted(stationIndex);
            else
                MilkFarmEvents.StationWaterDepleted(stationIndex);

            Debug.Log($"[TroughController] {name} boşaldı! (Üreten inek: {producingCowCount})");
        }

        void OnMouseDown()
        {
            FillTrough();
        }

        public void FillTrough()
        {
            if (currentFill >= 1f) return;

            // ✅ Önceki doluluk
            float previousFill = currentFill;

            float fillAmount = config != null
                ? (isFeedTrough ? config.feedingTapFill : config.wateringTapFill)
                : 0.25f;

            currentFill = Mathf.Min(1f, currentFill + fillAmount);
            wasDepleted = false;
            UpdateVisuals();

            if (isFeedTrough)
                MilkFarmEvents.StationFoodRefilled(stationIndex);
            else
                MilkFarmEvents.StationWaterRefilled(stationIndex);

            Debug.Log($"[TroughController] {name} dolduruldu: {currentFill * 100:F0}%");

            // ✅ YENİ: Event fire (yoktan var oldu mu?)
            if (previousFill <= 0f && currentFill > 0f)
            {
                MilkFarmEvents.TroughRefilled(stationIndex);
                Debug.Log($"[TroughController] 🔔 TroughRefilled event! Station {stationIndex}");
            }
        }

        public void SetActiveCowCount(int count)
        {
            activeCowCount = count;
            Debug.Log($"[TroughController] {name} - Aktif inek: {activeCowCount}");
        }

        public void SetProducingCowCount(int count)
        {
            producingCowCount = count;
            Debug.Log($"[TroughController] {name} - Üreten inek: {producingCowCount}");
        }

        public void OnCowStartProducing()
        {
            producingCowCount++;
            Debug.Log($"[TroughController] {name} - Üreten inek: {producingCowCount}");
        }

        public void OnCowStopProducing()
        {
            producingCowCount--;
            if (producingCowCount < 0) producingCowCount = 0;
            Debug.Log($"[TroughController] {name} - Üreten inek: {producingCowCount}");
        }

        void UpdateVisuals()
        {
            if (fillMesh != null)
            {
                fillMesh.localPosition = Vector3.Lerp(loweredPos, initialLocalPos, currentFill);
            }

            UpdateUI();
        }

        private void UpdateUI()
        {
            if (fillBar != null)
            {
                fillBar.fillAmount = currentFill;
            }

            if (fillText != null)
            {
                fillText.text = $"%{(currentFill * 100):F0}";
            }

            if (emptyIcon != null)
            {
                emptyIcon.SetActive(currentFill <= 0f);
            }
        }

        // === PUBLIC API ===

        public float GetFillAmount() => currentFill;

        public void SetFillAmount(float amount)
        {
            currentFill = Mathf.Clamp01(amount);
            wasDepleted = false;
            UpdateVisuals();
        }

        public int GetActiveCowCount() => activeCowCount;
        public int GetProducingCowCount() => producingCowCount;

        [ContextMenu("Debug: Fill Instantly")]
        public void DebugFillInstantly()
        {
            currentFill = 1f;
            wasDepleted = false;
            UpdateVisuals();
        }

        [ContextMenu("Debug: Empty Instantly")]
        public void DebugEmptyInstantly()
        {
            currentFill = 0f;
            UpdateVisuals();
            OnDepleted();
        }

        [ContextMenu("Debug: Set 50%")]
        public void DebugSet50Percent()
        {
            currentFill = 0.5f;
            wasDepleted = false;
            UpdateVisuals();
        }
        /// <summary>
        /// Save event handler
        /// </summary>
        private void HandleSaveRequested()
        {
            // StationManager Save metodunu çağıracak
            Debug.Log($"[TroughController {stationIndex}] 💾 Save requested - Fill: {currentFill * 100:F0}%");
        }

        /// <summary>
        /// Save to TroughSaveData
        /// </summary>
        public void SaveToData(TroughSaveData data)
        {
            data.currentAmount = currentFill * 100f; // 0.5 → 50
            data.maxCapacity = 100f;
        }

        /// <summary>
        /// Load from TroughSaveData
        /// </summary>
        public void LoadFromSaveData(TroughSaveData data)
        {
            currentFill = data.currentAmount / 100f; // 50 → 0.5
            currentFill = Mathf.Clamp01(currentFill);
            wasDepleted = (currentFill <= 0f);
            UpdateVisuals();

            Debug.Log($"[TroughController {stationIndex}] 📂 Loaded - Fill: {currentFill * 100:F0}%");
        }
    }
}