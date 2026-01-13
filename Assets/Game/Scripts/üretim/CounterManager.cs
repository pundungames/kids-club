using UnityEngine;
using System.Collections.Generic;

public class CounterManager : MonoBehaviour
{
    [Header("Ürün Ayarlarý")]
    public Transform[] itemSlots;
    public GameObject testItemPrefab;
    private List<GameObject> currentItems = new List<GameObject>();

    [Header("Sýra Ayarlarý")]
    public Transform queueStartPoint; // Sýranýn baþladýðý yer (Masanýn önü)
    public Vector3 queueDirection = new Vector3(0, 0, -1); // Sýra ne tarafa uzasýn? (Genelde arkaya)
    public float customerSpacing = 1.0f; // Müþteriler arasý mesafe

    // Sýrada bekleyen müþterilerin listesi
    public List<CustomerController> customerQueue = new List<CustomerController>();

    void Start()
    {
        FillCounter();
    }

    // --- SIRA YÖNETÝMÝ ---

    // Müþteri geldiðinde sýraya kaydolur
    public void JoinQueue(CustomerController customer)
    {
        if (!customerQueue.Contains(customer))
        {
            customerQueue.Add(customer);
        }
    }

    // Müþteri iþi bitince sýradan çýkar
    public void LeaveQueue(CustomerController customer)
    {
        if (customerQueue.Contains(customer))
        {
            customerQueue.Remove(customer);
            // Biri çýkýnca arkadakilere "Ýlerleyin" emri ver
            UpdateQueuePositions();
        }
    }

    // Herkesin durmasý gereken yeni yeri hesapla ve bildir
    void UpdateQueuePositions()
    {
        for (int i = 0; i < customerQueue.Count; i++)
        {
            if (customerQueue[i] != null)
            {
                // Formül: Baþlangýç Noktasý + (Yön * SýraNumarasý * Mesafe)
                Vector3 targetPos = queueStartPoint.position + (queueDirection * i * customerSpacing);
                customerQueue[i].UpdateQueuePosition(targetPos);
            }
        }
    }

    // Müþteri kendi sýrasýný sorar: "Ben kaçýncýyým?"
    public int GetQueueIndex(CustomerController customer)
    {
        return customerQueue.IndexOf(customer);
    }

    // Müþteri durmasý gereken yeri sorar
    public Vector3 GetMyPosition(CustomerController customer)
    {
        int index = GetQueueIndex(customer);
        if (index == -1) return queueStartPoint.position; // Listede yoksan baþa git

        return queueStartPoint.position + (queueDirection * index * customerSpacing);
    }

    // --- ÜRÜN YÖNETÝMÝ ---

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