using DG.Tweening;
using System.Collections;
using UnityEngine;
using Zenject;

public class InteractableResource : Damageable
{
    [Inject] PoolingSystem poolingSystem;
    [Inject] DiContainer container;
    internal bool breaked;
    protected override void Awake()
    {
        base.Awake();
    }
    public LootTableSO GetLoot() { return lootTable; }
    protected override void OnHit()
    {
        base.OnHit();

        Vector3 pos = transform.position;
        pos.y += .8f;
        GameObject vfx = poolingSystem.InstantiateAPS("hitVfx", pos);
        poolingSystem.DestroyAPS(vfx, 1f);
    }
    public override void TakeDamage(float damageAmount, bool loot, Transform hitVfxPos = null)
    {
        if (lootTable && lootTable.hitSfx != string.Empty && currentHealth - damageAmount > 0)
            audioManager.Play(lootTable.hitSfx);
        base.TakeDamage(damageAmount, loot, hitVfxPos);
    }
    public override void OnDeathOrBreak(bool loot, bool kamikaze = false)
    {
        breaked = true;
        if (lootTable && lootTable.hitVfx != string.Empty)
        {
            Vector3 pos = transform.position;
            pos.y += .8f;
            GameObject vfx = poolingSystem.InstantiateAPS(lootTable.hitVfx, pos);
            poolingSystem.DestroyAPS(vfx, 1f);
        }
        if (lootTable && lootTable.cutSfx != string.Empty)
            audioManager.Play(lootTable.cutSfx);

        base.OnDeathOrBreak(loot);


        gameObject.SetActive(false);
        Destroy(gameObject);
    }
    /*  IEnumerator SkinAnimation()
      {
          for (int i = 0; i < 3; i++)
          {
              Tween fireTween1 = DOTween.To(
            () => skin.GetBlendShapeWeight(i),
            x => skin.SetBlendShapeWeight(i, x),
            100f, // Hedef: 0 (Dýþarýda/Hazýr pozisyon)
            .2f
        ).SetEase(Ease.OutQuad); // Hýzlý hareket için OutQuad daha uygun olabilir
              yield return new WaitForSeconds(.2f);
          }
          gameObject.SetActive(false);
          Destroy(gameObject);
      }*/
}