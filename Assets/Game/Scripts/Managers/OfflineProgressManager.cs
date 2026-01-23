/*using UnityEngine;
using System;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// Offline Progress Manager
    /// Mathematical calculation - Instant, accurate
    /// </summary>
    public class OfflineProgressManager : MonoBehaviour
    {
        [Inject] private SaveManager saveManager;
        [Inject] private CowManager cowManager;
        [Inject] private GameConfig config;

        [Header("Settings")]
        [SerializeField] private float maxOfflineDays = 7f; // Max 7 gün
        [SerializeField] private bool showWelcomePopup = true;

        [Header("UI References (Optional)")]
        [SerializeField] private GameObject welcomePopup;
        [SerializeField] private TMPro.TextMeshProUGUI offlineTimeText;
        [SerializeField] private TMPro.TextMeshProUGUI milkProducedText;
        [SerializeField] private TMPro.TextMeshProUGUI warningText;

        private void Start()
        {
            // Oyun açıldığında offline progress hesapla
            CalculateOfflineProgress();
        }

        /// <summary>
        /// Offline progress hesaplama (Mathematical)
        /// </summary>
        public void CalculateOfflineProgress()
        {
            var saveData = saveManager.GetCurrentSaveData();

            // lastPlayTime yoksa ilk oyun - skip
            if (saveData.lastPlayTime == 0)
            {
                Debug.Log("[OfflineProgress] İlk oyun - offline progress yok");
                SaveCurrentTime();
                return;
            }

            // Time delta hesapla
            DateTime lastPlay = DateTimeFromUnixTimestamp(saveData.lastPlayTime);
            DateTime now = DateTime.Now;
            TimeSpan deltaTimeSpan = now - lastPlay;
            float deltaSeconds = (float)deltaTimeSpan.TotalSeconds;

            Debug.Log($"[OfflineProgress] Son oyun: {lastPlay}");
            Debug.Log($"[OfflineProgress] Şimdi: {now}");
            Debug.Log($"[OfflineProgress] Delta: {deltaTimeSpan}");

            // Validation
            if (!ValidateTimeDelta(deltaSeconds, out float validatedSeconds))
            {
                Debug.LogWarning("[OfflineProgress] Invalid time delta!");
                SaveCurrentTime();
                return;
            }

            deltaSeconds = validatedSeconds;

            // Çok kısa süre - skip (< 1 dakika)
            if (deltaSeconds < 60f)
            {
                Debug.Log("[OfflineProgress] Delta çok kısa (< 1 dk), skip");
                SaveCurrentTime();
                return;
            }

            Debug.Log($"[OfflineProgress] 🎯 Calculating for {deltaSeconds}s ({deltaSeconds / 60f} minutes)");

            // Process offline progress
            ProcessOfflineProgress(deltaSeconds, out OfflineProgressResult result);

            // Save
            SaveCurrentTime();

            // Show popup
            if (showWelcomePopup && result.totalMilkProduced > 0)
            {
                ShowWelcomePopup(result);
            }

            Debug.Log($"[OfflineProgress] ✅ Complete! Milk: {result.totalMilkProduced}, Duration: {result.effectiveDuration}s");
        }

        /// <summary>
        /// Time delta validation
        /// </summary>
        private bool ValidateTimeDelta(float deltaSeconds, out float validatedSeconds)
        {
            validatedSeconds = deltaSeconds;

            // Negative time (system clock went backwards)
            if (deltaSeconds < 0)
            {
                Debug.LogWarning("[OfflineProgress] ⚠️ Negative time! Clock manipulation?");
                validatedSeconds = 0;
                return false;
            }

            // Too long (> max offline days)
            float maxSeconds = maxOfflineDays * 24f * 3600f;
            if (deltaSeconds > maxSeconds)
            {
                Debug.LogWarning($"[OfflineProgress] ⚠️ Delta too long ({deltaSeconds / 86400f} days), capping to {maxOfflineDays} days");
                validatedSeconds = maxSeconds;
            }

            // Suspicious (> 1 year)
            if (deltaSeconds > 365f * 24f * 3600f)
            {
                Debug.LogWarning("[OfflineProgress] ⚠️ Suspicious delta (> 1 year)! Capping to max");
                validatedSeconds = maxSeconds;
            }

            return true;
        }

        /// <summary>
        /// Process offline progress (Mathematical calculation)
        /// </summary>
        private void ProcessOfflineProgress(float deltaSeconds, out OfflineProgressResult result)
        {
            result = new OfflineProgressResult();
            result.deltaTime = deltaSeconds;

            var saveData = saveManager.GetCurrentSaveData();
            var cows = cowManager.GetAllCows();

            // Her inek için hesapla
            for (int i = 0; i < cows.Count; i++)
            {
                var cow = cows[i];
                if (!cow.isUnlocked) continue;

                var cowSaveData = saveData.cows[i];

                // Station index
                int stationIndex = i / config.cowsPerStation;

                // Trough data
                var feedTrough = saveData.stations[stationIndex].feedTrough;
                var waterTrough = saveData.stations[stationIndex].waterTrough;

                // Trough lifetime hesapla (kaç saniye dayanır?)
                float troughLifetime = CalculateTroughLifetime(feedTrough, waterTrough, stationIndex, saveData);

                // Effective production time (trough bitene kadar)
                float effectiveTime = Mathf.Min(deltaSeconds, troughLifetime);
                result.effectiveDuration = Mathf.Max(result.effectiveDuration, effectiveTime);

                if (effectiveTime <= 0)
                {
                    Debug.Log($"[OfflineProgress] Cow {i}: Trough boş, üretim yok");
                    continue;
                }

                // Milk production calculation
                float productionTime = config.baseProductionTime;
                float currentTimer = cowSaveData.productionTimer;

                // Kalan süre + offline time
                float totalProductionTime = currentTimer + effectiveTime;
                int cyclesCompleted = Mathf.FloorToInt(totalProductionTime / productionTime);
                float remainingTimer = totalProductionTime % productionTime;

                // Stack limit
                int currentMilk = cowSaveData.storedMilk;
                int maxMilk = config.maxMilkStack;
                int milkProduced = Mathf.Min(cyclesCompleted, maxMilk - currentMilk);

                if (milkProduced > 0)
                {
                    cowSaveData.storedMilk += milkProduced;
                    result.totalMilkProduced += milkProduced;
                    Debug.Log($"[OfflineProgress] Cow {i}: +{milkProduced} milk (total: {cowSaveData.storedMilk})");
                }

                // Update timer
                cowSaveData.productionTimer = remainingTimer;

                // Trough consumption
                if (effectiveTime > 0)
                {
                    float feedConsumed = CalculateFeedConsumption(effectiveTime, stationIndex, saveData);
                    float waterConsumed = CalculateWaterConsumption(effectiveTime, stationIndex, saveData);

                    feedTrough.currentAmount = Mathf.Max(0, feedTrough.currentAmount - feedConsumed);
                    waterTrough.currentAmount = Mathf.Max(0, waterTrough.currentAmount - waterConsumed);
                }
            }

            // Check if troughs are empty
            result.feedEmpty = CheckIfAnyTroughEmpty(saveData, true);
            result.waterEmpty = CheckIfAnyTroughEmpty(saveData, false);

            // Save changes
            saveManager.SaveGame(saveData);
        }

        /// <summary>
        /// Trough lifetime hesaplama (kaç saniye dayanır?)
        /// </summary>
        private float CalculateTroughLifetime(TroughSaveData feed, TroughSaveData water, int stationIndex, GameSaveData saveData)
        {
            // Aktif inek sayısı
            int activeCowCount = GetActiveCowCountForStation(stationIndex, saveData);
            if (activeCowCount == 0) return float.MaxValue;

            // Consumption rates (per second per cow)
            float feedRate = config.baseTroughConsumption / config.baseProductionTime;
            float waterRate = config.baseTroughConsumption / config.baseProductionTime;

            // Total consumption rate
            float totalFeedRate = feedRate * activeCowCount;
            float totalWaterRate = waterRate * activeCowCount;

            // Lifetime
            float feedLifetime = totalFeedRate > 0 ? feed.currentAmount / totalFeedRate : float.MaxValue;
            float waterLifetime = totalWaterRate > 0 ? water.currentAmount / totalWaterRate : float.MaxValue;

            return Mathf.Min(feedLifetime, waterLifetime);
        }

        private float CalculateFeedConsumption(float deltaSeconds, int stationIndex, GameSaveData saveData)
        {
            int activeCowCount = GetActiveCowCountForStation(stationIndex, saveData);
            float rate = config.baseTroughConsumption / config.baseProductionTime;
            return deltaSeconds * rate * activeCowCount;
        }

        private float CalculateWaterConsumption(float deltaSeconds, int stationIndex, GameSaveData saveData)
        {
            int activeCowCount = GetActiveCowCountForStation(stationIndex, saveData);
            float rate = config.baseTroughConsumption / config.baseProductionTime;
            return deltaSeconds * rate * activeCowCount;
        }

        private int GetActiveCowCountForStation(int stationIndex, GameSaveData saveData)
        {
            int count = 0;
            int startIndex = stationIndex * config.cowsPerStation;
            int endIndex = startIndex + config.cowsPerStation;

            for (int i = startIndex; i < endIndex && i < saveData.cows.Count; i++)
            {
                if (saveData.cows[i].isUnlocked) count++;
            }

            return count;
        }

        private bool CheckIfAnyTroughEmpty(GameSaveData saveData, bool checkFeed)
        {
            foreach (var station in saveData.stations)
            {
                var trough = checkFeed ? station.feedTrough : station.waterTrough;
                if (trough.currentAmount <= 0) return true;
            }
            return false;
        }

        /// <summary>
        /// Show welcome back popup
        /// </summary>
        private void ShowWelcomePopup(OfflineProgressResult result)
        {
            if (welcomePopup == null)
            {
                Debug.Log($"[OfflineProgress] Welcome popup yok - console'da göster");
                LogWelcomeMessage(result);
                return;
            }

            // UI doldur
            if (offlineTimeText != null)
            {
                offlineTimeText.text = FormatDuration(result.deltaTime);
            }

            if (milkProducedText != null)
            {
                milkProducedText.text = $"{result.totalMilkProduced} 🥛";
            }

            if (warningText != null)
            {
                string warning = "";
                if (result.feedEmpty) warning += "⚠️ Saman bitti!\n";
                if (result.waterEmpty) warning += "⚠️ Su bitti!\n";
                warningText.text = warning;
                warningText.gameObject.SetActive(!string.IsNullOrEmpty(warning));
            }

            welcomePopup.SetActive(true);
            Debug.Log("[OfflineProgress] Welcome popup gösterildi");
        }

        private void LogWelcomeMessage(OfflineProgressResult result)
        {
            Debug.Log("========================================");
            Debug.Log("🎉 HOŞ GELDİN!");
            Debug.Log($"⏰ {FormatDuration(result.deltaTime)} geçti");
            Debug.Log($"🥛 {result.totalMilkProduced} süt üretildi");
            if (result.feedEmpty) Debug.Log("⚠️ Saman bitti!");
            if (result.waterEmpty) Debug.Log("⚠️ Su bitti!");
            Debug.Log("========================================");
        }

        /// <summary>
        /// Save current time
        /// </summary>
        private void SaveCurrentTime()
        {
            var saveData = saveManager.GetCurrentSaveData();
            saveData.lastPlayTime = GetCurrentUnixTimestamp();
            saveManager.SaveGame(saveData);
        }

        /// <summary>
        /// Time utilities
        /// </summary>
        private long GetCurrentUnixTimestamp()
        {
            return ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
        }

        private DateTime DateTimeFromUnixTimestamp(long timestamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
        }

        private string FormatDuration(float seconds)
        {
            if (seconds < 60) return $"{Mathf.FloorToInt(seconds)} saniye";
            if (seconds < 3600) return $"{Mathf.FloorToInt(seconds / 60f)} dakika";
            if (seconds < 86400) return $"{(seconds / 3600f):F1} saat";
            return $"{(seconds / 86400f):F1} gün";
        }

        // === DEBUG ===

        [ContextMenu("Debug: Simulate 1 Hour")]
        public void DebugSimulate1Hour()
        {
            ProcessOfflineProgress(3600f, out var result);
            LogWelcomeMessage(result);
        }

        [ContextMenu("Debug: Simulate 1 Day")]
        public void DebugSimulate1Day()
        {
            ProcessOfflineProgress(86400f, out var result);
            LogWelcomeMessage(result);
        }

        [ContextMenu("Debug: Reset Last Play Time")]
        public void DebugResetLastPlayTime()
        {
            var saveData = saveManager.GetCurrentSaveData();
            saveData.lastPlayTime = 0;
            saveManager.SaveGame(saveData);
            Debug.Log("[OfflineProgress] lastPlayTime reset!");
        }

        private void OnApplicationQuit()
        {
            SaveCurrentTime();
            Debug.Log("[OfflineProgress] Quit - Time saved");
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                SaveCurrentTime();
                Debug.Log("[OfflineProgress] Pause - Time saved");
            }
        }
    }

    /// <summary>
    /// Offline progress result
    /// </summary>
    public class OfflineProgressResult
    {
        public float deltaTime;
        public float effectiveDuration;
        public int totalMilkProduced;
        public bool feedEmpty;
        public bool waterEmpty;
    }
}*/