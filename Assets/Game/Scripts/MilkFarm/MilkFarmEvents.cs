using System;
using UnityEngine;

namespace MilkFarm
{
    /// <summary>
    /// Oyun içi tüm eventleri yöneten static sınıf
    /// </summary>
    public static class MilkFarmEvents
    {
        // === İNEK EVENTLERİ ===
        public static event Action<int> OnCowUnlocked;
        public static void CowUnlocked(int cowIndex) => OnCowUnlocked?.Invoke(cowIndex);

        public static event Action<int, int> OnCowLevelUp;
        public static void CowLevelUp(int cowIndex, int newLevel) => OnCowLevelUp?.Invoke(cowIndex, newLevel);

        public static event Action<int> OnCowMilkProduced;
        public static void CowMilkProduced(int cowIndex) => OnCowMilkProduced?.Invoke(cowIndex);

        public static event Action<int, int> OnCowMilkCollected;
        public static void CowMilkCollected(int cowIndex, int amount) => OnCowMilkCollected?.Invoke(cowIndex, amount);

        // === İSTASYON EVENTLERİ ===
        public static event Action<int> OnStationFoodDepleted;
        public static void StationFoodDepleted(int stationIndex) => OnStationFoodDepleted?.Invoke(stationIndex);

        public static event Action<int> OnStationWaterDepleted;
        public static void StationWaterDepleted(int stationIndex) => OnStationWaterDepleted?.Invoke(stationIndex);

        public static event Action<int> OnStationFoodRefilled;
        public static void StationFoodRefilled(int stationIndex) => OnStationFoodRefilled?.Invoke(stationIndex);

        public static event Action<int> OnStationWaterRefilled;
        public static void StationWaterRefilled(int stationIndex) => OnStationWaterRefilled?.Invoke(stationIndex);

        // === PAKETLEME EVENTLERİ ===
        public static event Action<int> OnMilkAddedToStation;
        public static void MilkAddedToStation(int milkCount) => OnMilkAddedToStation?.Invoke(milkCount);

        public static event Action<int> OnPackageCreated;
        public static void PackageCreated(int packageCount) => OnPackageCreated?.Invoke(packageCount);

        public static event Action<int> OnPackageSold;
        public static void PackageSold(int packageCount) => OnPackageSold?.Invoke(packageCount);

        public static event Action OnPackageStationFull;
        public static void PackageStationFull() => OnPackageStationFull?.Invoke();

        // === MÜŞTERİ EVENTLERİ ===
        public static event Action OnCustomerSpawned;
        public static void CustomerSpawned() => OnCustomerSpawned?.Invoke();

        public static event Action<float> OnCustomerServed;
        public static void CustomerServed(float payment) => OnCustomerServed?.Invoke(payment);

        public static event Action OnQueueFull;
        public static void QueueFull() => OnQueueFull?.Invoke();

        // === PARA EVENTLERİ ===
        public static event Action<float> OnMoneyEarned;
        public static void MoneyEarned(float amount) => OnMoneyEarned?.Invoke(amount);

        public static event Action<float> OnMoneyCollected;
        public static void MoneyCollected(float amount) => OnMoneyCollected?.Invoke(amount);

        public static event Action<float> OnMoneySpent;
        public static void MoneySpent(float amount) => OnMoneySpent?.Invoke(amount);

        // === UPGRADE EVENTLERİ ===
        public static event Action<string, int, float> OnUpgradePurchased;
        public static void UpgradePurchased(string upgradeType, int level, float cost) => OnUpgradePurchased?.Invoke(upgradeType, level, cost);

        // === IAP EVENTLERİ ===
        public static event Action<string> OnIAPPurchased;
        public static void IAPPurchased(string iapId) => OnIAPPurchased?.Invoke(iapId);

        // === OFFLİNE EVENTLERİ ===
        public static event Action<float> OnOfflineProgressCalculated;
        public static void OfflineProgressCalculated(float offlineSeconds) => OnOfflineProgressCalculated?.Invoke(offlineSeconds);

        // === AUTO WORKER EVENTLERİ ===
        public static event Action OnAutoWorkerTick;
        public static void AutoWorkerTick() => OnAutoWorkerTick?.Invoke();
    }
}
