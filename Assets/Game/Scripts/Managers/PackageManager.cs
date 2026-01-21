using UnityEngine;
using Zenject;
using System.Collections.Generic;

namespace MilkFarm
{
    public class PackageManager : MonoBehaviour
    {
        [Inject] private GameConfig config;
        [Inject] private SaveManager saveManager;
        [Inject] private MoneyManager moneyManager;

        [Header("Referanslar")]
        [SerializeField] private CustomerManager customerManager;

        [Header("1. ÜRETİM ALANI")]
        [SerializeField] private Transform caseSpawnPoint;
        [SerializeField] private GameObject cratePrefab;
        [SerializeField] private GameObject milkBottlePrefab;

        [Header("2. SATIŞ ALANI")]
        [SerializeField] private Transform[] salesSlots;

        private MilkCrate currentActiveCrate;
        private MilkCrate[] salesSlotsContents;
        private bool isProductionBusy = false;

        private int capacityLevel = 1;
        private int currentCapacity;

        private void Awake()
        {
            if (salesSlots != null)
            {
                salesSlotsContents = new MilkCrate[salesSlots.Length];
            }
        }

        private void Start()
        {
            LoadFromSaveData();
            UpdateCapacity();
            SpawnNewCrate();
        }

        public void LoadFromSaveData()
        {
            var saveData = saveManager.GetCurrentSaveData();
            capacityLevel = saveData.packaging.capacityLevel;
            UpdateCapacity();
        }

        public void SaveToData()
        {
            var saveData = saveManager.GetCurrentSaveData();
            saveData.packaging.capacityLevel = capacityLevel;
            saveManager.SaveGame(saveData);
        }

        private void UpdateCapacity()
        {
            currentCapacity = config.packageStationCapacityBase + (capacityLevel - 1) * 4;
        }

        private void SpawnNewCrate()
        {
            if (currentActiveCrate != null) return;
            if (caseSpawnPoint == null || cratePrefab == null) return;

            GameObject newCrateObj = Instantiate(cratePrefab, caseSpawnPoint.position, caseSpawnPoint.rotation);
            currentActiveCrate = newCrateObj.GetComponent<MilkCrate>();

            if (currentActiveCrate != null)
            {
                newCrateObj.transform.SetParent(caseSpawnPoint);
                newCrateObj.transform.localScale = Vector3.one * 1.2f;
            }

            isProductionBusy = false;
        }

        public void AddMilk(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                AddMilk(caseSpawnPoint != null ? caseSpawnPoint.position : transform.position);
            }
        }

        public void AddMilk(Vector3 milkStartPos)
        {
            if (currentActiveCrate == null || !currentActiveCrate.HasSpace || isProductionBusy)
            {
                Debug.LogWarning("[PackageManager] Kasa dolu veya meşgul!");
                return;
            }

            currentActiveCrate.AddMilkToCrate(milkBottlePrefab, milkStartPos, OnCrateFullyFilled);
            MilkFarmEvents.MilkAddedToStation(0);
        }

        private void OnCrateFullyFilled()
        {
            TryMoveCrateToSales();
        }

        private void TryMoveCrateToSales()
        {
            if (currentActiveCrate == null || !currentActiveCrate.IsPhysicallyFull) return;

            int emptySlotIndex = -1;
            for (int i = 0; i < salesSlotsContents.Length; i++)
            {
                if (salesSlotsContents[i] == null)
                {
                    emptySlotIndex = i;
                    break;
                }
            }

            if (emptySlotIndex != -1)
            {
                isProductionBusy = true;
                Transform targetSlot = salesSlots[emptySlotIndex];
                salesSlotsContents[emptySlotIndex] = currentActiveCrate;
                MilkCrate movingCrate = currentActiveCrate;
                currentActiveCrate = null;

                FlyingItem flyer = movingCrate.gameObject.GetComponent<FlyingItem>();
                if (flyer == null) flyer = movingCrate.gameObject.AddComponent<FlyingItem>();

                flyer.FlyTo(targetSlot.position, () =>
                {
                    if (movingCrate != null)
                    {
                        movingCrate.transform.SetParent(targetSlot);
                        movingCrate.transform.localPosition = Vector3.zero;
                        movingCrate.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                        movingCrate.transform.localScale = Vector3.one * 0.6f;
                    }

                    MilkFarmEvents.PackageCreated(0);
                    SaveToData();

                    Debug.Log($"[PackageManager] Kasa satış alanına taşındı!");

                    SpawnNewCrate();
                });
            }
            else
            {
                Debug.LogWarning("[PackageManager] Satış slotları dolu!");
                MilkFarmEvents.PackageStationFull();
            }
        }

