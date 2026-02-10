using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// Cow Controller - Timer Save/Resume
    /// Kaynak bitince timer durur, geri gelince kaldığı yerden devam eder
    /// </summary>
    public class CowController : MonoBehaviour
    {
        private PackageManager packageManager;
        private TroughController feedTrough;
        private TroughController waterTrough;
        [Inject] private CowManager cowManager;

        [Header("Görsel Ayarlar")]
        [SerializeField] private Image progressBar;
        [SerializeField] private GameObject timerCanvas;
        [SerializeField] private GameObject milkIndicator;
        [SerializeField] private TextMeshProUGUI milkCountText;
        [SerializeField] private GameObject needsIndicator;

        [Header("Üretim Ayarları")]
        [SerializeField] private float baseTimePerMilk = 30f;
        [SerializeField] private int maxMilkStack = 6;

        private bool isProducing = false;
        private bool isHolding = false;

        private int cowIndex = -1;
        private Cow cowData;
        private GameConfig config;
        private IAPManager iapManager;

        private int milkStack = 0;
        private Coroutine productionCoroutine;

        // ✅ YENİ: Timer save/resume için
        private float savedTimer = 0f; // Kalan süre (saniye)
        private bool isPaused = false;  // Pause durumu

        private void OnEnable()
        {
            MilkFarmEvents.OnTroughRefilled += HandleTroughRefilled;
        }

        private void OnDisable()
        {
            MilkFarmEvents.OnTroughRefilled -= HandleTroughRefilled;
        }
        private void Update()
        {
            SyncCowData();
        }
        private void SyncCowData()
        {
            if (cowData == null)
            {
                Debug.LogWarning($"[CowController {cowIndex}] ⚠️ cowData NULL!");
                return;
            }

            if (cowData.index != cowIndex)
            {
                Debug.LogError($"[CowController {cowIndex}] ❌ YANLIŞ COW!");
                return;
            }

            // Stack sync
            cowData.currentMilk = milkStack;

            // Timer sync
            if (isPaused)
            {
                cowData.productionTimer = savedTimer;
            }
            else if (isProducing && progressBar != null)
            {
                float productionTime = CalculateProductionTime();
                float elapsed = progressBar.fillAmount * productionTime;
                float remaining = productionTime - elapsed;
                cowData.productionTimer = remaining;
            }
            else
            {
                cowData.productionTimer = 0f;
            }
        }


        public void SetFeedTrough(TroughController trough) => feedTrough = trough;
        public void SetWaterTrough(TroughController trough) => waterTrough = trough;
        public void SetPackageManager(PackageManager manager) => packageManager = manager;

        public void Initialize(int index, Cow data, GameConfig gameConfig, IAPManager iap)
        {
            cowIndex = index;
            cowData = data;
            config = gameConfig;
            iapManager = iap;

            if (timerCanvas != null) timerCanvas.SetActive(false);
            if (milkIndicator != null) milkIndicator.SetActive(false);
            if (needsIndicator != null) needsIndicator.SetActive(false);

            // ✅ YENİ: Saved data'dan restore
            RestoreFromSaveData();

            UpdateMilkUI();

            Debug.Log($"[CowController {cowIndex}] Initialized - Stack: {milkStack}, Timer: {savedTimer:F1}s");
        }

        // ✅ YENİ METOD EKLE (Initialize'dan sonra):

        /// <summary>
        /// Save data'dan timer ve stack restore et
        /// </summary>
        private void RestoreFromSaveData()
        {
            if (cowData == null) return;

            // Milk stack restore
            milkStack = cowData.currentMilk;

            // Timer restore
            savedTimer = cowData.productionTimer;

            // Timer varsa ve kaynak varsa auto-start
            if (savedTimer > 0f || milkStack < maxMilkStack)
            {
                // Kaynak kontrolü
                if (feedTrough != null && waterTrough != null)
                {
                    if (feedTrough.HasResource && waterTrough.HasResource)
                    {
                        // Kısa delay sonra başlat (Start sonrası için)
                        StartCoroutine(DelayedAutoStart());
                    }
                    else
                    {
                        // Kaynak yok, pause durumunda başlat
                        isPaused = true;
                        Debug.Log($"[CowController {cowIndex}] 📂 Loaded paused (no resource) - Timer: {savedTimer:F1}s");
                    }
                }
            }

            Debug.Log($"[CowController {cowIndex}] 📂 Restored - Milk: {milkStack}, Timer: {savedTimer:F1}s, Paused: {isPaused}");
        }

        /// <summary>
        /// Kısa delay sonra production başlat
        /// </summary>
        private System.Collections.IEnumerator DelayedAutoStart()
        {
            yield return new WaitForSeconds(0.1f); // Frame delay

            if (!isProducing && !isPaused)
            {
                Debug.Log($"[CowController {cowIndex}] 🔄 Auto-starting production (from save)");
                TryStartProduction();
            }
        }

        private void HandleTroughRefilled(int stationIndex)
        {
            int myStation = cowIndex / (config != null ? config.cowsPerStation : 3);
            if (stationIndex != myStation) return;

            Debug.Log($"[CowController {cowIndex}] Trough refill algılandı!");

            // ✅ Pause durumundaysa RESUME
            if (isPaused)
            {
                if (feedTrough != null && waterTrough != null)
                {
                    if (feedTrough.HasResource && waterTrough.HasResource)
                    {
                        Debug.Log($"[CowController {cowIndex}] ✅ RESUME! Kalan timer: {savedTimer:F1}s");
                        ResumeProduction();
                    }
                }
            }
            // Üretim hiç başlamamışsa start
            else if (!isProducing && milkStack < maxMilkStack)
            {
                if (feedTrough != null && waterTrough != null)
                {
                    if (feedTrough.HasResource && waterTrough.HasResource)
                    {
                        TryStartProduction();
                    }
                }
            }
        }

        void OnMouseDown()
        {
            isHolding = true;

            if (IsPackageManagerFull()) return;

            if (milkStack > 0)
            {
                CollectMilk();
            }
            else if (!isProducing && !isPaused)
            {
                TryStartProduction();
            }
        }

        public void OnClicked()
        {
            if (IsPackageManagerFull()) return;

            if (milkStack > 0)
            {
                CollectMilk();
            }
            else if (!isProducing && !isPaused)
            {
                TryStartProduction();
            }
        }

        private bool IsPackageManagerFull()
        {
            if (packageManager == null) return false;
            int totalCases = packageManager.GetProductionStackCount();
            if (packageManager.HasActiveCrate()) totalCases++;
            return totalCases >= 8;
        }

        void OnMouseUp() { isHolding = false; }
        void OnMouseExit() { isHolding = false; }

        void TryStartProduction()
        {
            if (feedTrough == null || waterTrough == null)
            {
                ShowNeedsIndicator(true);
                return;
            }

            if (feedTrough.HasResource && waterTrough.HasResource)
            {
                productionCoroutine = StartCoroutine(ProductionRoutine());
            }
            else
            {
                ShowNeedsIndicator(true);
            }
        }

        /// <summary>
        /// Resume production (pause'dan devam)
        /// </summary>
        void ResumeProduction()
        {
            if (!isPaused) return;

            isPaused = false;
            ShowNeedsIndicator(false);

            // Coroutine restart (kalan timer ile)
            productionCoroutine = StartCoroutine(ProductionRoutine());
        }
        IEnumerator ProductionRoutine()
        {
            isProducing = true;
            isPaused = false;

            if (timerCanvas != null) timerCanvas.SetActive(true);
            ShowNeedsIndicator(false);

            if (feedTrough != null) feedTrough.OnCowStartProducing();
            if (waterTrough != null) waterTrough.OnCowStartProducing();

            while (true)
            {
                if (milkStack >= maxMilkStack)
                {
                    Debug.Log($"[CowController {cowIndex}] Stack dolu");
                    break;
                }

                if (IsPackageManagerFull())
                {
                    Debug.Log($"[CowController {cowIndex}] Paketleme dolu");
                    break;
                }

                float productionTime = CalculateProductionTime();

                // ✅ DÜZELTME: savedTimer = KALAN süre, timer = GEÇEN süre
                float timer;

                if (savedTimer > 0f)
                {
                    // savedTimer KALAN süre (örn: 4.1s)
                    // timer'ı GEÇEN süreye çevir (örn: 30 - 4.1 = 25.9s)
                    timer = productionTime - savedTimer;
                    savedTimer = 0f; // Reset

                    Debug.Log($"[CowController {cowIndex}] Resumed - Elapsed: {timer:F1}s / {productionTime:F1}s");
                }
                else
                {
                    // Yeni başlangıç
                    timer = 0f;
                    Debug.Log($"[CowController {cowIndex}] Started fresh - 0s / {productionTime:F1}s");
                }

                // ✅ ProgressBar başlangıç değeri
                if (progressBar != null)
                {
                    progressBar.fillAmount = timer / productionTime;

                    if (timer > 0f)
                    {
                        Debug.Log($"[CowController {cowIndex}] ProgressBar restored: {progressBar.fillAmount * 100:F0}%");
                    }
                }

                while (timer < productionTime)
                {
                    // Kaynak kontrolü
                    if (feedTrough != null && waterTrough != null)
                    {
                        if (!feedTrough.HasResource || !waterTrough.HasResource)
                        {
                            // ✅ PAUSE: KALAN süreyi kaydet
                            float remaining = productionTime - timer;
                            savedTimer = remaining;
                            isPaused = true;

                            ShowNeedsIndicator(true);

                            if (feedTrough != null) feedTrough.OnCowStopProducing();
                            if (waterTrough != null) waterTrough.OnCowStopProducing();

                            isProducing = false;
                            if (timerCanvas != null) timerCanvas.SetActive(false);

                            Debug.Log($"[CowController {cowIndex}] ⚠️ PAUSE! Elapsed: {timer:F1}s, Remaining: {remaining:F1}s");
                            yield break;
                        }
                    }

                    if (IsPackageManagerFull())
                    {
                        break;
                    }

                    float speedMultiplier = isHolding && config != null
                        ? config.tapHoldSpeedMultiplier
                        : (isHolding ? 0.75f : 1.0f);

                    // ✅ timer artıyor (GEÇEN süre: 0 → 30)
                    timer += Time.deltaTime * speedMultiplier;

                    // ✅ ProgressBar güncelle (GEÇEN süreye göre)
                    if (progressBar != null)
                        progressBar.fillAmount = timer / productionTime;

                    yield return null;
                }

                if (IsPackageManagerFull())
                {
                    break;
                }

                ProduceMilk();
            }

            if (feedTrough != null) feedTrough.OnCowStopProducing();
            if (waterTrough != null) waterTrough.OnCowStopProducing();

            isProducing = false;
            isPaused = false;
            savedTimer = 0f;

            if (timerCanvas != null) timerCanvas.SetActive(false);
        }
        private float CalculateProductionTime()
        {
            if (config == null) return 30f;

            // ✅ Level-based production time
            float baseTime = 30f; // Default

            if (cowManager != null && cowData != null)
            {
                baseTime = cowManager.GetProductionTime(cowData.level);
            }
            else if (config != null)
            {
                baseTime = config.baseProductionTime;
            }

            // IAP speed boost
            if (iapManager != null)
            {
                float multiplier = iapManager.GetSpeedMultiplier();
                baseTime /= multiplier;
            }

            return baseTime;
        }
        private void ProduceMilk()
        {
            milkStack++;
            UpdateMilkUI();

            if (cowData != null)
            {
                cowData.currentMilk = milkStack;

                MilkFarmEvents.CowMilkProduced(cowIndex);
            }

            Debug.Log($"[CowController {cowIndex}] Süt üretti! Stack: {milkStack}");
        }

        public void CollectMilk()
        {
            if (milkStack <= 0) return;
            if (packageManager == null) return;

            StartCoroutine(CollectMilkRoutine());
        }

        private IEnumerator CollectMilkRoutine()
        {
            int totalMilk = milkStack;

            milkStack = 0;
            if (cowData != null)
            {
                cowData.currentMilk = 0;
            }
            UpdateMilkUI();

            for (int i = 0; i < totalMilk; i++)
            {
                packageManager.AddMilk(transform.position);
                yield return new WaitForSeconds(0.1f);
            }

            MilkFarmEvents.CowMilkCollected(cowIndex, totalMilk);

            // Resume üretim
            if (!isProducing && !isPaused && feedTrough != null && waterTrough != null)
            {
                if (feedTrough.HasResource && waterTrough.HasResource)
                {
                    TryStartProduction();
                }
            }
        }

        private void UpdateMilkUI()
        {
            if (milkIndicator != null)
            {
                milkIndicator.SetActive(milkStack > 0);
            }

            if (milkCountText != null)
            {
                milkCountText.text = milkStack.ToString();
            }
        }

        private void ShowNeedsIndicator(bool show)
        {
            if (needsIndicator != null)
            {
                needsIndicator.SetActive(show);
            }
        }

        public bool IsProducing => isProducing;
        public int GetMilkStack() => milkStack;
        public float GetProductionProgress()
        {
            if (progressBar != null)
                return progressBar.fillAmount;
            return 0f;
        }
        private void OnApplicationQuit()
        {
            // Quit öncesi son bir kez sync
            SyncCowData();

            Debug.Log($"[CowController {cowIndex}] 💾 Quit sync - Timer: {cowData?.productionTimer:F1}s, Stack: {milkStack}");
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                // Pause öncesi son bir kez sync
                SyncCowData();

                Debug.Log($"[CowController {cowIndex}] 💾 Pause sync - Timer: {cowData?.productionTimer:F1}s, Stack: {milkStack}");
            }
        }
        /// <summary>
        /// Called when cow level changes (upgrade)
        /// </summary>
        public void OnLevelChanged(int newLevel)
        {
            if (cowData == null) return;
            cowData.level = newLevel;

            // Update sprite
            if (cowManager != null)
            {
                Sprite newSprite = cowManager.GetCowSprite(newLevel);
                if (newSprite != null)
                {
                    var renderer = GetComponentInChildren<SpriteRenderer>();
                    if (renderer != null)
                    {
                        renderer.sprite = newSprite;
                    }
                }
            }

            Debug.Log($"[CowController {cowIndex}] Level changed to {newLevel}");
        }

    }
}