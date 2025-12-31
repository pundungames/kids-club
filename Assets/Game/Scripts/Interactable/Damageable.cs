using UnityEngine;
using System.Collections;
using Zenject; // Coroutine'ler için gerekli

// Enemy ve ResourceNode için temel sýnýf. Can yönetimi ve hasar almayý saðlar.
public abstract class Damageable : MonoBehaviour
{
    [Inject] internal AudioManager audioManager;

    [Header("Base Health Settings")]
    [Tooltip("Maksimum can deðeri.")]
    [SerializeField] internal float maxHealth = 10f;
    [SerializeField] internal float currentHealth;
    public float CurrentHealth => currentHealth;
    public event System.Action<float, float> OnHealthChanged; // (current, max)
    public float GetMaxHealth() { return maxHealth; }


    [Header("Base Loot Settings")]
    [Tooltip("Yok edildiðinde/kýrýldýðýnda kullanýlacak LootTable Scriptable Object'i.")]
    [SerializeField] internal LootTableSO lootTable; // Alt sýnýflardan eriþilebilir olmalý


    [Header("Resource Specific Feedback")]
    [SerializeField] private float hitScaleFactor = 1.1f;
    [SerializeField] private float hitRotationAngle = 10f;
    [SerializeField] private float hitDuration = 0.4f;

    private Vector3 originalScale;
    private Quaternion originalRotation;
    private bool isHitCoroutineRunning = false;

    // IHealthProvider sadece player için kullanýldýðý için, düþmanlarda null kalacak.
    protected IHealthProvider healthProvider;


    // DOT Yönetimi için Alanlar
    private Coroutine dotCoroutine;
    private bool isBurning = false; // Basit kontrol bayraðý

    [SerializeField] EnemyDamageText enemyDamageText;
    [SerializeField] float damageTextOffset = 2f;
    protected virtual void Awake()
    {
        // 1. Görsel geri bildirim için baþlangýç deðerlerini kaydet (Herkes için lazým)
        originalScale = transform.localScale;
        originalRotation = transform.localRotation;

        // 2. IHealthProvider kontrolü (Oyuncu olup olmadýðýný anlamanýn bir yolu) (Herkes için lazým)
        healthProvider = GetComponent<IHealthProvider>();

        // 3. Can atamasýný sadece oyuncu olmayan objeler yapsýn (Veya ayrý metotta yapýlsýn)
        InitializeHealth();
    }

    // Yeni: Caný ayarlama iþlevini ayýrýyoruz.
    protected virtual void InitializeHealth()
    {
        // Eðer obje oyuncu deðilse (yani IHealthProvider yoksa), kendi canýný ayarla.
        if (healthProvider == null)
        {
            currentHealth = maxHealth;
        }
    }
    public virtual void TakeHealth(float healthAmount = 0)
    {
        currentHealth += healthAmount;
        if (currentHealth > GetMaxHealth())
            currentHealth = GetMaxHealth();
        OnHealthChanged?.Invoke(CurrentHealth, GetMaxHealth());
    }

