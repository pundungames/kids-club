using UnityEngine;
using System;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// Offline Progress Manager - COMPLETE VERSION
    /// - Level-based production time ✅
    /// - IAP speed boost ✅
    /// - Auto collect to packaging ✅
    /// - Trough consumption ✅
    /// - Production stops when trough empty ✅
    /// - Production stops when packaging full ✅
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

        private const int MAX_MILK_STACK = 6;        // Per cow
        private const int MAX_BOTTLES_PER_CASE = 6;  // Per milk case
        private const int MAX_PRODUCTION_STACK = 8;  // Max cases in production stack

        private void Start()
        {
            // ✅ Delay to let other managers load first
            StartCoroutine(DelayedCalculate());
        }

        private System.Collections.IEnumerator DelayedCalculate()
        {
            // Wait for all managers to finish Start()
            yield return new WaitForEndOfFrame();

            CalculateOfflineProgress();

            // ✅ Reload managers to reflect offline changes
            Debug.Log("[OfflineProgress] Reloading managers...");

            // Reload PackageManager
            var packageManager = FindObjectOfType<PackageManager>();
            if (packageManager != null)
            {
                packageManager.LoadFromSaveData();
            }

            // Reload StationManager (Troughs)
            var stationManager = FindObjectOfType<StationManager>();
            if (stationManager != null)
            {
                stationManager.LoadFromSaveData();
            }

            Debug.Log("[OfflineProgress] Managers reloaded!");
        }

        public void CalculateOfflineProgress()
        {
            var saveData = saveManager.GetCurrentSaveData();

            if (saveData.lastPlayTime == 0)
            {
                Debug.Log("[OfflineProgress] First play");
                SaveCurrentTime();
                return;
            }

            DateTime lastPlay = DateTimeOffset.FromUnixTimeSeconds(saveData.lastPlayTime).DateTime;
            DateTime now = DateTime.Now;
            TimeSpan deltaTimeSpan = now - lastPlay;
            float deltaSeconds = (float)deltaTimeSpan.TotalSeconds;

            Debug.Log($"[OfflineProgress] Delta: {deltaTimeSpan}");

            if (!ValidateTimeDelta(deltaSeconds, out float validatedSeconds))
            {
                SaveCurrentTime();
                return;
            }

            deltaSeconds = validatedSeconds;

            if (deltaSeconds < 60f) // Less than 1 minute
            {
                SaveCurrentTime();
                return;
            }

            Debug.Log($"[OfflineProgress] 🎯 Processing {deltaSeconds / 60f:F1} minutes offline");

            ProcessOfflineProgress(deltaSeconds, out OfflineProgressResult result);
            SaveCurrentTime();

            if (showWelcomePopup && (result.totalMilkProduced > 0 || result.totalPackagesCreated > 0))
            {
                ShowWelcomePopup(result);
            }

            Debug.Log($"[OfflineProgress] ✅ Milk: {result.totalMilkProduced}, Packages: {result.totalPackagesCreated}");
        }

        private bool ValidateTimeDelta(float deltaSeconds, out float validatedSeconds)
        {
            validatedSeconds = deltaSeconds;

            if (deltaSeconds < 0)
            {
                Debug.LogWarning("[OfflineProgress] Negative time detected!");
                validatedSeconds = 0;
                return false;
            }

            float maxSeconds = maxOfflineDays * 86400f;
            if (deltaSeconds > maxSeconds)
            {
                Debug.LogWarning($"[OfflineProgress] Capping to {maxOfflineDays} days");
                validatedSeconds = maxSeconds;
            }

            return true;
        }

        private void ProcessOfflineProgress(float deltaSeconds, out OfflineProgressResult result)
        {
            result = new OfflineProgressResult();
            result.deltaTime = deltaSeconds;

            var saveData = saveManager.GetCurrentSaveData();

            // ✅ IAP speed multiplier (applies to all cows)
            float iapSpeedMultiplier = 1f;
            if (saveData.iap != null)
            {
                switch (saveData.iap.speedTier)
                {
                    case 1: iapSpeedMultiplier = 1.5f; break;
                    case 2: iapSpeedMultiplier = 2f; break;
                }
            }

            Debug.Log($"[OfflineProgress] IAP Speed Multiplier: {iapSpeedMultiplier}x");
            Debug.Log($"[OfflineProgress] Total cows in save: {saveData.cows.Count}");

            // ✅ Process each cow (from SAVE DATA, not runtime!)
            for (int i = 0; i < saveData.cows.Count; i++)
            {
                var cowSaveData = saveData.cows[i];

                Debug.Log($"[OfflineProgress] --- Cow {i} ---");
                Debug.Log($"[OfflineProgress] Cow {i}: Unlocked={cowSaveData.isUnlocked}, Level={cowSaveData.level}");

                if (!cowSaveData.isUnlocked)
                {
                    Debug.Log($"[OfflineProgress] Cow {i}: LOCKED, skipping");
                    continue;
                }

                Debug.Log($"[OfflineProgress] Cow {i}: UNLOCKED, processing...");
                int stationIndex = i / config.cowsPerStation;

                // Get trough data
                var stationData = saveData.stations[stationIndex];
                var feedTrough = stationData.feedTrough;
                var waterTrough = stationData.waterTrough;

                float feedAmount = feedTrough.currentAmount;   // 0-100
                float waterAmount = waterTrough.currentAmount; // 0-100

                // Calculate how long troughs will last
                float troughLifetime = CalculateTroughLifetime(feedAmount, waterAmount, stationIndex, saveData);
                float effectiveTime = Mathf.Min(deltaSeconds, troughLifetime);

                if (effectiveTime <= 0)
                {
                    Debug.Log($"[OfflineProgress] Cow {i}: No resources, skipping");
                    continue;
                }

                // ✅ Level-based production time (from GameConfig)
                float baseProductionTime = config.GetProductionTime(cowSaveData.level);

                // ✅ Apply IAP speed boost
                float productionTime = baseProductionTime / iapSpeedMultiplier;

                Debug.Log($"[OfflineProgress] Cow {i} (Lv{cowSaveData.level}): Base {baseProductionTime}s → {productionTime}s (IAP {iapSpeedMultiplier}x)");

                // ✅ Timer is REMAINING time (not elapsed)
                // cowSaveData.productionTimer = how many seconds LEFT in current cycle
                // Example: timer = 10s (10s remaining)
                // Offline: 600s passed
                // Total production time: 10s (remaining) + 600s (offline) = 610s
                // Cycles: 610 / 30 = 20.33 → 20 complete cycles
                // New timer: 610 % 30 = 10s remaining

                float totalProductionTime = cowSaveData.productionTimer + effectiveTime;
                int cycles = Mathf.FloorToInt(totalProductionTime / productionTime);
                float newRemainingTimer = totalProductionTime % productionTime;

                Debug.Log($"[OfflineProgress] Cow {i}: Timer {cowSaveData.productionTimer:F1}s + Offline {effectiveTime:F1}s = {totalProductionTime:F1}s");
                Debug.Log($"[OfflineProgress] Cow {i}: Cycles: {cycles}, New timer: {newRemainingTimer:F1}s");

                // ✅ Produce milk (capped by cow stack)
                int currentMilk = cowSaveData.storedMilk;
                int milkCanProduce = Mathf.Min(cycles, MAX_MILK_STACK - currentMilk);

                if (milkCanProduce > 0)
                {
                    cowSaveData.storedMilk += milkCanProduce;
                    result.totalMilkProduced += milkCanProduce;
                    Debug.Log($"[OfflineProgress] Cow {i}: Produced {milkCanProduce} milk (stack: {currentMilk} → {cowSaveData.storedMilk})");
                }

                // ✅ Update timer (REMAINING time)
                cowSaveData.productionTimer = newRemainingTimer;

                // ✅ Consume troughs
                float feedConsumed = CalculateConsumption(effectiveTime, stationIndex, saveData);
                float waterConsumed = CalculateConsumption(effectiveTime, stationIndex, saveData);

                feedTrough.currentAmount = Mathf.Max(0, feedAmount - feedConsumed);
                waterTrough.currentAmount = Mathf.Max(0, waterAmount - waterConsumed);

                Debug.Log($"[OfflineProgress] Cow {i}: Troughs - Feed {feedAmount:F1} → {feedTrough.currentAmount:F1}, Water {waterAmount:F1} → {waterTrough.currentAmount:F1}");
            }

            // ✅ Auto-collect milk to packaging
            CollectMilkToPackaging(saveData, out int bottlesPackaged);
            result.totalBottlesPackaged = bottlesPackaged;
            result.totalPackagesCreated = bottlesPackaged / MAX_BOTTLES_PER_CASE;

            // Check warnings
            result.feedEmpty = CheckIfAnyTroughEmpty(saveData, true);
            result.waterEmpty = CheckIfAnyTroughEmpty(saveData, false);

            saveManager.SaveGame(saveData);
        }

        /// <summary>
        /// Auto-collect all milk from cows to packaging
        /// </summary>
        private void CollectMilkToPackaging(MilkFarmSaveData saveData, out int totalBottlesPackaged)
        {
            totalBottlesPackaged = 0;

            // Check current packaging capacity
            int currentCases = saveData.packaging.productionStackBottles.Count;
            int activeCrateBottles = saveData.packaging.activeCrateBottles;

            if (activeCrateBottles > 0) currentCases++; // Active crate counts

            if (currentCases >= MAX_PRODUCTION_STACK)
            {
                Debug.Log("[OfflineProgress] Packaging full, cannot collect milk");
                return;
            }

            // Collect from each cow
            foreach (var cowData in saveData.cows)
            {
                if (!cowData.isUnlocked) continue;
                if (cowData.storedMilk <= 0) continue;

                int milkToCollect = cowData.storedMilk;

                for (int i = 0; i < milkToCollect; i++)
                {
                    // Check if packaging is full
                    if (saveData.packaging.productionStackBottles.Count >= MAX_PRODUCTION_STACK)
                    {
                        Debug.Log("[OfflineProgress] Packaging full during collection");
                        return;
                    }

                    // Add to active crate or create new
                    if (saveData.packaging.activeCrateBottles < MAX_BOTTLES_PER_CASE)
                    {
                        saveData.packaging.activeCrateBottles++;
                        totalBottlesPackaged++;

                        // Complete crate?
                        if (saveData.packaging.activeCrateBottles >= MAX_BOTTLES_PER_CASE)
                        {
                            // Move to production stack
                            saveData.packaging.productionStackBottles.Add(MAX_BOTTLES_PER_CASE);
                            saveData.packaging.activeCrateBottles = 0;
                            Debug.Log($"[OfflineProgress] ✅ Crate completed! Stack: {saveData.packaging.productionStackBottles.Count}");
                        }
                    }
                    else
                    {
                        // Should not happen (active crate should be < 6)
                        Debug.LogWarning("[OfflineProgress] Active crate overflow!");
                    }
                }

                // Clear cow milk
                cowData.storedMilk = 0;
            }

            Debug.Log($"[OfflineProgress] 📦 Collected {totalBottlesPackaged} bottles, Created {totalBottlesPackaged / MAX_BOTTLES_PER_CASE} complete cases");
        }

        private float CalculateTroughLifetime(float feedAmount, float waterAmount, int stationIndex, MilkFarmSaveData saveData)
        {
            int activeCowCount = GetActiveCowCountForStation(stationIndex, saveData);
            if (activeCowCount == 0) return float.MaxValue;

            // ✅ Use feedingInterval/wateringInterval from config
            // How long does 100 units last with X cows?
            // If 1 cow drains 100 units in feedingInterval seconds:
            // consumptionPerSecond = 100 / feedingInterval
            // For X cows: totalRate = consumptionPerSecond * activeCowCount

            float feedConsumptionPerSecond = 100f / config.feedingInterval;  // e.g., 100 / 2000 = 0.05 per second
            float waterConsumptionPerSecond = 100f / config.wateringInterval; // e.g., 100 / 2000 = 0.05 per second

            float totalFeedRate = feedConsumptionPerSecond * activeCowCount;
            float totalWaterRate = waterConsumptionPerSecond * activeCowCount;

            float feedLifetime = totalFeedRate > 0 ? feedAmount / totalFeedRate : float.MaxValue;
            float waterLifetime = totalWaterRate > 0 ? waterAmount / totalWaterRate : float.MaxValue;

            return Mathf.Min(feedLifetime, waterLifetime);
        }

        private float CalculateConsumption(float deltaSeconds, int stationIndex, MilkFarmSaveData saveData)
        {
            int activeCowCount = GetActiveCowCountForStation(stationIndex, saveData);

            // ✅ Use feedingInterval from config
            float consumptionPerSecond = 100f / config.feedingInterval; // e.g., 100 / 2000 = 0.05

            return deltaSeconds * consumptionPerSecond * activeCowCount;
        }

        private int GetActiveCowCountForStation(int stationIndex, MilkFarmSaveData saveData)
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

        private bool CheckIfAnyTroughEmpty(MilkFarmSaveData saveData, bool checkFeed)
        {
            foreach (var station in saveData.stations)
            {
                var trough = checkFeed ? station.feedTrough : station.waterTrough;
                if (trough.currentAmount <= 0) return true;
            }
            return false;
        }

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
                string warning = "";
                if (result.feedEmpty) warning += "⚠️ Feed trough empty!\n";
                if (result.waterEmpty) warning += "⚠️ Water trough empty!\n";
                warningText.text = warning;
                warningText.gameObject.SetActive(!string.IsNullOrEmpty(warning));
            }

            welcomePopup.SetActive(true);
        }

        private void LogWelcomeMessage(OfflineProgressResult result)
        {
            Debug.Log("========================================");
            Debug.Log("🎉 WELCOME BACK!");
            Debug.Log($"⏰ {FormatDuration(result.deltaTime)}");
            Debug.Log($"🥛 {result.totalMilkProduced} milk produced");
            Debug.Log($"📦 {result.totalPackagesCreated} cases created");
            if (result.feedEmpty) Debug.Log("⚠️ Feed trough empty!");
            if (result.waterEmpty) Debug.Log("⚠️ Water trough empty!");
            Debug.Log("========================================");
        }

        private void SaveCurrentTime()
        {
            var saveData = saveManager.GetCurrentSaveData();
            saveData.lastPlayTime = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
            saveManager.SaveGame(saveData);
        }

        private string FormatDuration(float seconds)
        {
            if (seconds < 60) return $"{Mathf.FloorToInt(seconds)}s";
            if (seconds < 3600) return $"{Mathf.FloorToInt(seconds / 60f)}m";
            if (seconds < 86400) return $"{(seconds / 3600f):F1}h";
            return $"{(seconds / 86400f):F1}d";
        }

        [ContextMenu("Debug: Simulate 1 Hour")]
        public void DebugSimulate1Hour()
        {
            ProcessOfflineProgress(3600f, out var result);
            LogWelcomeMessage(result);
        }

        [ContextMenu("Debug: Reset Last Play Time")]
        public void DebugResetLastPlayTime()
        {
            var saveData = saveManager.GetCurrentSaveData();
            saveData.lastPlayTime = 0;
            saveManager.SaveGame(saveData);
            Debug.Log("[OfflineProgress] Reset!");
        }
    }

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