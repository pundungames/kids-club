using System;
using System.Collections.Generic;
using UnityEngine;

namespace MilkFarm
{
    /// <summary>
    /// Tek bir ineğin save datası
    /// </summary>
    [Serializable]
    public class CowSaveData
    {
        public bool isUnlocked;
        public int storedMilk;
        public float productionTimer;
        public int globalIndex;
        public int level;

        public CowSaveData(int index, bool unlocked = false)
        {
            globalIndex = index;
            isUnlocked = unlocked;
            storedMilk = 0;
            productionTimer = 0f;
            level = 1;
        }
    }

    /// <summary>
    /// Tek bir istasyonun save datası
    /// </summary>
    [Serializable]
    public class StationSaveData
    {
        public TroughSaveData feedTrough = new TroughSaveData();
        public TroughSaveData waterTrough = new TroughSaveData();
        public float foodFill;
        public float waterFill;
        public float feedingTimer;
        public float wateringTimer;

        public StationSaveData()
        {
            feedTrough = new TroughSaveData();
            waterTrough = new TroughSaveData();

            // ✅ Başlangıçta dolu (1f = %100)
            foodFill = 1f;
            waterFill = 1f;

            // ✅ Timer'ları 0 yap - InitializeWithConfig'de config'den doldurulacak
            feedingTimer = 0f;
            wateringTimer = 0f;
        }

        /// <summary>
        /// Initialize timers from config
        /// Call this after creating new save data
        /// </summary>
        public void InitializeWithConfig(GameConfig config)
        {
            if (config == null) return;

            feedingTimer = config.feedingInterval;   // e.g., 2000f
            wateringTimer = config.wateringInterval;  // e.g., 2000f

            feedTrough.currentAmount = 100f;  // Full
            waterTrough.currentAmount = 100f; // Full
        }
    }
    [Serializable]
    public class TroughSaveData
    {
        public float currentAmount;
        public float maxCapacity;

        public TroughSaveData()
        {
            currentAmount = 100f; // Başlangıç doluluk
            maxCapacity = 100f;
        }
    }

    /// <summary>
    /// Paketleme istasyonu save datası
    /// </summary>
    [Serializable]
    public class PackageSaveData
    {
        public int milkPool; // KULLANILMIYOR (deprecated)
        public int packageCount; // KULLANILMIYOR (deprecated)
        public int capacityLevel;

        // ✅ YENİ: Production stack
        public List<int> productionStackBottles; // Her case'in bottle count'u

        // ✅ YENİ: Sales slots
        public List<int> salesSlotBottles; // Her slot'un bottle count'u (null = -1)

        // ✅ YENİ: Active crate
        public int activeCrateBottles; // Aktif case'in bottle count'u

        public PackageSaveData()
        {
            milkPool = 0;
            packageCount = 0;
            capacityLevel = 1;
            productionStackBottles = new List<int>();
            salesSlotBottles = new List<int> { -1, -1, -1, -1 }; // 4 slot (2x2 grid)
            activeCrateBottles = 0;
        }
    }

    /// <summary>
    /// IAP durumları
    /// </summary>
    [Serializable]
    public class IAPSaveData
    {
        public int gems;
        public List<int> unlockedCows = new List<int>();
        public List<int> unlockedAreas = new List<int>();
        public List<int> unlockedTroughs = new List<int>();

        public bool hasAutoWorker;
        public bool hasAutoFeeder;
        public int speedTier;
        public int richCustomerTier;
        public int milkStorageBoostLevel;

        public IAPSaveData()
        {
            gems = 200; // Başlangıç gem'i
            unlockedCows = new List<int>();
            unlockedAreas = new List<int>();
            unlockedTroughs = new List<int>();
        }
    }

    /// <summary>
    /// Ana save data sınıfı - INITIALIZED
    /// </summary>
    [Serializable]
    public class MilkFarmSaveData
    {
        // Money
        public float currentMoney;
        public float pendingMoney;
        public int pendingCoins;

        // Cows
        public List<CowSaveData> cows = new List<CowSaveData>();

        // Stations (Troughs)
        public List<StationSaveData> stations = new List<StationSaveData>();

        // Packaging
        public PackageSaveData packaging = new PackageSaveData();

        // IAP
        public IAPSaveData iap = new IAPSaveData();

        // Timestamps
        public long lastSaveTimestamp;
        public long lastPlayTime;

        // Unlock Lists
        public List<int> unlockedCows = new List<int>();
        public List<int> unlockedAreas = new List<int>();

        /// <summary>
        /// Constructor - Yeni save için initialization
        /// </summary>
        /// <summary>
        /// Default constructor (for serialization)
        /// </summary>
        public MilkFarmSaveData()
        {
            // Initialize cows
            cows = new List<CowSaveData>();
            for (int i = 0; i < 12; i++)
            {
                CowSaveData cow = new CowSaveData(index: i, i == 0);
                cow.globalIndex = i;
                cow.isUnlocked = (i == 0); // Only cow 0 unlocked
                cow.level = 1;
                cow.storedMilk = 0;
                cow.productionTimer = 0f;
                cows.Add(cow);
            }

            // ✅ Initialize stations WITHOUT config (default values)
            InitializeStations(4);

            // Initialize packaging
            packaging = new PackageSaveData();

            // Initialize IAP
            iap = new IAPSaveData();

            Debug.Log("[MilkFarmSaveData] ✅ New save created: 12 cows (first unlocked), 4 stations");
        }

        /// <summary>
        /// Initialize stations with default values
        /// Config will be applied later in SaveManager.LoadGame()
        /// </summary>
        public void InitializeStations(int stationCount)
        {
            stations = new List<StationSaveData>();

            for (int i = 0; i < stationCount; i++)
            {
                StationSaveData station = new StationSaveData();
                // Default values, will be updated from config later
                stations.Add(station);
            }

            Debug.Log($"[MilkFarmSaveData] {stationCount} station initialized");
        }

        /// <summary>
        /// Apply config values to stations
        /// Called from SaveManager after config is available
        /// </summary>
        public void ApplyConfigToStations(GameConfig config)
        {
            if (config == null) return;

            foreach (var station in stations)
            {
                station.InitializeWithConfig(config);
            }

            Debug.Log($"[MilkFarmSaveData] ✅ Config applied to {stations.Count} stations");
        }
    }
}