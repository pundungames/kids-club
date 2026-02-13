using UnityEngine;
using Zenject;
using System.Collections.Generic;

namespace MilkFarm
{
    public class IAPManager : MonoBehaviour
    {
        [Inject] private SaveManager saveManager;
        [Inject] private IAnimalManager animalManager;
        [Inject] private StableManager stableManager;
        [Inject] AudioManager audioManager;
        // === GEM SYSTEM ===
        private void Start()
        {
            AddGems(100);
        }

        public int GetCurrentGems()
        {
            if (saveManager == null) return 0;
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData == null || saveData.iap == null) return 0;
            return saveData.iap.gems;
        }

        public bool CanAffordGems(int cost) => GetCurrentGems() >= cost;

        public bool SpendGems(int amount)
        {
            if (saveManager == null) return false;
            if (!CanAffordGems(amount)) return false;
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData.iap == null) saveData.iap = new IAPSaveData();
            saveData.iap.gems -= amount;
            saveManager.SaveGame(saveData);
            MilkFarmEvents.GemChanged();
            audioManager.Play("Unlock");
            return true;
        }

        public void AddGems(int amount)
        {
            if (saveManager == null) return;
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData.iap == null) saveData.iap = new IAPSaveData();
            saveData.iap.gems += amount;
            saveManager.SaveGame(saveData);
            MilkFarmEvents.GemChanged();
        }

        // === IAP BOOSTS ===

        public bool HasAutoWorker()
        {
            if (saveManager == null) return false;
            var saveData = saveManager.GetCurrentSaveData();
            return saveData?.iap?.hasAutoWorker ?? false;
        }

        public bool HasAutoFeeder()
        {
            if (saveManager == null) return false;
            var saveData = saveManager.GetCurrentSaveData();
            return saveData?.iap?.hasAutoFeeder ?? false;
        }

        public float GetGlobalSpeedMultiplier()
        {
            if (saveManager == null) return 1f;
            var saveData = saveManager.GetCurrentSaveData();
            int tier = saveData?.iap?.speedTier ?? 0;
            return tier == 2 ? 2f : (tier == 1 ? 1.5f : 1f);
        }

        public float GetCustomerRichMultiplier()
        {
            if (saveManager == null) return 1f;
            var saveData = saveManager.GetCurrentSaveData();
            int tier = saveData?.iap?.richCustomerTier ?? 0;
            return tier == 2 ? 2f : (tier == 1 ? 1.5f : 1f);
        }

        public int GetMilkStorageBoost()
        {
            if (saveManager == null) return 0;
            var saveData = saveManager.GetCurrentSaveData();
            return saveData?.iap?.milkStorageBoostLevel ?? 0;
        }

        public float GetSpeedMultiplier()
        {
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData == null || saveData.iap == null) return 1f;
            switch (saveData.iap.speedTier)
            {
                case 1: return 1.5f;
                case 2: return 2f;
                default: return 1f;
            }
        }

        // === PURCHASE PROCESSING ===

        public void ProcessPurchase(PurchaseItemData data)
        {
            if (data == null) return;
            Debug.Log($"[IAPManager] 🛒 Processing: {data.productName}");

            if (!data.isRealMoney && data.gemCost > 0)
            {
                if (!CanAffordGems(data.gemCost)) return;
                SpendGems(data.gemCost);

                switch (data.type)
                {
                    case PurchaseType.UnlockCow:
                        HandleCowUnlock(data.targetIndex);
                        break;
                    case PurchaseType.UnlockArea:
                        HandleAreaUnlock(data.targetIndex);
                        break;
                    case PurchaseType.UnlockChicken:
                        HandleChickenUnlock(data.targetIndex);
                        break;
                    case PurchaseType.UnlockChickenArea:
                        HandleChickenAreaUnlock(data.targetIndex);
                        break;
                    case PurchaseType.UnlockTrough:
                        Debug.Log($"[IAPManager] Trough unlock");
                        break;
                }
            }
            else if (data.isRealMoney)
            {
                Debug.Log("[IAPManager] Real money purchase");
            }
        }

        // === COW UNLOCK (inek save'ine yazar) ===

        private void HandleCowUnlock(int cowIndex)
        {
            if (animalManager == null) return;

            var saveData = saveManager.GetCurrentSaveData();
            if (saveData == null) return;
            if (cowIndex < 0 || cowIndex >= saveData.cows.Count) return;

            var cowData = saveData.cows[cowIndex];
            if (cowData.isUnlocked) return;

            // Save data güncelle
            cowData.isUnlocked = true;
            if (!saveData.unlockedCows.Contains(cowIndex))
                saveData.unlockedCows.Add(cowIndex);
            saveManager.SaveGame(saveData);

            // Runtime güncelle
            animalManager.MarkAnimalAsUnlocked(cowIndex, true);
            animalManager.SpawnAnimal(cowIndex);

            MilkFarmEvents.CowUnlocked(cowIndex);
            Debug.Log($"[IAPManager] ✅ Cow {cowIndex} unlocked!");
        }

        // === CHICKEN UNLOCK (tavuk save'ine yazar) ===

        private void HandleChickenUnlock(int chickenIndex)
        {
            if (animalManager == null) return;

            var saveData = saveManager.GetCurrentSaveData();
            if (saveData == null) return;

            // ✅ chickens listesini kontrol et (cows değil!)
            if (saveData.chickens == null || chickenIndex < 0 || chickenIndex >= saveData.chickens.Count)
            {
                Debug.LogError($"[IAPManager] Invalid chicken index: {chickenIndex}");
                return;
            }

            var chickenData = saveData.chickens[chickenIndex];
            if (chickenData.isUnlocked) return;

            // Save data güncelle (chickens listesine)
            chickenData.isUnlocked = true;
            if (!saveData.unlockedChickens.Contains(chickenIndex))
                saveData.unlockedChickens.Add(chickenIndex);
            saveManager.SaveGame(saveData);

            // Runtime güncelle (IAnimalManager = ChickenManager)
            animalManager.MarkAnimalAsUnlocked(chickenIndex, true);
            animalManager.SpawnAnimal(chickenIndex);

            MilkFarmEvents.CowUnlocked(chickenIndex); // Aynı event, UI güncellensin
            Debug.Log($"[IAPManager] ✅ Chicken {chickenIndex} unlocked!");
        }

        // === AREA UNLOCK (inek) ===

        private void HandleAreaUnlock(int areaIndex)
        {
            if (stableManager == null) return;
            if (stableManager.IsStableUnlocked(areaIndex)) return;

            stableManager.UnlockStable(areaIndex, free: true);

            // ✅ unlockedAreas'a ekle
            var saveData = saveManager.GetCurrentSaveData();
            if (!saveData.unlockedAreas.Contains(areaIndex))
            {
                saveData.unlockedAreas.Add(areaIndex);
                saveManager.SaveGame(saveData);
            }

            MilkFarmEvents.AreaUnlocked(areaIndex);
            Debug.Log($"[IAPManager] ✅ Cow Stable {areaIndex} unlocked!");
        }

        // === CHICKEN AREA UNLOCK (tavuk) ===

        private void HandleChickenAreaUnlock(int areaIndex)
        {
            if (stableManager == null) return;
            if (stableManager.IsChickenStableUnlocked(areaIndex)) return;

            stableManager.UnlockStable(areaIndex, free: true);

            // ✅ unlockedChickenAreas'a ekle (ayrı liste!)
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData.unlockedChickenAreas == null)
                saveData.unlockedChickenAreas = new List<int>();
            if (!saveData.unlockedChickenAreas.Contains(areaIndex))
            {
                saveData.unlockedChickenAreas.Add(areaIndex);
                saveManager.SaveGame(saveData);
            }

            MilkFarmEvents.AreaUnlocked(areaIndex);
            Debug.Log($"[IAPManager] ✅ Chicken Stable {areaIndex} unlocked!");
        }
        public void UnlocChickenArea(int areaIndex)
        {
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData == null) return;
            if (!saveData.unlockedChickenAreas.Contains(areaIndex))
            {
                saveData.unlockedChickenAreas.Add(areaIndex);
                saveManager.SaveGame(saveData);
            }
        }
        // === IAP TIER SETTERS ===

        private void SetSpeedTier(int tier)
        {
            if (saveManager == null) return;
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData.iap == null) saveData.iap = new IAPSaveData();
            saveData.iap.speedTier = tier;
            saveManager.SaveGame(saveData);
        }

        private void SetRichTier(int tier)
        {
            if (saveManager == null) return;
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData.iap == null) saveData.iap = new IAPSaveData();
            saveData.iap.richCustomerTier = tier;
            saveManager.SaveGame(saveData);
        }

        private void IncrementStorageBoost()
        {
            if (saveManager == null) return;
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData.iap == null) saveData.iap = new IAPSaveData();
            saveData.iap.milkStorageBoostLevel++;
            saveManager.SaveGame(saveData);
        }

        private void SetAutoFeeder(bool value)
        {
            if (saveManager == null) return;
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData.iap == null) saveData.iap = new IAPSaveData();
            saveData.iap.hasAutoFeeder = value;
            saveManager.SaveGame(saveData);
        }

        private void SetAutoWorker(bool value)
        {
            if (saveManager == null) return;
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData.iap == null) saveData.iap = new IAPSaveData();
            saveData.iap.hasAutoWorker = value;
            saveManager.SaveGame(saveData);
        }

        // === UNLOCK QUERY METHODS ===

        public bool IsAreaUnlocked(int areaIndex)
        {
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData == null) return false;
            return saveData.unlockedAreas.Contains(areaIndex);
        }

        public bool IsChickenAreaUnlocked(int areaIndex)
        {
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData == null) return false;
            if (saveData.unlockedChickenAreas == null) return false;
            return saveData.unlockedChickenAreas.Contains(areaIndex);
        }

        public void UnlockCow(int cowIndex)
        {
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData == null) return;
            if (!saveData.unlockedCows.Contains(cowIndex))
            {
                saveData.unlockedCows.Add(cowIndex);
                saveManager.SaveGame(saveData);
            }
        }

        public void UnlockArea(int areaIndex)
        {
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData == null) return;
            if (!saveData.unlockedAreas.Contains(areaIndex))
            {
                saveData.unlockedAreas.Add(areaIndex);
                saveManager.SaveGame(saveData);
            }
        }

        private void UnlockTrough(int areaIndex)
        {
            if (saveManager == null) return;
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData.iap == null) saveData.iap = new IAPSaveData();
            if (!saveData.iap.unlockedTroughs.Contains(areaIndex))
            {
                saveData.iap.unlockedTroughs.Add(areaIndex);
                saveManager.SaveGame(saveData);
            }
            MilkFarmEvents.TroughUnlocked(areaIndex);
        }

        // === DEBUG ===

        [ContextMenu("Add 100 Gems")]
        public void DebugAddGems() => AddGems(100);

        [ContextMenu("Print Status")]
        public void DebugPrintStatus()
        {
            if (saveManager == null) return;
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData?.iap == null) return;
            Debug.Log($"[IAPManager] Gems: {saveData.iap.gems}");
            Debug.Log($"[IAPManager] Unlocked Cows: {string.Join(", ", saveData.iap.unlockedCows)}");
            Debug.Log($"[IAPManager] Unlocked Chickens: {string.Join(", ", saveData.iap.unlockedChickens)}");
        }
    }
}