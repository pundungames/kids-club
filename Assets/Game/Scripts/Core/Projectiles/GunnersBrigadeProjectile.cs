// GunnersBrigadeProjectile.cs
using UnityEngine;

public class GunnersBrigadeProjectile : ProjectileBase
{
    // Bu mermi tipi, her düþmana isabet ettiðinde hasar verebilmelidir.

    protected override void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<Damageable>(out Damageable enemy))
        {
            // Base sýnýfýn AttackToEnemy metodunu çaðýr
            AttackToEnemy(enemy);

            // Merminin yok edilmesi Base sýnýfýn AttackToEnemy'de yapýlýyordu.
            // Eðer merminin *birden fazla* düþmana vurmasýný istiyorsak, 
            // AttackToEnemy metodunda mermiyi yok eden satýrý KÖKTEN KALDIRMALIYIZ (ProjectileBase'i deðiþtirerek)
            // Ya da sadece burada override ederek merminin yok edilmesini engelleriz:

            // NOT: Base sýnýfýn AttackToEnemy metodu mermiyi yok ediyor. 
            // Bu nedenle, çoklu vuruþ istiyorsak bu metodu override etmeliyiz.
        }
    }

    protected override void AttackToEnemy(Damageable target)
    {
        if (target == null) return;

        // Hasar verme
        target.TakeDamage(attackDamage, true);

        // Merminin yok edilmesi, menzili tamamlayana kadar ERTELENÝR.
        // Base sýnýfýn yok etme mantýðý, ProjectileFlight'ýn sonunda çalýþacaktýr.
    }
}