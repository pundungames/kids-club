using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// Tek bir ineğin verisi
    /// </summary>
    [System.Serializable]
    public class Cow
    {
        public int index;
        public bool isUnlocked;
        public int level;
        public int currentMilk;
        public float productionTimer;
        public bool isBoosted;

        // Visual referanslar
        public Transform transform;
        public CowController controller;
        public GameObject milkIndicator;

        public Cow(int idx)
        {
            index = idx;
            isUnlocked = false;
            level = 1;
            currentMilk = 0;
            productionTimer = 0f;
            isBoosted = false;
        }
    }

    /// <summary>
    /// Tüm inekleri yöneten merkezi manager
    /// Mevcut CowController sistemi ile entegre edildi
    /// </summary>
    public class CowManager : MonoBehaviour
    {
        [Inject] DiContainer container;
        [Inject] private GameConfig config;
        [Inject] private SaveManager saveManager;
        [Inject] private IAPManager iapManager;

        [Header("Referanslar")]
        [SerializeField] private StationManager stationManager;
        [SerializeField] private PackageManager packageManager;

        [Header("İnek Prefab ve Spawn")]
        [SerializeField] private GameObject cowPrefab;
        [SerializeField] private Transform[] cowSlots; // 12 slot

        [Header("UI Elemanları")]
        [SerializeField] private GameObject milkIndicatorPrefab;

        private List<Cow> cows = new List<Cow>();

        private void Awake()
        {
            InitializeCows();
        }

        private void Start()
        {
            LoadFromSaveData();
        }

        private void InitializeCows()
        {
            for (int i = 0; i < config.maxCowSlots; i++)
            {
                Cow cow = new Cow(i);
                cows.Add(cow);
            }
        }

        /// <summary>
        /// Save data'dan verileri yükle
        /// </summary>
        public void LoadFromSaveData()
        {
            var saveData = saveManager.GetCurrentSaveData();

            for (int i = 0; i < cows.Count && i < saveData.cows.Count; i++)
            {
                var cowData = saveData.cows[i];
                cows[i].isUnlocked = cowData.isUnlocked;
                cows[i].level = cowData.level;
                cows[i].currentMilk = cowData.storedMilk;
                cows[i].productionTimer = cowData.productionTimer;

                if (cows[i].isUnlocked && cowSlots[i] != null)
                {
                    SpawnCow(i);
                }
            }

            Debug.Log($"[CowManager] {cows.FindAll(c => c.isUnlocked).Count} inek yüklendi.");
        }

        /// <summary>
        /// Mevcut durumu save data'ya kaydet
        /// </summary>
        public void SaveToData()
        {
            var saveData = saveManager.GetCurrentSaveData();

            for (int i = 0; i < cows.Count; i++)
            {
                saveData.cows[i].isUnlocked = cows[i].isUnlocked;
                saveData.cows[i].level = cows[i].level;
                saveData.cows[i].storedMilk = cows[i].currentMilk;
                saveData.cows[i].productionTimer = cows[i].productionTimer;
            }

            saveManager.SaveGame(saveData);
        }

        /// <summary>
        /// İnek spawn etme - CowController ile entegre
        /// </summary>
        private void SpawnCow(int index)
        {
            if (cowPrefab == null || cowSlots[index] == null) return;

            GameObject cowObj = Instantiate(cowPrefab, cowSlots[index].position, cowSlots[index].rotation, cowSlots[index]);
            container.InjectGameObject(cowObj);
            cows[index].transform = cowObj.transform;

            // CowController'ı al
            CowController controller = cowObj.GetComponent<CowController>();
            if (controller != null)
            {
                cows[index].controller = controller;

                // İstasyon indexine göre yemlik/suluk referanslarını al
                int stationIndex = index / config.cowsPerStation;
                TroughController feedTrough = stationManager.GetFeedTrough(stationIndex);
                TroughController waterTrough = stationManager.GetWaterTrough(stationIndex);

                // Public SetTrough methodları ile ata (CowController'a ekleyeceğiz)
                controller.SetFeedTrough(feedTrough);
                controller.SetWaterTrough(waterTrough);
                controller.SetPackageManager(packageManager);

                // Initialize et
                controller.Initialize(index, cows[index], config, iapManager);

                Debug.Log($"[CowManager] İnek {index} spawn edildi. İstasyon: {stationIndex}, FeedTrough: {feedTrough != null}, WaterTrough: {waterTrough != null}");
            }
            else
            {
                Debug.LogError($"[CowManager] İnek prefab'ında CowController component'i bulunamadı!");
            }

            // Süt indikatörü oluştur
            if (milkIndicatorPrefab != null)
            {
                GameObject indicator = Instantiate(milkIndicatorPrefab, cowObj.transform);
                cows[index].milkIndicator = indicator;
                indicator.SetActive(false);
            }
        }

        /// <summary>
        /// Update döngüsü - CowController üretimi yönettiği için burada sadece monitoring
        /// </summary>
        private void Update()
        {
            // İnek durumlarını monitoring et
            for (int i = 0; i < cows.Count; i++)
            {
                Cow cow = cows[i];
                if (!cow.isUnlocked || cow.controller == null) continue;

                // Controller'dan mevcut süt miktarını sync et
                cow.currentMilk = cow.controller.GetCurrentMilk();

                // Storage limit kontrolü
                int storageLimit = GetMilkStorageLimit();
                if (cow.currentMilk >= storageLimit)
                {
                    UpdateCowMilkIndicator(i);
                }
            }
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
        /// İnek süt indikatörünü güncelle
        /// </summary>
        private void UpdateCowMilkIndicator(int index)
        {
            Cow cow = cows[index];
            if (cow.milkIndicator != null)
            {
                bool shouldShow = cow.currentMilk > 0;
                cow.milkIndicator.SetActive(shouldShow);
            }
        }

        // === PUBLIC API ===

        /// <summary>
        /// İnek açma (IAP ile)
        /// </summary>
        public bool UnlockCow(int index)
        {
            if (index < 0 || index >= cows.Count) return false;
            if (cows[index].isUnlocked) return false;

            cows[index].isUnlocked = true;
            cows[index].level = 1;
            cows[index].currentMilk = 0;
            cows[index].productionTimer = 0f;

            if (cowSlots[index] != null)
            {
                SpawnCow(index);
            }

            MilkFarmEvents.CowUnlocked(index);
            SaveToData();
            Debug.Log($"[CowManager] İnek {index} açıldı!");
            return true;
        }

        /// <summary>
        /// İnek level yükseltme (soft currency ile)
        /// </summary>
        public bool UpgradeCowLevel(int index, MoneyManager moneyManager)
        {
            if (index < 0 || index >= cows.Count) return false;
            if (!cows[index].isUnlocked) return false;

            Cow cow = cows[index];
            float cost = CalculateCowUpgradeCost(cow.level);

            if (!moneyManager.CanAfford(cost)) return false;

            moneyManager.SpendMoney(cost);
            cow.level++;

            MilkFarmEvents.CowLevelUp(index, cow.level);
            MilkFarmEvents.UpgradePurchased("CowLevel", cow.level, cost);
            SaveToData();

            Debug.Log($"[CowManager] İnek {index} Level {cow.level}'e yükseltildi!");
            return true;
        }

        /// <summary>
        /// İnek upgrade maliyeti
        /// </summary>
        public float CalculateCowUpgradeCost(int currentLevel)
        {
            return config.baseCostCow * Mathf.Pow(config.costMultiplierCow, currentLevel - 1);
        }

        /// <summary>
        /// Offline progress için süt üretme
        /// </summary>
        public void ProduceOfflineMilk(int index, int amount)
        {
            if (index < 0 || index >= cows.Count) return;
            if (!cows[index].isUnlocked) return;

            int storageLimit = GetMilkStorageLimit();
            int actualAmount = Mathf.Min(amount, storageLimit);
            cows[index].currentMilk = actualAmount;

            UpdateCowMilkIndicator(index);
        }

        /// <summary>
        /// İneğe tıklayarak süt toplama (Auto Worker için de kullanılır)
        /// </summary>
        public void CollectMilkFromCow(int index)
        {
            if (index < 0 || index >= cows.Count) return;

            Cow cow = cows[index];
            if (!cow.isUnlocked) return;

            // CowController'ı kullan
            if (cow.controller != null)
            {
                cow.controller.CollectMilk();
            }
            else
            {
                // Fallback: Direct collection
                if (cow.currentMilk > 0)
                {
                    int collected = cow.currentMilk;
                    cow.currentMilk = 0;

                    packageManager.AddMilk(collected);
                    UpdateCowMilkIndicator(index);
                    MilkFarmEvents.CowMilkCollected(index, collected);
                }
            }
        }

        public List<Cow> GetAllCows() => cows;
        public Cow GetCow(int index) => (index >= 0 && index < cows.Count) ? cows[index] : null;

        /// <summary>
        /// Debug: Tüm inekleri aç
        /// </summary>
        [ContextMenu("Debug: Unlock All Cows")]
        public void DebugUnlockAllCows()
        {
            for (int i = 0; i < cows.Count; i++)
            {
                if (!cows[i].isUnlocked)
                {
                    UnlockCow(i);
                }
            }
        }
    }
}