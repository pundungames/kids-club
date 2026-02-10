using UnityEngine;
using System;
using Zenject;

namespace MilkFarm
{
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
        [SerializeField] private TMPro.TextMeshProUGUI warningText;

        private void Start()
        {
            CalculateOfflineProgress();
        }

        public void CalculateOfflineProgress()
        {
            var saveData = saveManager.GetCurrentSaveData();

            if (saveData.lastPlayTime == 0)
            {
                Debug.Log("[OfflineProgress] İlk oyun");
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

            if (deltaSeconds < 60f)
            {
                SaveCurrentTime();
                return;
            }

            Debug.Log($"[OfflineProgress] 🎯 Processing {deltaSeconds / 60f:F1} minutes");

            ProcessOfflineProgress(deltaSeconds, out OfflineProgressResult result);
            SaveCurrentTime();

            if (showWelcomePopup && result.totalMilkProduced > 0)
            {
                ShowWelcomePopup(result);
            }

            Debug.Log($"[OfflineProgress] ✅ Milk: {result.totalMilkProduced}");
        }

        private bool ValidateTimeDelta(float deltaSeconds, out float validatedSeconds)
        {
            validatedSeconds = deltaSeconds;

            if (deltaSeconds < 0)
            {
                Debug.LogWarning("[OfflineProgress] Negative time!");
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
            var cows = cowManager.GetAllCows();

            for (int i = 0; i < cows.Count; i++)
            {
                var cow = cows[i];
                if (!cow.isUnlocked) continue;

                var cowSaveData = saveData.cows[i];
                int stationIndex = i / config.cowsPerStation;

                var stationData = saveData.stations[stationIndex];
                var feedTrough = stationData.feedTrough;
                var waterTrough = stationData.waterTrough;

                // ✅ Trough currentAmount'u 0-100 range'inde (fill değil!)
                float feedAmount = feedTrough.currentAmount; // 0-100
                float waterAmount = waterTrough.currentAmount; // 0-100

                float troughLifetime = CalculateTroughLifetime(feedAmount, waterAmount, stationIndex, saveData);
                float effectiveTime = Mathf.Min(deltaSeconds, troughLifetime);

                if (effectiveTime <= 0) continue;

                // Production
                // Production calculation
                float productionTime = config.baseProductionTime; // 30s

                // ✅ productionTimer = KALAN süre!
                // Örnek: timer 10s kaldı (remaining)
                // Offline: 600s geçti
                // Total: 10s + 600s = 610s
                // Cycles: 610 / 30 = 20.33 → 20 cycles
                float totalTime = cowSaveData.productionTimer + effectiveTime;
                int cycles = Mathf.FloorToInt(totalTime / productionTime);
                float remainingTimer = totalTime % productionTime;

                int currentMilk = cowSaveData.storedMilk;
                int maxMilk = config.maxMilkStack; // 6
                int milkProduced = Mathf.Min(cycles, maxMilk - currentMilk);

                if (milkProduced > 0)
                {
                    cowSaveData.storedMilk += milkProduced;
                    result.totalMilkProduced += milkProduced;
                    Debug.Log($"[OfflineProgress] Cow {i}: +{milkProduced} milk (cycles: {cycles}, capped by stack)");
                }

                // ✅ Yeni timer = kalan süre
                cowSaveData.productionTimer = remainingTimer;

                Debug.Log($"[OfflineProgress] Cow {i}: Timer {cowSaveData.productionTimer:F1}s → {remainingTimer:F1}s");

                cowSaveData.productionTimer = remainingTimer;

                // Consumption (0-100 range'inde)
                float feedConsumed = CalculateConsumption(effectiveTime, stationIndex, saveData);
                float waterConsumed = CalculateConsumption(effectiveTime, stationIndex, saveData);

                feedTrough.currentAmount = Mathf.Max(0, feedAmount - feedConsumed);
                waterTrough.currentAmount = Mathf.Max(0, waterAmount - waterConsumed);

                Debug.Log($"[OfflineProgress] Cow {i}: Feed {feedAmount:F1} → {feedTrough.currentAmount:F1}, Water {waterAmount:F1} → {waterTrough.currentAmount:F1}");
            }

            result.feedEmpty = CheckIfAnyTroughEmpty(saveData, true);
            result.waterEmpty = CheckIfAnyTroughEmpty(saveData, false);

            saveManager.SaveGame(saveData);
        }

        private float CalculateTroughLifetime(float feedAmount, float waterAmount, int stationIndex, MilkFarmSaveData saveData)
        {
            int activeCowCount = GetActiveCowCountForStation(stationIndex, saveData);
            if (activeCowCount == 0) return float.MaxValue;

            // Consumption per second per cow (0-100 range)
            float consumptionPerCycle = config.baseTroughConsumption; // 1.0
            float consumptionPerSecond = consumptionPerCycle / config.baseProductionTime; // 1/30 = 0.033
            float totalRate = consumptionPerSecond * activeCowCount;

            float feedLifetime = totalRate > 0 ? feedAmount / totalRate : float.MaxValue;
            float waterLifetime = totalRate > 0 ? waterAmount / totalRate : float.MaxValue;

            return Mathf.Min(feedLifetime, waterLifetime);
        }

        private float CalculateConsumption(float deltaSeconds, int stationIndex, MilkFarmSaveData saveData)
        {
            int activeCowCount = GetActiveCowCountForStation(stationIndex, saveData);
            float consumptionPerSecond = config.baseTroughConsumption / config.baseProductionTime;
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

            if (warningText != null)
            {
                string warning = "";
                if (result.feedEmpty) warning += "⚠️ Saman bitti!\n";
                if (result.waterEmpty) warning += "⚠️ Su bitti!\n";
                warningText.text = warning;
                warningText.gameObject.SetActive(!string.IsNullOrEmpty(warning));
            }

            welcomePopup.SetActive(true);
        }

        private void LogWelcomeMessage(OfflineProgressResult result)
        {
            Debug.Log("========================================");
            Debug.Log("🎉 HOŞ GELDİN!");
            Debug.Log($"⏰ {FormatDuration(result.deltaTime)}");
            Debug.Log($"🥛 {result.totalMilkProduced} süt");
            if (result.feedEmpty) Debug.Log("⚠️ Saman bitti!");
            if (result.waterEmpty) Debug.Log("⚠️ Su bitti!");
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
            if (seconds < 60) return $"{Mathf.FloorToInt(seconds)} saniye";
            if (seconds < 3600) return $"{Mathf.FloorToInt(seconds / 60f)} dakika";
            if (seconds < 86400) return $"{(seconds / 3600f):F1} saat";
            return $"{(seconds / 86400f):F1} gün";
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
        public float effectiveDuration;
        public int totalMilkProduced;
        public bool feedEmpty;
        public bool waterEmpty;
    }
}
