using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Bu bileþen, bir GameObject'e (Kale/Oyuncu) atanabilir
// veya UI prefab'ý içinde bulunup harici bir hedefe baðlanabilir.
public class HealthBarUI : MonoBehaviour
{
    [Header("UI Bileþenleri")]
    [Tooltip("Can deðerini gösterecek Slider.")]
    [SerializeField] private Slider healthSlider;

    [Tooltip("Can deðeri (örneðin: 50/100) gösterecek Text bileþeni.")]
    [SerializeField] private TMP_Text healthText;

    [Tooltip("Can barýnýn takip edeceði hedef (Can kaynaðý).")]
    [SerializeField] private GameObject targetObject;

    private IHealthProvider healthProvider;
   // private Damageable damageableTarget;

    private Transform mainCameraTransform;
    private void Start()
    {
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        // 2. IHealthProvider arayüzünü ara (Oyuncu için)
        healthProvider = targetObject.GetComponent<IHealthProvider>();

        if (healthProvider != null)
        {
            // OYUNCU (IHealthProvider) SÝSTEMÝ:
            healthProvider.OnHealthChanged += UpdateHealthUI;
            UpdateHealthUI(healthProvider.CurrentHealth, healthProvider.MaxHealth); // Property kullandýðýnýzý varsayarak güncellendi
        }
        else
        {
            // 3. DAMAGEABLE (KALE/DÜÞMAN) SÝSTEMÝ:
           /* damageableTarget = targetObject.GetComponent<Damageable>();

            if (damageableTarget != null)
            {
                // YENÝ: Damageable'ýn olayýna abone ol!
                damageableTarget.OnHealthChanged += UpdateHealthUI;

                // Baþlangýç deðerlerini al
                float current = damageableTarget.CurrentHealth;
                float max = damageableTarget.GetMaxHealth(); // Artýk MaxHealth'e doðrudan eriþilebilir
                UpdateHealthUI(current, max);
            }
            else
            {
                Debug.LogError($"HealthBarUI: Hedef obje ({targetObject.name}) üzerinde ne IHealthProvider ne de Damageable bulundu!");
            }*/
        }
    }
    private void LateUpdate()
    {
        if (mainCameraTransform == null) return;

        Vector3 directionToCamera = mainCameraTransform.position - transform.position;

        if (directionToCamera == Vector3.zero) return; 
        Quaternion targetRotation = Quaternion.LookRotation(-directionToCamera);
        targetRotation.y = 0;
        targetRotation.z = 0;
        transform.rotation = targetRotation;
    }
    private void OnDestroy()
    {
        // Abonelikten çýkmayý unutma!
        if (healthProvider != null)
        {
            healthProvider.OnHealthChanged -= UpdateHealthUI;
        }
        /*if (damageableTarget != null)
        {
           damageableTarget.OnHealthChanged -= UpdateHealthUI;
        }*/
    }
   
    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
        }
    }
}