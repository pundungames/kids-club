using UnityEngine;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// IAP (In-App Purchase) durumlarını yöneten manager
    /// Gerçek IAP entegrasyonu bu sınıfa bağlanır
    /// </summary>
    public class IAPManager : MonoBehaviour
    {
        [Inject] private GameConfig config;
        [Inject] private SaveManager saveManager;

        private IAPSaveData iapData;

        private void Start()
        {
            LoadIAPData();
        }

        private void LoadIAPData() 
        {
            var saveData = saveManager.GetCurrentSaveData();
            iapData = saveData.iap;
        }

        // === SPEED BOOST ===
        
        /// <summary>
        /// +%50 hız IAP satın al
        /// </summary>
        public void PurchaseSpeedBoost50()
        {
            if (iapData.speedTier < 1)
            {
                iapData.speedTier = 1;
                MilkFarmEvents.IAPPurchased("speed_boost_50");
                SaveIAPData();
                Debug.Log("[IAPManager] +%50 Hız IAP satın alındı!");
            }
        }

        /// <summary>
        /// +%100 hız IAP satın al (Altın İnekler)
        /// </summary>
        public void PurchaseSpeedBoost100()
        {
            if (iapData.speedTier < 2)
            {
                iapData.speedTier = 2;
                MilkFarmEvents.IAPPurchased("speed_boost_100");
                SaveIAPData();
                Debug.Log("[IAPManager] +%100 Hız IAP satın alındı!");
            }
        }

        /// <summary>
        /// Aktif global hız çarpanını döner
        /// </summary>
        public float GetGlobalSpeedMultiplier()
        {
            switch (iapData.speedTier)
            {
                case 0: return config.globalSpeedMultiplierNormal;
                case 1: return config.globalSpeedMultiplierFast;
                case 2: return config.globalSpeedMultiplierSuper;
                default: return config.globalSpeedMultiplierNormal;
            }
        }

        // === RICH CUSTOMER ===

        /// <summary>
        /// +%50 zengin müşteri IAP satın al
        /// </summary>
        public void PurchaseRichCustomer50()
        {
            if (iapData.richCustomerTier < 1)
            {
                iapData.richCustomerTier = 1;
                MilkFarmEvents.IAPPurchased("rich_customer_50");
                SaveIAPData();
                Debug.Log("[IAPManager] +%50 Zengin Müşteri IAP satın alındı!");
            }
        }

        /// <summary>
        /// +%100 zengin müşteri IAP satın al
        /// </summary>
        public void PurchaseRichCustomer100()
        {
            if (iapData.richCustomerTier < 2)
            {
                iapData.richCustomerTier = 2;
                MilkFarmEvents.IAPPurchased("rich_customer_100");
                SaveIAPData();
                Debug.Log("[IAPManager] +%100 Zengin Müşteri IAP satın alındı!");
            }
        }

        /// <summary>
        /// Aktif zengin müşteri çarpanını döner
        /// </summary>
        public float GetCustomerRichMultiplier()
        {
            switch (iapData.richCustomerTier)
            {
                case 0: return config.customerRichMultiplierNormal;
                case 1: return config.customerRichMultiplierPlus50;
                case 2: return config.customerRichMultiplierPlus100;
                default: return config.customerRichMultiplierNormal;
            }
        }

        // === MILK STORAGE BOOST ===

        /// <summary>
        /// Süt depolama kapasitesini artır (level bazlı)
        /// </summary>
        public void PurchaseMilkStorageBoost()
        {
            iapData.milkStorageBoostLevel++;
            MilkFarmEvents.IAPPurchased($"milk_storage_boost_{iapData.milkStorageBoostLevel}");
            SaveIAPData();
            Debug.Log($"[IAPManager] Süt depolama +{iapData.milkStorageBoostLevel} IAP satın alındı!");
        }

        /// <summary>
        /// Toplam süt depolama bonusu
        /// </summary>
        public int GetMilkStorageBoost()
        {
            return iapData.milkStorageBoostLevel;
        }

        // === AUTO FEEDER ===

        /// <summary>
        /// Oto yem & su doldurucu IAP satın al
        /// </summary>
        public void PurchaseAutoFeeder()
        {
            if (!iapData.hasAutoFeeder)
            {
                iapData.hasAutoFeeder = true;
                MilkFarmEvents.IAPPurchased("auto_feeder");
                SaveIAPData();
                Debug.Log("[IAPManager] Oto Yem & Su Doldurucu IAP satın alındı!");
            }
        }

        public bool HasAutoFeeder() => iapData.hasAutoFeeder;

        // === AUTO WORKER ===

        /// <summary>
        /// Eleman (Auto Worker) IAP satın al
        /// </summary>
        public void PurchaseAutoWorker()
        {
            if (!iapData.hasAutoWorker)
            {
                iapData.hasAutoWorker = true;
                MilkFarmEvents.IAPPurchased("auto_worker");
                SaveIAPData();
                Debug.Log("[IAPManager] Eleman (Auto Worker) IAP satın alındı!");
            }
        }

        public bool HasAutoWorker() => iapData.hasAutoWorker;

        // === SAVE ===

        private void SaveIAPData()
        {
            var saveData = saveManager.GetCurrentSaveData();
            saveData.iap = iapData;
            saveManager.SaveGame(saveData);
        }

        // === DEBUG ===
        
        [ContextMenu("Debug: Reset All IAPs")]
        public void DebugResetIAPs()
        {
            iapData = new IAPSaveData();
            SaveIAPData();
            Debug.Log("[IAPManager] Tüm IAP'ler sıfırlandı!");
        }

        [ContextMenu("Debug: Unlock All IAPs")]
        public void DebugUnlockAllIAPs()
        {
            iapData.hasAutoFeeder = true;
            iapData.hasAutoWorker = true;
            iapData.speedTier = 2;
            iapData.richCustomerTier = 2;
            iapData.milkStorageBoostLevel = 5;
            SaveIAPData();
            Debug.Log("[IAPManager] Tüm IAP'ler açıldı!");
        }
    }
}
