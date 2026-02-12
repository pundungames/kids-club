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
            UpdateStationCowCounts();
        }
        private void OnEnable()
        {
            MilkFarmEvents.OnCowUnlocked += HandleCowUnlocked;
            MilkFarmEvents.OnSaveRequested += HandleSaveRequested; // ✅ YENİ
        }

        private void OnDisable()
        {
            MilkFarmEvents.OnCowUnlocked -= HandleCowUnlocked;
            MilkFarmEvents.OnSaveRequested -= HandleSaveRequested; // ✅ YENİ
        }

        // ✅ Class sonuna EKLE:

        private void HandleSaveRequested()
        {
            SaveToData();
        }
        private void InitializeCows()
        {
            for (int i = 0; i < config.maxCowSlots; i++)
            {
                Cow cow = new Cow(i);
                cows.Add(cow);
            }
        }
        private void HandleCowUnlocked(int globalIndex)
        {
            Debug.Log($"[CowManager] 🔔 Event received: Cow {globalIndex} unlocked!");

            if (globalIndex < 0 || globalIndex >= cows.Count)
            {
                Debug.LogError($"[CowManager] Invalid cow index: {globalIndex}");
                return;
            }

            // Zaten unlock mu kontrol et
            if (cows[globalIndex].isUnlocked)
            {
                Debug.LogWarning($"[CowManager] Cow {globalIndex} zaten unlock!");
                return;
            }

            // Unlock & Spawn
            UnlockAndSpawnCow(globalIndex);
        }

        /// <summary>
        /// Save data'dan verileri yükle
        /// </summary>
        public void LoadFromSaveData()
        {
            var saveData = saveManager.GetCurrentSaveData();

            Debug.Log("[CowManager] 📂 Loading cows...");

            for (int i = 0; i < cows.Count && i < saveData.cows.Count; i++)
            {
                var cowData = saveData.cows[i];
                cows[i].isUnlocked = cowData.isUnlocked;
                cows[i].level = cowData.level;
                cows[i].currentMilk = cowData.storedMilk;
                cows[i].productionTimer = cowData.productionTimer;

                // Unlocked ise spawn et
                if (cows[i].isUnlocked && cowSlots[i] != null)
                {
                    SpawnCow(i);
                    Debug.Log($"[CowManager] Cow {i} loaded & spawned");
                }
            }

            Debug.Log($"[CowManager] ✅ {cows.FindAll(c => c.isUnlocked).Count} inek yüklendi.");
        }
        /// <summary>
        /// İneği unlock olarak işaretle (spawn olmadan)
        /// IAPManager load'dan önce çağrılır
        /// </summary>
        public void MarkCowAsUnlocked(int index, bool unlocked = true)
        {
            if (index < 0 || index >= cows.Count) return;

            cows[index].isUnlocked = unlocked;
            Debug.Log($"[CowManager] Cow {index} marked as unlocked: {unlocked}");
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
        public void SpawnCow(int index)
        {
            if (cowPrefab == null || cowSlots[index] == null) return;

            GameObject cowObj = Instantiate(cowPrefab, cowSlots[index].position, cowSlots[index].rotation, cowSlots[index]);
            Debug.LogError("cowSpawned");
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
                cow.currentMilk = cow.controller.GetMilkStack();

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


        private void UpdateStationCowCounts()
        {
            // Her istasyon için aktif inek sayısını hesapla
            for (int stationIndex = 0; stationIndex < 4; stationIndex++)
            {
                int cowCount = GetActiveCowCountForStation(stationIndex);

                // Yemlik ve suluğa bildir
                TroughController feedTrough = stationManager.GetFeedTrough(stationIndex);
                if (feedTrough != null)
                {
                    feedTrough.SetActiveCowCount(cowCount);
                }

                TroughController waterTrough = stationManager.GetWaterTrough(stationIndex);
                if (waterTrough != null)
                {
                    waterTrough.SetActiveCowCount(cowCount);
                }
            }
        }

        private int GetActiveCowCountForStation(int stationIndex)
        {
            int count = 0;
            int startIndex = stationIndex * config.cowsPerStation;
            int endIndex = startIndex + config.cowsPerStation;

            for (int i = startIndex; i < endIndex && i < cows.Count; i++)
            {
                if (cows[i].isUnlocked)
                {
                    count++;
                }
            }

            return count;
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
        // CowManager.cs'e ekle:

        /// <summary>
        /// İnek unlock ve spawn (IAP/AreaController'dan çağrılır)
        /// </summary>
        public void UnlockAndSpawnCow(int index)
        {
            if (index < 0 || index >= cows.Count)
            {
                Debug.LogError($"[CowManager] Geçersiz cow index: {index}");
                return;
            }

            Cow cow = cows[index];

            // Unlock
            cow.isUnlocked = true;

            // Spawn (trough bağlama ile)
            if (cowSlots != null && index < cowSlots.Length && cowSlots[index] != null)
            {
                SpawnCow(index);

                // Station cow count güncelle
                UpdateStationCowCounts();

                // Save
                SaveToData();

                Debug.Log($"[CowManager] ✅ Cow {index} unlocked & spawned!");
            }
            else
            {
                Debug.LogError($"[CowManager] Cow slot {index} bulunamadı!");
            }
        }


        /// <summary>
        /// Upgrade cow level (with money)
        /// </summary>
        public bool UpgradeCow(int globalIndex, IAPManager iapManager)
        {
            Debug.Log($"[CowManager] 🔧 Upgrade attempt: Cow {globalIndex}");

            if (globalIndex < 0 || globalIndex >= cows.Count)
            {
                Debug.LogError($"[CowManager] ❌ Invalid index: {globalIndex}");
                return false;
            }

            var cow = cows[globalIndex];

            if (!cow.isUnlocked)
            {
                Debug.LogError($"[CowManager] ❌ Cow {globalIndex} not unlocked!");
                return false;
            }

            if (cow.level >= 3)
            {
                Debug.LogWarning($"[CowManager] ❌ Cow {globalIndex} already max level!");
                return false;
            }

            int gemCost = GetUpgradeCostGems(cow.level);
            int currentGems = iapManager.GetCurrentGems();

            Debug.Log($"[CowManager] 💎 Cost: {gemCost} gems, Current gems: {currentGems}");

            if (!iapManager.CanAffordGems(gemCost))
            {
                Debug.LogWarning($"[CowManager] ❌ Not enough gems! Need {gemCost}, have {currentGems}");
                return false;
            }

            // Spend gems
            iapManager.SpendGems(gemCost);

            // Upgrade
            cow.level++;

            // Update controller if spawned
            if (cow.controller != null)
            {
                cow.controller.OnLevelChanged(cow.level);
            }

            MilkFarmEvents.CowUpgraded(globalIndex, cow.level);

            Debug.Log($"[CowManager] ✅ Cow {globalIndex} upgraded to Level {cow.level}!");
            return true;
        }
        /// <summary>
        /// Get upgrade cost for current level
        /// </summary>
        public int GetUpgradeCostGems(int currentLevel)
        {
            if (currentLevel >= 3) return 0; // Max level

            // Level 1 → 2: 50 gems
            // Level 2 → 3: 100 gems
            return currentLevel == 1 ? 50 : 100;
        }

        /// <summary>
        /// Get production time based on level (from GameConfig)
        /// </summary>
        public float GetProductionTime(int level)
        {
            if (config == null) return 30f;
            return config.GetProductionTime(level);
        }

        /// <summary>
        /// Get cow sprite based on level (from GameConfig)
        /// </summary>
        public Sprite GetCowSprite(int level)
        {
            if (config == null) return null;
            return config.GetCowSprite(level);
        }

        /// <summary>
        /// Purchase cow with gems
        /// </summary>
        public bool PurchaseCow(int globalIndex, IAPManager iap)
        {
            if (globalIndex < 0 || globalIndex >= cows.Count) return false;

            var cow = cows[globalIndex];
            if (cow.isUnlocked) return false;

            int gemCost = GetCowPurchaseCost(globalIndex);
            //if (!iap.SpendGems(gemCost)) return false;

            cow.isUnlocked = true;
          //  iap.UnlockCow(globalIndex);
            SpawnCow(globalIndex);

            MilkFarmEvents.CowUnlocked(globalIndex);
            Debug.Log($"[CowManager] Cow {globalIndex} purchased!");
            return true;
        }

        /// <summary>
        /// Get cow purchase cost (gems)
        /// </summary>
        public int GetCowPurchaseCost(int globalIndex)
        {
            if (globalIndex < 3) return 0;    // Cow 0-2: FREE
            if (globalIndex < 6) return 100;  // Cow 3-5: 100 gems
            if (globalIndex < 9) return 200;  // Cow 6-8: 200 gems
            return 300;                       // Cow 9-11: 300 gems
        }
    }
}