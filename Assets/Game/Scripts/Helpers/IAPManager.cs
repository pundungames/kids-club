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
        [Inject] private CowManager cowManager;
        [Inject] private StableManager stableManager;

        // === GEM SYSTEM ===
        private void Start()
        {
            AddGems(100);
        }
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

        public void ProcessPurchase(PurchaseItemData data)
        {
            if (data == null)
            {
                Debug.LogError("[IAPManager] Purchase data null!");
                return;
            }

            Debug.Log($"[IAPManager] 🛒 Processing: {data.productName}");

            // Gem purchase (stable system)
            if (!data.isRealMoney && data.gemCost > 0)
            {
                if (!CanAffordGems(data.gemCost))
                {
                    Debug.LogWarning("[IAPManager] ❌ Not enough gems!");
                    // TODO: Show "not enough gems" popup
                    return;
                }

                SpendGems(data.gemCost);

                // Handle unlock based on type
                switch (data.type)
                {
                    case PurchaseType.UnlockCow:
                        HandleCowUnlock(data.targetIndex);
                        break;

                    case PurchaseType.UnlockArea:
                        HandleAreaUnlock(data.targetIndex);
                        break;

                    case PurchaseType.UnlockTrough:
                        // Keep existing trough unlock logic
                        Debug.Log($"[IAPManager] Trough unlock - use existing system");
                        break;

                    default:
                        Debug.LogWarning($"[IAPManager] Unknown unlock type: {data.type}");
                        break;
                }

                Debug.Log($"[IAPManager] ✅ Purchase complete!");
            }
            // Real money IAP
            else if (data.isRealMoney)
            {
                // Existing real money IAP logic
                Debug.Log("[IAPManager] Real money purchase");
            }
        }
        private void HandleCowUnlock(int cowIndex)
        {
            if (cowManager == null)
            {
                Debug.LogError("[IAPManager] CowManager null!");
                return;
            }

            // Save data unlock
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData == null) return;

            if (cowIndex < 0 || cowIndex >= saveData.cows.Count)
            {
                Debug.LogError($"[IAPManager] Invalid cow index: {cowIndex}");
                return;
            }

            var cowData = saveData.cows[cowIndex];

            if (cowData.isUnlocked)
            {
                Debug.LogWarning($"[IAPManager] Cow {cowIndex} already unlocked!");
                return;
            }

            // Unlock in save data
            cowData.isUnlocked = true;
            UnlockCow(cowIndex); // Add to unlockedCows list
            saveManager.SaveGame(saveData);

            // Update runtime cow list
            var allCows = cowManager.GetAllCows();
            if (cowIndex < allCows.Count)
            {
                allCows[cowIndex].isUnlocked = true;
            }

            // Reload to spawn
            cowManager.LoadFromSaveData();

            // ✅ FIRE EVENT (3D scene + UI güncellenir)
            MilkFarmEvents.CowUnlocked(cowIndex);

            Debug.Log($"[IAPManager] ✅ Cow {cowIndex} unlocked!");
        }

        private void HandleAreaUnlock(int areaIndex)
        {
            if (stableManager == null)
            {
                Debug.LogError("[IAPManager] StableManager null!");
                return;
            }

            if (stableManager.IsStableUnlocked(areaIndex))
            {
                Debug.LogWarning($"[IAPManager] Stable {areaIndex} already unlocked!");
                return;
            }

            // Unlock stable (free=true because gems already spent)
            // This will add to unlockedAreas list
            stableManager.UnlockStable(areaIndex, free: true);

            // ✅ FIRE EVENT (3D scene güncellenir)
            MilkFarmEvents.AreaUnlocked(areaIndex);

            Debug.Log($"[IAPManager] ✅ Stable {areaIndex} unlocked!");
        }
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
        public bool IsAreaUnlocked(int areaIndex)
        {
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData == null) return false;
            return saveData.unlockedAreas.Contains(areaIndex);
        }
        public void UnlockCow(int cowIndex)
        {
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData == null) return;

            if (!saveData.unlockedCows.Contains(cowIndex))
            {
                saveData.unlockedCows.Add(cowIndex);
                saveManager.SaveGame(saveData);
                Debug.Log($"[IAPManager] ✅ Cow {cowIndex} unlocked!");
            }
        }
        /// <summary>
        /// Get speed multiplier from IAP tier
        /// </summary>
        public float GetSpeedMultiplier()
        {
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData == null || saveData.iap == null) return 1f;

            // speedTier: 0 = normal (1x), 1 = +50% (1.5x), 2 = +100% (2x)
            switch (saveData.iap.speedTier)
            {
                case 1: return 1.5f;
                case 2: return 2f;
                default: return 1f;
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
                Debug.Log($"[IAPManager] ✅ Area {areaIndex} unlocked!");
            }
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