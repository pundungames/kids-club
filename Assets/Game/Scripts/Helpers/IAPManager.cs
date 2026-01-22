using UnityEngine;
using Zenject;
using System.Collections.Generic;

namespace MilkFarm
{
    /// <summary>
    /// IAP Manager - NULL SAFE VERSION
    /// SaveManager null check'leri eklendi
    /// </summary>
    public class IAPManager : MonoBehaviour
    {
        [Inject] private SaveManager saveManager;

        // === GEM SYSTEM ===

        public int GetCurrentGems()
        {
            if (saveManager == null)
            {
                Debug.LogWarning("[IAPManager] SaveManager NULL!");
                return 0;
            }

            var saveData = saveManager.GetCurrentSaveData();
            if (saveData == null || saveData.iap == null)
            {
                return 0;
            }

            return saveData.iap.gems;
        }

        public bool CanAffordGems(int cost) => GetCurrentGems() >= cost;

        public bool SpendGems(int amount)
        {
            if (saveManager == null) return false;

            if (!CanAffordGems(amount))
            {
                Debug.LogWarning($"[IAPManager] ❌ Yetersiz gem!");
                return false;
            }

            var saveData = saveManager.GetCurrentSaveData();
            if (saveData.iap == null) saveData.iap = new IAPSaveData();

            saveData.iap.gems -= amount;
            saveManager.SaveGame(saveData);

            MilkFarmEvents.GemChanged();

            Debug.Log($"[IAPManager] 💎 {amount} gem harcandı. Kalan: {saveData.iap.gems}");
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

            Debug.Log($"[IAPManager] 💎 {amount} gem eklendi. Toplam: {saveData.iap.gems}");
        }

        // === IAP BOOSTS (NULL SAFE) ===

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

        // === PURCHASE ===

        public void ProcessPurchase(PurchaseItemData data)
        {
            if (saveManager == null)
            {
                Debug.LogError("[IAPManager] ❌ SaveManager NULL - Purchase iptal!");
                return;
            }

            if (data == null)
            {
                Debug.LogError("[IAPManager] ❌ Purchase data NULL!");
                return;
            }

            Debug.Log($"[IAPManager] 🛒 Purchase: {data.productName}");

            if (data.isRealMoney) return;

            if (data.gemCost > 0 && !SpendGems(data.gemCost))
            {
                return;
            }

            switch (data.type)
            {
                case PurchaseType.SpeedBoost50: SetSpeedTier(1); break;
                case PurchaseType.SpeedBoost100: SetSpeedTier(2); break;
                case PurchaseType.RichCustomer50: SetRichTier(1); break;
                case PurchaseType.RichCustomer100: SetRichTier(2); break;
                case PurchaseType.MilkStorage: IncrementStorageBoost(); break;
                case PurchaseType.AutoFeeder: SetAutoFeeder(true); break;
                case PurchaseType.AutoWorker: SetAutoWorker(true); break;
                case PurchaseType.UnlockArea: UnlockArea(data.targetIndex); break;
                case PurchaseType.UnlockTrough: UnlockTrough(data.targetIndex); break;
                case PurchaseType.UnlockCow: UnlockCow(data.targetIndex); break;
            }
        }

        // === IAP BOOST METHODS ===

        private void SetSpeedTier(int tier)
        {
            if (saveManager == null) return;

            var saveData = saveManager.GetCurrentSaveData();
            if (saveData.iap == null) saveData.iap = new IAPSaveData();
            saveData.iap.speedTier = tier;
            saveManager.SaveGame(saveData);
            Debug.Log($"[IAPManager] ✅ Speed tier: {tier}");
        }

        private void SetRichTier(int tier)
        {
            if (saveManager == null) return;

            var saveData = saveManager.GetCurrentSaveData();
            if (saveData.iap == null) saveData.iap = new IAPSaveData();
            saveData.iap.richCustomerTier = tier;
            saveManager.SaveGame(saveData);
            Debug.Log($"[IAPManager] ✅ Rich tier: {tier}");
        }

        private void IncrementStorageBoost()
        {
            if (saveManager == null) return;

            var saveData = saveManager.GetCurrentSaveData();
            if (saveData.iap == null) saveData.iap = new IAPSaveData();
            saveData.iap.milkStorageBoostLevel++;
            saveManager.SaveGame(saveData);
            Debug.Log($"[IAPManager] ✅ Storage boost: {saveData.iap.milkStorageBoostLevel}");
        }

        private void SetAutoFeeder(bool value)
        {
            if (saveManager == null) return;

            var saveData = saveManager.GetCurrentSaveData();
            if (saveData.iap == null) saveData.iap = new IAPSaveData();
            saveData.iap.hasAutoFeeder = value;
            saveManager.SaveGame(saveData);
            Debug.Log($"[IAPManager] ✅ Auto Feeder: {value}");
        }

        private void SetAutoWorker(bool value)
        {
            if (saveManager == null) return;

            var saveData = saveManager.GetCurrentSaveData();
            if (saveData.iap == null) saveData.iap = new IAPSaveData();
            saveData.iap.hasAutoWorker = value;
            saveManager.SaveGame(saveData);
            Debug.Log($"[IAPManager] ✅ Auto Worker: {value}");
        }

        // === UNLOCK METHODS ===

        private void UnlockCow(int globalIndex)
        {
            if (saveManager == null) return;

            Debug.Log($"[IAPManager] 🐄 UnlockCow: {globalIndex}");

            var saveData = saveManager.GetCurrentSaveData();
            if (saveData.iap == null) saveData.iap = new IAPSaveData();

            if (!saveData.iap.unlockedCows.Contains(globalIndex))
            {
                saveData.iap.unlockedCows.Add(globalIndex);
                saveManager.SaveGame(saveData);
            }

            MilkFarmEvents.CowUnlocked(globalIndex);
        }

        private void UnlockArea(int areaIndex)
        {
            if (saveManager == null) return;

            Debug.Log($"[IAPManager] 🏗️ UnlockArea: {areaIndex}");

            var saveData = saveManager.GetCurrentSaveData();
            if (saveData.iap == null) saveData.iap = new IAPSaveData();

            if (!saveData.iap.unlockedAreas.Contains(areaIndex))
            {
                saveData.iap.unlockedAreas.Add(areaIndex);
                saveManager.SaveGame(saveData);
            }

            MilkFarmEvents.AreaUnlocked(areaIndex);
        }

        private void UnlockTrough(int areaIndex)
        {
            if (saveManager == null) return;

            Debug.Log($"[IAPManager] 🥤 UnlockTrough: {areaIndex}");

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
            if (saveManager == null)
            {
                Debug.LogError("[IAPManager] SaveManager NULL!");
                return;
            }

            var saveData = saveManager.GetCurrentSaveData();
            if (saveData?.iap == null)
            {
                Debug.Log("[IAPManager] No IAP data");
                return;
            }

            Debug.Log($"[IAPManager] Gems: {saveData.iap.gems}");
            Debug.Log($"[IAPManager] Unlocked Cows: {string.Join(", ", saveData.iap.unlockedCows)}");
        }
    }
}