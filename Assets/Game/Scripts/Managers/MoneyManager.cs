using UnityEngine;
using Zenject;
using System.Collections.Generic;

namespace MilkFarm
{
    /// <summary>
    /// Para sistemini yöneten manager
    /// Kazanma, harcama, toplama işlemlerini yönetir
    /// </summary>
    public class MoneyManager : MonoBehaviour
    {
        [Inject] private SaveManager saveManager;

        [Header("Para UI")]
        [SerializeField] private Transform moneySpawnPoint;
        [SerializeField] private GameObject moneyPrefab;

        [Header("Para Stack Ayarları")]
        [SerializeField] private float moneySpacingX = 0.4f;
        [SerializeField] private float moneySpacingZ = 0.7f;
        [SerializeField] private float moneyHeight = 0.2f;
        [SerializeField] private float moneyStairOffset = 0.3f;
        [SerializeField] private int maxMoneyStacksOnTable = 20; // 4x5 grid

        private float currentMoney;
        private float pendingMoneyOnTable;
        private List<GameObject> spawnedMoneyObjects = new List<GameObject>();

        private const int GRID_SIZE = 4;

        private void Start()
        {
            LoadMoney();
        }

        private void LoadMoney()
        {
            var saveData = saveManager.GetCurrentSaveData();
            currentMoney = saveData.currentMoney;
            Debug.Log($"[MoneyManager] Para yüklendi: {currentMoney}");
        }

        /// <summary>
        /// Para kazanma (müşteriden)
        /// </summary>
        public void EarnMoney(float amount)
        {
            pendingMoneyOnTable += amount;
            MilkFarmEvents.MoneyEarned(amount);

            // Masada görsel para spawn et
            SpawnMoneyVisual(amount);

            Debug.Log($"[MoneyManager] Para kazanıldı: {amount}. Masada bekleyen: {pendingMoneyOnTable}");
        }

        /// <summary>
        /// Masadaki parayı topla
        /// </summary>
        public void CollectMoneyFromTable()
        {
            if (pendingMoneyOnTable <= 0f) return;

            currentMoney += pendingMoneyOnTable;
            float collected = pendingMoneyOnTable;
            pendingMoneyOnTable = 0f;

            // Görsel para objelerini temizle
            ClearMoneyVisuals();

            MilkFarmEvents.MoneyCollected(collected);
            SaveMoney();

            Debug.Log($"[MoneyManager] Masadan para toplandı: {collected}. Toplam para: {currentMoney}");
        }

        /// <summary>
        /// Para harcama
        /// </summary>
        public bool SpendMoney(float amount)
        {
            if (currentMoney < amount)
            {
                Debug.LogWarning($"[MoneyManager] Yetersiz para! Gerekli: {amount}, Mevcut: {currentMoney}");
                return false;
            }

            currentMoney -= amount;
            MilkFarmEvents.MoneySpent(amount);
            SaveMoney();

            Debug.Log($"[MoneyManager] Para harcandı: {amount}. Kalan: {currentMoney}");
            return true;
        }

        /// <summary>
        /// Yeterli para var mı?
        /// </summary>
        public bool CanAfford(float amount)
        {
            return currentMoney >= amount;
        }

        /// <summary>
        /// Görsel para spawn et
        /// </summary>
        private void SpawnMoneyVisual(float amount)
        {
            if (moneyPrefab == null || moneySpawnPoint == null) return;
            if (spawnedMoneyObjects.Count >= maxMoneyStacksOnTable)
            {
                Debug.LogWarning("[MoneyManager] Masa para ile dolu, yeni para spawn edilemiyor!");
                return;
            }

            int index = spawnedMoneyObjects.Count;
            int layer = index / GRID_SIZE;
            int floorIndex = index % GRID_SIZE;
            int row = floorIndex / 2;
            int col = floorIndex % 2;
            float stairShift = layer * moneyStairOffset;

            Vector3 targetPos = moneySpawnPoint.position +
                               (moneySpawnPoint.right * col * moneySpacingX) +
                               (moneySpawnPoint.forward * row * moneySpacingZ) +
                               (moneySpawnPoint.forward * stairShift) +
                               (Vector3.up * layer * moneyHeight);

            Quaternion moneyRot = Quaternion.Euler(0, -90, 0);
            GameObject moneyObj = Instantiate(moneyPrefab, targetPos, moneyRot);
            spawnedMoneyObjects.Add(moneyObj);

            // Flying animation eklenebilir
            // FlyToPosition(moneyObj, targetPos);
        }

        /// <summary>
        /// Görsel para objelerini temizle
        /// </summary>
        private void ClearMoneyVisuals()
        {
            foreach (var moneyObj in spawnedMoneyObjects)
            {
                if (moneyObj != null)
                {
                    Destroy(moneyObj);
                }
            }
            spawnedMoneyObjects.Clear();
        }

        /// <summary>
        /// Para kaydı
        /// </summary>
        private void SaveMoney()
        {
            var saveData = saveManager.GetCurrentSaveData();
            saveData.currentMoney = currentMoney;
            saveManager.SaveGame(saveData);
        }

        // === PUBLIC API ===

        public float GetCurrentMoney() => currentMoney;
        public float GetPendingMoney() => pendingMoneyOnTable;
        public bool HasPendingMoney() => pendingMoneyOnTable > 0f;

        /// <summary>
        /// Debug: Para ekle
        /// </summary>
        [ContextMenu("Debug: Add 1000 Money")]
        public void DebugAddMoney()
        {
            currentMoney += 1000f;
            SaveMoney();
            Debug.Log($"[MoneyManager] 1000 para eklendi! Toplam: {currentMoney}");
        }
    }
}
