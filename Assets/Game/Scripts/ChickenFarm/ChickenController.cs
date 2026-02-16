using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Zenject;
using MilkFarm;
using Dreamteck.Splines;

namespace ChickenFarm
{
    public class ChickenController : MonoBehaviour
    {
        private PackageManager packageManager;
        private TroughController feedTrough;
        private TroughController waterTrough;
        [Inject] private ChickenManager chickenManager;
        [Inject] AudioManager audioManager;
        [Header("Görsel Ayarlar")]
        [SerializeField] private Image progressBar;
        [SerializeField] private GameObject timerCanvas;
        [SerializeField] private GameObject eggIndicator;
        [SerializeField] private TextMeshProUGUI eggCountText;
        [SerializeField] private GameObject needsIndicator;
        [SerializeField] internal SplineComputer spline;
        [Header("Level Visuals")]
        [SerializeField] private GameObject[] levelModels;

        [Header("Üretim Ayarları")]
        [SerializeField] private int maxEggStack = 4;

        private bool isProducing = false;
        private bool isHolding = false;
        private int chickenIndex = -1;
        private Chicken chickenData;
        private GameConfig config;
        private IAPManager iapManager;
        private int eggStack = 0;
        private Coroutine productionCoroutine;
        private float savedTimer = 0f;
        private bool isPaused = false;

        private void OnEnable() => ChickenFarmEvents.OnTroughRefilled += HandleTroughRefilled;
        private void OnDisable() => ChickenFarmEvents.OnTroughRefilled -= HandleTroughRefilled;

        private void Update() => SyncChickenData();

        private void SyncChickenData()
        {
            if (chickenData == null || chickenData.index != chickenIndex) return;
            chickenData.currentEggs = eggStack;
            if (isPaused) chickenData.productionTimer = savedTimer;
            else if (isProducing && progressBar != null)
            {
                float pt = CalculateProductionTime();
                chickenData.productionTimer = pt - (progressBar.fillAmount * pt);
            }
            else chickenData.productionTimer = 0f;
        }

        public void SetFeedTrough(TroughController trough) => feedTrough = trough;
        public void SetWaterTrough(TroughController trough) => waterTrough = trough;
        public void SetPackageManager(PackageManager manager) => packageManager = manager;

        public void Initialize(int index, Chicken data, GameConfig gameConfig, IAPManager iap, SplineComputer spline)
        {
            if (spline != null) this.spline = spline;
            chickenIndex = index; chickenData = data; config = gameConfig; iapManager = iap;
            if (timerCanvas != null) timerCanvas.SetActive(false);
            if (eggIndicator != null) eggIndicator.SetActive(false);
            if (needsIndicator != null) needsIndicator.SetActive(false);
            RestoreFromSaveData();
            UpdateEggUI();
            if (chickenData != null) UpdateLevelModel(chickenData.level);
        }

        private void RestoreFromSaveData()
        {
            if (chickenData == null) return;
            eggStack = chickenData.currentEggs;
            savedTimer = chickenData.productionTimer;
            if (savedTimer > 0f || eggStack < maxEggStack)
            {
                if (feedTrough != null && waterTrough != null && feedTrough.HasResource && waterTrough.HasResource)
                    StartCoroutine(DelayedAutoStart());
                else isPaused = true;
            }
        }

        private IEnumerator DelayedAutoStart()
        {
            yield return new WaitForSeconds(0.1f);
            if (!isProducing && !isPaused) TryStartProduction();
        }

        private void HandleTroughRefilled(int stationIndex)
        {
            int myStation = chickenIndex / (config != null ? config.chickensPerStation : 3);
            if (stationIndex != myStation) return;
            if (isPaused && feedTrough != null && waterTrough != null && feedTrough.HasResource && waterTrough.HasResource)
                ResumeProduction();
            else if (!isProducing && eggStack < maxEggStack && feedTrough != null && waterTrough != null && feedTrough.HasResource && waterTrough.HasResource)
                TryStartProduction();
        }

        void OnMouseDown()
        {
            audioManager.Play("Tap");

            isHolding = true;
            if (IsPackageManagerFull()) return;
            if (eggStack > 0) CollectEggs();
            else if (!isProducing && !isPaused) TryStartProduction();
        }

        public void OnClicked()
        {
            if (IsPackageManagerFull()) return;
            if (eggStack > 0) CollectEggs();
            else if (!isProducing && !isPaused) TryStartProduction();
        }

        private bool IsPackageManagerFull()
        {
            if (packageManager == null) return false;
            int total = packageManager.GetProductionStackCount();
            if (packageManager.HasActiveCrate()) total++;
            return total >=16;
        }

        void OnMouseUp() { isHolding = false; }
        void OnMouseExit() { isHolding = false; }

