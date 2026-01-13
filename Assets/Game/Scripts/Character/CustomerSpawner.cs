using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Müþteri Prefabý")]
    public GameObject customerPrefab;

    [Header("Doðma Noktasý")]
    public Transform spawnPoint;

    [Header("Hedef Noktalarý (Listeler)")]
    public Transform[] counterPoints; // Çoklu kasa
    public Transform[] exitPoints;    // Çoklu çýkýþ

    [Header("Müþteri Çeþitliliði")]
    public Gender spawnGender = Gender.Both;   // <--- EKLENDI
    public SkinType spawnSkin = SkinType.Both; // <--- EKLENDI

    [Header("Zamanlama")]
    public float spawnInterval = 5f;

    void Start()
    {
        InvokeRepeating(nameof(SpawnCustomer), 1f, spawnInterval);
    }

    void SpawnCustomer()
    {
        if (counterPoints.Length == 0 || exitPoints.Length == 0) return;

        GameObject newCustomer = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);
        CustomerController ctrl = newCustomer.GetComponent<CustomerController>();

        if (ctrl != null)
        {
            Transform selectedCounter = counterPoints[Random.Range(0, counterPoints.Length)];
            Transform selectedExit = exitPoints[Random.Range(0, exitPoints.Length)];

            // --- HATA VEREN YER BURASIYDI, DUZELDI ---
            // Artýk 4 bilgiyi de gönderiyoruz:
            ctrl.Initialize(selectedCounter, selectedExit, spawnGender, spawnSkin);
        }
    }
}