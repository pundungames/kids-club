using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

namespace MilkFarm.UI
{
    /// <summary>
    /// Ana oyun UI'sini yöneten controller
    /// Para, paket sayısı, inek bilgileri vb gösterir
    /// </summary>
    public class MilkFarmUIController : MonoBehaviour
    {
        [Inject] private MoneyManager moneyManager;
        [Inject] private PackageManager packageManager;
        [Inject] private CustomerManager customerManager;
        [Inject] private CowManager cowManager;

        [Header("Para UI")]
        [SerializeField] private TextMeshProUGUI moneyText;

        [Header("Paket UI")]
        [SerializeField] private TextMeshProUGUI packageText;

        [Header("Müşteri UI")]
        [SerializeField] private TextMeshProUGUI queueText;

        [Header("İnek UI (Opsiyonel)")]
        [SerializeField] private TextMeshProUGUI cowInfoText;

        private void Start()
        {
            // Event dinleyicileri
            MilkFarmEvents.OnMoneyEarned += OnMoneyChanged;
            MilkFarmEvents.OnMoneyCollected += OnMoneyChanged;
            MilkFarmEvents.OnMoneySpent += OnMoneyChanged;
            MilkFarmEvents.OnPackageCreated += OnPackageChanged;
            MilkFarmEvents.OnPackageSold += OnPackageChanged;
            MilkFarmEvents.OnCustomerSpawned += OnQueueChanged;
            MilkFarmEvents.OnCustomerServed += OnQueueChanged;

            // İlk güncelleme
            UpdateAllUI();
        }

        private void OnDestroy()
        {
            // Event dinleyicilerini kaldır
            MilkFarmEvents.OnMoneyEarned -= OnMoneyChanged;
            MilkFarmEvents.OnMoneyCollected -= OnMoneyChanged;
            MilkFarmEvents.OnMoneySpent -= OnMoneyChanged;
            MilkFarmEvents.OnPackageCreated -= OnPackageChanged;
            MilkFarmEvents.OnPackageSold -= OnPackageChanged;
            MilkFarmEvents.OnCustomerSpawned -= OnQueueChanged;
            MilkFarmEvents.OnCustomerServed -= OnQueueChanged;
        }

        private void Update()
        {
            // Her frame UI güncelle (veya event-based yapabilirsin)
            UpdateAllUI();
        }

        private void UpdateAllUI()
        {
            UpdateMoneyUI();
            UpdatePackageUI();
            UpdateQueueUI();
            UpdateCowUI();
        }

        private void OnMoneyChanged(float amount)
        {
            UpdateMoneyUI();
        }

        private void OnPackageChanged(int count)
        {
            UpdatePackageUI();
        }

        private void OnQueueChanged(float payment)
        {
            UpdateQueueUI();
        }

        private void OnQueueChanged()
        {
            UpdateQueueUI();
        }

        private void UpdateMoneyUI()
        {
            if (moneyText != null)
            {
                float currentMoney = moneyManager.GetCurrentMoney();
                float pendingMoney = moneyManager.GetPendingMoney();
                moneyText.text = $"Para: {currentMoney:F0}";
                
                if (pendingMoney > 0f)
                {
                    moneyText.text += $" (+{pendingMoney:F0})";
                }
            }
        }

        private void UpdatePackageUI()
        {
            if (packageText != null)
            {
                int packages = packageManager.GetCurrentPackageCount();
                int capacity = packageManager.GetCurrentCapacity();
                packageText.text = $"Paket: {packages}/{capacity}";
            }
        }

        private void UpdateQueueUI()
        {
            if (queueText != null)
            {
                int queueLength = customerManager.GetQueueLength();
                queueText.text = $"Müşteri: {queueLength}";
            }
        }

        private void UpdateCowUI()
        {
            if (cowInfoText != null)
            {
                var cows = cowManager.GetAllCows();
                int unlockedCount = cows.FindAll(c => c.isUnlocked).Count;
                int totalMilk = 0;
                
                foreach (var cow in cows)
                {
                    if (cow.isUnlocked)
                    {
                        totalMilk += cow.currentMilk;
                    }
                }

                cowInfoText.text = $"İnek: {unlockedCount}/12\nSüt: {totalMilk}";
            }
        }
    }
}
