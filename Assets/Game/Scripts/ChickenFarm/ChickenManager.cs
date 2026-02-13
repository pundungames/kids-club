using System.Collections.Generic;
using UnityEngine;
using Zenject;
using MilkFarm;

namespace ChickenFarm
{
    [System.Serializable]
    public class Chicken
    {
        public int index;
        public bool isUnlocked;
        public int level;
        public int currentEggs;
        public float productionTimer;
        public bool isBoosted;
        public Transform transform;
        public ChickenController controller;
        public GameObject eggIndicator;

        public Chicken(int idx)
        {
            index = idx;
            isUnlocked = false;
            level = 1;
            currentEggs = 0;
            productionTimer = 0f;
            isBoosted = false;
        }
    }

    public class ChickenManager : MonoBehaviour, IAnimalManager
    {
        [Inject] DiContainer container;
        [Inject] private GameConfig config;
        [Inject] private SaveManager saveManager;
        [Inject] private IAPManager iapManager;

        [Header("Referanslar")]
        [SerializeField] private StationManager stationManager;
        [SerializeField] private PackageManager packageManager;

        [Header("Tavuk Prefab ve Spawn")]
        [SerializeField] private GameObject chickenPrefab;
        [SerializeField] private Transform[] chickenSlots;

        [Header("UI Elemanları")]
        [SerializeField] private GameObject eggIndicatorPrefab;

        private List<Chicken> chickens = new List<Chicken>();

        private void Awake()
        {
            InitializeChickens();
        }

        private void Start()
        {
            LoadFromSaveData();
            UpdateStationChickenCounts();
        }

        private void OnEnable()
        {
            ChickenFarmEvents.OnChickenUnlocked += HandleChickenUnlocked;
            ChickenFarmEvents.OnSaveRequested += HandleSaveRequested;
        }

        private void OnDisable()
        {
            ChickenFarmEvents.OnChickenUnlocked -= HandleChickenUnlocked;
            ChickenFarmEvents.OnSaveRequested -= HandleSaveRequested;
        }

        private void HandleSaveRequested() => SaveToData();

        private void InitializeChickens()
        {
            for (int i = 0; i < config.maxChickenSlots; i++)
                chickens.Add(new Chicken(i));
        }

        private void HandleChickenUnlocked(int globalIndex)
        {
            if (globalIndex < 0 || globalIndex >= chickens.Count) return;
            var chicken = chickens[globalIndex];
            if (!chicken.isUnlocked) chicken.isUnlocked = true;
            if (chicken.transform == null)
            {
                SpawnChicken(globalIndex);
                UpdateStationChickenCounts();
                SaveToData();
            }
        }

        // ✅ chickens listesinden oku (cows DEĞİL!)
        private void LoadFromSaveData()
        {
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData == null) return;

            if (saveData.chickens == null || saveData.chickens.Count == 0)
            {
                Debug.LogWarning("[ChickenManager] Save data'da chicken yok, skip");
                return;
            }

            for (int i = 0; i < chickens.Count && i < saveData.chickens.Count; i++)
            {
                chickens[i].isUnlocked = saveData.chickens[i].isUnlocked;
                chickens[i].level = saveData.chickens[i].level;
                chickens[i].currentEggs = saveData.chickens[i].storedEggs;
                chickens[i].productionTimer = saveData.chickens[i].productionTimer;

                if (chickens[i].isUnlocked)
                    SpawnChicken(i);
            }
        }

        // ✅ chickens listesine yaz (cows DEĞİL!)
        public void SaveToData()
        {
            var saveData = saveManager.GetCurrentSaveData();

            if (saveData.chickens == null)
                saveData.chickens = new List<ChickenSaveData>();

            while (saveData.chickens.Count < chickens.Count)
                saveData.chickens.Add(new ChickenSaveData(saveData.chickens.Count));

            for (int i = 0; i < chickens.Count && i < saveData.chickens.Count; i++)
            {
                saveData.chickens[i].isUnlocked = chickens[i].isUnlocked;
                saveData.chickens[i].level = chickens[i].level;
                saveData.chickens[i].storedEggs = chickens[i].currentEggs;
                saveData.chickens[i].productionTimer = chickens[i].productionTimer;
            }
            saveManager.SaveGame(saveData);
        }

        public void SpawnChicken(int index)
        {
            if (chickenPrefab == null || chickenSlots[index] == null) return;

            GameObject obj = Instantiate(chickenPrefab, chickenSlots[index].position, chickenSlots[index].rotation, chickenSlots[index]);
            container.InjectGameObject(obj);
            chickens[index].transform = obj.transform;
            obj.transform.localScale = Vector3.one * 5f;
            ChickenController ctrl = obj.GetComponent<ChickenController>();
            if (ctrl != null)
            {
                chickens[index].controller = ctrl;
                int stationIndex = index / config.chickensPerStation;
                ctrl.SetFeedTrough(stationManager.GetFeedTrough(stationIndex));
                ctrl.SetWaterTrough(stationManager.GetWaterTrough(stationIndex));
                ctrl.SetPackageManager(packageManager);
                ctrl.Initialize(index, chickens[index], config, iapManager, chickenSlots[index].transform.parent.parent.GetComponent<AreaController>().spline);
            }

            if (eggIndicatorPrefab != null)
            {
                GameObject indicator = Instantiate(eggIndicatorPrefab, obj.transform);
                chickens[index].eggIndicator = indicator;
                indicator.SetActive(false);
            }
        }

