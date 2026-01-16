using MilkFarm;
using UnityEngine;
using Zenject;

/// <summary>
/// Müþteri spawn eden component - Zenject ile
/// </summary>
public class CustomerSpawner : MonoBehaviour
{
    [Inject] private DiContainer container;
    [Inject] private CustomerManager customerManager;

    [Header("Müþteri Ayarlarý")]
    public GameObject customerPrefab;
    public Transform spawnPoint;
    public Transform exitPoint;

    [Header("Özellikler")]
    public Gender spawnGender = Gender.Both;
    public SkinType spawnSkin = SkinType.Both;
    public float spawnInterval = 4f;

    void Start()
    {
        if (customerManager == null)
        {
            Debug.LogError("[CustomerSpawner] CustomerManager inject edilmedi! Zenject binding kontrol et!");
            return;
        }

        Debug.Log("[CustomerSpawner] CustomerManager inject edildi (Zenject). Spawn baþlýyor...");

        // Otomatik spawn baþlat
        InvokeRepeating(nameof(SpawnCustomer), 1f, spawnInterval);
    }

    void SpawnCustomer()
    {
        // Kuyruk dolu mu?
        if (customerManager != null && customerManager.IsQueueFull)
        {
            Debug.Log("[CustomerSpawner] Kuyruk dolu, müþteri gelmiyor.");
            return;
        }

        if (customerPrefab == null || spawnPoint == null)
        {
            Debug.LogError("[CustomerSpawner] Prefab veya spawn point atanmamýþ!");
            return;
        }

        // Müþteriyi spawn et
        GameObject newCustomer = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);

        // Zenject ile inject et
        container.InjectGameObject(newCustomer);

        CustomerController ctrl = newCustomer.GetComponent<CustomerController>();
        if (ctrl != null)
        {
            // Rastgele görünüm
            Gender gender = spawnGender == Gender.Both
                ? (Gender)Random.Range(0, 2)
                : spawnGender;

            SkinType skin = spawnSkin == SkinType.Both
                ? (SkinType)Random.Range(0, System.Enum.GetValues(typeof(SkinType)).Length)
                : spawnSkin;

            // Initialize - artýk counterTransform gerekmiyor!
            ctrl.Initialize(exitPoint, gender, skin);

            Debug.Log($"[CustomerSpawner] Müþteri spawn edildi (Zenject).");
        }
    }

    [ContextMenu("Debug: Spawn Now")]
    public void DebugSpawnNow()
    {
        SpawnCustomer();
    }
}