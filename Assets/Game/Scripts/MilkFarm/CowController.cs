using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Zenject;

namespace MilkFarm
{
    public class CowController : MonoBehaviour
    {
        private PackageManager packageManager;
        private TroughController feedTrough;
        private TroughController waterTrough;

        [Header("Görsel Ayarlar")]
        [SerializeField] private Image progressBar;
        [SerializeField] private GameObject timerCanvas;
        [SerializeField] private GameObject milkIndicator; // Stack UI (Canvas)
        [SerializeField] private TextMeshProUGUI milkCountText; // "3" şişe var
        [SerializeField] private GameObject needsIndicator;

        [Header("Üretim Ayarları")]
        [SerializeField] private float baseTimePerMilk = 30f;
        [SerializeField] private int maxMilkStack = 6; // Milk case kapasitesi

        private bool isProducing = false;
        private bool isHolding = false;

        private int cowIndex = -1;
        private Cow cowData;
        private GameConfig config;
        private IAPManager iapManager;

        // Stack (manuel toplama için)
        private int milkStack = 0; // Üretilip bekleyen süt sayısı

        public void SetFeedTrough(TroughController trough)
        {
            feedTrough = trough;
        }

        public void SetWaterTrough(TroughController trough)
        {
            waterTrough = trough;
        }

        public void SetPackageManager(PackageManager manager)
        {
            packageManager = manager;
        }

        public void Initialize(int index, Cow data, GameConfig gameConfig, IAPManager iap)
        {
            cowIndex = index;
            cowData = data;
            config = gameConfig;
            iapManager = iap;

            if (timerCanvas != null) timerCanvas.SetActive(false);
            if (milkIndicator != null) milkIndicator.SetActive(false);
            if (needsIndicator != null) needsIndicator.SetActive(false);

            UpdateMilkUI();

            Debug.Log($"[CowController {cowIndex}] Initialize edildi.");
        }

        // === INPUT SİSTEMİ ===

        void OnMouseDown()
        {
            isHolding = true;

            // Package manager max stack kontrolü
            if (IsPackageManagerFull())
            {
                Debug.LogWarning($"[CowController {cowIndex}] Paketleme dolu! Tap yapılamaz.");
                return;
            }

            // Eğer süt varsa topla, yoksa üretim başlat
            if (milkStack > 0)
            {
                CollectMilk();
            }
            else if (!isProducing)
            {
                TryStartProduction();
            }
        }

        public void OnClicked()
        {
            if (IsPackageManagerFull())
            {
                Debug.LogWarning($"[CowController {cowIndex}] Paketleme dolu!");
                return;
            }

            if (milkStack > 0)
            {
                CollectMilk();
            }
            else if (!isProducing)
            {
                TryStartProduction();
            }
        }

        /// <summary>
        /// Package manager dolu mu kontrol et
        /// </summary>
        private bool IsPackageManagerFull()
        {
            if (packageManager == null) return false;

            // Production stack count + current active case
            int totalCases = packageManager.GetProductionStackCount();
            if (packageManager.HasActiveCrate()) totalCases++;

            return totalCases >= 8; // Max limit
        }

        void OnMouseUp() { isHolding = false; }
        void OnMouseExit() { isHolding = false; }

        // === ÜRETİM ===

        void TryStartProduction()
        {
            if (feedTrough == null || waterTrough == null)
            {
                Debug.LogError($"[CowController {cowIndex}] Yemlik/Suluk yok!");
                ShowNeedsIndicator(true);
                return;
            }

            if (feedTrough.HasResource && waterTrough.HasResource)
            {
                StartCoroutine(ProductionRoutine());
            }
            else
            {
                Debug.Log($"[CowController {cowIndex}] Yem/su yok!");
                ShowNeedsIndicator(true);
            }
        }

