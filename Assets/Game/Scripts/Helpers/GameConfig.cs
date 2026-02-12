using UnityEngine;

namespace MilkFarm
{
    /// <summary>
    /// Oyunun tüm temel parametrelerini içeren ScriptableObject config
    /// Offline progress dahil tüm değerler burada tanımlı
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "MilkFarm/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("=== GENEL ===")]
        [Tooltip("Maksimum inek slot sayısı")]
        public int maxCowSlots = 12;

        [Tooltip("Her istasyon kaç ineğe bakar")]
        public int cowsPerStation = 4;


        [Header("=== ÜRETİM ===")]
        [Tooltip("Temel süt üretim süresi (saniye)")]
        public float baseProductionTime = 30f;

        [Tooltip("Her inek en fazla kaç süt depolayabilir (base)")]
        public int baseMilkStorageLimit = 3;

        [Tooltip("Tap & Hold sırasında hız çarpanı (0.75 = %25 daha hızlı)")]
        public float tapHoldSpeedMultiplier = 0.75f;

        [Tooltip("Minimum üretim süresi (saniye)")]
        public float minProductionTime = 10f;


        [Header("=== OFFLINE PROGRESS ===")]
        [Tooltip("Offline üretimde bir ineğin max biriktirebileceği süt stack sayısı")]
        public int maxMilkStack = 6;

        [Tooltip("Her production cycle başına yem tüketimi (offline dahil)")]
        public float baseTroughConsumption = 1f;

        [Tooltip("Offline üretim için özel production süresi (istersen kullan)")]
        public float offlineBaseProductionTime = 600f;


        [Header("=== PAKETLEME ===")]
        [Tooltip("Bir paket için gereken süt sayısı")]
        public int packageSize = 4;

        [Tooltip("Paketleme istasyonunun başlangıç kapasitesi")]
        public int packageStationCapacityBase = 8;


        [Header("=== MÜŞTERİLER ===")]
        [Tooltip("Maksimum kuyruk uzunluğu")]
        public int customerQueueMax = 4;

        [Tooltip("Müşteri minimum paket talebi")]
        public int customerMinRequest = 1;

        [Tooltip("Müşteri maksimum paket talebi")]
        public int customerMaxRequest = 4;

        [Tooltip("Süt başına kazanılan para (base)")]
        public float moneyPerMilk = 1f;


        [Header("=== YEM & SU ===")]
        [Tooltip("Yemlik boşalma süresi (saniye)")]
        public float feedingInterval = 30f;

        [Tooltip("Suluk boşalma süresi (saniye)")]
        public float wateringInterval = 30f;

        [Tooltip("Bir tap ile dolacak miktar (0.25 = %25)")]
        public float feedingTapFill = 0.25f;

        [Tooltip("Bir tap ile dolacak miktar (0.25 = %25)")]
        public float wateringTapFill = 0.25f;


        [Header("=== UPGRADE EKONOMİ ===")]
        [Tooltip("İnek level up başlangıç maliyeti")]
        public float baseCostCow = 50f;

        [Tooltip("İnek level up maliyet çarpanı")]
        public float costMultiplierCow = 1.5f;

        [Tooltip("Depo kapasitesi upgrade başlangıç maliyeti")]
        public float baseCostDepo = 100f;

        [Tooltip("Depo kapasitesi upgrade maliyet çarpanı")]
        public float costMultiplierDepo = 1.7f;


        [Header("=== IAP MULTIPLIERS ===")]
        [Tooltip("Normal hız (IAP yok)")]
        public float globalSpeedMultiplierNormal = 1.0f;

        [Tooltip("+%50 hız IAP")]
        public float globalSpeedMultiplierFast = 0.67f;

        [Tooltip("+%100 hız IAP (Altın İnekler)")]
        public float globalSpeedMultiplierSuper = 0.5f;

        [Tooltip("Normal müşteri kazancı")]
        public float customerRichMultiplierNormal = 1.0f;

        [Tooltip("+%50 zengin müşteri IAP")]
        public float customerRichMultiplierPlus50 = 1.5f;

        [Tooltip("+%100 zengin müşteri IAP")]
        public float customerRichMultiplierPlus100 = 2.0f;


        [Header("=== AUTO WORKER ===")]
        [Tooltip("Auto worker tick interval (saniye)")]
        public float autoWorkerInterval = 1f;


        [Header("=== OFFLINE LIMIT ===")]
        [Tooltip("Offline'da en fazla hesaplanacak süre (saniye)")]
        public float maxOfflineTime = 86400f; // 24 saat

        [Header("Cow Level System")]
        public Sprite[] cowSpritesPerLevel;
        public float[] productionTimesPerLevel = new float[3]
{
    30f,  // Level 1: 30 seconds
    25f,  // Level 2: 25 seconds
    20f   // Level 3: 20 seconds
};


        /// <summary>
        /// Get production time for level
        /// </summary>
        public float GetProductionTime(int level)
        {
            int index = Mathf.Clamp(level - 1, 0, productionTimesPerLevel.Length - 1);
            return productionTimesPerLevel[index];
        }

        /// <summary>
        /// Get cow sprite for level (UI icon)
        /// </summary>
        public Sprite GetCowSprite(int level)
        {
            if (cowSpritesPerLevel == null || cowSpritesPerLevel.Length == 0) return null;
            int index = Mathf.Clamp(level - 1, 0, cowSpritesPerLevel.Length - 1);
            return cowSpritesPerLevel[index];
        }
    }
}
