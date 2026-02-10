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

            // Başlangıçta dolu
            foodFill = 1f;
            waterFill = 1f;
            feedingTimer = 300f; // 5 dakika
            wateringTimer = 300f;
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

        public float pendingMoney; // ✅ YENİ: Stackde bekleyen
        public int pendingCoins;   // ✅ YENİ: Stack'teki coin sayısı
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

        [Header("Unlock Lists")]
        public List<int> unlockedCows = new List<int>();
        public List<int> unlockedAreas = new List<int>();
        /// <summary>
        /// Constructor - Yeni save için initialization
        /// </summary>
        public MilkFarmSaveData()
        {
            currentMoney = 0f;
            pendingMoney = 0f;
            pendingCoins = 0;
            packaging = new PackageSaveData();
            iap = new IAPSaveData();
            lastSaveTimestamp = 0;
            lastPlayTime = 0;

            // ✅ 12 inek initialize et (ilk inek unlocked)
            InitializeCows(12, true); // İlk inek unlocked

            // ✅ 4 istasyon initialize et
            InitializeStations(4);

            unlockedCows = new List<int> { 0, 1, 2 }; // First 3 free
            unlockedAreas = new List<int> { 0 };      // First stable free

            Debug.Log("[MilkFarmSaveData] Yeni save oluşturuldu: 12 inek, 4 istasyon (İlk inek unlocked)");
        }

        /// <summary>
        /// İnekleri initialize et
        /// </summary>
        private void InitializeCows(int count, bool firstUnlocked = true)
        {
            cows = new List<CowSaveData>();

            for (int i = 0; i < count; i++)
            {
                bool unlocked = (i == 0 && firstUnlocked); // İlk inek unlocked
                cows.Add(new CowSaveData(i, unlocked));
            }

            Debug.Log($"[MilkFarmSaveData] {count} inek initialize edildi. İlk inek: {(firstUnlocked ? "UNLOCKED" : "locked")}");
        }

        /// <summary>
        /// İstasyonları initialize et
        /// </summary>
        private void InitializeStations(int count)
        {
            stations = new List<StationSaveData>();

            for (int i = 0; i < count; i++)
            {
                stations.Add(new StationSaveData());
            }

            Debug.Log($"[MilkFarmSaveData] {count} istasyon initialize edildi (dolu)");
        }
    }
}