using UnityEngine;
using Zenject;
using System.Collections;
using System.Collections.Generic;

namespace MilkFarm
{
    /// <summary>
    /// Money Manager - Multi Coin Version
    /// 1 şişe = 1 coin
    /// </summary>
    public class MoneyManager : MonoBehaviour
    {
        [Inject] private SaveManager saveManager;
        [Inject] private CurrencyManager currencyManager;

        [Header("Coin Spawn")]
        [SerializeField] private Transform coinSpawnPoint;
        [SerializeField] private GameObject coinPrefab;

        [Header("Coin Stack Settings")]
        [SerializeField] private float coinSpacingX = 0.25f;
        [SerializeField] private float coinSpacingZ = 0.25f;
        [SerializeField] private float coinSpacingY = 0.072f;
        [SerializeField] private int gridSize = 3;
        [SerializeField] private int maxLayers = 5;

        [Header("Collection")]
        [SerializeField] private int coinsPerClick = 9;

        [Header("Spawn Delay")]
        [SerializeField] private float coinSpawnDelay = 0.1f; // Coin'ler arasında delay

        private float currentMoney;
        private float pendingMoney;
        private List<GameObject> spawnedCoins = new List<GameObject>();

        private Vector3 basePosition;
        private Vector3 xDirection;
        private Vector3 zDirection;
        private Vector3 yDirection;

        private void Start()
        {
            CalculateGridDirections();
            LoadMoney();
        }
        private void OnEnable()
        {
            MilkFarmEvents.OnSaveRequested += HandleSaveRequested;
        }

        private void OnDisable()
        {
            MilkFarmEvents.OnSaveRequested -= HandleSaveRequested;
        }

        // ✅ HandleSaveRequested metodu EKLE:

        private void HandleSaveRequested()
        {
            SaveMoney();
        }
        private void CalculateGridDirections()
        {
            if (coinSpawnPoint == null)
            {
                Debug.LogError("[MoneyManager] Coin spawn point NULL!");
                return;
            }

            basePosition = coinSpawnPoint.position;

            // SpawnPoint’in kendi yönleri (local axis)
            xDirection = coinSpawnPoint.right * coinSpacingX;
            zDirection = coinSpawnPoint.forward * coinSpacingZ;
            yDirection = coinSpawnPoint.up * coinSpacingY;

            Debug.Log("[MoneyManager] Grid directions calculated dynamically!");
        }


        private void LoadMoney()
        {
            var saveData = saveManager.GetCurrentSaveData();

            currentMoney = saveData.currentMoney;
            pendingMoney = saveData.pendingMoney; // ✅ YENİ

            // ✅ YENİ: Pending coin'leri spawn et
            int coinCount = saveData.pendingCoins;
            if (coinCount > 0)
            {
                Debug.Log($"[MoneyManager] 📂 Loading {coinCount} pending coins...");
                StartCoroutine(SpawnPendingCoinsOnLoad(coinCount));
            }

            if (currencyManager != null)
            {
                currencyManager.UpdateCashUI(currentMoney);
            }

            Debug.Log($"[MoneyManager] 💵 Loaded - Money: {currentMoney}, Pending: {pendingMoney}, Coins: {coinCount}");
        }

        // ✅ YENİ METOD: Load'dan sonra coin spawn

        private IEnumerator SpawnPendingCoinsOnLoad(int count)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnSingleCoin(coinSpawnPoint != null ? coinSpawnPoint.position : transform.position);

                // Instant görünmesi için frame delay (opsiyonel)
                if (i % 9 == 0) yield return null;
            }

