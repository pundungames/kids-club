using UnityEngine;
using Zenject;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;

namespace MilkFarm
{
    public class PackageManager : MonoBehaviour
    {
        [Inject] private GameConfig config;
        [Inject] private SaveManager saveManager;
        [Inject] private MoneyManager moneyManager;
        [Inject] DiContainer container;

        [Header("Referanslar")]
        [SerializeField] private CustomerManager customerManager;
        [SerializeField] private bool isChickenScene = false; // Inspector'dan ayarla

        [Header("1. ÜRETİM ALANI (Production Stack)")]
        [SerializeField] private Transform caseSpawnPoint;
        [SerializeField] private GameObject cratePrefab;
        [SerializeField] private GameObject milkBottlePrefab;
        [SerializeField] float crateScale = 1f;
        [SerializeField] private Vector3 gridOffset = new Vector3(-0.434f, 0.309f, -0.315f);
        [SerializeField] private int maxProductionStack = 8;
        [SerializeField] private GameObject productionFullIcon;

        [Header("2. SATIŞ ALANI (2x2 Grid)")]
        [SerializeField] private Transform[] salesSlots;
        [SerializeField] private GameObject salesFullIcon;

        private List<ProductCrate> productionStack = new List<ProductCrate>();
        private ProductCrate currentActiveCrate;
        private ProductCrate[] salesSlotsContents;

        private int capacityLevel = 1;
        private int currentCapacity;

        private bool isAddingMilk = false;
        private Queue<MilkQueueItem> milkQueue = new Queue<MilkQueueItem>();

