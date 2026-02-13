using UnityEngine;

namespace MilkFarm
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "MilkFarm/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("=== GENEL ===")]
        public int maxCowSlots = 12;
        public int cowsPerStation = 4;

        [Header("=== ÜRETİM ===")]
        public float baseProductionTime = 30f;
        public int baseMilkStorageLimit = 3;
        public float tapHoldSpeedMultiplier = 0.75f;
        public float minProductionTime = 10f;

        [Header("=== OFFLINE PROGRESS ===")]
        public int maxMilkStack = 6;
        public float baseTroughConsumption = 1f;
        public float offlineBaseProductionTime = 600f;

        [Header("=== PAKETLEME ===")]
        public int packageSize = 4;
        public int packageStationCapacityBase = 8;

        [Header("=== MÜŞTERİLER ===")]
        public int customerQueueMax = 4;
        public int customerMinRequest = 1;
        public int customerMaxRequest = 4;
        public float moneyPerMilk = 1f;

        [Header("=== YEM & SU ===")]
        public float feedingInterval = 30f;
        public float wateringInterval = 30f;
        public float feedingTapFill = 0.25f;
        public float wateringTapFill = 0.25f;

        [Header("=== UPGRADE EKONOMİ ===")]
        public float baseCostCow = 50f;
        public float costMultiplierCow = 1.5f;
        public float baseCostDepo = 100f;
        public float costMultiplierDepo = 1.7f;

        [Header("=== IAP MULTIPLIERS ===")]
        public float globalSpeedMultiplierNormal = 1.0f;
        public float globalSpeedMultiplierFast = 0.67f;
        public float globalSpeedMultiplierSuper = 0.5f;
        public float customerRichMultiplierNormal = 1.0f;
        public float customerRichMultiplierPlus50 = 1.5f;
        public float customerRichMultiplierPlus100 = 2.0f;

        [Header("=== AUTO WORKER ===")]
        public float autoWorkerInterval = 1f;

        [Header("=== OFFLINE LIMIT ===")]
        public float maxOfflineTime = 86400f;

        [Header("Cow Level System")]
        public Sprite[] cowSpritesPerLevel;
        public float[] productionTimesPerLevel = new float[3] { 30f, 25f, 20f };

        // ══════════════════════════════════════════
        //  ✅ CHICKEN FARM DEĞERLERİ (YENİ)
        // ══════════════════════════════════════════

        [Header("=== 🐔 CHICKEN FARM ===")]
        public int maxChickenSlots = 12;
        public int chickensPerStation = 3;
        public float chickenBaseProductionTime = 25f;
        public int baseEggStorageLimit = 4;
        public int maxEggStack = 4;
        public float moneyPerEgg = 1.5f;
        public float baseCostChicken = 40f;
        public float costMultiplierChicken = 1.5f;

        [Header("🐔 Chicken Level System")]
        public Sprite[] chickenSpritesPerLevel;
        public float[] chickenProductionTimesPerLevel = new float[3] { 25f, 20f, 15f };

        // ══════════════════════════════════════════
        //  METHODS
        // ══════════════════════════════════════════

        public float GetProductionTime(int level)
        {
            int index = Mathf.Clamp(level - 1, 0, productionTimesPerLevel.Length - 1);
            return productionTimesPerLevel[index];
        }

        public Sprite GetCowSprite(int level)
        {
            if (cowSpritesPerLevel == null || cowSpritesPerLevel.Length == 0) return null;
            int index = Mathf.Clamp(level - 1, 0, cowSpritesPerLevel.Length - 1);
            return cowSpritesPerLevel[index];
        }

        public float GetChickenProductionTime(int level)
        {
            int index = Mathf.Clamp(level - 1, 0, chickenProductionTimesPerLevel.Length - 1);
            return chickenProductionTimesPerLevel[index];
        }

        public Sprite GetChickenSprite(int level)
        {
            if (chickenSpritesPerLevel == null || chickenSpritesPerLevel.Length == 0) return null;
            int index = Mathf.Clamp(level - 1, 0, chickenSpritesPerLevel.Length - 1);
            return chickenSpritesPerLevel[index];
        }
    }
}