        IEnumerator ProductionRoutine()
        {
            isProducing = true;
            if (timerCanvas != null) timerCanvas.SetActive(true);
            ShowNeedsIndicator(false);

            // Trough'a bildir: Üretim başladı
            if (feedTrough != null) feedTrough.OnCowStartProducing();
            if (waterTrough != null) waterTrough.OnCowStartProducing();

            // Sonsuz döngü - stack dolana kadar
            while (true)
            {
                // Stack doluysa dur
                if (milkStack >= maxMilkStack)
                {
                    Debug.Log($"[CowController {cowIndex}] Stack dolu ({maxMilkStack}), üretim durdu.");
                    break;
                }

                // Package manager doluysa dur
                if (IsPackageManagerFull())
                {
                    Debug.Log($"[CowController {cowIndex}] Paketleme dolu, üretim durdu.");
                    break;
                }

                float productionTime = CalculateProductionTime();
                float timer = 0f;

                while (timer < productionTime)
                {
                    // Yem/su kontrol
                    if (feedTrough != null && waterTrough != null)
                    {
                        if (!feedTrough.HasResource || !waterTrough.HasResource)
                        {
                            ShowNeedsIndicator(true);

                            // Trough'a bildir: Üretim durdu
                            if (feedTrough != null) feedTrough.OnCowStopProducing();
                            if (waterTrough != null) waterTrough.OnCowStopProducing();

                            isProducing = false;
                            if (timerCanvas != null) timerCanvas.SetActive(false);
                            yield break;
                        }
                    }

                    // Package manager kontrol (üretim sırasında)
                    if (IsPackageManagerFull())
                    {
                        Debug.Log($"[CowController {cowIndex}] Paketleme doldu, üretim durduruluyor.");
                        break; // İç döngüden çık
                    }

                    float speedMultiplier = isHolding && config != null
                        ? config.tapHoldSpeedMultiplier
                        : (isHolding ? 0.75f : 1.0f);

                    timer += Time.deltaTime * speedMultiplier;

                    if (progressBar != null)
                        progressBar.fillAmount = timer / productionTime;

                    yield return null;
                }

                // Package manager doluysa çık
                if (IsPackageManagerFull())
                {
                    break; // Dış döngüden çık
                }

                // 1 şişe üretildi!
                ProduceMilk();
            }

            // Stack dolu veya paketleme dolu, üretim durdu
            // Trough'a bildir: Üretim bitti
            if (feedTrough != null) feedTrough.OnCowStopProducing();
            if (waterTrough != null) waterTrough.OnCowStopProducing();

            isProducing = false;
            if (timerCanvas != null) timerCanvas.SetActive(false);
        }

        private float CalculateProductionTime()
        {
            if (config == null || cowData == null)
                return baseTimePerMilk;

            float baseTime = config.baseMilkProductionTime;
            float levelMultiplier = Mathf.Pow(0.9f, cowData.level - 1);
            float leveledTime = baseTime * levelMultiplier;

            leveledTime = Mathf.Max(config.minProductionTime, leveledTime);

            if (iapManager != null)
            {
                leveledTime *= iapManager.GetGlobalSpeedMultiplier();
            }

            return leveledTime;
        }

        /// <summary>
        /// Süt üret (stack'e ekle, otomatik kasaya ATMA)
        /// </summary>
        private void ProduceMilk()
        {
            milkStack++; // Stack artır

            // UI'ı göster
            UpdateMilkUI();

            // CowData güncelle
            if (cowData != null)
            {
                cowData.currentMilk = milkStack;
                MilkFarmEvents.CowMilkProduced(cowIndex);
            }

            Debug.Log($"[CowController {cowIndex}] Süt üretildi! Stack: {milkStack}");
        }

        /// <summary>
        /// Süt topla (manuel - tıkla)
        /// Stack'teki tüm sütleri kasaya at
        /// </summary>
        public void CollectMilk()
        {
            if (milkStack <= 0)
            {
                Debug.LogWarning($"[CowController {cowIndex}] Toplanacak süt yok!");
                return;
            }

            if (packageManager == null)
            {
                Debug.LogError($"[CowController {cowIndex}] PackageManager yok!");
                return;
            }

            // Coroutine ile sıralı gönder
            StartCoroutine(CollectMilkRoutine());
        }

        /// <summary>
        /// Süt toplama routine (sıralı animasyon için)
        /// </summary>
        private IEnumerator CollectMilkRoutine()
        {
            int totalMilk = milkStack;
            Debug.Log($"[CowController {cowIndex}] {totalMilk} şişe kasaya gönderiliyor...");

            // Stack'i hemen sıfırla (UI kapansın)
            milkStack = 0;
            if (cowData != null)
            {
                cowData.currentMilk = 0;
            }
            UpdateMilkUI();

            // Her şişe için sırayla
            for (int i = 0; i < totalMilk; i++)
            {
                packageManager.AddMilk(transform.position);

                // Kısa delay (flying animasyon için)
                yield return new WaitForSeconds(0.1f);
            }

            MilkFarmEvents.CowMilkCollected(cowIndex, totalMilk);

            Debug.Log($"[CowController {cowIndex}] Tüm şişeler gönderildi!");

            // Üretim durmuşsa yeniden başlat
            if (!isProducing && feedTrough != null && waterTrough != null)
            {
                if (feedTrough.HasResource && waterTrough.HasResource)
                {
                    StartCoroutine(ProductionRoutine());
                }
            }
        }

        /// <summary>
        /// Milk indicator UI güncelle
        /// </summary>
        private void UpdateMilkUI()
        {
            if (milkIndicator != null)
            {
                // Stack varsa göster, yoksa gizle
                milkIndicator.SetActive(milkStack > 0);
            }

            if (milkCountText != null)
            {
                // Sayıyı yaz
                milkCountText.text = milkStack.ToString();
            }
        }

        // === VİSUAL ===

        private void ShowNeedsIndicator(bool show)
        {
            if (needsIndicator != null)
            {
                needsIndicator.SetActive(show);
            }
        }

        // === PUBLIC API ===

        public bool IsProducing => isProducing;
        public int GetMilkStack() => milkStack;
        public float GetProductionProgress()
        {
            if (progressBar != null)
                return progressBar.fillAmount;
            return 0f;
        }
    }
}