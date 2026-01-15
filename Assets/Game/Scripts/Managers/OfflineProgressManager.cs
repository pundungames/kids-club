using System;
using UnityEngine;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// Offline süt üretimini hesaplayan manager
    /// GDD v2'ye göre tam implementation
    /// Offline'da her inek en fazla kendi kapasitesi kadar süt üretebilir
    /// </summary>
    public class OfflineProgressManager : MonoBehaviour
    {
        [Inject] private GameConfig config;
        [Inject] private SaveManager saveManager;
        [Inject] private CowManager cowManager;
        [Inject] private StationManager stationManager;
        [Inject] private IAPManager iapManager;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private void Start()
        {
            CalculateOfflineProgress();
        }

        /// <summary>
        /// Offline progress hesapla ve uygula
        /// </summary>
        public void CalculateOfflineProgress()
        {
            var saveData = saveManager.GetCurrentSaveData();

            // Son kayıt zamanını al
            long lastSaveTimestamp = saveData.lastSaveTimestamp;
            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Offline süreyi hesapla
            float offlineSeconds = currentTimestamp - lastSaveTimestamp;

            // Max limit kontrolü
            offlineSeconds = Mathf.Min(offlineSeconds, config.maxOfflineTime);

            if (offlineSeconds < 1f)
            {
                Debug.Log("[OfflineProgressManager] Offline süre yok veya çok kısa.");
                return;
            }

            LogDebug($"Offline süre: {offlineSeconds} saniye ({offlineSeconds / 60f:F1} dakika)");

            // İstasyonları offline süre kadar işle
            stationManager.ProcessOfflineTime(offlineSeconds);

            // Her inek için offline üretim hesapla
            var cows = cowManager.GetAllCows();
            int totalOfflineMilk = 0;

            for (int i = 0; i < cows.Count; i++)
            {
                var cow = cows[i];
                if (!cow.isUnlocked) continue;

                // İstasyon kontrolü
                int stationIndex = i / config.cowsPerStation;
                
                // Auto Feeder yoksa ve istasyonda yem/su bittiyse üretim yok
                if (!iapManager.HasAutoFeeder())
                {
                    if (!stationManager.HasFoodAndWater(stationIndex))
                    {
                        LogDebug($"İnek {i}: İstasyon {stationIndex} kaynakları yok, offline üretim yok.");
                        continue;
                    }
                }

                // Bu inek için üretim süresini hesapla
                float productionTime = CalculateOfflineCowProductionTime(cow);
                
                // Offline sürede kaç süt üretebilir?
                int potentialMilk = Mathf.FloorToInt(offlineSeconds / productionTime);
                
                // Storage limit kontrolü (GDD: en fazla envanter kadar)
                int storageLimit = GetMilkStorageLimit();
                int actualMilk = Mathf.Min(potentialMilk, storageLimit);

                if (actualMilk > 0)
                {
                    cowManager.ProduceOfflineMilk(i, actualMilk);
                    totalOfflineMilk += actualMilk;
                    LogDebug($"İnek {i}: {actualMilk} süt üretildi (potansiyel: {potentialMilk}, limit: {storageLimit})");
                }
            }

            MilkFarmEvents.OfflineProgressCalculated(offlineSeconds);

            Debug.Log($"[OfflineProgressManager] Offline üretim tamamlandı! Toplam: {totalOfflineMilk} süt");

            // UI bildirimi gösterilebilir
            ShowOfflineRewardUI(offlineSeconds, totalOfflineMilk);
        }

        /// <summary>
        /// Offline için inek üretim süresini hesapla
        /// Not: Tap & Hold offline'da yok, sadece base + level + IAP
        /// </summary>
        private float CalculateOfflineCowProductionTime(Cow cow)
        {
            // Base süre
            float baseTime = config.baseMilkProductionTime;

            // Level bonus: Her level %10 hızlandırma
            float levelMultiplier = Mathf.Pow(0.9f, cow.level - 1);
            float leveledTime = baseTime * levelMultiplier;

            // Minimum limit
            leveledTime = Mathf.Max(config.minProductionTime, leveledTime);

            // IAP global speed boost
            leveledTime *= iapManager.GetGlobalSpeedMultiplier();

            return leveledTime;
        }

        /// <summary>
        /// Süt depolama limiti (IAP'e göre)
        /// </summary>
        private int GetMilkStorageLimit()
        {
            int baseLimit = config.baseMilkStorageLimit;
            baseLimit += iapManager.GetMilkStorageBoost();
            return baseLimit;
        }

        /// <summary>
        /// Offline ödül UI'sini göster
        /// </summary>
        private void ShowOfflineRewardUI(float offlineSeconds, int totalMilk)
        {
            // TODO: UI panel göster
            // Örnek: OfflineRewardPanel.Show(offlineSeconds, totalMilk);
            
            string timeText = FormatTime(offlineSeconds);
            Debug.Log($"=== OFFLİNE ÖDÜL ===\nSüre: {timeText}\nÜretilen Süt: {totalMilk}");
        }

        /// <summary>
        /// Zamanı formatla (1s, 5m, 2h, 1d vb)
        /// </summary>
        private string FormatTime(float seconds)
        {
            if (seconds < 60f)
                return $"{seconds:F0}s";
            else if (seconds < 3600f)
                return $"{seconds / 60f:F0}m";
            else if (seconds < 86400f)
                return $"{seconds / 3600f:F1}h";
            else
                return $"{seconds / 86400f:F1}d";
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[OfflineProgressManager] {message}");
            }
        }

        /// <summary>
        /// Debug: Offline süreyi simüle et
        /// </summary>
        [ContextMenu("Debug: Simulate 1 Hour Offline")]
        public void DebugSimulate1HourOffline()
        {
            var saveData = saveManager.GetCurrentSaveData();
            saveData.lastSaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 3600; // 1 saat önce
            saveManager.SaveGame(saveData);
            CalculateOfflineProgress();
        }

        [ContextMenu("Debug: Simulate 8 Hours Offline")]
        public void DebugSimulate8HoursOffline()
        {
            var saveData = saveManager.GetCurrentSaveData();
            saveData.lastSaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - (3600 * 8); // 8 saat önce
            saveManager.SaveGame(saveData);
            CalculateOfflineProgress();
        }
    }
}
