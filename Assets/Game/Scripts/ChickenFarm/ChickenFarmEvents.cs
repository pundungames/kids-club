using System;

namespace ChickenFarm
{
    public static class ChickenFarmEvents
    {
        public static event Action<int> OnChickenUnlocked;
        public static void ChickenUnlocked(int globalIndex) => OnChickenUnlocked?.Invoke(globalIndex);

        public static event Action<int> OnChickenEggProduced;
        public static void ChickenEggProduced(int chickenIndex) => OnChickenEggProduced?.Invoke(chickenIndex);

        public static event Action<int, int> OnChickenEggCollected;
        public static void ChickenEggCollected(int chickenIndex, int amount) => OnChickenEggCollected?.Invoke(chickenIndex, amount);

        public static event Action<int, int> OnChickenUpgraded;
        public static void ChickenUpgraded(int globalIndex, int newLevel) => OnChickenUpgraded?.Invoke(globalIndex, newLevel);

        public static event Action<int> OnAreaUnlocked;
        public static void AreaUnlocked(int areaIndex) => OnAreaUnlocked?.Invoke(areaIndex);

        public static event Action<int> OnTroughUnlocked;
        public static void TroughUnlocked(int areaIndex) => OnTroughUnlocked?.Invoke(areaIndex);

        public static event Action<int> OnTroughRefilled;
        public static void TroughRefilled(int stationIndex) => OnTroughRefilled?.Invoke(stationIndex);

        public static event Action<int> OnEggAddedToStation;
        public static void EggAddedToStation(int stationIndex) => OnEggAddedToStation?.Invoke(stationIndex);

        public static event Action<int> OnPackageCreated;
        public static void PackageCreated(int stationIndex) => OnPackageCreated?.Invoke(stationIndex);

        public static event Action<int> OnPackageSold;
        public static void PackageSold(int stationIndex) => OnPackageSold?.Invoke(stationIndex);

        public static event Action OnCustomerSpawned;
        public static void CustomerSpawned() => OnCustomerSpawned?.Invoke();

        public static event Action<float> OnCustomerServed;
        public static void CustomerServed(float payment) => OnCustomerServed?.Invoke(payment);

        public static event Action OnGemChanged;
        public static void GemChanged() => OnGemChanged?.Invoke();

        public static event Action OnSaveRequested;
        public static void SaveRequested() => OnSaveRequested?.Invoke();

        public static event Action OnAutoWorkerTick;
        public static void AutoWorkerTick() => OnAutoWorkerTick?.Invoke();
    }
}