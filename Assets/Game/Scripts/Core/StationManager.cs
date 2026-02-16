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
        /// Tap handler ekle (yemlik veya suluk için)
        /// </summary>
        private void AddTapHandler(GameObject obj, int stationIndex, bool isFeed)
        {
            // Collider yoksa ekle
            if (obj.GetComponent<Collider>() == null)
            {
                BoxCollider collider = obj.AddComponent<BoxCollider>();
                collider.size = new Vector3(1f, 0.5f, 1f);
            }

            EventTrigger trigger = obj.GetComponent<EventTrigger>();
            if (trigger == null) trigger = obj.AddComponent<EventTrigger>();

            EventTrigger.Entry pointerClick = new EventTrigger.Entry();
            pointerClick.eventID = EventTriggerType.PointerClick;
            pointerClick.callback.AddListener((data) =>
            {
                if (isFeed)
                    OnFeedTroughClicked(stationIndex);
                else
                    OnWaterTroughClicked(stationIndex);
            });
            trigger.triggers.Add(pointerClick);
        }

        /// <summary>
        /// Yemliğe tıklama
        /// </summary>
        private void OnFeedTroughClicked(int stationIndex)
        {
            if (stationIndex < 0 || stationIndex >= stations.Count) return;

            Station station = stations[stationIndex];

            // Zaten doluysa işlem yapma
            if (station.foodFill >= 1f) return;

            // Her tap %25 doldurur
            station.foodFill = Mathf.Min(1f, station.foodFill + config.feedingTapFill);
            station.feedingTimer = config.feedingInterval; // Timer'ı sıfırla

            MilkFarmEvents.StationFoodRefilled(stationIndex);
            UpdateStationVisuals(stationIndex);

            Debug.Log($"[Station {stationIndex}] Yemlik dolduruldu! Doluluk: {station.foodFill * 100}%");
        }

        /// <summary>
        /// Suluğa tıklama
        /// </summary>
        private void OnWaterTroughClicked(int stationIndex)
        {
            if (stationIndex < 0 || stationIndex >= stations.Count) return;

            Station station = stations[stationIndex];

            // Zaten doluysa işlem yapma
            if (station.waterFill >= 1f) return;

            // Her tap %25 doldurur
            station.waterFill = Mathf.Min(1f, station.waterFill + config.wateringTapFill);
            station.wateringTimer = config.wateringInterval; // Timer'ı sıfırla

            MilkFarmEvents.StationWaterRefilled(stationIndex);
            UpdateStationVisuals(stationIndex);

            Debug.Log($"[Station {stationIndex}] Suluk dolduruldu! Doluluk: {station.waterFill * 100}%");
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
            var stationsSave = GetStationsSaveData(saveData); // ✅

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

                UpdateStationVisuals(i);
            }

            Debug.Log($"[StationManager] 📂 {stations.Count} istasyon yüklendi (chicken: {isChickenScene})");
        }
        private List<StationSaveData> GetStationsSaveData(MilkFarmSaveData saveData)
        {
            if (isChickenScene)
            {
                if (saveData.chickenStations == null)
                    saveData.chickenStations = new List<StationSaveData>();

                // Listeyi genişlet gerekirse
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
            var stationsSave = GetStationsSaveData(saveData); // ✅

            for (int i = 0; i < stations.Count && i < stationsSave.Count; i++)
            {
                stationsSave[i].foodFill = stations[i].foodFill;
                stationsSave[i].waterFill = stations[i].waterFill;
                stationsSave[i].feedingTimer = stations[i].feedingTimer;
                stationsSave[i].wateringTimer = stations[i].wateringTimer;

                if (stations[i].feedTroughController != null)
                    stations[i].feedTroughController.SaveToData(stationsSave[i].feedTrough);

                if (stations[i].waterTroughController != null)
                    stations[i].waterTroughController.SaveToData(stationsSave[i].waterTrough);
            }

            saveManager.SaveGame(saveData);
            Debug.Log($"[StationManager] 💾 {stations.Count} istasyon kaydedildi (chicken: {isChickenScene})");
        }
        private void Update()
        {
            UpdateAllStations(Time.deltaTime);
        }

        /// <summary>
        /// Tüm istasyonların yem/su timer'larını güncelle
        /// </summary>
        private void UpdateAllStations(float deltaTime)
        {
            // Auto Feeder IAP varsa tüm istasyonları dolu tut
            if (iapManager != null && iapManager.HasAutoFeeder())
            {
                for (int i = 0; i < stations.Count; i++)
                {
                    Station station = stations[i];
                    if (station.foodFill < 1f || station.waterFill < 1f)
                    {
                        station.foodFill = 1f;
                        station.waterFill = 1f;
                        station.feedingTimer = config.feedingInterval;
                        station.wateringTimer = config.wateringInterval;
                        UpdateStationVisuals(i);
                    }
                }
            }

            // NOT: Manuel mod timer'ları CowController'da azaltılıyor
            // Burada sadece Auto Feeder check yapıyoruz
        }
        /// <summary>
        /// İstasyon görsellerini güncelle (progress bar vs)
        /// </summary>
        private void UpdateStationVisuals(int stationIndex)
        {
            // TODO: Progress bar veya yemlik/suluk görsellerini güncelle
            // Örnek:
            // if (feedProgressBars[stationIndex] != null)
            //     feedProgressBars[stationIndex].fillAmount = stations[stationIndex].foodFill;
        }

        // === PUBLIC API ===

        /// <summary>
        /// İstasyonda yem ve su var mı?
        /// </summary>
        public bool HasFoodAndWater(int stationIndex)
        {
            if (stationIndex < 0 || stationIndex >= stations.Count) return false;
            return stations[stationIndex].HasFoodAndWater;
        }

        /// <summary>
        /// İstasyonda yem var mı?
        /// </summary>
        public bool HasFood(int stationIndex)
        {
            if (stationIndex < 0 || stationIndex >= stations.Count) return false;
            return stations[stationIndex].HasFood;
        }

        /// <summary>
        /// İstasyonda su var mı?
        /// </summary>
        public bool HasWater(int stationIndex)
        {
            if (stationIndex < 0 || stationIndex >= stations.Count) return false;
            return stations[stationIndex].HasWater;
        }

        /// <summary>
        /// İstasyon bilgisini al
        /// </summary>
        public Station GetStation(int stationIndex)
        {
            if (stationIndex < 0 || stationIndex >= stations.Count) return null;
            return stations[stationIndex];
        }
        public Station GetStationByIndex(int index)
        {
            if (index < 0 || index >= stations.Count)
                return null;

            return stations[index];
        }

        /// <summary>
        /// Offline progress sonrası istasyonları güncelle
        /// </summary>
        public void ProcessOfflineTime(float offlineSeconds)
        {
            // Auto Feeder varsa hiçbir şey yapma
            if (iapManager != null && iapManager.HasAutoFeeder()) return;

            for (int i = 0; i < stations.Count; i++)
            {
                Station station = stations[i];

                // Yem ve su bitmiş olabilir
                station.feedingTimer -= offlineSeconds;
                station.wateringTimer -= offlineSeconds;

                if (station.feedingTimer <= 0f)
                {
                    station.foodFill = 0f;
                    station.feedingTimer = 0f;
                }

                if (station.wateringTimer <= 0f)
                {
                    station.waterFill = 0f;
                    station.wateringTimer = 0f;
                }

                UpdateStationVisuals(i);
            }

            Debug.Log("[StationManager] Offline süre işlendi, istasyonlar güncellendi.");
        }

        /// <summary>
        /// Debug: Tüm istasyonları doldur
        /// </summary>
        [ContextMenu("Debug: Fill All Stations")]
        public void DebugFillAllStations()
        {
            for (int i = 0; i < stations.Count; i++)
            {
                stations[i].foodFill = 1f;
                stations[i].waterFill = 1f;
                stations[i].feedingTimer = config.feedingInterval;
                stations[i].wateringTimer = config.wateringInterval;
                UpdateStationVisuals(i);
            }
            SaveToData();
            Debug.Log("[StationManager] Tüm istasyonlar dolduruldu!");
        }
    }
}