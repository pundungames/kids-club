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
    private bool isProductionBusy = false; // Kasa hareket halindeyken inek týklanmasýn

    [Header("2. SATIÞ ALANI")]
    public Transform[] salesSlots;
    private MilkCrate[] salesSlotsContents;

    [Header("3. PARA ALANI")]
    public Transform moneySpawnPoint;
    public GameObject moneyPrefab;
    private List<MoneyItem> spawnedMonies = new List<MoneyItem>();

    private const int GRID_SIZE = 4;
    private const int MAX_LAYERS = 5;

    [Header("Para Ayarlarý")]
    public float moneySpacingX = 0.4f;
    public float moneySpacingZ = 0.7f;
    public float moneyHeight = 0.2f;
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

        isProductionBusy = false; // Yeni kasa geldi, üretim serbest
    }

    public void AddMilk(Vector3 milkStartPos)
    {
        // Kasa yoksa, hareket halindeyse veya mantýksal olarak doluysa iþlem yapma
        if (currentActiveCrate == null || !currentActiveCrate.HasSpace || isProductionBusy) return;

        // Sütü gönder ve þunu söyle: "Fiziksel olarak dolduðunda OnCrateFullyFilled fonksiyonunu çalýþtýr"
        currentActiveCrate.AddMilkToCrate(milkBottlePrefab, milkStartPos, OnCrateFullyFilled);
    }

    // Bu fonksiyon sadece son süt þiþesi yerine oturduðunda çalýþýr
    void OnCrateFullyFilled()
    {
        TryMoveCrateToSales();
    }

    void TryMoveCrateToSales()
    {
        // Kasa fiziksel olarak tam dolu deðilse asla taþýma!
        if (currentActiveCrate == null || !currentActiveCrate.IsPhysicallyFull) return;

        int emptySlotIndex = -1;
        for (int i = 0; i < salesSlotsContents.Length; i++)
        {
            if (salesSlotsContents[i] == null) { emptySlotIndex = i; break; }
        }

        if (emptySlotIndex != -1)
        {
            isProductionBusy = true; // Kasa uçuþa geçti, bu sýrada süt eklenemez

            Transform targetSlot = salesSlots[emptySlotIndex];

            // Slotu rezerve et (Müþteri buraya bakabilir artýk)
            salesSlotsContents[emptySlotIndex] = currentActiveCrate;

            // Kasa referansýný boþa çýkar ki inek yanlýþ kasaya süt atmasýn
            MilkCrate movingCrate = currentActiveCrate;
            currentActiveCrate = null;

            // Uçuþ Motoru
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

                // Uçuþ bitti, yeni kasa yarat
                SpawnNewCrate();
            });
        }
        else
        {
            Debug.Log("Satýþ rafý dolu, kasa bekliyor.");
            // Yer açýlýnca CheckProductionLine çaðrýlacak.
        }
    }

    public bool TryGiveItem()
    {
        if (spawnedMonies.Count >= GRID_SIZE * MAX_LAYERS) return false;

        int indexToSell = -1;
        for (int i = 0; i < salesSlotsContents.Length; i++)
        {
            // Sadece dolu slotlara bak ve fiziksel olarak yerleþmiþ mi kontrol et
            if (salesSlotsContents[i] != null && salesSlotsContents[i].transform.parent == salesSlots[i])
            {
                indexToSell = i;
                break;
            }
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
        float stairShift = layer * moneyStairOffset;

        Vector3 targetPos = moneySpawnPoint.position +
                           (moneySpawnPoint.right * col * moneySpacingX) +
                           (moneySpawnPoint.forward * row * moneySpacingZ) +
                           (moneySpawnPoint.forward * stairShift) +
                           (Vector3.up * layer * moneyHeight);

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

    // Satýþ yapýldýðýnda üretim bandýnda bekleyen TAM DOLU kasa var mý diye bakar
    void CheckProductionLine()
    {
        // Burada currentActiveCrate'in null olup olmamasýný deðil,
        // fiziksel olarak dolu olup olmadýðýný kontrol etmeliyiz.
        // Ama yukarýdaki logic (OnCrateFullyFilled) zaten dolunca tetikleniyor.
        // Sadece yer yoksa bekliyordu. Tekrar tetikleyelim.

        // Eðer üretimde bir kasa varsa ve fiziksel olarak dolduysa ama gidememiþse:
        if (currentActiveCrate != null && currentActiveCrate.IsPhysicallyFull)
        {
            TryMoveCrateToSales();
        }
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