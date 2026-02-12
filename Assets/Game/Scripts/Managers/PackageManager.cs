using UnityEngine;
using Zenject;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MilkFarm
{
    public class PackageManager : MonoBehaviour
    {
        [Inject] private GameConfig config;
        [Inject] private SaveManager saveManager;
        [Inject] private MoneyManager moneyManager;

        [Header("Referanslar")]
        [SerializeField] private CustomerManager customerManager;

        [Header("1. ÜRETİM ALANI (Production Stack)")]
        [SerializeField] private Transform caseSpawnPoint;
        [SerializeField] private GameObject cratePrefab;
        [SerializeField] private GameObject milkBottlePrefab;
        [SerializeField] private Vector3 gridOffset = new Vector3(-0.434f, 0.309f, -0.315f);
        [SerializeField] private int maxProductionStack = 8;
        [SerializeField] private GameObject productionFullIcon;

        [Header("2. SATIŞ ALANI (2x2 Grid)")]
        [SerializeField] private Transform[] salesSlots;
        [SerializeField] private GameObject salesFullIcon;

        private List<MilkCrate> productionStack = new List<MilkCrate>();
        private MilkCrate currentActiveCrate;
        private MilkCrate[] salesSlotsContents;

        private int capacityLevel = 1;
        private int currentCapacity;

        private bool isAddingMilk = false;
        private Queue<Vector3> milkQueue = new Queue<Vector3>();

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

            if (productionFullIcon != null) productionFullIcon.SetActive(false);
            if (salesFullIcon != null) salesFullIcon.SetActive(false);
        }
        private void OnEnable()
        {
            MilkFarmEvents.OnSaveRequested += HandleSaveRequested;
        }

        private void OnDisable()
        {
            MilkFarmEvents.OnSaveRequested -= HandleSaveRequested;
        }

        // ✅ HandleSaveRequested EKLE:

        private void HandleSaveRequested()
        {
            SaveToData();
        }
        public void LoadFromSaveData()
        {
            var saveData = saveManager.GetCurrentSaveData();

            // Capacity
            capacityLevel = saveData.packaging.capacityLevel;
            UpdateCapacity();

            // ✅ CRITICAL: Clear existing cases before loading!
            ClearAllCases();

            // ✅ YENİ: Production stack load
            if (saveData.packaging.productionStackBottles != null)
            {
                foreach (int bottleCount in saveData.packaging.productionStackBottles)
                {
                    if (bottleCount > 0)
                    {
                        SpawnCrateWithBottles(bottleCount, isActiveCase: false);
                    }
                }
            }

            // ✅ YENİ: Active crate load
            if (saveData.packaging.activeCrateBottles > 0)
            {
                SpawnCrateWithBottles(saveData.packaging.activeCrateBottles, isActiveCase: true);
            }

            // ✅ YENİ: Sales slots load
            if (saveData.packaging.salesSlotBottles != null)
            {
                for (int i = 0; i < saveData.packaging.salesSlotBottles.Count && i < salesSlots.Length; i++)
                {
                    int bottleCount = saveData.packaging.salesSlotBottles[i];
                    if (bottleCount > 0)
                    {
                        SpawnCrateInSalesSlot(i, bottleCount);
                    }
                }
            }

            UpdateProductionFullIcon();
            UpdateSalesFullIcon();

            Debug.Log($"[PackageManager] 📂 Loaded - Stack: {productionStack.Count}, Active: {(currentActiveCrate != null ? currentActiveCrate.landedMilkCount : 0)}/6");
        }
        private void ClearAllCases()
        {
            // Clear production stack
            foreach (var crate in productionStack)
            {
                if (crate != null && crate.gameObject != null)
                {
                    Destroy(crate.gameObject);
                }
            }
            productionStack.Clear();

            // Clear active crate
            if (currentActiveCrate != null && currentActiveCrate.gameObject != null)
            {
                Destroy(currentActiveCrate.gameObject);
            }
            currentActiveCrate = null;

            // Clear sales slots
            for (int i = 0; i < salesSlotsContents.Length; i++)
            {
                if (salesSlotsContents[i] != null && salesSlotsContents[i].gameObject != null)
                {
                    Destroy(salesSlotsContents[i].gameObject);
                }
                salesSlotsContents[i] = null;
            }

            Debug.Log("[PackageManager] 🧹 Cleared all existing cases");
        }
        public void SaveToData()
        {
            var saveData = saveManager.GetCurrentSaveData();

            // Capacity
            saveData.packaging.capacityLevel = capacityLevel;

            // Production stack
            saveData.packaging.productionStackBottles.Clear();
            foreach (var crate in productionStack)
            {
                if (crate != null)
                {
                    saveData.packaging.productionStackBottles.Add(crate.landedMilkCount);
                }
            }

            // Active crate
            saveData.packaging.activeCrateBottles = (currentActiveCrate != null)
                ? currentActiveCrate.landedMilkCount
                : 0;

            // ✅ Sales slots - LIST SIZE CHECK!
            // Ensure list is big enough
            while (saveData.packaging.salesSlotBottles.Count < salesSlotsContents.Length)
            {
                saveData.packaging.salesSlotBottles.Add(-1);
            }

            // Now safe to assign
            for (int i = 0; i < salesSlotsContents.Length; i++)
            {
                if (salesSlotsContents[i] != null)
                {
                    saveData.packaging.salesSlotBottles[i] = salesSlotsContents[i].CurrentBottleCount;
                }
                else
                {
                    saveData.packaging.salesSlotBottles[i] = -1;
                }
            }

            saveManager.SaveGame(saveData);

            Debug.Log($"[PackageManager] 💾 Saved - Stack: {saveData.packaging.productionStackBottles.Count}, Active: {saveData.packaging.activeCrateBottles}");
        }

        private void SpawnCrateWithBottles(int bottleCount, bool isActiveCase)
        {
            if (bottleCount <= 0) return;

            int spawnIndex = isActiveCase ? productionStack.Count : productionStack.Count;
            Vector3 spawnPos = GetStackPosition(spawnIndex);

            GameObject newCrateObj = Instantiate(cratePrefab, spawnPos, caseSpawnPoint.rotation);
            MilkCrate crate = newCrateObj.GetComponent<MilkCrate>();

            if (crate != null)
            {
                newCrateObj.transform.SetParent(caseSpawnPoint);
                newCrateObj.transform.localScale = Vector3.one * 1.0f;

                // ✅ Bottle'ları instant spawn (AddBottleInstant kullan)
                for (int i = 0; i < bottleCount; i++)
                {
                    crate.AddBottleInstant(milkBottlePrefab);
                }

                if (isActiveCase)
                {
                    currentActiveCrate = crate;
                    Debug.Log($"[PackageManager] 📂 Active crate loaded: {bottleCount}/6");
                }
                else
                {
                    productionStack.Add(crate);
                    Debug.Log($"[PackageManager] 📂 Stack crate loaded: {bottleCount}/6");
                }
            }
        }

        private void SpawnCrateInSalesSlot(int slotIndex, int bottleCount)
        {
            if (slotIndex < 0 || slotIndex >= salesSlots.Length) return;
            if (bottleCount <= 0) return;

            Transform slot = salesSlots[slotIndex];

            GameObject crateObj = Instantiate(cratePrefab, slot.position, slot.rotation);
            MilkCrate crate = crateObj.GetComponent<MilkCrate>();

            if (crate != null)
            {
                crateObj.transform.SetParent(slot);
                crateObj.transform.localPosition = Vector3.zero;
                crateObj.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                crateObj.transform.localScale = Vector3.one * 0.6f;

                // ✅ Bottle'ları instant spawn (AddBottleInstant kullan)
                for (int i = 0; i < bottleCount; i++)
                {
                    crate.AddBottleInstant(milkBottlePrefab);
                }

                salesSlotsContents[slotIndex] = crate;

                Debug.Log($"[PackageManager] 📂 Sales slot {slotIndex} loaded: {bottleCount}/6");
            }
        }
        private void UpdateCapacity()
        {
            currentCapacity = config.packageStationCapacityBase + (capacityLevel - 1) * 4;
        }

        // === CASE SPAWN ===

        private void SpawnNewCrate()
        {
            if (currentActiveCrate != null) return;
            if (caseSpawnPoint == null || cratePrefab == null) return;

            int spawnIndex = productionStack.Count;
            Vector3 spawnPos = GetStackPosition(spawnIndex);

            GameObject newCrateObj = Instantiate(cratePrefab, spawnPos, caseSpawnPoint.rotation);
            currentActiveCrate = newCrateObj.GetComponent<MilkCrate>();

            if (currentActiveCrate != null)
            {
                newCrateObj.transform.SetParent(caseSpawnPoint);
                newCrateObj.transform.localScale = Vector3.one * 1.0f;
            }

            Debug.Log($"[PackageManager] Case spawn. Index: {spawnIndex}, Stack: {productionStack.Count}");
            UpdateProductionFullIcon();
        }

        // === SÜT EKLEME (QUEUE) ===

        public void AddMilk(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                AddMilk(caseSpawnPoint != null ? caseSpawnPoint.position : transform.position);
            }
        }

        public void AddMilk(Vector3 milkStartPos)
        {
            milkQueue.Enqueue(milkStartPos);

            if (!isAddingMilk)
            {
                StartCoroutine(ProcessMilkQueue());
            }
        }

        /// <summary>
        /// Süt queue'sunu işle (DÜZELTME)
        /// </summary>
        private IEnumerator ProcessMilkQueue()
        {
            isAddingMilk = true;

            while (milkQueue.Count > 0)
            {
                // === 1. CASE KONTROL ===

                // Case yoksa spawn et
                if (currentActiveCrate == null)
                {
                    // Stack zaten max mı kontrol et
                    if (productionStack.Count >= maxProductionStack)
                    {
                        Debug.LogWarning($"[PackageManager] Production FULL! Stack: {productionStack.Count}/{maxProductionStack}");
                        UpdateProductionFullIcon();

                        // Queue'da bekle (skip etme!)
                        yield return new WaitForSeconds(0.5f);
                        continue;
                    }

                    SpawnNewCrate();
                    yield return new WaitForSeconds(0.1f); // Spawn'ın tamamlanmasını bekle
                }

                // Hala case yoksa skip
                if (currentActiveCrate == null)
                {
                    Debug.LogError("[PackageManager] Case spawn FAILED!");
                    milkQueue.Dequeue(); // Bu şişeyi skip et
                    continue;
                }

                // === 2. PENDING MILK BEKLE ===

                int safetyCounter = 0;
                while (currentActiveCrate != null &&
                       currentActiveCrate.targetMilkCount > currentActiveCrate.landedMilkCount &&
                       safetyCounter < 200)
                {
                    yield return null;
                    safetyCounter++;
                }

                if (safetyCounter >= 200)
                {
                    Debug.LogWarning("[PackageManager] Pending milk TIMEOUT!");
                }

                // === 3. DOLU MU KONTROL ===

                // Case fiziksel olarak dolu mu?
                if (currentActiveCrate != null && currentActiveCrate.IsPhysicallyFull)
                {
                    Debug.Log($"[PackageManager] Case FULL! Landed: {currentActiveCrate.landedMilkCount}/6");
                    CompleteCrate();
                    UpdateProductionFullIcon();

                    // Bir sonraki iteration'da yeni case spawn edilecek (max kontrolü ile)
                    yield return null;
                    continue; // Bu iteration'ı bitir, yeni case'i başta kontrol et
                }

                // === 4. ŞİŞE EKLE ===

                if (currentActiveCrate != null && currentActiveCrate.HasSpace)
                {
                    Vector3 milkPos = milkQueue.Dequeue();

                    currentActiveCrate.AddMilkToCrate(milkBottlePrefab, milkPos, null);
                    MilkFarmEvents.MilkAddedToStation(0);

                    Debug.Log($"[PackageManager] Şişe eklendi. Case: {currentActiveCrate.landedMilkCount}/6, Queue: {milkQueue.Count}");
                }
                else
                {
                    Debug.LogWarning("[PackageManager] Case dolu ama complete edilmemiş!");
                    yield return null;
                    continue;
                }

                yield return new WaitForSeconds(0.1f); // Flying animasyon için delay
            }

            Debug.Log("[PackageManager] Queue işleme tamamlandı.");
            isAddingMilk = false;
        }

        private void CompleteCrate()
        {
            if (currentActiveCrate == null) return;

            productionStack.Add(currentActiveCrate);

            int stackIndex = productionStack.Count - 1;
            Vector3 stackPos = GetStackPosition(stackIndex);

            currentActiveCrate.transform.SetParent(caseSpawnPoint);
            currentActiveCrate.transform.position = stackPos;
            currentActiveCrate.transform.localScale = Vector3.one * 1.0f;

            Debug.Log($"[PackageManager] Case completed. Stack: {productionStack.Count}/{maxProductionStack}, Pos: {stackPos}");

            MilkFarmEvents.PackageCreated(0);
            SaveToData();

            currentActiveCrate = null;
        }

        private Vector3 GetStackPosition(int index)
        {
            Vector3 basePos = caseSpawnPoint.position;

            if (index == 0 && productionStack.Count == 0)
            {
                return basePos;
            }

            int layer = index / 4;
            int posInLayer = index % 4;
            int row = posInLayer / 2;
            int col = posInLayer % 2;

            Vector3 finalPos = basePos;

            if (row == 1) finalPos.x += gridOffset.x;
            if (col == 1) finalPos.z += gridOffset.z;
            finalPos.y += layer * gridOffset.y;

            return finalPos;
        }

        // === MANUEL TAŞIMA ===

        public void OnProductionAreaClicked()
        {
            if (currentActiveCrate != null && currentActiveCrate.landedMilkCount > 0)
            {
                Debug.Log("[PackageManager] Current case taşınıyor...");
                TryMoveCurrentCrateToSales();
                return;
            }

            if (productionStack.Count == 0)
            {
                Debug.LogWarning("[PackageManager] Stack boş!");
                return;
            }

            TryMoveCrateToSales();
        }

        private void TryMoveCurrentCrateToSales()
        {
            if (currentActiveCrate == null) return;

            MilkCrate crateToMove = currentActiveCrate;
            currentActiveCrate = null;

            int emptySlotIndex = GetEmptySalesSlot();
            if (emptySlotIndex == -1)
            {
                Debug.LogWarning("[PackageManager] Sales FULL!");
                currentActiveCrate = crateToMove;
                UpdateSalesFullIcon();
                return;
            }

            MoveCrateToSalesSlot(crateToMove, emptySlotIndex);

            SpawnNewCrate();
            UpdateProductionFullIcon();
            UpdateSalesFullIcon();
        }

        private void TryMoveCrateToSales()
        {
            if (productionStack.Count == 0) return;

            MilkCrate crateToMove = productionStack[0];
            productionStack.RemoveAt(0);

            UpdateStackPositions();

            int emptySlotIndex = GetEmptySalesSlot();
            if (emptySlotIndex == -1)
            {
                Debug.LogWarning("[PackageManager] Sales FULL!");
                productionStack.Insert(0, crateToMove);
                UpdateStackPositions();
                UpdateSalesFullIcon();
                return;
            }

            MoveCrateToSalesSlot(crateToMove, emptySlotIndex);
            UpdateProductionFullIcon();
            UpdateSalesFullIcon();
        }

        private int GetEmptySalesSlot()
        {
            for (int i = 0; i < salesSlotsContents.Length; i++)
            {
                if (salesSlotsContents[i] == null)
                {
                    return i;
                }
            }
            return -1;
        }

        private void MoveCrateToSalesSlot(MilkCrate crate, int slotIndex)
        {
            Transform targetSlot = salesSlots[slotIndex];
            salesSlotsContents[slotIndex] = crate;

            FlyingItem flyer = crate.gameObject.GetComponent<FlyingItem>();
            if (flyer == null) flyer = crate.gameObject.AddComponent<FlyingItem>();

            flyer.FlyTo(targetSlot.position, () =>
            {
                if (crate != null)
                {
                    crate.transform.SetParent(targetSlot);
                    crate.transform.localPosition = Vector3.zero;
                    crate.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                    crate.transform.localScale = Vector3.one * 0.6f;
                }
            });

            Debug.Log($"[PackageManager] Case → Sales slot: {slotIndex}");
        }

        private void UpdateStackPositions()
        {
            for (int i = 0; i < productionStack.Count; i++)
            {
                Vector3 stackPos = GetStackPosition(i);
                productionStack[i].transform.position = stackPos;
            }
        }

        private void UpdateProductionFullIcon()
        {
            if (productionFullIcon == null) return;

            int totalCases = productionStack.Count + (currentActiveCrate != null ? 1 : 0);
            bool isFull = totalCases >= maxProductionStack;

            productionFullIcon.SetActive(isFull);

            if (isFull)
            {
                Debug.Log($"[PackageManager] Production FULL! {totalCases}/{maxProductionStack}");
            }
        }

        private void UpdateSalesFullIcon()
        {
            if (salesFullIcon == null) return;

            int filledSlots = 0;
            for (int i = 0; i < salesSlotsContents.Length; i++)
            {
                if (salesSlotsContents[i] != null) filledSlots++;
            }

            bool isFull = filledSlots >= salesSlotsContents.Length;
            salesFullIcon.SetActive(isFull);

            if (isFull)
            {
                Debug.Log($"[PackageManager] Sales FULL! {filledSlots}/{salesSlotsContents.Length}");
            }
        }

        // === SATIŞ ===

        public void OnStationClicked()
        {
            TrySellToCustomer();
        }

        public bool TrySellToCustomer()
        {
            if (customerManager == null || !customerManager.HasWaitingCustomer())
            {
                return false;
            }

            Customer firstCustomer = customerManager.GetFirstCustomer();
            if (firstCustomer == null || firstCustomer.controller == null)
            {
                return false;
            }

            int needed = firstCustomer.controller.GetRemainingBottles();
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
                        customerManager.ServeBottleToCustomer();
                    }
                    else
                    {
                        break;
                    }
                }

                if (crate.CurrentBottleCount <= 0)
                {
                    Destroy(crate.gameObject);
                    salesSlotsContents[i] = null;
                }
            }

            if (given > 0)
            {
                MilkFarmEvents.PackageSold(0);
                SaveToData();
                UpdateSalesFullIcon();
                return true;
            }

            return false;
        }

        // === API ===

        public int GetProductionStackCount() => productionStack.Count;
        public bool HasActiveCrate() => currentActiveCrate != null;
        public int GetActiveCrateBottleCount() => currentActiveCrate != null ? currentActiveCrate.landedMilkCount : 0;

        public int GetTotalBottleCount()
        {
            int total = 0;
            foreach (var crate in salesSlotsContents)
            {
                if (crate != null) total += crate.CurrentBottleCount;
            }
            return total;
        }

        public int GetCurrentPackageCount() => GetTotalBottleCount();
        public int GetCurrentCapacity() => currentCapacity;
        public bool HasPackages() => GetTotalBottleCount() > 0;
        public bool HasBottles() => GetTotalBottleCount() > 0;
        public int GetAvailableBottles() => GetTotalBottleCount();

        public bool UpgradeCapacity(MoneyManager money)
        {
            float cost = CalculateCapacityUpgradeCost();
            if (!money.CanAfford(cost)) return false;

            money.SpendMoney(cost);
            capacityLevel++;
            UpdateCapacity();
            MilkFarmEvents.UpgradePurchased("PackageCapacity", capacityLevel, cost);
            SaveToData();
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
            int activeCrateCount = currentActiveCrate != null ? currentActiveCrate.landedMilkCount : 0;
            Debug.Log($"[DEBUG] Stack: {productionStack.Count}, Active: {activeCrateCount}/6, Queue: {milkQueue.Count}, Sales: {GetTotalBottleCount()}");
        }
    }
}