        // === MÜŞTERİ BAZLI SATIŞ ===

        public void OnStationClicked()
        {
            Debug.Log("[PackageManager] Masaya tıklandı! Müşteri bazlı satış...");
            TrySellToCustomer();
        }

        /// <summary>
        /// Müşteriye tamamını sat (tek tık)
        /// </summary>
        public bool TrySellToCustomer()
        {
            // Müşteri var mı?
            if (customerManager == null || !customerManager.HasWaitingCustomer())
            {
                Debug.LogWarning("[PackageManager] Bekleyen müşteri yok!");
                return false;
            }

            // İlk müşteriyi al
            Customer firstCustomer = customerManager.GetFirstCustomer();
            if (firstCustomer == null || firstCustomer.controller == null)
            {
                Debug.LogWarning("[PackageManager] Müşteri verisi bulunamadı!");
                return false;
            }

            // Kaç şişe gerekiyor?
            int needed = firstCustomer.controller.GetRemainingBottles();
            Debug.Log($"[PackageManager] Müşteri {needed} şişe istiyor.");

            // Kasalardan şişe çıkar
            int given = 0;
            for (int i = 0; i < salesSlotsContents.Length && given < needed; i++)
            {
                MilkCrate crate = salesSlotsContents[i];
                if (crate == null) continue;

                while (crate.CurrentBottleCount > 0 && given < needed)
                {
                    if (crate.RemoveOneBottle())
                    {
                        given++;

                        // Müşteriye ver
                        customerManager.ServeBottleToCustomer();
                    }
                    else
                    {
                        break;
                    }
                }

                // Kasa boşaldıysa yok et
                if (crate.CurrentBottleCount <= 0)
                {
                    Debug.Log($"[PackageManager] Kasa {i} boşaldı, yok ediliyor.");
                    Destroy(crate.gameObject);
                    salesSlotsContents[i] = null;
                }
            }

            Debug.Log($"[PackageManager] {given} şişe verildi.");

            if (given > 0)
            {
                MilkFarmEvents.PackageSold(0);
                SaveToData();
                return true;
            }

            Debug.LogWarning("[PackageManager] Stokta yeterli şişe yok!");
            return false;
        }

        /// <summary>
        /// Toplam kaç şişe var?
        /// </summary>
        public int GetTotalBottleCount()
        {
            int total = 0;
            foreach (var crate in salesSlotsContents)
            {
                if (crate != null)
                {
                    total += crate.CurrentBottleCount;
                }
            }
            return total;
        }

        // === ESKİ API ===

        public int GetCurrentPackageCount() => GetTotalBottleCount();
        public int GetCurrentCapacity() => currentCapacity;
        public bool HasPackages() => GetTotalBottleCount() > 0;
        public bool HasBottles() => GetTotalBottleCount() > 0;
        public int GetAvailableBottles() => GetTotalBottleCount();
        public bool IsFull() => GetTotalBottleCount() >= currentCapacity;

        public bool UpgradeCapacity(MoneyManager money)
        {
            float cost = CalculateCapacityUpgradeCost();
            if (!money.CanAfford(cost)) return false;

            money.SpendMoney(cost);
            capacityLevel++;
            UpdateCapacity();

            MilkFarmEvents.UpgradePurchased("PackageCapacity", capacityLevel, cost);
            SaveToData();

            Debug.Log($"[PackageManager] Kapasite upgrade! Yeni: {currentCapacity}");
            return true;
        }

        public float CalculateCapacityUpgradeCost()
        {
            return config.baseCostDepo * Mathf.Pow(config.costMultiplierDepo, capacityLevel - 1);
        }

        [ContextMenu("Debug: Add 10 Milk")]
        public void DebugAddMilk()
        {
            AddMilk(10);
        }

        [ContextMenu("Debug: Print Stock")]
        public void DebugPrintStock()
        {
            Debug.Log($"[PackageManager] Toplam stok: {GetTotalBottleCount()} şişe");
            for (int i = 0; i < salesSlotsContents.Length; i++)
            {
                if (salesSlotsContents[i] != null)
                {
                    Debug.Log($"  Kasa {i}: {salesSlotsContents[i].CurrentBottleCount} şişe");
                }
            }
        }
    }
}