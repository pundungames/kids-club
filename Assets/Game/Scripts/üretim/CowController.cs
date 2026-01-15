using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CowController : MonoBehaviour
{
    [Header("Baðlantýlar")]
    public CounterManager counterManager;

    // --- YENÝ EKLENENLER: YALAKLAR ---
    public TroughController feedTrough;  // Yemlik
    public TroughController waterTrough; // Suluk

    [Header("Görsel Ayarlar")]
    public Image progressBar;
    public GameObject timerCanvas;

    [Header("Üretim Ayarlarý")]
    public int productionBatchSize = 3;
    public float timePerMilk = 30f;
    public float boostMultiplier = 1.25f;

    private bool isProducing = false;
    private bool isHolding = false;

    void Start()
    {
        if (timerCanvas != null) timerCanvas.SetActive(false);
    }

    // --- INPUT GÜNCELLEMESÝ ---
    void OnMouseDown()
    {
        isHolding = true;

        // Eðer þu an üretim yoksa, YENÝ ÜRETÝMÝ DENE
        if (!isProducing)
        {
            TryStartProduction();
        }
    }

    // Eski InputManager için (Hala kullanýyorsan)
    public void OnClicked()
    {
        if (!isProducing)
        {
            TryStartProduction();
        }
    }

    // --- KONTROL MEKANÝZMASI ---
    void TryStartProduction()
    {
        // 1. Yalaklar tanýmlý mý? (Hata almamak için)
        if (feedTrough == null || waterTrough == null)
        {
            Debug.LogError("HATA: Ýneðe Yemlik veya Suluk baðlanmamýþ!");
            return;
        }

        // 2. Ýkisi de dolu mu? (HasResource = Seviye > 0)
        if (feedTrough.HasResource && waterTrough.HasResource)
        {
            // HER ÞEY TAMAM, BAÞLA!
            StartCoroutine(ProductionRoutine());
        }
        else
        {
            // BÝRÝSÝ BOÞ!
            Debug.Log("Ýnek Aç veya Susuz! Üretim yapýlamýyor.");
            // Buraya bir "Mööö" sesi veya "Kýzgýn Surat" ikonu koyabilirsin.
        }
    }

    void OnMouseUp() { isHolding = false; }
    void OnMouseExit() { isHolding = false; }

    IEnumerator ProductionRoutine()
    {
        isProducing = true;
        if (timerCanvas != null) timerCanvas.SetActive(true);

        for (int i = 0; i < productionBatchSize; i++)
        {
            float timer = 0f;

            while (timer < timePerMilk)
            {
                float speed = isHolding ? boostMultiplier : 1.0f;
                timer += Time.deltaTime * speed;

                if (progressBar != null) progressBar.fillAmount = timer / timePerMilk;

                yield return null;
            }

            if (counterManager != null)
            {
                counterManager.AddMilk(transform.position);
            }
        }

        // Döngü bitti. Yeni döngü için tekrar týklanmasý ve kaynaklarýn dolu olmasý gerekecek.
        isProducing = false;
        if (timerCanvas != null) timerCanvas.SetActive(false);
    }
}