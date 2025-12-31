public interface IHealthProvider
{
    /// <summary>
    /// Maksimum can deðerini döndürür.
    /// </summary>
    float MaxHealth { get; }

    /// <summary>
    /// Mevcut can deðerini döndürür.
    /// </summary>
    float CurrentHealth { get; }

    /// <summary>
    /// Can deðerleri deðiþtiðinde (hasar alma, iyileþme) tetiklenir.
    /// </summary>
    // System.Action<MevcutCan, MaksimumCan> imzasýna sahiptir.
    public event System.Action<float, float> OnHealthChanged;

    /// <summary>
    /// Belirtilen miktarda can ekler (iyileþme).
    /// </summary>
    void RestoreHealth(float amount);

    // Not: Hasar alma iþlemi genellikle bu interface'i uygulayan
    // Damageable veya CharacterStatManager'ýn içinden çaðrýldýðý için 
    // buraya "TakeDamage" eklenmesi zorunlu deðildir.
}