using UnityEngine;
using System.Collections.Generic;
using Zenject;

// IceBallista'nýn mermisi için özel sýnýf
public class IceBallistaProjectile : ProjectileBase
{
    // Hangi düþmanlara vurduðumuzu takip etmek için bir liste
    private readonly List<Damageable> hitEnemies = new List<Damageable>();

    // Bir düþmana ilk vuruþ yapýlýp yapýlmadýðýný kontrol eden bayrak
    private bool hasHitFirstTarget = false;

    // ProjectileBase'den kalýtým aldýðýmýz için sadece ihtiyacýmýz olan metodu override ediyoruz.

    // OnCollisionEnter metodunu kullanacaðýz çünkü merminin çarptýktan sonra yok olmamasý gerekiyor.
    protected override void OnTriggerEnter(Collider other)
    {
        // Temas edilen nesnenin bir düþman olup olmadýðýný kontrol et.
        if (other.gameObject.TryGetComponent<Damageable>(out Damageable enemy))
        {
            // Bu düþmana daha önce vurulmadýysa VE
            // Henüz ilk hedefe vurulmadýysa (bu merminin vuruþ hakký varsa)
            if (!hitEnemies.Contains(enemy) && !hasHitFirstTarget)
            {
                // HASAR VERME ÝÞLEMÝ
                AttackToEnemy(enemy);

                // Bu düþmaný vuruþ listesine ekle (þimdilik bu projede gerek yok, ama olasý geliþtirmeler için iyi)
                // hitEnemies.Add(enemy); 

                // Merminin ilk ve tek vuruþ hakkýný kullandýðýný iþaretle
                hasHitFirstTarget = true;

                // Ýlk vuruþ yapýldýðý için buradaki iþlem biter, mermi yoluna devam eder.
            }
            // NOT: hasHitFirstTarget true ise, mermi diðer düþmanlarýn içinden hasar vermeden geçmeye devam edecektir.
        }
    }

    // NOT: Mermi yok olma iþlemini OnCollisionEnter yerine,
    // Ekran dýþýna çýktýðýnda (Out of Bounds) veya
    // Bir süre sonra otomatik olarak (örneðin 5-10 saniye sonra) yapmalýsýnýz.
    // Base sýnýftaki OnCollisionEnter metodunu iptal etmek için OnCollisionEnter metodunu kaldýrmalý veya boþ býrakmalýyýz:
    // Merminiz þu anki "ProjectileBase" sýnýfýnýzda "OnCollisionEnter" ile çalýþýyor.
    // Çarpýþmanýn bir kez olmasýný saðlamak için **Base sýnýftaki `OnCollisionEnter` metodunu boþ býrakmalýyýz**
    // ancak `ProjectileBase` bir soyut (abstract) sýnýf olduðundan, buna gerek kalmayabilir.
    // Eðer merminiz bir Collider deðil de bir **Trigger Collider** kullanacaksa,
    // **`OnCollisionEnter`** yerine **`OnTriggerEnter`** kullanmanýz gerekir, böylece içinden geçer.

    // Eðer fiziksel çarpýþma (OnCollisionEnter) kullanmak istiyorsanýz ve içinden geçmesini saðlamak zor ise,
    // en kolay yol budur. Mermi objenizin **Is Trigger** olarak ayarlandýðýndan emin olun.

    // Eðer hala OnCollisionEnter kullanmak istiyorsanýz ve sadece ilk hedefe vurmak istiyorsanýz:
    // ----------------------------------------------------------------------------------------------------------------------
    // public override void AttackToEnemy(EnemyBase target) { ... } metodunuzu override etmeniz GEREKMEZ.
    // Çünkü `AttackToEnemy` hasarý verip mermiyi siliyor.
    // Base sýnýftaki `OnCollisionEnter`'ý iptal edip, kendi `OnCollisionEnter`'ýnýzý yazýn:

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<Damageable>(out Damageable enemy))
        {
            if (!hasHitFirstTarget)
            {
                // HASAR VERME
                AttackToEnemy(enemy); // BU METOT ZATEN SONUNDA MÜHMÝYÝ SÝLÝYOR!

                // Merminin yok olmasýný istemediðimiz için, Base sýnýftaki `AttackToEnemy` metodunu da deðiþtirmemiz gerekiyor!
            }
            // else: Mermi ikinci düþmana çarptý, hasar vermeden içinden geçiyor (Eðer fiziksel çarpýþma (Collision) yerine Trigger kullanýrsanýz)
            // Eðer fiziksel çarpýþma kullanýyorsanýz, mermi burada duracak veya sektirecektir. Bu yüzden **Trigger** kullanmak en doðrusudur.
        }
    }

    // ----------------------------------------------------------------------------------------------------------------------

    // Ýlk hedefe vurduktan sonra yok olmamasý için, base sýnýftaki AttackToEnemy metodunu geçersiz kýlmalýsýnýz.
    protected override void AttackToEnemy(Damageable target)
    {
        if (target == null) return;

        target.TakeDamage(attackDamage, true);

        // Vuruþ VFX'lerini oynat.
        if (poolingSystem != null && hitVfxName != string.Empty)
        {
            Vector3 pos = target.transform.position; pos.y = transform.position.y;
            GameObject vfx = poolingSystem.InstantiateAPS(hitVfxName, pos);
            poolingSystem.DestroyAPS(vfx, 2f);
        }

        // Mermiyi yok etme kodunu BURADAN KALDIRIYORUZ!
        // poolingSystem.DestroyAPS(gameObject);

        // Ýlk hedefe vurduðunu iþaretle.
        hasHitFirstTarget = true;
    }

    // Mermi geri çaðrýldýðýnda (pool'a döndüðünde) veya devre dýþý býrakýldýðýnda vuruþ durumunu sýfýrlayýn.
    protected override void OnDisable()
    {
        base.OnDisable();
        hasHitFirstTarget = false;
        hitEnemies.Clear();
        // Merminin sonsuz gitmesi gerektiði için, yok olma iþlemini burada yapmayýnýz. 
        // Bunu bir zamanlayýcý veya ekran dýþý kontrolü ile yapýn.
        // Þimdilik, sadece merminin Base sýnýftan kalýtým alarak pool'a döndüðünü varsayýyorum.
    }
}

// NOT: Yukarýdaki kodun çalýþmasý için, mermi objenizin **Collider bileþeninde Is Trigger** iþaretli olmalýdýr!
// Ayrýca Base sýnýftaki `OnCollisionEnter` metodunu **mutlaka silmeli veya boþ býrakmalýsýnýz**.