            Debug.Log($"[MoneyManager] ✅ {count} coin spawn edildi!");
        }

        /// <summary>
        /// Para kazan - ÇOKLU COIN SPAWN
        /// </summary>
        /// <param name="amount">Toplam para</param>
        /// <param name="bottleCount">Şişe sayısı (coin sayısı)</param>
        /// <param name="spawnPosition">Spawn başlangıç pozisyonu</param>
        public void EarnMoney(float amount, int bottleCount, Vector3? spawnPosition = null)
        {
            pendingMoney += amount;

            // Coin'leri delay ile spawn et
            Vector3 startPos = spawnPosition ?? coinSpawnPoint.position;
            StartCoroutine(SpawnMultipleCoins(bottleCount, startPos));

            MilkFarmEvents.MoneyEarned(amount);

            Debug.Log($"[MoneyManager] 💰 Para kazanıldı: {amount} ({bottleCount} coin). Masada: {pendingMoney}");
        }

        /// <summary>
        /// Eski API uyumluluğu (tek coin)
        /// </summary>
        public void EarnMoney(float amount, Vector3? spawnPosition = null)
        {
            EarnMoney(amount, 1, spawnPosition);
        }

        // === COIN SPAWN ===

        /// <summary>
        /// Çoklu coin spawn (delay ile)
        /// </summary>
        private IEnumerator SpawnMultipleCoins(int count, Vector3 startPos)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnSingleCoin(startPos);

                // Delay (opsiyonel - animasyon için)
                if (coinSpawnDelay > 0 && i < count - 1)
                {
                    yield return new WaitForSeconds(coinSpawnDelay);
                }
            }
        }

        /// <summary>
        /// Tek coin spawn
        /// </summary>
        private void SpawnSingleCoin(Vector3 startPos)
        {
            if (coinPrefab == null || coinSpawnPoint == null) return;

            int maxCoins = gridSize * gridSize * maxLayers;
            if (spawnedCoins.Count >= maxCoins)
            {
                Debug.LogWarning("[MoneyManager] Coin stack FULL!");
                return;
            }

            int index = spawnedCoins.Count;
            Vector3 targetPos = CalculateCoinPosition(index);

            GameObject coin = Instantiate(coinPrefab, startPos, Quaternion.identity);
            coin.transform.SetParent(coinSpawnPoint);
            spawnedCoins.Add(coin);

            FlyingItem flyer = coin.GetComponent<FlyingItem>();
            if (flyer == null) flyer = coin.AddComponent<FlyingItem>();
            flyer.FlyTo(targetPos);
        }

        private Vector3 CalculateCoinPosition(int index)
        {
            int layer = index / (gridSize * gridSize);
            int gridIndex = index % (gridSize * gridSize);
            int row = gridIndex / gridSize;
            int col = gridIndex % gridSize;

            Vector3 position = basePosition;
            position += xDirection * col;
            position += zDirection * row;
            position += yDirection * layer;

            return position;
        }

        // === COLLECT MONEY ===

        public void CollectCoins()
        {
            if (spawnedCoins.Count == 0) return;

            int coinsToCollect = Mathf.Min(coinsPerClick, spawnedCoins.Count);

            for (int i = 0; i < coinsToCollect; i++)
            {
                int lastIndex = spawnedCoins.Count - 1;
                GameObject coin = spawnedCoins[lastIndex];

                if (coin != null) Destroy(coin);
                spawnedCoins.RemoveAt(lastIndex);
            }

            Debug.Log($"[MoneyManager] {coinsToCollect} coin toplandı! Kalan: {spawnedCoins.Count}");
            CollectMoneyFromTable();
        }

        public void CollectMoneyFromTable()
        {
            if (pendingMoney <= 0f && spawnedCoins.Count == 0) return;

            if (pendingMoney > 0f)
            {
                currentMoney += pendingMoney;
                float collected = pendingMoney;
                pendingMoney = 0f;

                if (currencyManager != null)
                {
                    currencyManager.UpdateCashUI(currentMoney);
                }

                SaveMoney();
                MilkFarmEvents.MoneyCollected(collected);

                Debug.Log($"[MoneyManager] ✅ {collected} para toplandı! Toplam: {currentMoney}");
            }

            CollectCoins();
        }

        // === SPEND MONEY ===

        public bool SpendMoney(float amount)
        {
            if (currentMoney < amount)
            {
                Debug.LogWarning($"[MoneyManager] ❌ Yetersiz para!");
                return false;
            }

            currentMoney -= amount;

            if (currencyManager != null)
            {
                currencyManager.UpdateCashUI(currentMoney);
            }

            SaveMoney();
            MilkFarmEvents.MoneySpent(amount);

            Debug.Log($"[MoneyManager] 💸 {amount} harcandı. Kalan: {currentMoney}");
            return true;
        }

        public bool CanAfford(float amount) => currentMoney >= amount;

        // === GETTERS ===

        public float GetCurrentMoney() => currentMoney;
        public float GetPendingMoney() => pendingMoney;
        public bool HasPendingMoney() => pendingMoney > 0f;
        public int GetCoinCount() => spawnedCoins.Count;
        public bool HasCoins() => spawnedCoins.Count > 0;

        // === SAVE ===

        private void SaveMoney()
        {
            var saveData = saveManager.GetCurrentSaveData();

            saveData.currentMoney = currentMoney;
            saveData.pendingMoney = pendingMoney; // ✅ YENİ
            saveData.pendingCoins = spawnedCoins.Count; // ✅ YENİ

            saveManager.SaveGame(saveData);

            Debug.Log($"[MoneyManager] 💾 Saved - Money: {currentMoney}, Pending: {pendingMoney}, Coins: {spawnedCoins.Count}");
        }

        // === DEBUG ===

        [ContextMenu("Debug: Earn 1000 (10 coins)")]
        public void DebugEarnMoney()
        {
            EarnMoney(1000f, 10);
        }

        [ContextMenu("Debug: Collect Money")]
        public void DebugCollectMoney()
        {
            CollectMoneyFromTable();
        }
    }
}