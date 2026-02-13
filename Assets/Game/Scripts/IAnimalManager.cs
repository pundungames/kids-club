using System.Collections.Generic;
using UnityEngine;

namespace MilkFarm
{
    /// <summary>
    /// Ortak hayvan verisi - Cow ve Chicken ikisi de bunu kullanır
    /// StableManager, StableInfoPanel, CowRowUI gibi UI kodları
    /// artık bu interface üzerinden çalışır
    /// </summary>
    [System.Serializable]
    public class AnimalData
    {
        public int index;
        public bool isUnlocked;
        public int level;
        public int currentProduct; // milk veya egg
        public float productionTimer;

        public AnimalData(int idx)
        {
            index = idx;
            isUnlocked = false;
            level = 1;
            currentProduct = 0;
            productionTimer = 0f;
        }
    }

    /// <summary>
    /// CowManager ve ChickenManager'ın ortak interface'i
    /// Mevcut UI kodları (StableManager, StableInfoPanel, CowRowUI, 
    /// AutoWorkerManager, UpgradeManager, IAPManager) bu interface'i kullanır
    /// </summary>
    public interface IAnimalManager
    {
        // === DATA ===
        List<AnimalData> GetAllAnimals();
        AnimalData GetAnimal(int index);

        // === SPAWN / UNLOCK ===
        void SpawnAnimal(int index);
        void UnlockAndSpawnAnimal(int index);
        void MarkAnimalAsUnlocked(int index, bool unlocked = true);

        // === UPGRADE ===
        bool UpgradeAnimal(int globalIndex, IAPManager iapManager);
        int GetUpgradeCostGems(int currentLevel);
        float GetProductionTime(int level);
        Sprite GetAnimalSprite(int level);
        float CalculateUpgradeCost(int currentLevel);

        // === PURCHASE ===
        int GetPurchaseCost(int globalIndex);

        // === COLLECT ===
        void CollectFromAnimal(int index);

        // === SAVE ===
        void SaveToData();
    }
}
