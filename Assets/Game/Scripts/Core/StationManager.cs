using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// Tek bir istasyonun verisi (Yemlik + Suluk)
    /// </summary>
    [System.Serializable]
    public class Station
    {
        public int index;
        public float foodFill;
        public float waterFill;
        public float feedingTimer;
        public float wateringTimer;

        public Transform transform;
        public TroughController feedTroughController;
        public TroughController waterTroughController;

        public Station(int idx)
        {
            index = idx;
            foodFill = 1f;
            waterFill = 1f;

            // ✅ Timer'ları 0 yap - InitializeStations'da config'den doldurulacak
            feedingTimer = 0f;
            wateringTimer = 0f;
        }

        /// <summary>
        /// Initialize with config values
        /// </summary>
        public void InitializeWithConfig(GameConfig config)
        {
            if (config == null) return;

            feedingTimer = config.feedingInterval;   // e.g., 2000f
            wateringTimer = config.wateringInterval; // e.g., 2000f
        }

        public bool HasFood => foodFill > 0f;
        public bool HasWater => waterFill > 0f;
        public bool HasFoodAndWater => HasFood && HasWater;
    }
    /// <summary>
    /// Yemlik ve Suluk sistemini yöneten manager
    /// GDD v2'ye göre tam implementation
    /// </summary>
    public class StationManager : MonoBehaviour
    {
        [Inject] private GameConfig config;
        [Inject] private SaveManager saveManager;
        [Inject] private IAPManager iapManager;
        [SerializeField] private bool isChickenScene = false;

        [Header("İstasyon Prefab ve Spawn")]
        [SerializeField] private Transform[] stationSlots; // 3 istasyon

        [Header("Yemlik ve Suluk Referansları")]
        [SerializeField] private TroughController[] feedTroughs; // 3 yemlik
        [SerializeField] private TroughController[] waterTroughs; // 3 suluk

        [Header("UI Progress Bars (Opsiyonel)")]
        [SerializeField] private GameObject feedProgressBarPrefab;
        [SerializeField] private GameObject waterProgressBarPrefab;

        internal List<Station> stations = new List<Station>();

        private void Awake()
        {
            InitializeStations();
        }

        private void Start()
        {
            LoadFromSaveData();
        }
        private void OnEnable()
        {
            MilkFarmEvents.OnSaveRequested += HandleSaveRequested;
        }

        private void OnDisable()
        {
            MilkFarmEvents.OnSaveRequested -= HandleSaveRequested;
        }
        private void HandleSaveRequested()
        {
            SaveToData();
        }
        private void InitializeStations()
        {
            int maxSlots, perStation;

            if (isChickenScene)
            {
                maxSlots = config.maxChickenSlots;       // 12
                perStation = config.chickensPerStation;   // 3
            }
            else
            {
                maxSlots = config.maxCowSlots;            // 12
                perStation = config.cowsPerStation;        // 4
            }

            int stationCount = Mathf.CeilToInt((float)maxSlots / perStation);

            Debug.Log($"[StationManager] İstasyon sayısı: {stationCount} (chicken: {isChickenScene})");

            for (int i = 0; i < stationCount; i++)
            {
                Station station = new Station(i);
                station.InitializeWithConfig(config);
                stations.Add(station);

                if (i < stationSlots.Length)
                    station.transform = stationSlots[i];

                if (feedTroughs != null && i < feedTroughs.Length && feedTroughs[i] != null)
                {
                    station.feedTroughController = feedTroughs[i];
                    station.feedTroughController.Initialize(i, true, config, iapManager);
                }

                if (waterTroughs != null && i < waterTroughs.Length && waterTroughs[i] != null)
                {
                    station.waterTroughController = waterTroughs[i];
                    station.waterTroughController.Initialize(i, false, config, iapManager);
                }
            }
        }

        /// <summary>
        /// İstasyonun yemlik controller'ını al
        /// </summary>
        public TroughController GetFeedTrough(int stationIndex)
        {
            if (stationIndex < 0 || stationIndex >= stations.Count) return null;
            return stations[stationIndex].feedTroughController;
        }

        /// <summary>
        /// İstasyonun suluk controller'ını al
        /// </summary>
        public TroughController GetWaterTrough(int stationIndex)
        {
            if (stationIndex < 0 || stationIndex >= stations.Count) return null;
            return stations[stationIndex].waterTroughController;
        }
        /// <summary>
        /// Save data'dan verileri yükle
        /// </summary>
        public void LoadFromSaveData()
        {
            var saveData = saveManager.GetCurrentSaveData();
            var stationsSave = GetStationsSaveData(saveData);

            for (int i = 0; i < stations.Count && i < stationsSave.Count; i++)
            {
                var stationData = stationsSave[i];

                stations[i].foodFill = stationData.foodFill;
                stations[i].waterFill = stationData.waterFill;
                stations[i].feedingTimer = stationData.feedingTimer;
                stations[i].wateringTimer = stationData.wateringTimer;

                if (stations[i].feedTroughController != null)
                    stations[i].feedTroughController.LoadFromSaveData(stationData.feedTrough);

                if (stations[i].waterTroughController != null)
                    stations[i].waterTroughController.LoadFromSaveData(stationData.waterTrough);

            }

            Debug.Log($"[StationManager] 📂 {stations.Count} istasyon yüklendi (chicken: {isChickenScene})");
        }
        private List<StationSaveData> GetStationsSaveData(MilkFarmSaveData saveData)
        {
            if (isChickenScene)
            {
                if (saveData.chickenStations == null)
                    saveData.chickenStations = new List<StationSaveData>();
                while (saveData.chickenStations.Count < stations.Count)
                    saveData.chickenStations.Add(new StationSaveData());
                return saveData.chickenStations;
            }
            return saveData.stations;
        }

        /// <summary>
        /// Mevcut durumu save data'ya kaydet
        /// </summary>
        public void SaveToData()
        {
            var saveData = saveManager.GetCurrentSaveData();
            var stationsSave = GetStationsSaveData(saveData);

            for (int i = 0; i < stations.Count && i < stationsSave.Count; i++)
            {
                // ✅ TroughController'dan GÜNCEL değeri al
                if (stations[i].feedTroughController != null)
                {
                    stationsSave[i].foodFill = stations[i].feedTroughController.GetFillAmount();
                    stations[i].feedTroughController.SaveToData(stationsSave[i].feedTrough);
                }

                if (stations[i].waterTroughController != null)
                {
                    stationsSave[i].waterFill = stations[i].waterTroughController.GetFillAmount();
                    stations[i].waterTroughController.SaveToData(stationsSave[i].waterTrough);
                }

                stationsSave[i].feedingTimer = stations[i].feedingTimer;
                stationsSave[i].wateringTimer = stations[i].wateringTimer;
            }

            saveManager.SaveGame(saveData);
        }
        /// <summary>
        /// İstasyon bilgisini al
        /// </summary>
        public Station GetStation(int stationIndex)
        {
            if (stationIndex < 0 || stationIndex >= stations.Count) return null;
            return stations[stationIndex];
        }
    }
}