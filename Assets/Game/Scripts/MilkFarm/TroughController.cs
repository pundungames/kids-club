using UnityEngine;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// Yemlik/Suluk controller'ı
    /// Mevcut visual sistem korundu, GDD v2 parametreleri entegre edildi
    /// </summary>
    public class TroughController : MonoBehaviour
    {
        [Header("Görsel Ayarlar")]
        [SerializeField] private Transform fillMesh; // Yükselip alçalacak olan Yem/Su objesi
        [SerializeField] private float loweredYOffset = -0.5f; // Boşken ne kadar aşağı insin?

        [Header("Tür")]
        [SerializeField] private bool isFeedTrough = true; // true=Yemlik, false=Suluk

        [Header("Durum")]
        [Range(0f, 1f)]
        [SerializeField] private float currentFill = 1f; // 0=Boş, 1=Dolu (GDD: 0-1 arası)

        // Tüketim
        private float drainTimer = 0f;
        private float drainInterval = 30f; // GDD'den gelecek

        // Başlangıç pozisyonlarını saklayalım
        private Vector3 initialLocalPos;
        private Vector3 initialScale;

        // GDD v2 entegrasyonu
        private GameConfig config;
        private IAPManager iapManager;
        private int stationIndex = -1;

        // İnek kontrolü için: İçinde bir şey var mı?
        public bool HasResource => currentFill > 0f;

        /// <summary>
        /// StationManager tarafından initialize edilir
        /// </summary>
        public void Initialize(int station, bool isFeed, GameConfig gameConfig, IAPManager iap)
        {
            stationIndex = station;
            isFeedTrough = isFeed;
            config = gameConfig;
            iapManager = iap;

            // GDD'den interval al
            drainInterval = isFeed ? config.feedingInterval : config.wateringInterval;

            if (fillMesh != null)
            {
                initialLocalPos = fillMesh.localPosition;
                initialScale = fillMesh.localScale;
                UpdateVisuals();
            }
        }

        void Start()
        {
            // Fallback config yoksa
            if (config == null)
            {
                drainInterval = 30f;
            }

            if (fillMesh != null)
            {
                initialLocalPos = fillMesh.localPosition;
                initialScale = fillMesh.localScale;
                UpdateVisuals();
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
                    drainTimer = 0f;
                    UpdateVisuals();
                }
                return; // Auto feeder varsa tüketim yok
            }

            // Eğer içinde bir şey varsa zamanla azaldır
            if (currentFill > 0f)
            {
                drainTimer += Time.deltaTime;

                // Süre dolunca boşal
                if (drainTimer >= drainInterval)
                {
                    currentFill = 0f;
                    drainTimer = 0f;
                    UpdateVisuals();

                    // Event fırlat
                    if (isFeedTrough)
                        MilkFarmEvents.StationFoodDepleted(stationIndex);
                    else
                        MilkFarmEvents.StationWaterDepleted(stationIndex);

                    Debug.Log($"[TroughController] {name} boşaldı!");
                }
            }
        }

        // Tıklayınca Doldur (Clicker Mantığı)
        void OnMouseDown()
        {
            FillTrough();
        }

        /// <summary>
        /// Yemlik/suluğu doldur
        /// GDD: Her tap %25 doldurur (0.25)
        /// </summary>
        public void FillTrough()
        {
            if (currentFill >= 1f) return; // Zaten dolu

            float fillAmount = config != null 
                ? (isFeedTrough ? config.feedingTapFill : config.wateringTapFill)
                : 0.25f; // Fallback

            currentFill = Mathf.Min(1f, currentFill + fillAmount);
            drainTimer = 0f; // Timer'ı sıfırla ki hemen düşmesin
            UpdateVisuals();

            // Event fırlat
            if (isFeedTrough)
                MilkFarmEvents.StationFoodRefilled(stationIndex);
            else
                MilkFarmEvents.StationWaterRefilled(stationIndex);

            Debug.Log($"[TroughController] {name} dolduruldu: {currentFill * 100}%");
        }

        // === GÖRSEL ANİMASYON (SCALE + POZİSYON) ===
        void UpdateVisuals()
        {
            if (fillMesh == null) return;

            // 1. SCALE AYARI: 0'dan Orijinal boyutuna
            fillMesh.localScale = Vector3.Lerp(Vector3.zero, initialScale, currentFill);

            // 2. POZİSYON AYARI: Aşağıdan Orijinal yerine
            Vector3 emptyPos = initialLocalPos + new Vector3(0, loweredYOffset, 0);
            fillMesh.localPosition = Vector3.Lerp(emptyPos, initialLocalPos, currentFill);
        }

        // === PUBLIC API ===

        public float GetFillAmount() => currentFill;

        public void SetFillAmount(float amount)
        {
            currentFill = Mathf.Clamp01(amount);
            UpdateVisuals();
        }

        /// <summary>
        /// Offline progress için timer ayarla
        /// </summary>
        public void SetDrainTimer(float timer)
        {
            drainTimer = timer;
        }

        public float GetDrainTimer() => drainTimer;

        /// <summary>
        /// Debug: Anında doldur
        /// </summary>
        [ContextMenu("Debug: Fill Instantly")]
        public void DebugFillInstantly()
        {
            currentFill = 1f;
            drainTimer = 0f;
            UpdateVisuals();
            Debug.Log($"[TroughController] {name} anında dolduruldu!");
        }

        /// <summary>
        /// Debug: Anında boşalt
        /// </summary>
        [ContextMenu("Debug: Empty Instantly")]
        public void DebugEmptyInstantly()
        {
            currentFill = 0f;
            drainTimer = 0f;
            UpdateVisuals();
            Debug.Log($"[TroughController] {name} anında boşaltıldı!");
        }
    }
}
