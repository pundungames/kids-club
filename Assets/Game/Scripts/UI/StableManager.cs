using UnityEngine;
using System.Collections.Generic;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// Stable Management - 4 stables, 3 cows each
    /// </summary>
    public class StableManager : MonoBehaviour
    {
        [Inject] private CowManager cowManager;
        [Inject] private IAPManager iapManager;
        [Inject] private GameConfig config;

        [Header("Stable Settings")]
        [SerializeField] private int totalStables = 4;
        [SerializeField] private int cowsPerStable = 3;

        [Header("Stable Unlock Costs (Gems)")]
        [SerializeField] private int[] stableUnlockCosts = { 0, 500, 1000, 2000 }; // Stable 1 free

        private void Start()
        {
            // Stable 1 default unlocked
            if (!IsStableUnlocked(0))
            {
                UnlockStable(0, free: true);
            }
        }

        /// <summary>
        /// Check if stable is unlocked
        /// </summary>
        public bool IsStableUnlocked(int stableIndex)
        {
            if (stableIndex < 0 || stableIndex >= totalStables) return false;
            return iapManager.IsAreaUnlocked(stableIndex);
        }

        /// <summary>
        /// Unlock stable with gems
        /// </summary>
        public bool UnlockStable(int stableIndex, bool free = false)
        {
            if (stableIndex < 0 || stableIndex >= totalStables) return false;
            if (IsStableUnlocked(stableIndex)) return false;

            if (!free)
            {
                int cost = GetStableUnlockCost(stableIndex);
                if (!iapManager.SpendGems(cost)) return false;
            }

            iapManager.UnlockArea(stableIndex);
            Debug.Log($"[StableManager] Stable {stableIndex} unlocked!");
            return true;
        }

        /// <summary>
        /// Get unlock cost for stable
        /// </summary>
        public int GetStableUnlockCost(int stableIndex)
        {
            if (stableIndex < 0 || stableIndex >= stableUnlockCosts.Length)
                return 1000;
            return stableUnlockCosts[stableIndex];
        }

        /// <summary>
        /// Get all cows in a stable
        /// </summary>
        public List<Cow> GetCowsInStable(int stableIndex)
        {
            List<Cow> cows = new List<Cow>();
            int startIndex = stableIndex * cowsPerStable;
            int endIndex = startIndex + cowsPerStable;
            var allCows = cowManager.GetAllCows();

            for (int i = startIndex; i < endIndex && i < allCows.Count; i++)
            {
                cows.Add(allCows[i]);
            }
            return cows;
        }

        /// <summary>
        /// Get stable capacity (current/max)
        /// </summary>
        public (int current, int max) GetStableCapacity(int stableIndex)
        {
            var cows = GetCowsInStable(stableIndex);
            int unlocked = 0;
            foreach (var cow in cows)
            {
                if (cow.isUnlocked) unlocked++;
            }
            return (unlocked, cowsPerStable);
        }

        public int GetTotalStables() => totalStables;
        public int GetCowsPerStable() => cowsPerStable;
    }
}