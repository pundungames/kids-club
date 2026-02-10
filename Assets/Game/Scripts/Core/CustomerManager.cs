using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

namespace MilkFarm
{
    [System.Serializable]
    public class Customer
    {
        public int id;
        public int requestedBottles;
        public int remainingBottles;
        public float totalPayment;
        public CustomerController controller;

        public Customer(int customerId, int bottles, float payment)
        {
            id = customerId;
            requestedBottles = bottles;
            remainingBottles = bottles;
            totalPayment = payment;
        }

        public bool IsServed => remainingBottles <= 0;
    }

    public class CustomerManager : MonoBehaviour
    {
        [Inject] private GameConfig config;
        [Inject] private IAPManager iapManager;
        [Inject] private MoneyManager moneyManager;

        [Header("Sıra Sistemi")]
        [SerializeField] internal Transform queueStartPoint;
        [SerializeField] private float rowSpacing = 2.0f;
        [SerializeField] private float colSpacing = 1.5f;
        [SerializeField] private int maxQueueSize = 10;

        private List<Customer> customerQueue = new List<Customer>();
        private int customerIdCounter = 0;

        public bool IsQueueFull => customerQueue.Count >= maxQueueSize;

        private void Start()
        {
            Debug.Log("[CustomerManager] Başlatıldı.");
        }

        public void JoinQueue(CustomerController controller)
        {
            if (customerQueue.Count >= maxQueueSize)
            {
                Debug.LogWarning("[CustomerManager] Kuyruk dolu!");
                Destroy(controller.gameObject);
                return;
            }

            int requestedBottles = Random.Range(config.customerMinRequest, config.customerMaxRequest + 1);
            float basePayment = requestedBottles * config.moneyPerMilk;
            float richMultiplier = iapManager.GetCustomerRichMultiplier();
            float totalPayment = basePayment * richMultiplier;

            Customer customer = new Customer(customerIdCounter++, requestedBottles, totalPayment);
            customer.controller = controller;

            customerQueue.Add(customer);
            MilkFarmEvents.CustomerSpawned();

            UpdateQueuePositions();

            controller.SetRequestedBottles(requestedBottles);

            Debug.Log($"[CustomerManager] Müşteri #{customer.id} kuyruğa katıldı. Talep: {requestedBottles} şişe");
        }

        /// <summary>
        /// Şişe bazlı servis (tek şişe)
        /// </summary>
        public void ServeBottleToCustomer()
        {
            if (customerQueue.Count == 0)
            {
                Debug.LogWarning("[CustomerManager] Kuyrukta müşteri yok!");
                return;
            }

            Customer currentCustomer = customerQueue[0];

            if (currentCustomer.controller != null && !currentCustomer.controller.isReadyToBuy)
            {
                Debug.LogWarning("[CustomerManager] Müşteri hazır değil!");
                return;
            }

            currentCustomer.remainingBottles--;

            if (currentCustomer.controller != null)
            {
                currentCustomer.controller.GiveBottle();
            }

            Debug.Log($"[CustomerManager] Şişe verildi. Kalan: {currentCustomer.remainingBottles}/{currentCustomer.requestedBottles}");

            // Son şişe mi kontrol et
            if (currentCustomer.IsServed)
            {
                CompleteCustomerOrder(currentCustomer);
            }
        }

        /// <summary>
        /// Eski API - Geriye uyumluluk (AutoWorker için)
        /// </summary>
        public void ServePackageToCustomer()
        {
            ServeBottleToCustomer();
        }

        /// <summary>
        /// Müşteri siparişini tamamla - Çoklu coin spawn
        /// </summary>
        private void CompleteCustomerOrder(Customer customer)
        {
            Vector3 salePosition = customer.controller != null
                ? customer.controller.transform.position
                : transform.position;

            // ✅ YENİ: Şişe sayısı ile coin spawn
            moneyManager.EarnMoney(
                customer.totalPayment,           // Toplam para
                customer.requestedBottles,       // Şişe sayısı = Coin sayısı
                salePosition                     // Spawn pozisyonu
            );
            MilkFarmEvents.CustomerServed(customer.totalPayment);

          
            Debug.Log($"[CustomerManager] ✅ Müşteri #{customer.id} tamamlandı! " +
                      $"Ödeme: {customer.totalPayment}, Şişe: {customer.requestedBottles} coin");

            if (customer.controller != null)
            {
                customer.controller.CompletePurchase();
            }

            customerQueue.Remove(customer);
            StartCoroutine(CustomerPurchased(customer));
        }
        IEnumerator CustomerPurchased(Customer customer)
        {
            yield return new WaitForSeconds(1f);
            UpdateQueuePositions();

        }
        private void UpdateQueuePositions()
        {
            for (int i = 0; i < customerQueue.Count; i++)
            {
                if (customerQueue[i].controller != null)
                {
                    Vector3 newPos = GetPositionForIndex(i);
                    customerQueue[i].controller.UpdateQueuePosition(newPos);
                }
            }
        }

        public Vector3 GetPositionForIndex(int index)
        {
            if (queueStartPoint == null)
            {
                Debug.LogError("[CustomerManager] queueStartPoint null!");
                return transform.position;
            }

            int row = index / 2;
            int col = index % 2;

            Vector3 targetPos = queueStartPoint.position +
                               (queueStartPoint.forward * (-row * rowSpacing)) +
                               (queueStartPoint.right * (col * colSpacing));

            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, 2.0f, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return targetPos;
        }

        public void LeaveQueue(CustomerController controller)
        {
            Customer customer = customerQueue.Find(c => c.controller == controller);
            if (customer != null)
            {
                customerQueue.Remove(customer);
                UpdateQueuePositions();
            }
        }

        public int GetQueueIndex(CustomerController controller)
        {
            Customer customer = customerQueue.Find(c => c.controller == controller);
            return customer != null ? customerQueue.IndexOf(customer) : -1;
        }

        public bool HasWaitingCustomer()
        {
            if (customerQueue.Count == 0) return false;

            Customer first = customerQueue[0];
            if (first.controller != null)
            {
                return first.controller.isReadyToBuy;
            }

            return false;
        }

        public Customer GetFirstCustomer()
        {
            return customerQueue.Count > 0 ? customerQueue[0] : null;
        }

        public int GetQueueLength()
        {
            return customerQueue.Count;
        }

        // === DEBUG ===

        [ContextMenu("Debug: Clear All Customers")]
        public void DebugClearAllCustomers()
        {
            foreach (var customer in customerQueue)
            {
                if (customer.controller != null)
                {
                    Destroy(customer.controller.gameObject);
                }
            }
            customerQueue.Clear();
        }

        [ContextMenu("Debug: Print Queue")]
        public void DebugPrintQueue()
        {
            Debug.Log($"[CustomerManager] Kuyruk: {customerQueue.Count} müşteri");
            for (int i = 0; i < customerQueue.Count; i++)
            {
                var c = customerQueue[i];
                Debug.Log($"  [{i}] Müşteri #{c.id} - {c.remainingBottles}/{c.requestedBottles} şişe, {c.totalPayment}₺");
            }
        }
    }
}