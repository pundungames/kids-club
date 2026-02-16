using UnityEngine;
using Zenject;
using System.Collections;
using System.Collections.Generic;

namespace MilkFarm
{
    public class MoneyManager : MonoBehaviour
    {
        [Inject] private SaveManager saveManager;
        [Inject] private CurrencyManager currencyManager;
        [SerializeField] private bool isChickenScene = false;

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
        [SerializeField] private float coinSpawnDelay = 0.1f;

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
            xDirection = coinSpawnPoint.right * coinSpacingX;
            zDirection = coinSpawnPoint.forward * coinSpacingZ;
            yDirection = coinSpawnPoint.up * coinSpacingY;
        }

        private void LoadMoney()
        {
            var saveData = saveManager.GetCurrentSaveData();

            // ✅ HEPSİ sahneye göre ayrı
            if (isChickenScene)
            {
                currentMoney = saveData.chickenMoney;
                pendingMoney = saveData.chickenPendingMoney;
                int coinCount = saveData.chickenPendingCoins;

                if (coinCount > 0)
                    StartCoroutine(SpawnPendingCoinsOnLoad(coinCount));

                Debug.Log($"[MoneyManager-Chicken] 💵 Loaded - Money: {currentMoney}, Pending: {pendingMoney}, Coins: {coinCount}");
            }
            else
            {
                currentMoney = saveData.currentMoney;
                pendingMoney = saveData.pendingMoney;
                int coinCount = saveData.pendingCoins;

                if (coinCount > 0)
                    StartCoroutine(SpawnPendingCoinsOnLoad(coinCount));

                Debug.Log($"[MoneyManager-Cow] 💵 Loaded - Money: {currentMoney}, Pending: {pendingMoney}, Coins: {coinCount}");
            }

            if (currencyManager != null)
                currencyManager.UpdateCashUI(currentMoney);
        }

        private IEnumerator SpawnPendingCoinsOnLoad(int count)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnSingleCoin(coinSpawnPoint != null ? coinSpawnPoint.position : transform.position);
                if (i % 9 == 0) yield return null;
            }
        }

        // === EARN MONEY ===

        public void EarnMoney(float amount, int bottleCount, Vector3? spawnPosition = null)
        {
            pendingMoney += amount;

            Vector3 startPos = spawnPosition ?? coinSpawnPoint.position;
            StartCoroutine(SpawnMultipleCoins(bottleCount, startPos));

            SaveMoney(); // ✅ Kazandıktan hemen sonra save!

            MilkFarmEvents.MoneyEarned(amount);
            Debug.Log($"[MoneyManager] 💰 +{amount} ({bottleCount} coin). Pending: {pendingMoney}");
        }

        public void EarnMoney(float amount, Vector3? spawnPosition = null)
        {
            EarnMoney(amount, 1, spawnPosition);
        }

        // === COIN SPAWN ===

        private IEnumerator SpawnMultipleCoins(int count, Vector3 startPos)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnSingleCoin(startPos);
                if (coinSpawnDelay > 0 && i < count - 1)
                    yield return new WaitForSeconds(coinSpawnDelay);
            }
        }

        private void SpawnSingleCoin(Vector3 startPos)
        {
            if (coinPrefab == null || coinSpawnPoint == null) return;

            int maxCoins = gridSize * gridSize * maxLayers;
            if (spawnedCoins.Count >= maxCoins) return;

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
                    currencyManager.UpdateCashUI(currentMoney);

                SaveMoney();
                MilkFarmEvents.MoneyCollected(collected);

                Debug.Log($"[MoneyManager] ✅ {collected} toplandı! Toplam: {currentMoney}");
            }

            CollectCoins();
        }

        // === SPEND MONEY ===

        public bool SpendMoney(float amount)
        {
            if (currentMoney < amount) return false;

            currentMoney -= amount;

            if (currencyManager != null)
                currencyManager.UpdateCashUI(currentMoney);

            SaveMoney();
            MilkFarmEvents.MoneySpent(amount);
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

            // ✅ HEPSİ sahneye göre ayrı
            if (isChickenScene)
            {
                saveData.chickenMoney = currentMoney;
                saveData.chickenPendingMoney = pendingMoney;
                saveData.chickenPendingCoins = spawnedCoins.Count;
            }
            else
            {
                saveData.currentMoney = currentMoney;
                saveData.pendingMoney = pendingMoney;
                saveData.pendingCoins = spawnedCoins.Count;
            }

            saveManager.SaveGame(saveData);
        }

        // === DEBUG ===

        [ContextMenu("Debug: Earn 1000 (10 coins)")]
        public void DebugEarnMoney() => EarnMoney(1000f, 10);

        [ContextMenu("Debug: Collect Money")]
        public void DebugCollectMoney() => CollectMoneyFromTable();
    }
}