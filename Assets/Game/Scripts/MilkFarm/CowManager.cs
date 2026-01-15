using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
        public int currentMilk; // Üretilen süt (envanter)
        public float productionTimer; // Kalan süre
        public bool isBoosted; // Tap & Hold aktif mi?
        public Transform transform; // İnek objesi
        public GameObject milkIndicator; // Süt icon UI

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
    /// GDD v2'ye göre tam implementation
    /// </summary>
    public class CowManager : MonoBehaviour
    {
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
        [SerializeField] private GameObject milkIndicatorPrefab; // Süt icon prefab
        [SerializeField] private GameObject needsIndicatorPrefab; // Yem/su eksik icon

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
        /// İnek spawn etme
        /// </summary>
        private void SpawnCow(int index)
        {
            if (cowPrefab == null || cowSlots[index] == null) return;

            GameObject cowObj = Instantiate(cowPrefab, cowSlots[index].position, cowSlots[index].rotation, cowSlots[index]);
            cows[index].transform = cowObj.transform;

            // Tap & Hold için event trigger ekle
            AddTapHandlers(cowObj, index);

            // Süt indikatörü oluştur
            if (milkIndicatorPrefab != null)
            {
                GameObject indicator = Instantiate(milkIndicatorPrefab, cowObj.transform);
                cows[index].milkIndicator = indicator;
                indicator.SetActive(false);
            }
        }

        /// <summary>
        /// Tap & Hold input handler'ları ekle
        /// </summary>
        private void AddTapHandlers(GameObject cowObj, int cowIndex)
        {
            // Collider yoksa ekle
            if (cowObj.GetComponent<Collider>() == null)
            {
                BoxCollider collider = cowObj.AddComponent<BoxCollider>();
                collider.size = new Vector3(1f, 2f, 1f);
                collider.center = new Vector3(0f, 1f, 0f);
            }

            EventTrigger trigger = cowObj.GetComponent<EventTrigger>();
            if (trigger == null) trigger = cowObj.AddComponent<EventTrigger>();

            // Pointer Down - Boost başlat
            EventTrigger.Entry pointerDown = new EventTrigger.Entry();
            pointerDown.eventID = EventTriggerType.PointerDown;
            pointerDown.callback.AddListener((data) => { OnCowPointerDown(cowIndex); });
            trigger.triggers.Add(pointerDown);

            // Pointer Up - Boost bitir
            EventTrigger.Entry pointerUp = new EventTrigger.Entry();
            pointerUp.eventID = EventTriggerType.PointerUp;
            pointerUp.callback.AddListener((data) => { OnCowPointerUp(cowIndex); });
            trigger.triggers.Add(pointerUp);

            // Pointer Exit - Boost bitir
            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => { OnCowPointerUp(cowIndex); });
            trigger.triggers.Add(pointerExit);

            // Click - Süt topla
            EventTrigger.Entry pointerClick = new EventTrigger.Entry();
            pointerClick.eventID = EventTriggerType.PointerClick;
            pointerClick.callback.AddListener((data) => { OnCowClicked(cowIndex); });
            trigger.triggers.Add(pointerClick);
        }

        private void OnCowPointerDown(int index)
        {
            if (!cows[index].isUnlocked) return;
            cows[index].isBoosted = true;
            Debug.Log($"[Cow {index}] Boost başlatıldı!");
        }

        private void OnCowPointerUp(int index)
        {
            if (!cows[index].isUnlocked) return;
            cows[index].isBoosted = false;
            Debug.Log($"[Cow {index}] Boost durduruldu!");
        }

        /// <summary>
        /// İneğe tıklama - süt toplama
        /// </summary>
        private void OnCowClicked(int index)
        {
            Cow cow = cows[index];
            if (!cow.isUnlocked) return;

            // Süt varsa topla
            if (cow.currentMilk > 0)
            {
                int collectedMilk = cow.currentMilk;
                cow.currentMilk = 0;
                cow.productionTimer = 0f; // Timer'ı sıfırla, yeniden üretime başlasın

                // Paketleme istasyonuna gönder
                packageManager.AddMilk(collectedMilk);

                // UI güncelle
                UpdateCowMilkIndicator(index);

                MilkFarmEvents.CowMilkCollected(index, collectedMilk);
                Debug.Log($"[Cow {index}] {collectedMilk} süt toplandı!");
            }
        }

        private void Update()
        {
            UpdateAllCows(Time.deltaTime);
        }

        /// <summary>
        /// Tüm ineklerin üretim döngüsünü güncelle
        /// </summary>
        private void UpdateAllCows(float deltaTime)
        {
            for (int i = 0; i < cows.Count; i++)
            {
                Cow cow = cows[i];
                if (!cow.isUnlocked) continue;

                // Süt envanteri dolu mu?
                int storageLimit = GetMilkStorageLimit();
                if (cow.currentMilk >= storageLimit)
                {
                    // Envanter dolu, üretim durdu
                    UpdateCowMilkIndicator(i);
                    continue;
                }

                // İstasyonu kontrol et
                int stationIndex = i / config.cowsPerStation;
                bool hasResources = stationManager.HasFoodAndWater(stationIndex);

                if (!hasResources)
                {
                    // Yem veya su yok, üretim pause
                    // Needs indikatörünü göster
                    ShowNeedsIndicator(i, true);
                    continue;
                }
                else
                {
                    ShowNeedsIndicator(i, false);
                }

                // Üretim süresini hesapla
                float productionTime = CalculateCowProductionTime(cow);

                // Timer'ı ilerlet
                float speedFactor = cow.isBoosted ? config.tapHoldSpeedMultiplier : 1f;
                cow.productionTimer += deltaTime * speedFactor;

                // Süt üretildi mi?
                if (cow.productionTimer >= productionTime)
                {
                    cow.productionTimer = 0f;
                    cow.currentMilk++;

                    MilkFarmEvents.CowMilkProduced(i);
                    UpdateCowMilkIndicator(i);

                    Debug.Log($"[Cow {i}] Süt üretildi! Toplam: {cow.currentMilk}/{storageLimit}");
                }
            }
        }

        /// <summary>
        /// İnek için üretim süresini hesapla (level ve IAP'e göre)
        /// GDD Formül: cow_production_time(level) = max(10, base * (0.9 ^ (level - 1)))
        /// </summary>
        private float CalculateCowProductionTime(Cow cow)
        {
            // Base süre
            float baseTime = config.baseMilkProductionTime;

            // Level bonus: Her level %10 hızlandırma
            float levelMultiplier = Mathf.Pow(0.9f, cow.level - 1);
            float leveledTime = baseTime * levelMultiplier;

            // Minimum limit
            leveledTime = Mathf.Max(config.minProductionTime, leveledTime);

            // IAP global speed boost
            leveledTime *= iapManager.GetGlobalSpeedMultiplier();

            return leveledTime;
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

        /// <summary>
        /// Yem/su eksik indikatörünü göster/gizle
        /// </summary>
        private void ShowNeedsIndicator(int index, bool show)
        {
            // TODO: İnek üzerinde yem/su eksik ikonu göster
            // Şimdilik log
            if (show && Time.frameCount % 120 == 0) // Her 2 saniyede bir log
            {
                Debug.Log($"[Cow {index}] Yem veya su eksik!");
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
        /// GDD Formül: base_cost * (multiplier ^ (level - 1))
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
