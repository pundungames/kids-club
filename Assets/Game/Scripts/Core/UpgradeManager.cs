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
        [Inject] private IAnimalManager animalManager;
        [Inject] private PackageManager packageManager;
        [Inject] private MoneyManager moneyManager;
        [Inject] IAPManager iapManager;

        // === İNEK LEVEL UP ===

        /// <summary>
        /// İnek level'ını yükselt
        /// </summary>
        public bool UpgradeCowLevel(int cowIndex)
        {
            var animal = animalManager.GetAnimal(cowIndex);
            if (animal == null || !animal.isUnlocked) return false;
            // Not: UpgradeAnimal zaten para kontrolü yapıyor
            return animalManager.UpgradeAnimal(cowIndex, iapManager);
        }
        /// <summary>
        /// İnek upgrade maliyetini al
        /// </summary>
        public float GetCowUpgradeCost(int cowIndex)
        {
            var animal = animalManager.GetAnimal(cowIndex);
            if (animal == null) return float.MaxValue;
            return animalManager.CalculateUpgradeCost(animal.level);
        }
        /// <summary>
        /// İnek upgrade yapılabilir mi?
        /// </summary>
        public bool CanUpgradeCow(int cowIndex)
        {
            var animal = animalManager.GetAnimal(cowIndex);
            if (animal == null || !animal.isUnlocked) return false;
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
    }
}
