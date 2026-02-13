using System;
using System.Collections.Generic;
using UnityEngine;

namespace MilkFarm
{
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
            foodFill = 1f;
            waterFill = 1f;
            feedingTimer = 0f;
            wateringTimer = 0f;
        }

        public void InitializeWithConfig(GameConfig config)
        {
            if (config == null) return;
            feedingTimer = config.feedingInterval;
            wateringTimer = config.wateringInterval;
            feedTrough.currentAmount = 100f;
            waterTrough.currentAmount = 100f;
        }
    }

    [Serializable]
    public class TroughSaveData
    {
        public float currentAmount;
        public float maxCapacity;

        public TroughSaveData()
        {
            currentAmount = 100f;
            maxCapacity = 100f;
        }
    }

    [Serializable]
    public class PackageSaveData
    {
        public int milkPool;
        public int packageCount;
        public int capacityLevel;
        public List<int> productionStackBottles;
        public List<int> salesSlotBottles;
        public int activeCrateBottles;

        public PackageSaveData()
        {
            milkPool = 0;
            packageCount = 0;
            capacityLevel = 1;
            productionStackBottles = new List<int>();
            salesSlotBottles = new List<int> { -1, -1, -1, -1 };
            activeCrateBottles = 0;
        }
    }

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

        // ✅ Chicken unlock listesi
        public List<int> unlockedChickens = new List<int>();
        public List<int> unlockedChickenAreas = new List<int>();

        public IAPSaveData()
        {
            gems = 200;
            unlockedCows = new List<int>();
            unlockedAreas = new List<int>();
            unlockedTroughs = new List<int>();
            unlockedChickens = new List<int>();
            unlockedChickenAreas = new List<int>();
        }
    }

    // ✅ Tavuk save data (CowSaveData ile aynı yapı)
    [Serializable]
    public class ChickenSaveData
    {
        public bool isUnlocked;
        public int storedEggs;
        public float productionTimer;
        public int globalIndex;
        public int level;

        public ChickenSaveData(int index, bool unlocked = false)
        {
            globalIndex = index;
            isUnlocked = unlocked;
            storedEggs = 0;
            productionTimer = 0f;
            level = 1;
        }
    }

    [Serializable]
    public class MilkFarmSaveData
    {
        // Money
        public float currentMoney;
        public float pendingMoney;
        public int pendingCoins;

        // Cows
        public List<CowSaveData> cows = new List<CowSaveData>();

        // ✅ Chickens (YENİ - ayrı save)
        public List<ChickenSaveData> chickens = new List<ChickenSaveData>();

        // Stations (Troughs)
        public List<StationSaveData> stations = new List<StationSaveData>();

        // Packaging
        public PackageSaveData packaging = new PackageSaveData();

        // ✅ Chicken Packaging (YENİ - ayrı save)
        public PackageSaveData chickenPackaging = new PackageSaveData();

        // IAP
        public IAPSaveData iap = new IAPSaveData();

        // Timestamps
        public long lastSaveTimestamp;
        public long lastPlayTime;

        // Unlock Lists
        public List<int> unlockedCows = new List<int>();
        public List<int> unlockedAreas = new List<int>();
        public List<int> unlockedChickens = new List<int>();
        public List<int> unlockedChickenAreas = new List<int>();

        public MilkFarmSaveData()
        {
            // Initialize cows
            cows = new List<CowSaveData>();
            for (int i = 0; i < 12; i++)
            {
                CowSaveData cow = new CowSaveData(index: i, i == 0);
                cow.globalIndex = i;
                cow.isUnlocked = (i == 0);
                cow.level = 1;
                cow.storedMilk = 0;
                cow.productionTimer = 0f;
                cows.Add(cow);
            }

            // ✅ Initialize chickens
            chickens = new List<ChickenSaveData>();
            for (int i = 0; i < 12; i++)
            {
                ChickenSaveData chicken = new ChickenSaveData(index: i, i == 0);
                chicken.globalIndex = i;
                chicken.isUnlocked = (i == 0);
                chicken.level = 1;
                chicken.storedEggs = 0;
                chicken.productionTimer = 0f;
                chickens.Add(chicken);
            }

            InitializeStations(4);
            packaging = new PackageSaveData();
            chickenPackaging = new PackageSaveData();
            iap = new IAPSaveData();

            Debug.Log("[MilkFarmSaveData] ✅ New save: 12 cows + 12 chickens (first unlocked), 4 stations");
        }

        public void InitializeStations(int stationCount)
        {
            stations = new List<StationSaveData>();
            for (int i = 0; i < stationCount; i++)
                stations.Add(new StationSaveData());
        }

        public void ApplyConfigToStations(GameConfig config)
        {
            if (config == null) return;
            foreach (var station in stations)
                station.InitializeWithConfig(config);
        }
    }
}