// FragmentationMortarProjectile.cs
using UnityEngine;
using Zenject;
using System.Collections.Generic;

public class FragmentationMortarProjectile : ProjectileBase
{
    // [Inject] public LayerMask enemyLayerMask; // Eðer ProjectileBase'de yoksa, DI ile enjekte edin

    // DamageTower'dan gelmesi gereken enemyLayerMask'i alabilmek için geçici bir deðiþken.
    // Normalde bu bilgi kule tarafýndan SetTarget'a parametre olarak gönderilmelidir.
    private LayerMask enemyLayerMask;
   // private DamageTower tower;
    [SerializeField] ParticleSystem extraVfx;
    // Tower'dan gelen verileri tutmak için yeni bir SetParams metodu ekleyebiliriz.
    public void SetParams(LayerMask enemyLayer, float radius/*, DamageTower tower*/)
    {
        this.enemyLayerMask = enemyLayer;
        this.damageRange = radius; // Patlama yarýçapý: 2 blok
     //   this.tower = tower;
    }

    // BASE SINIFTAKÝ ThrowBomb METODU ÇOK UZUN VE GENEL DEÐÝL. 
    // Patlama mantýðýný uygulayacak bir metodu buradan çaðýracaðýz.
    public override void ThrowBomb(Transform owner, Vector3 target, float damage, float bombDelay, ParticleSystem extraVfx = null)
    {
        // Temel fýrlatma animasyonu (ThrowBomb'dan kopyalanan kod)
        // ... (DOTween Sequence ve DOJump mantýðý) ...

        // Burayý temiz tutmak için base sýnýfýn ThrowBomb metodunu override etmeyeceðiz. 
        // Ancak base sýnýfýn ThrowBomb'ý final hasar verme kýsmýný kapsadýðý için, 
        // ya onu deðiþtirmeli ya da burada kendi patlama mantýðýmýzý eklemeliyiz.

        base.ThrowBomb(owner, target, damage, bombDelay, this.extraVfx);

        // Eðer base.ThrowBomb'un sonunda hasar verme kýsmý boþsa (þu an yorum satýrýnda), 
        // patlama anýnda çalýþacak yeni bir callback yazmalýyýz.
        // Base sýnýftaki ThrowBomb metodu incelendiðinde, hasar verme kýsmý yorum satýrýnda:
        // //   List<Transform> targets = SkillUtility.GetUnitsInRange(...);
        // Bu yüzden, patlama mantýðýmýzý patlamanýn olduðu yere eklemeliyiz.
    }

    // Base sýnýftaki ThrowBomb'u patlama anýnda bu metodu çaðýracak þekilde düzenleyelim.
    // Varsayalým ki, bu mermi objesi, DOTween animasyonu bittiðinde çaðrýlacak bir metot içermeli.

    // Patlama anýnda çaðrýlacak yeni bir metot tanýmlýyoruz.
    protected override void Explode(Vector3 explosionCenter)
    {
        // Range: 2 blok çapýndaki radius
        Collider[] hitColliders = Physics.OverlapSphere(explosionCenter, damageRange, enemyLayerMask);

        // AoE alan hasarý: her düþmana 30 damage
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent(out Damageable target))
            {
                // Hasar uygulama (30 damage)
                /*if (target is EnemyBase)
                    target.TakeDamage(tower.CallculateEnemyDamage(target as EnemyBase, attackDamage), damageType, true); // True: Loot düþürsün*/
            }
        }
        Debug.Log("Explode Mortar");
        extraVfx.Play();
        Destroy(gameObject, .5f);
        // NOT: Patlama VFX'i ve merminin yok edilmesi, base sýnýfýn ThrowBomb metodu içinde hallediliyor olmalý.
        // Eðer edilmiyorsa, buraya eklenmeli.
        // Base sýnýfýn ThrowBomb'ý zaten sona erdiðinde poolingSystem.DestroyAPS(gameObject) çaðýrýyor.
    }

    // Bu mermi türü isabetle tetiklenmediði için OnTriggerEnter'a ihtiyacýmýz yok.
    protected override void OnTriggerEnter(Collider other)
    {
        // Boþ býrak
    }
}