        void TryStartProduction()
        {
            if (feedTrough == null || waterTrough == null) { ShowNeedsIndicator(true); return; }
            if (feedTrough.HasResource && waterTrough.HasResource)
                productionCoroutine = StartCoroutine(ProductionRoutine());
            else ShowNeedsIndicator(true);
        }

        void ResumeProduction()
        {
            if (!isPaused) return;
            isPaused = false; ShowNeedsIndicator(false);
            productionCoroutine = StartCoroutine(ProductionRoutine());
        }

        IEnumerator ProductionRoutine()
        {
            isProducing = true; isPaused = false;
            if (timerCanvas != null) timerCanvas.SetActive(true);
            ShowNeedsIndicator(false);
            if (feedTrough != null) feedTrough.OnCowStartProducing();
            if (waterTrough != null) waterTrough.OnCowStartProducing();

            while (true)
            {
                if (eggStack >= maxEggStack) break;
                float productionTime = CalculateProductionTime();
                float timer = savedTimer > 0f ? productionTime - savedTimer : 0f;
                savedTimer = 0f;
                if (progressBar != null) progressBar.fillAmount = timer / productionTime;

                while (timer < productionTime)
                {
                    if (feedTrough != null && waterTrough != null && (!feedTrough.HasResource || !waterTrough.HasResource))
                    {
                        savedTimer = productionTime - timer; isPaused = true;
                        ShowNeedsIndicator(true);
                        if (feedTrough != null) feedTrough.OnCowStopProducing();
                        if (waterTrough != null) waterTrough.OnCowStopProducing();
                        isProducing = false;
                        if (timerCanvas != null) timerCanvas.SetActive(false);
                        yield break;
                    }
                    float spd = isHolding && config != null ? config.tapHoldSpeedMultiplier : (isHolding ? 0.75f : 1.0f);
                    timer += Time.deltaTime * spd;
                    if (progressBar != null) progressBar.fillAmount = timer / productionTime;
                    yield return null;
                }
                ProduceEgg();
            }

            if (feedTrough != null) feedTrough.OnCowStopProducing();
            if (waterTrough != null) waterTrough.OnCowStopProducing();
            isProducing = false; isPaused = false; savedTimer = 0f;
            if (timerCanvas != null) timerCanvas.SetActive(false);
        }

        private float CalculateProductionTime()
        {
            if (config == null) return 25f;
            float baseTime = config.GetChickenProductionTime(chickenData?.level ?? 1);
            if (iapManager != null) baseTime /= iapManager.GetSpeedMultiplier();
            return baseTime;
        }

        private void ProduceEgg()
        {
            eggStack++; UpdateEggUI();
            if (chickenData != null) { chickenData.currentEggs = eggStack; ChickenFarmEvents.ChickenEggProduced(chickenIndex); }
        }

        public void CollectEggs()
        {
            if (eggStack <= 0 || packageManager == null) return;
            StartCoroutine(CollectEggsRoutine());
        }

        private IEnumerator CollectEggsRoutine()
        {
            int total = eggStack; eggStack = 0;
            if (chickenData != null) chickenData.currentEggs = 0;
            UpdateEggUI();
            for (int i = 0; i < total; i++) { packageManager.AddMilk(transform.position, spline, cooldown: 10f); yield return new WaitForSeconds(0.05f); }
            ChickenFarmEvents.ChickenEggCollected(chickenIndex, total);
            if (!isProducing && !isPaused && feedTrough != null && waterTrough != null && feedTrough.HasResource && waterTrough.HasResource)
                TryStartProduction();
        }

        private void UpdateEggUI()
        {
            if (eggIndicator != null) eggIndicator.SetActive(eggStack > 0);
            if (eggCountText != null) eggCountText.text = eggStack.ToString();
        }

        private void ShowNeedsIndicator(bool show) { if (needsIndicator != null) needsIndicator.SetActive(show); }

        public bool IsProducing => isProducing;
        public int GetEggStack() => eggStack;

        public void OnLevelChanged(int newLevel)
        {
            if (chickenData == null) return;
            chickenData.level = newLevel;
            if (chickenManager != null)
            {
                Sprite s = chickenManager.GetChickenSprite(newLevel);
                if (s != null) { var r = GetComponentInChildren<SpriteRenderer>(); if (r != null) r.sprite = s; }
            }
            UpdateLevelModel(newLevel);
        }

        private void UpdateLevelModel(int level)
        {
            if (levelModels == null || levelModels.Length == 0) return;
            for (int i = 0; i < levelModels.Length; i++) if (levelModels[i] != null) levelModels[i].SetActive(false);
            int idx = Mathf.Clamp(level - 1, 0, levelModels.Length - 1);
            if (levelModels[idx] != null) levelModels[idx].SetActive(true);
        }

        private void OnApplicationQuit() => SyncChickenData();
        private void OnApplicationPause(bool p) { if (p) SyncChickenData(); }
    }
}