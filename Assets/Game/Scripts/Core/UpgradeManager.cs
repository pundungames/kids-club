using UnityEngine;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// Soft currency (altın) ile yapılacak upgrade'leri yöneten manager
    /// - İnek Level Up (Speed)
    /// - Paketleme Depo Kapasitesi
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        [Inject] private CowManager cowManager;
        [Inject] private PackageManager packageManager;
        [Inject] private MoneyManager moneyManager;

        // === İNEK LEVEL UP ===

        /// <summary>
        /// İnek level'ını yükselt
        /// </summary>
        public bool UpgradeCowLevel(int cowIndex)
        {
            var cow = cowManager.GetCow(cowIndex);
            if (cow == null || !cow.isUnlocked)
            {
                Debug.LogWarning($"[UpgradeManager] İnek {cowIndex} açılmamış veya geçersiz!");
                return false;
            }

            return cowManager.UpgradeCowLevel(cowIndex, moneyManager);
        }

        /// <summary>
        /// İnek upgrade maliyetini al
        /// </summary>
        public float GetCowUpgradeCost(int cowIndex)
        {
            var cow = cowManager.GetCow(cowIndex);
            if (cow == null) return float.MaxValue;

            return cowManager.CalculateCowUpgradeCost(cow.level);
        }

        /// <summary>
        /// İnek upgrade yapılabilir mi?
        /// </summary>
        public bool CanUpgradeCow(int cowIndex)
        {
            var cow = cowManager.GetCow(cowIndex);
            if (cow == null || !cow.isUnlocked) return false;

            float cost = GetCowUpgradeCost(cowIndex);
            return moneyManager.CanAfford(cost);
        }

        // === PAKETLEME KAPASİTESİ ===

        /// <summary>
        /// Paketleme depo kapasitesini artır
        /// </summary>
        public bool UpgradePackageCapacity()
        {
            return packageManager.UpgradeCapacity(moneyManager);
        }

        /// <summary>
        /// Paketleme kapasite upgrade maliyetini al
        /// </summary>
        public float GetPackageCapacityUpgradeCost()
        {
            return packageManager.CalculateCapacityUpgradeCost();
        }

        /// <summary>
        /// Paketleme kapasite upgrade yapılabilir mi?
        /// </summary>
        public bool CanUpgradePackageCapacity()
        {
            float cost = GetPackageCapacityUpgradeCost();
            return moneyManager.CanAfford(cost);
        }

        // === GENEL ===

        /// <summary>
        /// Mevcut para miktarını al
        /// </summary>
        public float GetCurrentMoney()
        {
            return moneyManager.GetCurrentMoney();
        }

        /// <summary>
        /// Debug: Tüm inekleri max level yap
        /// </summary>
        [ContextMenu("Debug: Max All Cow Levels")]
        public void DebugMaxAllCowLevels()
        {
            var cows = cowManager.GetAllCows();
            foreach (var cow in cows)
            {
                if (cow.isUnlocked)
                {
                    cow.level = 10; // Max level örneği
                }
            }
            Debug.Log("[UpgradeManager] Tüm inekler max level yapıldı!");
        }
    }
}
