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
        public int level;
        public int storedMilk;
        public float productionTimer;

        public CowSaveData()
        {
            isUnlocked = false;
            level = 1;
            storedMilk = 0;
            productionTimer = 0f;
        }
    }

    /// <summary>
    /// Tek bir istasyonun save datası
    /// </summary>
    [Serializable]
    public class StationSaveData
    {
        public float foodFill;
        public float waterFill;
        public float feedingTimer;
        public float wateringTimer;

        public StationSaveData()
        {
            foodFill = 1f;
            waterFill = 1f;
            feedingTimer = 30f;
            wateringTimer = 30f;
        }
    }

    /// <summary>
    /// Paketleme istasyonu save datası
    /// </summary>
    [Serializable]
    public class PackageSaveData
    {
        public int milkPool;
        public int packageCount;
        public int capacityLevel;

        public PackageSaveData()
        {
            milkPool = 0;
            packageCount = 0;
            capacityLevel = 1;
        }
    }

    /// <summary>
    /// IAP durumları
    /// </summary>
    [Serializable]
    public class IAPSaveData
    {
        public bool hasAutoFeeder;
        public bool hasAutoWorker;
        public int speedTier; // 0=normal, 1=+50%, 2=+100%
        public int richCustomerTier; // 0=normal, 1=+50%, 2=+100%
        public int milkStorageBoostLevel; // 0=+0, 1=+1, 2=+2...

        public IAPSaveData()
        {
            hasAutoFeeder = false;
            hasAutoWorker = false;
            speedTier = 0;
            richCustomerTier = 0;
            milkStorageBoostLevel = 0;
        }
    }

    /// <summary>
    /// Ana save data sınıfı
    /// </summary>
    [Serializable]
    public class MilkFarmSaveData
    {
        // Versiyon bilgisi
        public int saveVersion = 1;

        // Temel bilgiler
        public float currentMoney;
        public long lastSaveTimestamp;

        // İnekler
        public List<CowSaveData> cows;

        // İstasyonlar
        public List<StationSaveData> stations;

        // Paketleme
        public PackageSaveData packaging;

        // IAP durumları
        public IAPSaveData iap;

        public MilkFarmSaveData()
        {
            saveVersion = 1;
            currentMoney = 0f;
            lastSaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // 12 inek
            cows = new List<CowSaveData>();
            for (int i = 0; i < 12; i++)
            {
                cows.Add(new CowSaveData());
            }

            // İlk inek açık
            cows[0].isUnlocked = true;

            // 3 istasyon
            stations = new List<StationSaveData>();
            for (int i = 0; i < 3; i++)
            {
                stations.Add(new StationSaveData());
            }

            packaging = new PackageSaveData();
            iap = new IAPSaveData();
        }
    }
}
