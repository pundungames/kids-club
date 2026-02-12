using UnityEngine;
using System;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// Offline Progress Manager v3 - SIFIRDAN
    /// 
    /// Mantık (senin tanımın):
    /// ─────────────────────────────────────────────────────────
    /// 1 area = 1 yem trough + 1 su trough (TroughController)
    /// 1 area max 3 inek
    /// 
    /// feedingInterval = trough'un 1 inekle kaç saniyede biteceği
    /// 3 inek varsa → feedingInterval / 3 saniyede biter
    /// 
    /// Yani 1 ineğin üretim yapabileceği süre = feedingInterval / inekSayısı
    /// 
    /// productionTime = 1 süt üretimi kaç saniye (level bazlı)
    /// üretilebilecek süt = effectiveTime / productionTime
    /// 
    /// TroughSaveData.currentAmount: 0-100 (save formatı)
    /// TroughController.currentFill: 0-1 (runtime formatı)
    /// Dönüşüm: currentFill = currentAmount / 100
    /// ─────────────────────────────────────────────────────────
    /// 
    /// BU SINIF SADECE SAVEDATA ÜZERİNDE ÇALIŞIR.
    /// Runtime objelere (TroughController, CowController) DOKUNMAZ.
    /// İşi bitince ReloadAllManagers() çağırır, runtime güncel saveData'yı yükler.
    /// </summary>
    public class OfflineProgressManager : MonoBehaviour
    {
        [Inject] private SaveManager saveManager;
        [Inject] private CowManager cowManager;
        [Inject] private GameConfig config;

        [Header("Settings")]
        [SerializeField] private float maxOfflineDays = 7f;
        [SerializeField] private bool showWelcomePopup = true;

        [Header("UI (Optional)")]
        [SerializeField] private GameObject welcomePopup;
        [SerializeField] private TMPro.TextMeshProUGUI offlineTimeText;
        [SerializeField] private TMPro.TextMeshProUGUI milkProducedText;
        [SerializeField] private TMPro.TextMeshProUGUI packagesText;
        [SerializeField] private TMPro.TextMeshProUGUI warningText;

        private const int MAX_MILK_STACK = 6;
        private const int MAX_BOTTLES_PER_CASE = 6;
        private const int MAX_PRODUCTION_STACK = 8;

        // ═══════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════

        private void Start()
        {
            StartCoroutine(DelayedCalculate());
        }

        private System.Collections.IEnumerator DelayedCalculate()
        {
            // Diğer manager'lar Start() bitsin
            yield return new WaitForEndOfFrame();

            CalculateOfflineProgress();
            ReloadAllManagers();
        }

        // ═══════════════════════════════════════
        //  ANA GİRİŞ NOKTASI
        // ═══════════════════════════════════════

        public void CalculateOfflineProgress()
        {
            var saveData = saveManager.GetCurrentSaveData();

            // ── İlk oyun ──
            if (saveData.lastPlayTime == 0)
            {
                Debug.Log("[Offline] İlk oyun, skip");
                SaveCurrentTime();
                return;
            }

            // ── Geçen süre ──
            float deltaSeconds = GetOfflineSeconds(saveData);

            if (deltaSeconds < 0f)
            {
                Debug.LogWarning("[Offline] Negatif zaman, skip");
                SaveCurrentTime();
                return;
            }

            if (deltaSeconds < 60f)
            {
                Debug.Log($"[Offline] {deltaSeconds:F0}s < 60s, skip");
                SaveCurrentTime();
                return;
            }

            // Max cap
            float maxSec = maxOfflineDays * 86400f;
            if (deltaSeconds > maxSec) deltaSeconds = maxSec;

            Debug.Log($"[Offline] ═══════════════════════════════════");
            Debug.Log($"[Offline] 🕐 Offline süresi: {FormatDuration(deltaSeconds)} ({deltaSeconds:F0}s)");

            // ── DEBUG: İşlem ÖNCE saveData snapshot ──
            LogSaveDataSnapshot("İŞLEM ÖNCESİ", saveData);

            // ── İşle ──
            OfflineProgressResult result = Process(saveData, deltaSeconds);

            // ── DEBUG: İşlem SONRA saveData snapshot ──
            LogSaveDataSnapshot("İŞLEM SONRASI", saveData);

            // ── Kaydet ──
            saveManager.SaveGame(saveData);
            SaveCurrentTime();

            // ── Popup ──
            if (showWelcomePopup && (result.totalMilkProduced > 0 || result.totalPackagesCreated > 0))
            {
                ShowWelcomePopup(result);
            }

            Debug.Log($"[Offline] ✅ Sonuç: {result.totalMilkProduced} süt, {result.totalPackagesCreated} paket");
            Debug.Log($"[Offline] ═══════════════════════════════════");

            // OfflineProgressResult sınıfından veya hesaplanan değişkenlerden verileri çekiyoruz
            float passedMinutes = deltaSeconds / 60f;
            int totalProducedMilk = result.totalMilkProduced; // Senin değişken ismine göre revize et
            int totalSpawnedCrates = result.totalPackagesCreated; // Senin değişken ismine göre revize et

            Debug.LogError(string.Format(
                "<color=yellow>[OFFLINE ÖZET]</color> Geçen Süre: <b>{0:F1} dk</b> | Üretilen Süt: <b>{1}</b> | Oluşturulan Kasa: <b>{2}</b>",
                passedMinutes,
                totalProducedMilk,
                totalSpawnedCrates
            ));
        }

        // ═══════════════════════════════════════
        //  İŞLEME
        // ═══════════════════════════════════════

        private OfflineProgressResult Process(MilkFarmSaveData saveData, float offlineSeconds)
        {
            var result = new OfflineProgressResult();
            result.deltaTime = offlineSeconds;
            float iapSpeed = GetIAPSpeed(saveData);

            int totalPotentialMilk = 0;

            for (int s = 0; s < saveData.stations.Count; s++)
            {
                var station = saveData.stations[s];
                int cowCount = CountCowsInStation(s, saveData);
                if (cowCount == 0 || station.feedTrough.currentAmount <= 0f || station.waterTrough.currentAmount <= 0f) continue;

                float feedFullLife = config.feedingInterval / cowCount;
                float waterFullLife = config.wateringInterval / cowCount;
                float troughLife = Mathf.Min((station.feedTrough.currentAmount / 100f) * feedFullLife, (station.waterTrough.currentAmount / 100f) * waterFullLife);
                float effectiveTime = Mathf.Min(offlineSeconds, troughLife);

                // Kaynak azaltma
                station.feedTrough.currentAmount -= (effectiveTime / feedFullLife) * 100f;
                station.waterTrough.currentAmount -= (effectiveTime / waterFullLife) * 100f;
                station.foodFill = station.feedTrough.currentAmount / 100f;
                station.waterFill = station.waterTrough.currentAmount / 100f;

                // İneklerin üretimini hesapla
                int startCow = s * config.cowsPerStation;
                int endCow = startCow + config.cowsPerStation;

                for (int c = startCow; c < endCow && c < saveData.cows.Count; c++)
                {
                    var cow = saveData.cows[c];
                    if (!cow.isUnlocked) continue;

                    float prodTime = config.GetProductionTime(cow.level) / iapSpeed;
                    float totalTime = cow.productionTimer + effectiveTime;

                    int cycles = Mathf.FloorToInt(totalTime / prodTime);
                    cow.productionTimer = totalTime % prodTime; // Timer her zaman güncellenir

                    totalPotentialMilk += cycles;
                }
            }

            // ─────────────────────────────────────
            // ŞİMDİ DAĞITIM YAPALIM
            // ─────────────────────────────────────

            // 1. Önce paketleme alanına (kasalara) göndermeyi dene
            int remainingMilk = DistributeToPackaging(saveData, totalPotentialMilk, result);

            // 2. Eğer paketleme alanı dolduysa ve hala süt varsa, ineklerin sırtına (storedMilk) ekle
            if (remainingMilk > 0)
            {
                DistributeToCowStacks(saveData, remainingMilk);
                Debug.Log($"[Offline] Paketleme doldu! {remainingMilk} adet süt ineklerin üzerinde biriktirildi.");
            }

            result.totalMilkProduced = totalPotentialMilk; // Toplamda kaç süt "üretilmeye çalışıldı"
            return result;
        }
        // ═══════════════════════════════════════
        //  PAKETLEME
        // ═══════════════════════════════════════
        private int DistributeToPackaging(MilkFarmSaveData saveData, int amount, OfflineProgressResult result)
        {
            int distributed = 0;

            for (int i = 0; i < amount; i++)
            {
                // Paketleme kapasite kontrolü
                int currentCases = saveData.packaging.productionStackBottles.Count;
                if (currentCases >= MAX_PRODUCTION_STACK) break;

                saveData.packaging.activeCrateBottles++;
                distributed++;

                if (saveData.packaging.activeCrateBottles >= MAX_BOTTLES_PER_CASE)
                {
                    saveData.packaging.productionStackBottles.Add(MAX_BOTTLES_PER_CASE);
                    saveData.packaging.activeCrateBottles = 0;
                    result.totalPackagesCreated++;
                }
            }

            result.totalBottlesPackaged = distributed;
            return amount - distributed; // Paketlenemeyen, artan süt miktarı
        }

        private void DistributeToCowStacks(MilkFarmSaveData saveData, int remainingMilk)
        {
            // Sadece unlock edilmiş inekleri filtrele
            var unlockedCows = saveData.cows.FindAll(c => c.isUnlocked);
            if (unlockedCows.Count == 0) return;

            int milkPerCow = remainingMilk;

            foreach (var cow in unlockedCows)
            {
                if (milkPerCow <= 0) break;

                int currentSpace = MAX_MILK_STACK - cow.storedMilk;
                int toAdd = Mathf.Min(milkPerCow, currentSpace);

                cow.storedMilk += toAdd;
                milkPerCow -= toAdd;
            }
        }
        private void CollectMilkToPackaging(MilkFarmSaveData saveData, OfflineProgressResult result)
        {
            int totalBottles = 0;

            int currentCases = saveData.packaging.productionStackBottles.Count;
            if (saveData.packaging.activeCrateBottles > 0) currentCases++;

            if (currentCases >= MAX_PRODUCTION_STACK)
            {
                Debug.Log("[Offline] Paketleme dolu, toplama yok");
                return;
            }

            foreach (var cow in saveData.cows)
            {
                if (!cow.isUnlocked || cow.storedMilk <= 0) continue;

                int milk = cow.storedMilk;

                for (int m = 0; m < milk; m++)
                {
                    if (saveData.packaging.productionStackBottles.Count >= MAX_PRODUCTION_STACK)
                        goto done;

                    saveData.packaging.activeCrateBottles++;
                    totalBottles++;

                    if (saveData.packaging.activeCrateBottles >= MAX_BOTTLES_PER_CASE)
                    {
                        saveData.packaging.productionStackBottles.Add(MAX_BOTTLES_PER_CASE);
                        saveData.packaging.activeCrateBottles = 0;
                    }
                }

                cow.storedMilk = 0;
            }

        done:
            result.totalBottlesPackaged = totalBottles;
            result.totalPackagesCreated = totalBottles / MAX_BOTTLES_PER_CASE;

            Debug.Log($"[Offline] 📦 {totalBottles} şişe → {result.totalPackagesCreated} kasa");
        }

        // ═══════════════════════════════════════
        //  YARDIMCI
        // ═══════════════════════════════════════

        private int CountCowsInStation(int stationIndex, MilkFarmSaveData saveData)
        {
            int count = 0;
            int start = stationIndex * config.cowsPerStation;
            int end = start + config.cowsPerStation;
            for (int i = start; i < end && i < saveData.cows.Count; i++)
            {
                if (saveData.cows[i].isUnlocked) count++;
            }
            return count;
        }

        private float GetIAPSpeed(MilkFarmSaveData saveData)
        {
            if (saveData.iap == null) return 1f;
            switch (saveData.iap.speedTier)
            {
                case 1: return 1.5f;
                case 2: return 2f;
                default: return 1f;
            }
        }

        private float GetOfflineSeconds(MilkFarmSaveData saveData)
        {
            // Kaydedilen zamanı UTC olarak yorumla
            DateTimeOffset lastPlay = DateTimeOffset.FromUnixTimeSeconds(saveData.lastPlayTime);
            // Şu anki zamanı UTC olarak al
            DateTimeOffset now = DateTimeOffset.UtcNow;

            return (float)(now - lastPlay).TotalSeconds;
        }

        private void SaveCurrentTime()
        {
            var saveData = saveManager.GetCurrentSaveData();
            // ToUnixTimeSeconds() zaten UTC tabanlı çalışır, 
            // ama UtcNow ile çağırmak kafa karışıklığını önler.
            saveData.lastPlayTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            saveManager.SaveGame(saveData);
        }
        // ═══════════════════════════════════════
        //  RELOAD
        // ═══════════════════════════════════════

        /// <summary>
        /// SaveData güncelledik, şimdi runtime manager'ları reload et.
        /// StationManager.LoadFromSaveData() → TroughController.LoadFromSaveData()
        ///   → currentFill = data.currentAmount / 100f
        /// Bu sayede runtime trough'lar güncel değeri alır.
        /// </summary>
        private void ReloadAllManagers()
        {
            Debug.Log("[Offline] 🔄 Manager reload başlıyor...");

            var packageManager = FindObjectOfType<PackageManager>();
            if (packageManager != null)
            {
                packageManager.LoadFromSaveData();
                Debug.Log("[Offline] ✅ PackageManager reloaded");
            }

            var stationManager = FindObjectOfType<StationManager>();
            if (stationManager != null)
            {
                // ÖNEMLI: Bu çağrı TroughController.LoadFromSaveData()'yı da çağırır
                // LoadFromSaveData içinde: currentFill = data.currentAmount / 100f
                stationManager.LoadFromSaveData();
                Debug.Log("[Offline] ✅ StationManager reloaded");

                // DEBUG: Reload sonrası trough değerlerini logla
                for (int i = 0; i < stationManager.stations.Count; i++)
                {
                    var s = stationManager.stations[i];
                    float feedFill = s.feedTroughController != null ? s.feedTroughController.GetFillAmount() : -1f;
                    float waterFill = s.waterTroughController != null ? s.waterTroughController.GetFillAmount() : -1f;
                    Debug.Log($"[Offline] RELOAD SONRASI Station {i}: " +
                              $"feedTrough.currentFill={feedFill:F3} ({feedFill * 100:F1}%), " +
                              $"waterTrough.currentFill={waterFill:F3} ({waterFill * 100:F1}%), " +
                              $"station.foodFill={s.foodFill:F3}, station.waterFill={s.waterFill:F3}");
                }
            }

            // CowManager reload ETME! CowController zaten trough referanslarını tutuyor.
            // Trough reload olunca currentFill güncellenmiş oluyor,
            // CowController.HasResource kontrolü otomatik çalışır.

            Debug.Log("[Offline] 🔄 Manager reload bitti");
        }

        // ═══════════════════════════════════════
        //  DEBUG LOG
        // ═══════════════════════════════════════

        private void LogSaveDataSnapshot(string label, MilkFarmSaveData saveData)
        {
            Debug.Log($"[Offline] ── {label} ──");
            for (int s = 0; s < saveData.stations.Count; s++)
            {
                var st = saveData.stations[s];
                Debug.Log($"[Offline]   Station {s}: feed.currentAmount={st.feedTrough.currentAmount:F1}, " +
                          $"water.currentAmount={st.waterTrough.currentAmount:F1}, " +
                          $"foodFill={st.foodFill:F3}, waterFill={st.waterFill:F3}");
            }
            for (int c = 0; c < saveData.cows.Count; c++)
            {
                var cow = saveData.cows[c];
                if (cow.isUnlocked)
                {
                    Debug.Log($"[Offline]   Cow {c}: milk={cow.storedMilk}, timer={cow.productionTimer:F1}s, lv={cow.level}");
                }
            }
            Debug.Log($"[Offline]   Packaging: activeCrate={saveData.packaging.activeCrateBottles}, " +
                      $"stack={saveData.packaging.productionStackBottles.Count}");
        }

        // ═══════════════════════════════════════
        //  UI
        // ═══════════════════════════════════════

        private void ShowWelcomePopup(OfflineProgressResult result)
        {
            if (welcomePopup == null)
            {
                LogWelcomeMessage(result);
                return;
            }

            if (offlineTimeText != null)
                offlineTimeText.text = FormatDuration(result.deltaTime);
            if (milkProducedText != null)
                milkProducedText.text = $"{result.totalMilkProduced} 🥛";
            if (packagesText != null)
                packagesText.text = $"{result.totalPackagesCreated} 📦";

            if (warningText != null)
            {
                string w = "";
                if (result.feedEmpty) w += "⚠️ Yemlik boş!\n";
                if (result.waterEmpty) w += "⚠️ Suluk boş!\n";
                warningText.text = w;
                warningText.gameObject.SetActive(w.Length > 0);
            }

            welcomePopup.SetActive(true);
        }

        private void LogWelcomeMessage(OfflineProgressResult result)
        {
            Debug.Log("════════════════════════════════");
            Debug.Log("🎉 HOŞ GELDİN!");
            Debug.Log($"⏰ {FormatDuration(result.deltaTime)}");
            Debug.Log($"🥛 {result.totalMilkProduced} süt");
            Debug.Log($"📦 {result.totalPackagesCreated} kasa");
            if (result.feedEmpty) Debug.Log("⚠️ Yemlik boş!");
            if (result.waterEmpty) Debug.Log("⚠️ Suluk boş!");
            Debug.Log("════════════════════════════════");
        }

        private string FormatDuration(float seconds)
        {
            if (seconds < 60) return $"{Mathf.FloorToInt(seconds)}s";
            if (seconds < 3600) return $"{Mathf.FloorToInt(seconds / 60f)}dk";
            if (seconds < 86400) return $"{(seconds / 3600f):F1}sa";
            return $"{(seconds / 86400f):F1}gün";
        }

        // ═══════════════════════════════════════
        //  CONTEXT MENU DEBUG
        // ═══════════════════════════════════════

        [ContextMenu("Debug: Simulate 1 Hour")]
        public void DebugSimulate1Hour()
        {
            var saveData = saveManager.GetCurrentSaveData();
            LogSaveDataSnapshot("SİMÜLASYON ÖNCESİ", saveData);
            var result = Process(saveData, 3600f);
            LogSaveDataSnapshot("SİMÜLASYON SONRASI", saveData);
            saveManager.SaveGame(saveData);
            LogWelcomeMessage(result);
            ReloadAllManagers();
        }

        [ContextMenu("Debug: Simulate 8 Hours")]
        public void DebugSimulate8Hours()
        {
            var saveData = saveManager.GetCurrentSaveData();
            var result = Process(saveData, 28800f);
            saveManager.SaveGame(saveData);
            LogWelcomeMessage(result);
            ReloadAllManagers();
        }

        [ContextMenu("Debug: Log Current SaveData")]
        public void DebugLogSaveData()
        {
            var saveData = saveManager.GetCurrentSaveData();
            LogSaveDataSnapshot("MEVCUT DURUM", saveData);
        }

        [ContextMenu("Debug: Reset Last Play Time")]
        public void DebugResetLastPlayTime()
        {
            var saveData = saveManager.GetCurrentSaveData();
            saveData.lastPlayTime = 0;
            saveManager.SaveGame(saveData);
            Debug.Log("[Offline] lastPlayTime sıfırlandı");
        }
    }

    /// <summary>
    /// Offline sonuç (eski class adıyla backward compat)
    /// </summary>
    public class OfflineProgressResult
    {
        public float deltaTime;
        public int totalMilkProduced;
        public int totalBottlesPackaged;
        public int totalPackagesCreated;
        public bool feedEmpty;
        public bool waterEmpty;
    }
}