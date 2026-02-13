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
        [Inject] private IAnimalManager animalManager;
        [Inject] private IAPManager iapManager;
        [Inject] private GameConfig config;

        [Header("Stable Settings")]
        [SerializeField] private int totalStables = 4;
        [SerializeField] private int cowsPerStable = 3;
        [SerializeField] internal bool isChicken;
        [Header("Stable Unlock Costs (Gems)")]
        [SerializeField] private int[] stableUnlockCosts = { 0, 10, 20, 30 }; // Stable 1 free

        private void Start()
        {
            // Stable 1 default unlocked
            if (!IsStableUnlocked(0))
            {
                UnlockStable(0, free: true);
            }
            if (!IsChickenStableUnlocked(0))
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
        public bool IsChickenStableUnlocked(int stableIndex)
        {
            if (stableIndex < 0 || stableIndex >= totalStables) return false;
            return iapManager.IsChickenAreaUnlocked(stableIndex);
        }
        /// <summary>
        /// Unlock stable with gems
        /// </summary>
        public bool UnlockStable(int stableIndex, bool free = false)
        {
            if (stableIndex < 0 || stableIndex >= totalStables) return false;
            if (isChicken)
            {
                if (IsChickenStableUnlocked(stableIndex)) return false;
            }
            else
            {
                if (IsStableUnlocked(stableIndex)) return false;
            }
            if (!free)
            {
                int cost = GetStableUnlockCost(stableIndex);
                if (!iapManager.SpendGems(cost)) return false;
            }

            if (isChicken) iapManager.UnlocChickenArea(stableIndex);
            else
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
        public List<AnimalData> GetAnimalsInStable(int stableIndex)
        {
            List<AnimalData> animals = new List<AnimalData>();
            int startIndex = stableIndex * cowsPerStable;
            int endIndex = startIndex + cowsPerStable;
            var allAnimals = animalManager.GetAllAnimals();
            for (int i = startIndex; i < endIndex && i < allAnimals.Count; i++)
                animals.Add(allAnimals[i]);
            return animals;
        }
        public List<Cow> GetCowsInStable(int stableIndex)
        {
            // Sadece CowManager sahnesinde çalýþýr
            var cowMgr = animalManager as CowManager;
            if (cowMgr == null) return new List<Cow>();
            var allCows = cowMgr.GetAllCows();
            List<Cow> cows = new List<Cow>();
            int start = stableIndex * cowsPerStable;
            int end = start + cowsPerStable;
            for (int i = start; i < end && i < allCows.Count; i++)
                cows.Add(allCows[i]);
            return cows;
        }
        /// <summary>
        /// Get stable capacity (current/max)
        /// </summary>
        public (int current, int max) GetStableCapacity(int stableIndex)
        {
            var animals = GetAnimalsInStable(stableIndex);
            int unlocked = 0;
            foreach (var a in animals) if (a.isUnlocked) unlocked++;
            return (unlocked, cowsPerStable);
        }

        public int GetTotalStables() => totalStables;
        public int GetCowsPerStable() => cowsPerStable;
    }
}