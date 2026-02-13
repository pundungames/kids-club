using System.Collections;
using UnityEngine;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// Auto Worker (Eleman) sistemini yöneten manager
    /// GDD v2'ye göre tam implementation
    /// Sıralama: Yem/Su → Süt Toplama → Paket Satış → Para Toplama
    /// </summary>
    public class AutoWorkerManager : MonoBehaviour
    {
        [Inject] private GameConfig config;
        [Inject] private IAPManager iapManager;
        [Inject] private IAnimalManager animalManager;
        [Inject] private StationManager stationManager;
        [Inject] private PackageManager packageManager;
        [Inject] private CustomerManager customerManager;
        [Inject] private MoneyManager moneyManager;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;

        private Coroutine autoWorkerCoroutine;

        private void Start()
        {
            StartAutoWorker();
        }

        /// <summary>
        /// Auto Worker'ı başlat
        /// </summary>
        private void StartAutoWorker()
        {
            if (autoWorkerCoroutine != null)
            {
                StopCoroutine(autoWorkerCoroutine);
            }

            autoWorkerCoroutine = StartCoroutine(AutoWorkerRoutine());
        }

        /// <summary>
        /// Auto Worker ana döngüsü
        /// Her interval'da sırayla tüm işlemleri yapar
        /// </summary>
        private IEnumerator AutoWorkerRoutine()
        {
            while (true)
            {
                // Auto Worker aktif mi kontrol et
                if (iapManager.HasAutoWorker())
                {
                    PerformAutoWorkerTick();
                    MilkFarmEvents.AutoWorkerTick();
                }

                // GDD: 1 saniye interval
                yield return new WaitForSeconds(config.autoWorkerInterval);
            }
        }

        /// <summary>
        /// Tek bir Auto Worker tick'i
        /// Sıralama: Yem/Su → Süt Toplama → Paket Satış → Para Toplama
        /// </summary>
        private void PerformAutoWorkerTick()
        {
            LogDebug("=== Auto Worker Tick ===");

            // 1. YEM & SU KONTROLÜ
            // Not: Auto Feeder IAP varsa bu adım otomatik yapılıyor zaten
            if (!iapManager.HasAutoFeeder())
            {
                AutoFillStations();
            }

            // 2. SÜT TOPLAMA
            AutoCollectMilk();

            // 3. PAKET SATIŞI
            AutoSellPackages();

            // 4. PARA TOPLAMA
            AutoCollectMoney();
        }

        /// <summary>
        /// 1. Yem & Su Doldurma (Auto Feeder yoksa)
        /// </summary>
        private void AutoFillStations()
        {
            // Her istasyonu kontrol et
            for (int i = 0; i < 3; i++) // 3 istasyon
            {
                var station = stationManager.GetStation(i);
                if (station == null) continue;

                // Yem boş mu?
                if (!station.HasFood)
                {
                    // Yemliği doldur (simüle tap)
                    // Not: StationManager içinde direkt dolduran bir method ekleyebiliriz
                    // Şimdilik event trigger ile simüle ediyoruz
                    station.foodFill = 1f;
                    station.feedingTimer = config.feedingInterval;
                    MilkFarmEvents.StationFoodRefilled(i);
                    LogDebug($"İstasyon {i} yemlik dolduruldu.");
                }

                // Su boş mu?
                if (!station.HasWater)
                {
                    station.waterFill = 1f;
                    station.wateringTimer = config.wateringInterval;
                    MilkFarmEvents.StationWaterRefilled(i);
                    LogDebug($"İstasyon {i} suluk dolduruldu.");
                }
            }
        }

        /// <summary>
        /// 2. Süt Toplama
        /// Üzerinde süt olan inekleri dolaş, süt topla
        /// </summary>
        private void AutoCollectMilk()
        {
            var animals = animalManager.GetAllAnimals();
            foreach (var animal in animals)
            {
                if (!animal.isUnlocked) continue;
                if (animal.currentProduct <= 0) continue;
                animalManager.CollectFromAnimal(animal.index);
            }
        }

        /// <summary>
        /// 3. Paket Satışı
        /// Paket varsa ve müşteri varsa sat
        /// </summary>
        private void AutoSellPackages()
        {
            // Paket ve müşteri varken sat
            while (packageManager.HasPackages() && customerManager.HasWaitingCustomer())
            {
                // 1 paket sat
                customerManager.ServePackageToCustomer();
                LogDebug("1 paket satıldı (Auto Worker).");
                
                // Performans için her tick 1 paket (isterseniz daha fazla)
                break;
            }
        }

        /// <summary>
        /// 4. Para Toplama
        /// Masada para varsa topla
        /// </summary>
        private void AutoCollectMoney()
        {
            if (moneyManager.HasPendingMoney())
            {
                float pendingAmount = moneyManager.GetPendingMoney();
                moneyManager.CollectMoneyFromTable();
                LogDebug($"{pendingAmount} para toplandı (Auto Worker).");
            }
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[AutoWorkerManager] {message}");
            }
        }

        private void OnDestroy()
        {
            if (autoWorkerCoroutine != null)
            {
                StopCoroutine(autoWorkerCoroutine);
            }
        }

        /// <summary>
        /// Debug: Auto Worker'ı zorla çalıştır
        /// </summary>
        [ContextMenu("Debug: Force Auto Worker Tick")]
        public void DebugForceAutoWorkerTick()
        {
            if (iapManager.HasAutoWorker())
            {
                PerformAutoWorkerTick();
                Debug.Log("[AutoWorkerManager] Manual tick yapıldı!");
            }
            else
            {
                Debug.LogWarning("[AutoWorkerManager] Auto Worker satın alınmamış!");
            }
        }
    }
}
