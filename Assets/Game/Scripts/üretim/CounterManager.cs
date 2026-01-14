using UnityEngine;
using UnityEngine.AI; // Bunu eklemeyi unutma
using System.Collections.Generic;

public class CounterManager : MonoBehaviour
{
    [Header("Ürün Ayarlarý")]
    public Transform[] itemSlots;
    public GameObject testItemPrefab;
    private List<GameObject> currentItems = new List<GameObject>();

    [Header("Sýra Ayarlarý")]
    public Transform queueStartPoint;
    public int maxQueueSize = 10;
    public float rowSpacing = 1.0f;
    public float colSpacing = 0.8f;

    public List<CustomerController> customerQueue = new List<CustomerController>();

    public int CurrentCount => customerQueue.Count;
    public bool IsQueueFull => customerQueue.Count >= maxQueueSize;

    void Start()
    {
        FillCounter();
    }

    public void JoinQueue(CustomerController customer)
    {
        if (!customerQueue.Contains(customer))
        {
            customerQueue.Add(customer);
            UpdateQueuePositions();
        }
    }

    public void LeaveQueue(CustomerController customer)
    {
        if (customerQueue.Contains(customer))
        {
            customerQueue.Remove(customer);
            UpdateQueuePositions();
        }
    }

    public int GetQueueIndex(CustomerController customer)
    {
        return customerQueue.IndexOf(customer);
    }

    public Vector3 GetPositionForIndex(int index)
    {
        int row = index / 2;
        int col = index % 2;

        // Not: Burada dükkanýnýn yönüne göre Vector3.back veya forward'ý deðiþtirebilirsin
        Vector3 targetPos = queueStartPoint.position + (Vector3.back * (row * rowSpacing)) + (Vector3.right * (col * colSpacing));

        // --- GÜVENLÝK KODU ---
        // Hesaplanan nokta NavMesh üzerinde mi? Deðilse en yakýn zemini bul.
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, 2.0f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return targetPos; // Bulamazsa mecburen ham pozisyonu döndür
    }

    void UpdateQueuePositions()
    {
        for (int i = 0; i < customerQueue.Count; i++)
        {
            if (customerQueue[i] != null)
            {
                // Herkesin yerini güncelle
                customerQueue[i].UpdateQueuePosition(GetPositionForIndex(i));
            }
        }
    }

    public void FillCounter()
    {
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (currentItems.Count <= i)
            {
                GameObject newItem = Instantiate(testItemPrefab, itemSlots[i].position, itemSlots[i].rotation);
                currentItems.Add(newItem);
            }
        }
    }

    public bool TryGiveItem()
    {
        if (currentItems.Count > 0)
        {
            GameObject itemToGive = currentItems[currentItems.Count - 1];
            currentItems.RemoveAt(currentItems.Count - 1);
            Destroy(itemToGive);
            return true;
        }
        return false;
    }
}