        private void Awake()
        {
            if (salesSlots != null)
            {
                salesSlotsContents = new ProductCrate[salesSlots.Length];
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
        private void Update()
        {
            // Crate fiziksel olarak doluysa ve queue çalışmıyorsa complete et
            if (currentActiveCrate != null &&
                currentActiveCrate.IsPhysicallyFull &&
                !isAddingMilk)
            {
                CompleteCrate();
                UpdateProductionFullIcon();

                // Queue boş değilse devam et
                if (milkQueue.Count > 0)
                    StartCoroutine(ProcessMilkQueue());
            }
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
            var packaging = GetPackagingSaveData(saveData); // ✅ TEK DEĞİŞİKLİK

            capacityLevel = packaging.capacityLevel;
            UpdateCapacity();

            ClearAllCases();

            if (packaging.productionStackBottles != null)
            {
                foreach (int bottleCount in packaging.productionStackBottles)
                {
                    if (bottleCount > 0)
                        SpawnCrateWithBottles(bottleCount, isActiveCase: false);
                }
            }

            if (packaging.activeCrateBottles > 0)
                SpawnCrateWithBottles(packaging.activeCrateBottles, isActiveCase: true);

            if (packaging.salesSlotBottles != null)
            {
                for (int i = 0; i < packaging.salesSlotBottles.Count && i < salesSlots.Length; i++)
                {
                    int bottleCount = packaging.salesSlotBottles[i];
                    if (bottleCount > 0)
                        SpawnCrateInSalesSlot(i, bottleCount);
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
            var packaging = GetPackagingSaveData(saveData); // ✅ TEK DEĞİŞİKLİK

            packaging.capacityLevel = capacityLevel;

            packaging.productionStackBottles.Clear();
            foreach (var crate in productionStack)
            {
                if (crate != null)
                    packaging.productionStackBottles.Add(crate.landedMilkCount);
            }

            packaging.activeCrateBottles = (currentActiveCrate != null)
                ? currentActiveCrate.landedMilkCount : 0;

            while (packaging.salesSlotBottles.Count < salesSlotsContents.Length)
                packaging.salesSlotBottles.Add(-1);

            for (int i = 0; i < salesSlotsContents.Length; i++)
            {
                if (salesSlotsContents[i] != null)
                    packaging.salesSlotBottles[i] = salesSlotsContents[i].CurrentBottleCount;
                else
                    packaging.salesSlotBottles[i] = -1;
            }

            saveManager.SaveGame(saveData);
            Debug.Log($"[PackageManager] 💾 Saved - Stack: {packaging.productionStackBottles.Count}, Active: {packaging.activeCrateBottles}");
        }
        private PackageSaveData GetPackagingSaveData(MilkFarmSaveData saveData)
        {
            if (isChickenScene)
            {
                if (saveData.chickenPackaging == null)
                    saveData.chickenPackaging = new PackageSaveData();
                return saveData.chickenPackaging;
            }
            return saveData.packaging;
        }
        private void SpawnCrateWithBottles(int bottleCount, bool isActiveCase)
        {
            if (bottleCount <= 0) return;

            int spawnIndex = isActiveCase ? productionStack.Count : productionStack.Count;
            Vector3 spawnPos = GetStackPosition(spawnIndex);

            GameObject newCrateObj = Instantiate(cratePrefab, spawnPos, caseSpawnPoint.rotation);
            container.InjectGameObject(newCrateObj);
            ProductCrate crate = newCrateObj.GetComponent<ProductCrate>();

            if (crate != null)
            {
                newCrateObj.transform.SetParent(caseSpawnPoint);
                newCrateObj.transform.localScale = Vector3.one * crateScale;

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
            container.InjectGameObject(crateObj);
            ProductCrate crate = crateObj.GetComponent<ProductCrate>();

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
            if (isChickenScene)
                currentCapacity = config.chickenPackageStationCapacityBase + (capacityLevel - 1) * 4;
            else
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
            container.InjectGameObject(newCrateObj);
            currentActiveCrate = newCrateObj.GetComponent<ProductCrate>();

            if (currentActiveCrate != null)
            {
                newCrateObj.transform.SetParent(caseSpawnPoint);
                newCrateObj.transform.localScale = Vector3.one * crateScale;
            }

            Debug.Log($"[PackageManager] Case spawn. Index: {spawnIndex}, Stack: {productionStack.Count}");
            UpdateProductionFullIcon();
        }

        public void AddMilk(int amount)
        {
            for (int i = 0; i < amount; i++)
                AddMilk(caseSpawnPoint != null ? caseSpawnPoint.position : transform.position, null);
        }

        public void AddMilk(Vector3 milkStartPos, SplineComputer spline = null, float cooldown = 200f)
        {
            milkQueue.Enqueue(new MilkQueueItem(milkStartPos, spline));

            if (!isAddingMilk)
                StartCoroutine(ProcessMilkQueue());
        }

        /// <summary>
        /// Süt queue'sunu işle (DÜZELTME)
        /// </summary>
        private IEnumerator ProcessMilkQueue()
        {
            isAddingMilk = true;

            while (milkQueue.Count > 0)
            {
                // === CRATE YOKSA SPAWN ===
                if (currentActiveCrate == null)
                {
                    if (productionStack.Count >= maxProductionStack)
                    {
                        UpdateProductionFullIcon();
                        yield return new WaitForSeconds(0.5f);
                        continue;
                    }

                    SpawnNewCrate();
                    yield return new WaitForSeconds(0.05f);

                    if (currentActiveCrate == null)
                    {
                        milkQueue.Dequeue();
                        continue;
                    }
                }

                // === CRATE RESERVE DOLU MU? (target bazlı) ===
                if (!currentActiveCrate.HasSpace)
                {
                    // Tüm slotlar reserve edilmiş, fiziksel varışı bekle
                    while (currentActiveCrate != null && !currentActiveCrate.IsPhysicallyFull)
                        yield return null;

                    // Fiziksel olarak doldu → complete
                    if (currentActiveCrate != null)
                    {
                        CompleteCrate();
                        UpdateProductionFullIcon();
                    }
                    // Döngü başına dön → yeni crate spawn olacak
                    continue;
                }

                // === ÜRÜNÜ ANINDA YOLA ÇIKAR ===
                MilkQueueItem item = milkQueue.Dequeue();
                currentActiveCrate.AddMilkToCrate(milkBottlePrefab, item.position, null, item.spline);
                MilkFarmEvents.MilkAddedToStation(0);

                // Çok kısa delay - sadece görsel spawn ayrımı için
                yield return new WaitForSeconds(0.05f);
            }

            // === QUEUE BİTTİ ===
            // Son crate dolmuş olabilir, kontrol et
            if (currentActiveCrate != null && currentActiveCrate.IsPhysicallyFull)
            {
                CompleteCrate();
                UpdateProductionFullIcon();
            }

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
            currentActiveCrate.transform.localScale = Vector3.one * crateScale;

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

            ProductCrate crateToMove = currentActiveCrate;
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

            ProductCrate crateToMove = productionStack[0];
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

        private void MoveCrateToSalesSlot(ProductCrate crate, int slotIndex)
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
                ProductCrate crate = salesSlotsContents[i];
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
    struct MilkQueueItem
    {
        public Vector3 position;
        public SplineComputer spline;

        public MilkQueueItem(Vector3 pos, SplineComputer spl)
        {
            position = pos;
            spline = spl;
        }
    }
}