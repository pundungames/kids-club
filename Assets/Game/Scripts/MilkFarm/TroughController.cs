using UnityEngine;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// Yemlik/Suluk controller - İNEK BAZLI TÜKETİM
    /// İnek sayısına göre azalır
    /// </summary>
    public class TroughController : MonoBehaviour
    {
        [Header("Görsel Ayarlar")]
        [SerializeField] private Transform fillMesh;
        [SerializeField] private float loweredYOffset = -0.5f;

        [Header("UI İkon")]
        [SerializeField] private GameObject emptyIcon;

        [Header("Tür")]
        [SerializeField] private bool isFeedTrough = true;

        [Header("Durum")]
        [Range(0f, 1f)]
        [SerializeField] private float currentFill = 1f;

        private Vector3 initialLocalPos;
        private Vector3 initialScale;
        private Vector3 loweredPos;

        // GDD v2
        private GameConfig config;
        private IAPManager iapManager;
        private int stationIndex = -1;
        private int activeCowCount = 0; // Bu istasyondaki aktif inek sayısı

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

            if (emptyIcon != null)
            {
                emptyIcon.SetActive(currentFill <= 0f);
            }
        }

        void Update()
        {
            // Auto Feeder IAP varsa otomatik doldur
            if (iapManager != null && iapManager.HasAutoFeeder())
            {
                if (currentFill < 1f)
                {
                    currentFill = 1f;
                    UpdateVisuals();
                }
                return;
            }

            // İnek yoksa tüketim yok!
            if (activeCowCount <= 0 || currentFill <= 0f)
            {
                return;
            }

            // İnek bazlı tüketim hızı
            // Her inek için drain rate hesapla
            float drainInterval = isFeedTrough ? config.feedingInterval : config.wateringInterval;
            float consumptionPerSecond = (1f / drainInterval) * activeCowCount; // İnek sayısı kadar hızlı

            currentFill -= consumptionPerSecond * Time.deltaTime;
            currentFill = Mathf.Max(0f, currentFill);

            UpdateVisuals();

            // Tam boşaldığında event fırlat (bir kere)
            if (currentFill <= 0f)
            {
                OnDepleted();
            }
        }

        private bool wasDepleted = false;

        private void OnDepleted()
        {
            if (wasDepleted) return; // Zaten event fırlatıldı

            wasDepleted = true;

            if (isFeedTrough)
                MilkFarmEvents.StationFoodDepleted(stationIndex);
            else
                MilkFarmEvents.StationWaterDepleted(stationIndex);

            Debug.Log($"[TroughController] {name} boşaldı! (İnek sayısı: {activeCowCount})");
        }

        void OnMouseDown()
        {
            FillTrough();
        }

        public void FillTrough()
        {
            if (currentFill >= 1f) return;

            float fillAmount = config != null
                ? (isFeedTrough ? config.feedingTapFill : config.wateringTapFill)
                : 0.25f;

            currentFill = Mathf.Min(1f, currentFill + fillAmount);
            wasDepleted = false; // Reset flag
            UpdateVisuals();

            if (isFeedTrough)
                MilkFarmEvents.StationFoodRefilled(stationIndex);
            else
                MilkFarmEvents.StationWaterRefilled(stationIndex);

            Debug.Log($"[TroughController] {name} dolduruldu: {currentFill * 100:F0}%");
        }

        /// <summary>
        /// İnek sayısını set et (CowManager'dan çağrılır)
        /// </summary>
        public void SetActiveCowCount(int count)
        {
            activeCowCount = count;
            Debug.Log($"[TroughController] {name} - Aktif inek sayısı: {activeCowCount}");
        }

        void UpdateVisuals()
        {
            if (fillMesh != null)
            {
                fillMesh.localPosition = Vector3.Lerp(loweredPos, initialLocalPos, currentFill);

                fillMesh.localScale = Vector3.Lerp(
                    new Vector3(initialScale.x, 0f, initialScale.z),
                    initialScale,
                    currentFill
                );
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
    }
}