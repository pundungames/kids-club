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
            MilkFarmEvents.OnSaveRequested += HandleSaveRequested; // ✅ YENİ
        }

        private void OnDisable()
        {
            MilkFarmEvents.OnTroughRefilled -= HandleTroughRefilled;
            MilkFarmEvents.OnSaveRequested -= HandleSaveRequested; // ✅ YENİ
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

        // ✅ CowController.cs - ProductionRoutine'i DEĞİŞTİR:

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

                // ✅ Saved timer varsa ondan başla, yoksa sıfırdan
                float timer = savedTimer > 0 ? savedTimer : 0f;
                savedTimer = 0f; // Reset

                // ✅ YENİ: ProgressBar'ı başlangıç değerine set et
                if (progressBar != null && timer > 0f)
                {
                    progressBar.fillAmount = timer / productionTime;
                    Debug.Log($"[CowController {cowIndex}] ProgressBar restored: {progressBar.fillAmount * 100:F0}%");
                }

                Debug.Log($"[CowController {cowIndex}] Timer başlatıldı: {timer:F1}s / {productionTime:F1}s");

                while (timer < productionTime)
                {
                    // Kaynak kontrolü
                    if (feedTrough != null && waterTrough != null)
                    {
                        if (!feedTrough.HasResource || !waterTrough.HasResource)
                        {
                            // ✅ PAUSE: Timer'ı kaydet
                            savedTimer = timer;
                            isPaused = true;

                            ShowNeedsIndicator(true);

                            if (feedTrough != null) feedTrough.OnCowStopProducing();
                            if (waterTrough != null) waterTrough.OnCowStopProducing();

                            isProducing = false;
                            if (timerCanvas != null) timerCanvas.SetActive(false);

                            Debug.Log($"[CowController {cowIndex}] ⚠️ PAUSE! Timer: {savedTimer:F1}s / {productionTime:F1}s");
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

                    timer += Time.deltaTime * speedMultiplier;

                    // ✅ ProgressBar güncelle
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
            if (config == null || cowData == null)
                return baseTimePerMilk;

            float baseTime = config.baseProductionTime;
            float levelMultiplier = Mathf.Pow(0.9f, cowData.level - 1);
            float leveledTime = baseTime * levelMultiplier;

            leveledTime = Mathf.Max(config.minProductionTime, leveledTime);

            if (iapManager != null)
            {
                leveledTime *= iapManager.GetGlobalSpeedMultiplier();
            }

            return leveledTime;
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

        // === DEBUG ===

        [ContextMenu("Debug: Print Status")]
        private void DebugPrintStatus()
        {
            Debug.Log($"[Cow {cowIndex}] Producing: {isProducing}, Paused: {isPaused}, Timer: {savedTimer:F1}s, Stack: {milkStack}");
        }
        /// <summary>
        /// Save event handler - Timer ve stack'i cowData'ya kaydet
        /// </summary>
        private void HandleSaveRequested()
        {
            if (cowData == null) return;

            // Timer save (pause durumundaysa savedTimer, değilse hesapla)
            if (isPaused)
            {
                cowData.productionTimer = savedTimer;
            }
            else if (isProducing && progressBar != null)
            {
                // Şu anki timer'ı hesapla (progress'ten)
                float productionTime = CalculateProductionTime();
                float currentProgress = progressBar.fillAmount; // 0-1
                float elapsed = currentProgress * productionTime;
                float remaining = productionTime - elapsed;
                cowData.productionTimer = remaining;
            }
            else
            {
                cowData.productionTimer = 0f;
            }

            // Stack save
            cowData.currentMilk = milkStack;

            Debug.Log($"[CowController {cowIndex}] 💾 Saved - Timer: {cowData.productionTimer:F1}s, Stack: {milkStack}");
        }

    }
}