        private void Update()
        {
            for (int i = 0; i < chickens.Count; i++)
            {
                var c = chickens[i];
                if (!c.isUnlocked || c.controller == null) continue;
                c.currentEggs = c.controller.GetEggStack();
            }
        }

        private int GetEggStorageLimit()
        {
            return config.baseEggStorageLimit + iapManager.GetMilkStorageBoost();
        }

        private void UpdateStationChickenCounts()
        {
            for (int s = 0; s < 4; s++)
            {
                int count = GetActiveCountForStation(s);
                var ft = stationManager.GetFeedTrough(s);
                if (ft != null) ft.SetActiveCowCount(count);
                var wt = stationManager.GetWaterTrough(s);
                if (wt != null) wt.SetActiveCowCount(count);
            }
        }

        public int GetActiveCountForStation(int stationIndex)
        {
            int count = 0;
            int start = stationIndex * config.chickensPerStation;
            int end = Mathf.Min(start + config.chickensPerStation, chickens.Count);
            for (int i = start; i < end; i++)
                if (chickens[i].isUnlocked) count++;
            return count;
        }

        // === UPGRADE ===
        public bool UpgradeChicken(int globalIndex, IAPManager iapMgr)
        {
            if (globalIndex < 0 || globalIndex >= chickens.Count) return false;
            var c = chickens[globalIndex];
            if (!c.isUnlocked || c.level >= 3) return false;
            int cost = GetUpgradeCostGems(c.level);
            if (!iapMgr.CanAffordGems(cost)) return false;
            iapMgr.SpendGems(cost);
            c.level++;
            if (c.controller != null) c.controller.OnLevelChanged(c.level);
            ChickenFarmEvents.ChickenUpgraded(globalIndex, c.level);
            return true;
        }

        public int GetUpgradeCostGems(int currentLevel)
        {
            if (currentLevel >= 3) return 0;
            return currentLevel == 1 ? 50 : 100;
        }

        public Sprite GetChickenSprite(int level) => config.GetChickenSprite(level);

        public void CollectEggsFromChicken(int index)
        {
            if (index < 0 || index >= chickens.Count) return;
            var c = chickens[index];
            if (!c.isUnlocked) return;
            if (c.controller != null) c.controller.CollectEggs();
            else if (c.currentEggs > 0)
            {
                int collected = c.currentEggs;
                c.currentEggs = 0;
                packageManager.AddMilk(collected);
                ChickenFarmEvents.ChickenEggCollected(index, collected);
            }
        }

        public void UnlockAndSpawnChicken(int index)
        {
            if (index < 0 || index >= chickens.Count) return;
            chickens[index].isUnlocked = true;
            if (chickenSlots != null && index < chickenSlots.Length && chickenSlots[index] != null)
            {
                SpawnChicken(index);
                UpdateStationChickenCounts();
                SaveToData();
            }
        }

        // ══════════════════════════════════════════
        //  IAnimalManager IMPLEMENTATION
        // ══════════════════════════════════════════

        public List<AnimalData> GetAllAnimals()
        {
            var animals = new List<AnimalData>();
            foreach (var c in chickens)
            {
                var data = new AnimalData(c.index);
                data.isUnlocked = c.isUnlocked;
                data.level = c.level;
                data.currentProduct = c.currentEggs;
                data.productionTimer = c.productionTimer;
                animals.Add(data);
            }
            return animals;
        }

        public AnimalData GetAnimal(int index)
        {
            if (index < 0 || index >= chickens.Count) return null;
            var c = chickens[index];
            var data = new AnimalData(c.index);
            data.isUnlocked = c.isUnlocked;
            data.level = c.level;
            data.currentProduct = c.currentEggs;
            data.productionTimer = c.productionTimer;
            return data;
        }

        public void SpawnAnimal(int index) => SpawnChicken(index);
        public void UnlockAndSpawnAnimal(int index) => UnlockAndSpawnChicken(index);
        public void MarkAnimalAsUnlocked(int index, bool unlocked = true)
        {
            if (index >= 0 && index < chickens.Count) chickens[index].isUnlocked = unlocked;
        }

        public bool UpgradeAnimal(int globalIndex, IAPManager iapMgr) => UpgradeChicken(globalIndex, iapMgr);
        public float GetProductionTime(int level) => config.GetChickenProductionTime(level);
        public Sprite GetAnimalSprite(int level) => config.GetChickenSprite(level);
        public float CalculateUpgradeCost(int currentLevel) => config.baseCostChicken * Mathf.Pow(config.costMultiplierChicken, currentLevel - 1);
        public int GetPurchaseCost(int globalIndex)
        {
            if (globalIndex < 3) return 0;
            if (globalIndex < 6) return 100;
            if (globalIndex < 9) return 200;
            return 300;
        }
        public void CollectFromAnimal(int index) => CollectEggsFromChicken(index);

        public List<Chicken> GetAllChickens() => chickens;
        public Chicken GetChicken(int index) => (index >= 0 && index < chickens.Count) ? chickens[index] : null;
    }
}