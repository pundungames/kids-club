using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// Tek bir inek objesinin controller'ı
    /// Mevcut CowController'ı geliştirerek GDD v2'ye uyumlu hale getirildi
    /// Visual feedback ve coroutine yapısı korundu
    /// </summary>
    public class CowController : MonoBehaviour
    {
        [Header("Bağlantılar")]
        [SerializeField] private CounterManager counterManager; // Eski sistem için (backward compat)
        [SerializeField] private PackageManager packageManager; // Yeni sistem için
        
        [Header("Yalaklar")]
        [SerializeField] private TroughController feedTrough;  // Yemlik
        [SerializeField] private TroughController waterTrough; // Suluk

        [Header("Görsel Ayarlar")]
        [SerializeField] private Image progressBar;
        [SerializeField] private GameObject timerCanvas;
        [SerializeField] private GameObject milkIndicator; // Süt ikonu
        [SerializeField] private GameObject needsIndicator; // Yem/su eksik ikonu

        [Header("Üretim Ayarları")]
        [SerializeField] private int productionBatchSize = 3; // Kaç süt üretilecek
        [SerializeField] private float baseTimePerMilk = 30f;

        // Tap & Hold
        private bool isProducing = false;
        private bool isHolding = false;

        // Yeni sistem entegrasyonu
        private int cowIndex = -1;
        private Cow cowData; // CowManager'dan inject edilecek
        private GameConfig config;
        private IAPManager iapManager;

        /// <summary>
        /// CowManager tarafından setup edilir
        /// </summary>
        public void Initialize(int index, Cow data, GameConfig gameConfig, IAPManager iap)
        {
            cowIndex = index;
            cowData = data;
            config = gameConfig;
            iapManager = iap;

            // Visual başlangıç durumu
            if (timerCanvas != null) timerCanvas.SetActive(false);
            if (milkIndicator != null) milkIndicator.SetActive(false);
            if (needsIndicator != null) needsIndicator.SetActive(false);
        }

        // === INPUT SİSTEMİ ===

        void OnMouseDown()
        {
            isHolding = true;

            // Eğer şu an üretim yoksa, yeni üretimi dene
            if (!isProducing)
            {
                TryStartProduction();
            }
        }

        // Eski InputManager için (backward compat)
        public void OnClicked()
        {
            if (!isProducing)
            {
                TryStartProduction();
            }
        }

        void OnMouseUp() { isHolding = false; }
        void OnMouseExit() { isHolding = false; }

        // === ÜRETİM KONTROLLERİ ===

        void TryStartProduction()
        {
            // 1. Yalaklar tanımlı mı?
            if (feedTrough == null || waterTrough == null)
            {
                Debug.LogError($"[CowController {cowIndex}] Yemlik veya Suluk bağlanmamış!");
                return;
            }

            // 2. İkisi de dolu mu?
            if (feedTrough.HasResource && waterTrough.HasResource)
            {
                // HER ŞEY TAMAM, BAŞLA!
                StartCoroutine(ProductionRoutine());
            }
            else
            {
                // BİRİSİ BOŞ!
                Debug.Log($"[CowController {cowIndex}] İnek Aç veya Susuz! Üretim yapılamıyor.");
                ShowNeedsIndicator(true);
            }
        }

        IEnumerator ProductionRoutine()
        {
            isProducing = true;
            if (timerCanvas != null) timerCanvas.SetActive(true);
            ShowNeedsIndicator(false);

            for (int i = 0; i < productionBatchSize; i++)
            {
                // Her süt için üretim süresi hesapla
                float productionTime = CalculateProductionTime();
                float timer = 0f;

                while (timer < productionTime)
                {
                    // Yem/su kontrolü (üretim sırasında biterse dur)
                    if (!feedTrough.HasResource || !waterTrough.HasResource)
                    {
                        ShowNeedsIndicator(true);
                        isProducing = false;
                        if (timerCanvas != null) timerCanvas.SetActive(false);
                        yield break;
                    }

                    // Tap & Hold hızlandırma
                    float speedMultiplier = isHolding ? config.tapHoldSpeedMultiplier : 1.0f;
                    timer += Time.deltaTime * speedMultiplier;

                    // Progress bar güncelle
                    if (progressBar != null) 
                        progressBar.fillAmount = timer / productionTime;

                    yield return null;
                }

                // 1 süt üretildi!
                ProduceMilk();
            }

            // Döngü bitti
            isProducing = false;
            if (timerCanvas != null) timerCanvas.SetActive(false);
        }

        /// <summary>
        /// Üretim süresini hesapla (level ve IAP'e göre)
        /// </summary>
        private float CalculateProductionTime()
        {
            if (config == null || cowData == null)
                return baseTimePerMilk; // Fallback

            // GDD Formül: base * (0.9 ^ (level - 1))
            float baseTime = config.baseMilkProductionTime;
            float levelMultiplier = Mathf.Pow(0.9f, cowData.level - 1);
            float leveledTime = baseTime * levelMultiplier;

            // Minimum limit
            leveledTime = Mathf.Max(config.minProductionTime, leveledTime);

            // IAP global speed boost
            if (iapManager != null)
            {
                leveledTime *= iapManager.GetGlobalSpeedMultiplier();
            }

            return leveledTime;
        }

        /// <summary>
        /// Süt üret
        /// </summary>
        private void ProduceMilk()
        {
            // Eski sistem için backward compat
            if (counterManager != null)
            {
                counterManager.AddMilk(transform.position);
            }

            // Yeni sistem
            if (packageManager != null)
            {
                packageManager.AddMilk(1);
            }

            // CowData güncelle
            if (cowData != null)
            {
                cowData.currentMilk++;
                
                // Storage limit kontrolü
                int storageLimit = GetStorageLimit();
                if (cowData.currentMilk >= storageLimit)
                {
                    // Envanter dolu, süt ikonunu göster
                    ShowMilkIndicator(true);
                }

                MilkFarmEvents.CowMilkProduced(cowIndex);
            }
        }

        /// <summary>
        /// Storage limiti hesapla
        /// </summary>
        private int GetStorageLimit()
        {
            if (config == null) return 3; // Fallback

            int baseLimit = config.baseMilkStorageLimit;
            if (iapManager != null)
            {
                baseLimit += iapManager.GetMilkStorageBoost();
            }
            return baseLimit;
        }

        // === VİSUAL FEEDBACK ===

        private void ShowMilkIndicator(bool show)
        {
            if (milkIndicator != null)
            {
                milkIndicator.SetActive(show);
            }
        }

        private void ShowNeedsIndicator(bool show)
        {
            if (needsIndicator != null)
            {
                needsIndicator.SetActive(show);
            }
        }

        /// <summary>
        /// Süt toplama (dışarıdan çağrılabilir)
        /// </summary>
        public void CollectMilk()
        {
            if (cowData != null && cowData.currentMilk > 0)
            {
                int collected = cowData.currentMilk;
                cowData.currentMilk = 0;
                
                if (packageManager != null)
                {
                    packageManager.AddMilk(collected);
                }

                ShowMilkIndicator(false);
                MilkFarmEvents.CowMilkCollected(cowIndex, collected);
                
                Debug.Log($"[CowController {cowIndex}] {collected} süt toplandı!");
            }
        }

        // === PUBLIC API ===

        public bool IsProducing => isProducing;
        public int GetCurrentMilk() => cowData?.currentMilk ?? 0;
        public float GetProductionProgress()
        {
            if (progressBar != null)
                return progressBar.fillAmount;
            return 0f;
        }
    }
}
