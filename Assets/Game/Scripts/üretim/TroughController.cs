using UnityEngine;

public class TroughController : MonoBehaviour
{
    [Header("Görsel Ayarlar")]
    public Transform fillMesh; // Yükselip alçalacak olan Yem/Su objesi (Modeli)
    public float loweredYOffset = -0.5f; // Boþken ne kadar aþaðý insin? (Örn: -0.5 birim)

    [Header("Durum")]
    [Range(0, 4)]
    public int currentLevel = 0; // 0=Boþ, 4=Dolu
    private const int MAX_LEVEL = 4;

    // Tüketim Ayarlarý
    private float totalDrainTime = 30f; // Toplam 30 saniyede biter
    private float timePerLevel;         // Her bir kademe kaç saniye? (Otomatik hesaplanýr)
    private float drainTimer = 0f;

    // Baþlangýç pozisyonlarýný saklayalým
    private Vector3 initialLocalPos;
    private Vector3 initialScale;

    // Ýnek kontrolü için: Ýçinde bir þey var mý?
    public bool HasResource => currentLevel > 0;

    void Start()
    {
        // 30 saniyeyi 4 kademeye böl (Her kademe 7.5 sn sürer)
        timePerLevel = totalDrainTime / MAX_LEVEL;

        if (fillMesh != null)
        {
            initialLocalPos = fillMesh.localPosition;
            initialScale = fillMesh.localScale;
            UpdateVisuals(); // Baþlangýçta durumu uygula
        }
    }

    void Update()
    {
        // Eðer içinde bir þey varsa zamanla azalýr
        if (currentLevel > 0)
        {
            drainTimer += Time.deltaTime;

            // Süre dolunca bir kademe düþ
            if (drainTimer >= timePerLevel)
            {
                drainTimer = 0f;
                currentLevel--;
                UpdateVisuals();
            }
        }
    }

    // Týklayýnca Doldur (Clicker Mantýðý)
    void OnMouseDown()
    {
        if (currentLevel < MAX_LEVEL)
        {
            currentLevel++; // 1 Kademe artýr
            drainTimer = 0f; // Sayacý sýfýrla ki hemen düþmesin
            UpdateVisuals();

            // Ufak bir efekt veya ses eklenebilir
            Debug.Log($"{name} seviyesi arttý: {currentLevel}/4");
        }
    }

    // --- GÖRSEL ANÝMASYON (SCALE + POZÝSYON) ---
    void UpdateVisuals()
    {
        if (fillMesh == null) return;

        // Yüzde kaç doluyuz? (0.0, 0.25, 0.50, 0.75, 1.0)
        float percent = (float)currentLevel / MAX_LEVEL;

        // 1. SCALE AYARI: 0'dan Orijinal boyutuna
        fillMesh.localScale = Vector3.Lerp(Vector3.zero, initialScale, percent);

        // 2. POZÝSYON AYARI: Aþaðýdan Orijinal yerine
        // Boþ pozisyon = Orijinal yerin biraz altý (loweredYOffset kadar)
        Vector3 emptyPos = initialLocalPos + new Vector3(0, loweredYOffset, 0);

        fillMesh.localPosition = Vector3.Lerp(emptyPos, initialLocalPos, percent);
    }
}