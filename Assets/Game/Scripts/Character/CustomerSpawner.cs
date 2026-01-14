using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Müþteri Ayarlarý")]
    public GameObject customerPrefab;
    public Transform spawnPoint;
    public Transform[] counterPoints; // Masalar
    public Transform[] exitPoints;

    [Header("Özellikler")]
    public Gender spawnGender = Gender.Both;
    public SkinType spawnSkin = SkinType.Both;
    public float spawnInterval = 4f;

    void Start()
    {
        InvokeRepeating(nameof(SpawnCustomer), 1f, spawnInterval);
    }

    void SpawnCustomer()
    {
        if (counterPoints.Length == 0) return;

        // 1. RASTGELE BÝR MASA SEÇ
        Transform randomCounterPoint = counterPoints[Random.Range(0, counterPoints.Length)];
        CounterManager counterManager = randomCounterPoint.GetComponentInParent<CounterManager>();

        // 2. KONTROL ET: O masada 10 kiþi var mý?
        if (counterManager != null && counterManager.IsQueueFull)
        {
            // Masa dolu! Bu turu pas geç. Spawn etme.
            Debug.Log("Masa dolu, müþteri gelmiyor.");
            return;
        }

        // 3. SPAWN ET
        GameObject newCustomer = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);
        CustomerController ctrl = newCustomer.GetComponent<CustomerController>();

        if (ctrl != null)
        {
            Transform selectedExit = exitPoints[Random.Range(0, exitPoints.Length)];
            ctrl.Initialize(randomCounterPoint, selectedExit, spawnGender, spawnSkin);
        }
    }
}