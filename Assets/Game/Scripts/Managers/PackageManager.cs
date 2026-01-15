using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// Paketleme istasyonunu yöneten manager
    /// Süt → Paket dönüşümü ve kapasite yönetimi
    /// GDD v2'ye göre tam implementation
    /// </summary>
    public class PackageManager : MonoBehaviour
    {
        [Inject] private GameConfig config;
        [Inject] private SaveManager saveManager;

        [Header("Referanslar")]
        [SerializeField] private CustomerManager customerManager;

        [Header("Paketleme İstasyonu")]
        [SerializeField] private Transform packageStationTransform;

        [Header("UI (Opsiyonel)")]
        [SerializeField] private GameObject packageVisualPrefab;

        private int milkPool; // Toplanan sütler
        private int packageCount; // Oluşturulan paketler
        private int capacityLevel; // Kapasite upgrade level'ı
        private int currentCapacity; // Mevcut kapasite

        private void Start()
        {
            LoadFromSaveData();
            SetupTapHandler();
        }

        /// <summary>
        /// Paketleme istasyonuna tap handler ekle
        /// </summary>
        private void SetupTapHandler()
        {
            if (packageStationTransform == null) return;

            // Collider yoksa ekle
            if (packageStationTransform.GetComponent<Collider>() == null)
            {
                BoxCollider collider = packageStationTransform.gameObject.AddComponent<BoxCollider>();
                collider.size = new Vector3(2f, 1f, 2f);
            }

            EventTrigger trigger = packageStationTransform.GetComponent<EventTrigger>();
            if (trigger == null) trigger = packageStationTransform.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry pointerClick = new EventTrigger.Entry();
            pointerClick.eventID = EventTriggerType.PointerClick;
            pointerClick.callback.AddListener((data) => { OnPackageStationClicked(); });
            trigger.triggers.Add(pointerClick);
        }

        /// <summary>
        /// Paketleme istasyonuna tıklama - Müşteriye paket gönder
        /// </summary>
        private void OnPackageStationClicked()
        {
            // Paket var mı?
            if (packageCount <= 0)
            {
                Debug.Log("[PackageManager] Satılacak paket yok!");
                return;
            }

            // Müşteri var mı?
            if (!customerManager.HasWaitingCustomer())
            {
                Debug.Log("[PackageManager] Bekleyen müşteri yok!");
                return;
            }

            // 1 paket müşteriye gönder (instant)
            packageCount--;
            customerManager.ServePackageToCustomer();

            MilkFarmEvents.PackageSold(packageCount);
            UpdateVisuals();
            SaveToData();

            Debug.Log($"[PackageManager] 1 paket satıldı! Kalan: {packageCount}");
        }

        /// <summary>
        /// Save data'dan verileri yükle
        /// </summary>
        public void LoadFromSaveData()
        {
            var saveData = saveManager.GetCurrentSaveData();
            var packData = saveData.packaging;

            milkPool = packData.milkPool;
            packageCount = packData.packageCount;
            capacityLevel = packData.capacityLevel;

            UpdateCapacity();
            UpdateVisuals();

            Debug.Log($"[PackageManager] Yüklendi: {packageCount} paket, {milkPool} süt pool");
        }

        /// <summary>
        /// Mevcut durumu save data'ya kaydet
        /// </summary>
        public void SaveToData()
        {
            var saveData = saveManager.GetCurrentSaveData();
            saveData.packaging.milkPool = milkPool;
            saveData.packaging.packageCount = packageCount;
            saveData.packaging.capacityLevel = capacityLevel;

            saveManager.SaveGame(saveData);
        }

        /// <summary>
        /// Kapasiteyi güncelle
        /// </summary>
        private void UpdateCapacity()
        {
            // GDD: base_capacity = 8, her level için artış
            currentCapacity = config.packageStationCapacityBase + (capacityLevel - 1) * 4;
        }

        /// <summary>
        /// İneklerden süt ekle
        /// </summary>
        public void AddMilk(int amount)
        {
            milkPool += amount;
            MilkFarmEvents.MilkAddedToStation(milkPool);

            // Paket oluşturma kontrolü
            TryCreatePackages();

            Debug.Log($"[PackageManager] {amount} süt eklendi. Pool: {milkPool}");
        }

        /// <summary>
        /// Paket oluşturma denemesi
        /// GDD: 4 süt = 1 paket
        /// </summary>
        private void TryCreatePackages()
        {
            while (milkPool >= config.packageSize && packageCount < currentCapacity)
            {
                milkPool -= config.packageSize;
                packageCount++;

                MilkFarmEvents.PackageCreated(packageCount);
                UpdateVisuals();

                Debug.Log($"[PackageManager] Yeni paket oluşturuldu! Toplam: {packageCount}/{currentCapacity}");
            }

            // Kapasite doldu mu?
            if (packageCount >= currentCapacity)
            {
                MilkFarmEvents.PackageStationFull();
                Debug.Log($"[PackageManager] Paketleme istasyonu dolu! ({packageCount}/{currentCapacity})");
            }

            SaveToData();
        }

        /// <summary>
        /// Görsel güncelleme
        /// </summary>
        private void UpdateVisuals()
        {
            // TODO: Paket görsellerini güncelle
            // Örnek: Paket sayısı kadar visual spawn et
        }

        // === PUBLIC API ===

        /// <summary>
        /// Kapasite upgrade'i (soft currency ile)
        /// </summary>
        public bool UpgradeCapacity(MoneyManager moneyManager)
        {
            float cost = CalculateCapacityUpgradeCost();

            if (!moneyManager.CanAfford(cost)) return false;

            moneyManager.SpendMoney(cost);
            capacityLevel++;
            UpdateCapacity();

            MilkFarmEvents.UpgradePurchased("PackageCapacity", capacityLevel, cost);
            SaveToData();

            Debug.Log($"[PackageManager] Kapasite upgrade edildi! Yeni kapasite: {currentCapacity}");
            return true;
        }

        /// <summary>
        /// Kapasite upgrade maliyeti
        /// GDD Formül: base_cost_depo * (cost_multiplier_depo ^ (level - 1))
        /// </summary>
        public float CalculateCapacityUpgradeCost()
        {
            return config.baseCostDepo * Mathf.Pow(config.costMultiplierDepo, capacityLevel - 1);
        }

        public int GetCurrentPackageCount() => packageCount;
        public int GetCurrentCapacity() => currentCapacity;
        public int GetMilkPool() => milkPool;
        public bool IsFull() => packageCount >= currentCapacity;
        public bool HasPackages() => packageCount > 0;

        /// <summary>
        /// Debug: Paket ekle
        /// </summary>
        [ContextMenu("Debug: Add 10 Packages")]
        public void DebugAddPackages()
        {
            packageCount = Mathf.Min(packageCount + 10, currentCapacity);
            UpdateVisuals();
            SaveToData();
            Debug.Log($"[PackageManager] 10 paket eklendi! Toplam: {packageCount}");
        }
    }
}
