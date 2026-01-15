using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// Tek bir müşterinin verisi
    /// </summary>
    [System.Serializable]
    public class Customer
    {
        public int id;
        public int requestedPackages;
        public int remainingPackages;
        public float totalPayment;
        public GameObject gameObject;

        public Customer(int customerId, int packages, float payment)
        {
            id = customerId;
            requestedPackages = packages;
            remainingPackages = packages;
            totalPayment = payment;
        }

        public bool IsServed => remainingPackages <= 0;
    }

    /// <summary>
    /// Müşteri kuyruğu ve sipariş sistemini yöneten manager
    /// GDD v2'ye göre tam implementation
    /// </summary>
    public class CustomerManager : MonoBehaviour
    {
        [Inject] private GameConfig config;
        [Inject] private IAPManager iapManager;
        [Inject] private MoneyManager moneyManager;

        [Header("Müşteri Spawn")]
        [SerializeField] private GameObject customerPrefab;
        [SerializeField] private Transform[] queuePositions; // 4 pozisyon (2x2)
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform exitPoint;

        [Header("Para Spawn")]
        [SerializeField] private Transform moneyTableTransform;

        private List<Customer> customerQueue = new List<Customer>();
        private int customerIdCounter = 0;

        private void Start()
        {
            // Başlangıçta kuyruğu doldur
            FillQueue();
        }

        /// <summary>
        /// Kuyruğu doldur
        /// GDD: Queue her zaman dolu kalır
        /// </summary>
        private void FillQueue()
        {
            while (customerQueue.Count < config.customerQueueMax)
            {
                SpawnCustomer();
            }
        }

        /// <summary>
        /// Yeni müşteri spawn et
        /// </summary>
        private void SpawnCustomer()
        {
            if (customerQueue.Count >= config.customerQueueMax)
            {
                MilkFarmEvents.QueueFull();
                return;
            }

            // Rastgele paket talebi
            int requestedPackages = Random.Range(config.customerMinRequest, config.customerMaxRequest + 1);

            // Ödeme hesapla
            float basePayment = requestedPackages * config.packageSize * config.moneyPerMilk;
            float richMultiplier = iapManager.GetCustomerRichMultiplier();
            float totalPayment = basePayment * richMultiplier;

            // Müşteri oluştur
            Customer customer = new Customer(customerIdCounter++, requestedPackages, totalPayment);

            // Görsel spawn
            if (customerPrefab != null && queuePositions != null && customerQueue.Count < queuePositions.Length)
            {
                Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : queuePositions[customerQueue.Count].position;
                GameObject customerObj = Instantiate(customerPrefab, spawnPos, Quaternion.identity);
                customer.gameObject = customerObj;

                // Müşteriyi kuyruğa götür
                MoveCustomerToQueue(customer, customerQueue.Count);
            }

            customerQueue.Add(customer);
            MilkFarmEvents.CustomerSpawned();

            Debug.Log($"[CustomerManager] Müşteri #{customer.id} spawn edildi. Talep: {requestedPackages} paket, Ödeme: {totalPayment}");
        }

        /// <summary>
        /// Müşteriyi kuyruk pozisyonuna götür
        /// </summary>
        private void MoveCustomerToQueue(Customer customer, int queueIndex)
        {
            if (customer.gameObject == null || queuePositions == null || queueIndex >= queuePositions.Length) return;

            // Basit pozisyon ataması (NavMesh entegrasyonu için CustomerController kullanılabilir)
            customer.gameObject.transform.position = queuePositions[queueIndex].position;
            customer.gameObject.transform.rotation = queuePositions[queueIndex].rotation;
        }

        /// <summary>
        /// Müşteriye paket servis et
        /// PackageManager tarafından çağrılır
        /// </summary>
        public void ServePackageToCustomer()
        {
            if (customerQueue.Count == 0) return;

            Customer currentCustomer = customerQueue[0];
            currentCustomer.remainingPackages--;

            Debug.Log($"[CustomerManager] Müşteri #{currentCustomer.id}'ye paket verildi. Kalan: {currentCustomer.remainingPackages}");

            // Müşteri siparişini tamamladı mı?
            if (currentCustomer.IsServed)
            {
                CompleteCustomerOrder(currentCustomer);
            }
        }

        /// <summary>
        /// Müşteri siparişini tamamla
        /// </summary>
        private void CompleteCustomerOrder(Customer customer)
        {
            // Para öde
            moneyManager.EarnMoney(customer.totalPayment);
            MilkFarmEvents.CustomerServed(customer.totalPayment);

            Debug.Log($"[CustomerManager] Müşteri #{customer.id} tamamlandı! Ödeme: {customer.totalPayment}");

            // Müşteriyi kuyruktan çıkar
            customerQueue.Remove(customer);

            // Görsel çıkart
            if (customer.gameObject != null)
            {
                // Exit animasyonu
                MoveCustomerToExit(customer);
            }

            // Kuyruktaki diğer müşterileri ileri kaydır
            UpdateQueuePositions();

            // Yeni müşteri spawn et
            SpawnCustomer();
        }

        /// <summary>
        /// Müşteriyi çıkışa götür ve destroy et
        /// </summary>
        private void MoveCustomerToExit(Customer customer)
        {
            if (customer.gameObject == null) return;

            // Basit çözüm: Anında destroy
            // TODO: NavMesh ile çıkışa götür, sonra destroy
            if (exitPoint != null)
            {
                customer.gameObject.transform.position = exitPoint.position;
            }
            
            Destroy(customer.gameObject, 0.5f);
        }

        /// <summary>
        /// Kuyruk pozisyonlarını güncelle
        /// </summary>
        private void UpdateQueuePositions()
        {
            for (int i = 0; i < customerQueue.Count; i++)
            {
                MoveCustomerToQueue(customerQueue[i], i);
            }
        }

        // === PUBLIC API ===

        /// <summary>
        /// Bekleyen müşteri var mı?
        /// </summary>
        public bool HasWaitingCustomer()
        {
            return customerQueue.Count > 0;
        }

        /// <summary>
        /// İlk müşteriyi al
        /// </summary>
        public Customer GetFirstCustomer()
        {
            return customerQueue.Count > 0 ? customerQueue[0] : null;
        }

        /// <summary>
        /// Kuyruk uzunluğu
        /// </summary>
        public int GetQueueLength()
        {
            return customerQueue.Count;
        }

        /// <summary>
        /// Debug: Tüm müşterileri temizle
        /// </summary>
        [ContextMenu("Debug: Clear All Customers")]
        public void DebugClearAllCustomers()
        {
            foreach (var customer in customerQueue)
            {
                if (customer.gameObject != null)
                {
                    Destroy(customer.gameObject);
                }
            }
            customerQueue.Clear();
            Debug.Log("[CustomerManager] Tüm müşteriler temizlendi!");
        }

        /// <summary>
        /// Debug: Kuyruğu doldur
        /// </summary>
        [ContextMenu("Debug: Fill Queue")]
        public void DebugFillQueue()
        {
            FillQueue();
        }
    }
}