    /// <summary>
    /// Dýþarýdan hasar almayý saðlayan ana metot.
    /// Oyuncu dýþýndaki tüm objeler için (Düþman, Kaynak, Kale) can azaltma mantýðýný içerir.
    /// </summary>
    /// <param name="damageAmount">Alýnan hasar miktarý.</param>
    public virtual void TakeDamage(float damageAmount, bool loot, Transform hitVfxPos = null) // Ýmza Deðiþti!
    {
        // Eðer bu obje IHealthProvider uyguluyorsa (yani oyuncuysa),
        // hasarý ona yönlendir ve temel mantýðý atla.
        if (healthProvider != null)
        {
            // manager.ProcessIncomingDamage(damageAmount);
            OnHit(); // Oyuncu da vurulma feedback'i almalý
            return;
        }

        // Hasar almayý zaten sýfýra düþürmüþ objeler için kontrol
        if (currentHealth <= 0f) return;
        if (damageAmount <= 0f) return;

        // 1. Caný Düþür (Düþman/Kaynak/Kale)
        currentHealth -= damageAmount;
        OnHealthChanged?.Invoke(CurrentHealth, GetMaxHealth());
        if (enemyDamageText)
        {
            Vector3 pos = transform.position; pos.y += damageTextOffset;
            EnemyDamageText text = Instantiate(enemyDamageText, pos, Quaternion.identity, transform);
            text.SetTextAnimation(Mathf.CeilToInt(damageAmount).ToString("F0"));
        }
        // 2. Vurulma Geri Bildirimi
        OnHit();

        // Debug.Log($"{gameObject.name} Hasar Aldý. Kalan Can: {currentHealth}");

        // 3. Ölüm Kontrolü
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            OnDeathOrBreak(loot);
        }
    }

    /// <summary>
    /// Hasar alýndýðýnda tetiklenecek eylemleri (Geri bildirim, ses) yöneten metot.
    /// </summary>
    protected virtual void OnHit()
    {
        if (!isHitCoroutineRunning)
        {
            StartCoroutine(HitFeedbackCoroutine());
        }
        // Alt sýnýflar (EnemyBase, Castle) ekstra efektler eklemek için bu metodu override edebilir.
    }

    private IEnumerator HitFeedbackCoroutine()
    {
        // ... (Bu metot ayný kalýr, görsel feedback saðlar)
        isHitCoroutineRunning = true;
        float timer = 0f;

        // Gerekirse orijinal Scale ve Rotation deðerlerinin alýndýðýndan emin ol (Awake'te atandý)
        if (originalScale == Vector3.zero) originalScale = transform.localScale;
        if (originalRotation == Quaternion.identity) originalRotation = transform.localRotation;

        while (timer < hitDuration)
        {
            float shakeIntensity = 1f - (timer / hitDuration);
            if (!enemyDamageText)
            {
                Quaternion randomShakeRotation = Quaternion.Euler(Random.insideUnitSphere * hitRotationAngle * shakeIntensity);
                transform.localRotation = originalRotation * randomShakeRotation;
            }

            float scalePop = 1 + Mathf.Sin(Mathf.PI * (timer / hitDuration)) * (hitScaleFactor - 1);
            transform.localScale = originalScale * scalePop;
            timer += Time.deltaTime;
            yield return null;
        }
        transform.localScale = originalScale;
        transform.localRotation = originalRotation;
        isHitCoroutineRunning = false;
    }

    /// <summary>
    /// Caný sýfýra düþtüðünde tetiklenen ortak mantýk.
    /// </summary>
    public virtual void OnDeathOrBreak(bool loot, bool kamikaze = false)
    {
        if (lootTable != null)
        {
            if (loot)
            {
                LootService.DropLoot(lootTable, transform.position);
            }
        }
        else
        {
        }

        // 2. Yok Etme (Alt sýnýflara býrakýlýr)
        // Destory(gameObject) komutunu alt sýnýflara býrakmak daha güvenlidir, 
        // çünkü bazý düþmanlar ölüm animasyonu oynatabilir.
    }


    public void ApplyDamageOverTime(float damagePerTick, float tickInterval, float duration)
    {
        // 1. Yeni DOT hasarýný uygulayacaðýz. Önceki ayný tip DOT varsa durdur.
        if (isBurning && dotCoroutine != null)
        {
            return;
        }


        dotCoroutine = StartCoroutine(DamageOverTimeRoutine(damagePerTick, tickInterval, duration));
    }

    private IEnumerator DamageOverTimeRoutine(float damagePerTick, float tickInterval, float duration)
    {
        isBurning = true;
        float elapsedTime = 0f;

        // Düþman üzerindeki görsel efekti baþlat (Örn: Ateþ VFX)
        // StartDotVFX(damageType); 

        while (elapsedTime < duration && CurrentHealth > 0)
        {
            // Belirtilen hasar tikini uygula
            TakeDamage(damagePerTick, false); // false: DOT hasarý loot düþürmez

            // Zamaný ilerlet
            elapsedTime += tickInterval;

            // Tik aralýðý kadar bekle
            yield return new WaitForSeconds(tickInterval);
        }

        // DOT süresi bitti veya düþman öldü.
        isBurning = false;
        dotCoroutine = null;

        // Düþman üzerindeki görsel efekti durdur
        // StopDotVFX(damageType);
    }
}