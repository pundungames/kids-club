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
        public float foodFill; // 0-1 arası
        public float waterFill; // 0-1 arası
        public float feedingTimer; // Boşalma timer'ı
        public float wateringTimer; // Boşalma timer'ı
        public Transform transform;
        public Transform feedTroughTransform;
        public Transform waterTroughTransform;

        public Station(int idx)
        {
            index = idx;
            foodFill = 1f;
            waterFill = 1f;
            feedingTimer = 30f;
            wateringTimer = 30f;
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

        [Header("İstasyon Prefab ve Spawn")]
        [SerializeField] private Transform[] stationSlots; // 3 istasyon

        [Header("Yemlik ve Suluk Referansları")]
        [SerializeField] private Transform[] feedTroughs; // 3 yemlik
        [SerializeField] private Transform[] waterTroughs; // 3 suluk

        [Header("UI Progress Bars (Opsiyonel)")]
        [SerializeField] private GameObject feedProgressBarPrefab;
        [SerializeField] private GameObject waterProgressBarPrefab;

        private List<Station> stations = new List<Station>();

        private void Awake()
        {
            InitializeStations();
        }

        private void Start()
        {
            LoadFromSaveData();
        }

        private void InitializeStations()
        {
            int stationCount = Mathf.CeilToInt((float)config.maxCowSlots / config.cowsPerStation);
            
            for (int i = 0; i < stationCount; i++)
            {
                Station station = new Station(i);
                stations.Add(station);

                if (i < stationSlots.Length)
                {
                    station.transform = stationSlots[i];
                }

                if (i < feedTroughs.Length)
                {
                    station.feedTroughTransform = feedTroughs[i];
                    AddTapHandler(feedTroughs[i].gameObject, i, true);
                }

                if (i < waterTroughs.Length)
                {
                    station.waterTroughTransform = waterTroughs[i];
                    AddTapHandler(waterTroughs[i].gameObject, i, false);
                }
            }

            Debug.Log($"[StationManager] {stations.Count} istasyon başlatıldı.");
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
        /// Save data'dan verileri yükle
        /// </summary>
        public void LoadFromSaveData()
        {
            var saveData = saveManager.GetCurrentSaveData();

            for (int i = 0; i < stations.Count && i < saveData.stations.Count; i++)
            {
                var stationData = saveData.stations[i];
                stations[i].foodFill = stationData.foodFill;
                stations[i].waterFill = stationData.waterFill;
                stations[i].feedingTimer = stationData.feedingTimer;
                stations[i].wateringTimer = stationData.wateringTimer;

                UpdateStationVisuals(i);
            }

            Debug.Log($"[StationManager] {stations.Count} istasyon yüklendi.");
        }

        /// <summary>
        /// Mevcut durumu save data'ya kaydet
        /// </summary>
        public void SaveToData()
        {
            var saveData = saveManager.GetCurrentSaveData();

            for (int i = 0; i < stations.Count; i++)
            {
                saveData.stations[i].foodFill = stations[i].foodFill;
                saveData.stations[i].waterFill = stations[i].waterFill;
                saveData.stations[i].feedingTimer = stations[i].feedingTimer;
                saveData.stations[i].wateringTimer = stations[i].wateringTimer;
            }

            saveManager.SaveGame(saveData);
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
            if (iapManager.HasAutoFeeder())
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
                return;
            }

            // Manuel mod
            for (int i = 0; i < stations.Count; i++)
            {
                Station station = stations[i];

                // Yemlik timer'ı
                if (station.foodFill > 0f)
                {
                    station.feedingTimer -= deltaTime;
                    if (station.feedingTimer <= 0f)
                    {
                        station.foodFill = 0f;
                        station.feedingTimer = 0f;
                        MilkFarmEvents.StationFoodDepleted(i);
                        Debug.Log($"[Station {i}] Yemlik boşaldı!");
                    }
                }

                // Suluk timer'ı
                if (station.waterFill > 0f)
                {
                    station.wateringTimer -= deltaTime;
                    if (station.wateringTimer <= 0f)
                    {
                        station.waterFill = 0f;
                        station.wateringTimer = 0f;
                        MilkFarmEvents.StationWaterDepleted(i);
                        Debug.Log($"[Station {i}] Suluk boşaldı!");
                    }
                }

                UpdateStationVisuals(i);
            }
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

        /// <summary>
        /// Offline progress sonrası istasyonları güncelle
        /// </summary>
        public void ProcessOfflineTime(float offlineSeconds)
        {
            // Auto Feeder varsa hiçbir şey yapma
            if (iapManager.HasAutoFeeder()) return;

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
