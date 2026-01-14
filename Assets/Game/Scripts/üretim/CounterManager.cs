using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class CounterManager : MonoBehaviour
{
    [Header("1. ÜRETÝM ALANI")]
    public Transform caseSpawnPoint;
    public GameObject cratePrefab;
    public GameObject milkBottlePrefab;

    private MilkCrate currentActiveCrate;

    [Header("2. SATIÞ ALANI")]
    public Transform[] salesSlots;
    private MilkCrate[] salesSlotsContents;

    [Header("3. PARA ALANI")]
    public Transform moneySpawnPoint;
    public GameObject moneyPrefab;
    private List<MoneyItem> spawnedMonies = new List<MoneyItem>();

    // --- GÜNCELLEME: PARA GÖRSEL AYARLARI ---
    private const int GRID_SIZE = 4;
    private const int MAX_LAYERS = 5;

    [Header("Para Dizilim Ayarlarý")]
    public float moneySpacingX = 0.4f; // Yan yana boþluk
    public float moneySpacingZ = 0.7f; // Arka arkaya boþluk
    public float moneyHeight = 0.2f;   // Yükseklik (Kat arasý)

    // YENÝ ÖZELLÝK: MERDÝVEN ETKÝSÝ
    // Orthographic kamerada derinlik belli olsun diye her katý biraz kaydýrýr.
    [Tooltip("Her katta paranýn ne kadar öne/arkaya kayacaðýný belirler.")]
    public float moneyStairOffset = 0.3f;

    [Header("Sýra Sistemi")]
    public Transform queueStartPoint;
    public List<CustomerController> customerQueue = new List<CustomerController>();
    public float rowSpacing = 1.0f;
    public float colSpacing = 0.8f;
    public int maxQueueSize = 10;
    public bool IsQueueFull => customerQueue.Count >= maxQueueSize;

    void Awake() { salesSlotsContents = new MilkCrate[salesSlots.Length]; }

    void Start() { SpawnNewCrate(); }

    void SpawnNewCrate()
    {
        if (currentActiveCrate != null) return;
        GameObject newCrateObj = Instantiate(cratePrefab, caseSpawnPoint.position, caseSpawnPoint.rotation);
        currentActiveCrate = newCrateObj.GetComponent<MilkCrate>();
        newCrateObj.transform.SetParent(caseSpawnPoint);
        newCrateObj.transform.localScale = Vector3.one * 1.2f;
    }

    public void AddMilk(Vector3 milkStartPos)
    {
        if (currentActiveCrate == null || currentActiveCrate.IsFull) return;
        currentActiveCrate.AddMilkToCrate(milkBottlePrefab, milkStartPos);
        if (currentActiveCrate.IsFull) TryMoveCrateToSales();
    }

    void TryMoveCrateToSales()
    {
        if (currentActiveCrate == null || !currentActiveCrate.IsFull) return;

        int emptySlotIndex = -1;
        for (int i = 0; i < salesSlotsContents.Length; i++)
        {
            if (salesSlotsContents[i] == null) { emptySlotIndex = i; break; }
        }

        if (emptySlotIndex != -1)
        {
            Transform targetSlot = salesSlots[emptySlotIndex];
            currentActiveCrate.transform.SetParent(targetSlot);
            currentActiveCrate.transform.localPosition = Vector3.zero;
            currentActiveCrate.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            currentActiveCrate.transform.localScale = Vector3.one * 0.6f;
            salesSlotsContents[emptySlotIndex] = currentActiveCrate;
            currentActiveCrate = null;
            SpawnNewCrate();
        }
    }

    public bool TryGiveItem()
    {
        if (spawnedMonies.Count >= GRID_SIZE * MAX_LAYERS) return false;

        int indexToSell = -1;
        for (int i = 0; i < salesSlotsContents.Length; i++)
        {
            if (salesSlotsContents[i] != null) { indexToSell = i; break; }
        }

        if (indexToSell != -1)
        {
            MilkCrate crateToSell = salesSlotsContents[indexToSell];
            Vector3 salePosition = crateToSell.transform.position;

            salesSlotsContents[indexToSell] = null;
            if (crateToSell != null) Destroy(crateToSell.gameObject);

            SpawnMoney(salePosition);
            Invoke(nameof(CheckProductionLine), 0.2f);
            return true;
        }
        return false;
    }

    void SpawnMoney(Vector3 startPos)
    {
        if (moneyPrefab == null || moneySpawnPoint == null) return;

        int index = spawnedMonies.Count;
        int layer = index / 4;
        int floorIndex = index % 4;
        int row = floorIndex / 2;
        int col = floorIndex % 2;

        // --- MERDÝVEN HESABI ---
        // Her kat (layer) arttýðýnda, Z ekseninde 'moneyStairOffset' kadar kaydýrýyoruz.
        float stairShift = layer * moneyStairOffset;

        Vector3 targetPos = moneySpawnPoint.position +
                           (moneySpawnPoint.right * col * moneySpacingX) +
                           (moneySpawnPoint.forward * row * moneySpacingZ) +
                           (moneySpawnPoint.forward * stairShift) +  // <-- Merdiven Etkisi Burasý
                           (Vector3.up * layer * moneyHeight);

        // Y Ekseninde -90 derece rotasyon
        Quaternion moneyRot = Quaternion.Euler(0, -90, 0);
        GameObject newMoney = Instantiate(moneyPrefab, startPos, moneyRot);

        MoneyItem moneyScript = newMoney.GetComponent<MoneyItem>();
        if (moneyScript == null) moneyScript = newMoney.AddComponent<MoneyItem>();
        moneyScript.Initialize(this);
        spawnedMonies.Add(moneyScript);

        FlyingItem flyer = newMoney.GetComponent<FlyingItem>();
        if (flyer == null) flyer = newMoney.AddComponent<FlyingItem>();

        flyer.FlyTo(targetPos);
    }

    public void OnMoneyCollected(MoneyItem money)
    {
        if (spawnedMonies.Contains(money)) spawnedMonies.Remove(money);
    }

    void CheckProductionLine()
    {
        if (currentActiveCrate != null && currentActiveCrate.IsFull) TryMoveCrateToSales();
    }

    public void JoinQueue(CustomerController customer)
    { if (!customerQueue.Contains(customer)) { customerQueue.Add(customer); UpdateQueuePositions(); } }
    public void LeaveQueue(CustomerController customer)
    { if (customerQueue.Contains(customer)) { customerQueue.Remove(customer); UpdateQueuePositions(); } }
    public int GetQueueIndex(CustomerController customer) => customerQueue.IndexOf(customer);

    public Vector3 GetPositionForIndex(int index)
    {
        if (queueStartPoint == null) return transform.position;
        int row = index / 2;
        int col = index % 2;
        Vector3 targetPos = queueStartPoint.position + (Vector3.back * (row * rowSpacing)) + (Vector3.right * (col * colSpacing));
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, 2.0f, NavMesh.AllAreas)) return hit.position;
        return targetPos;
    }
    void UpdateQueuePositions()
    { for (int i = 0; i < customerQueue.Count; i++) if (customerQueue[i] != null) customerQueue[i].UpdateQueuePosition(GetPositionForIndex(i)); }
}