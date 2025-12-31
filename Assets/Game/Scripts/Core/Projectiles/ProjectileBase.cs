using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public abstract class ProjectileBase : MonoBehaviour
{
    [Inject] protected PoolingSystem poolingSystem;
    [Inject] protected AudioManager audioManager;

    [SerializeField] ProjectileType projectileType;
    [SerializeField] private AnimationCurve customCurve;
    [SerializeField] private TrailRenderer trail;

    [SerializeField] public float attackDamage;

    [SerializeField] protected string vfxName;
    [SerializeField] protected string hitVfxName;
    [SerializeField] protected string hitSfxName;

    [SerializeField] Transform trailVfx;
    [SerializeField] internal float damageRange = 1.5f;

    protected virtual void Start()
    {

    }
    protected virtual void Update()
    {

    }

    private void OnEnable()
    {
        if (trail)
        {
            trail.enabled = false;
            Invoke("Delay", .07f);
        }

        transform.localScale = Vector3.one;
        if (trailVfx)
        {
            trailVfx.gameObject.SetActive(true);
            if (trailVfx.TryGetComponent<ParticleSystem>(out ParticleSystem particle))
            {
                particle.Stop();
                Invoke("DelayTrailVfx", .2f);
            }
        }
        SceneManager.activeSceneChanged += OnSceneChanged;
    }
    void Delay()
    {
        trail.enabled = true;
    }
    void DelayTrailVfx()
    {
        if (trailVfx.TryGetComponent<ParticleSystem>(out ParticleSystem particle))
            particle.Play();
    }
    protected virtual void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
        if (trailVfx)
            trailVfx.gameObject.SetActive(false);
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        if (gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
        }
    }
    public virtual void SetTarget(Vector3 target, float damage, bool infinityArrow = false, Action action = null)
    {
        attackDamage = damage;

        Vector3 launchTargetPos = target + Vector3.up * .5f; // sabitlenmiþ hedef
        Vector3 direction = (launchTargetPos - transform.position).normalized;
        Quaternion lookRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Lerp(transform.rotation, lookRot, Time.deltaTime * 10f);

        float distance = Vector3.Distance(transform.position, launchTargetPos);
        float speed = 20f;
        float travelTime = distance / speed;
        if (infinityArrow) travelTime *= 2f;
        if (trailVfx)
        {
            trailVfx.transform.position = transform.position;
            trailVfx.transform.rotation = transform.rotation;
            trailVfx.GetComponent<ParticleSystem>().Play();
            trailVfx.transform.parent = null;
        }

        StartCoroutine(ProjectileFlight(launchTargetPos, direction, travelTime, action));
    }
    IEnumerator ProjectileFlight(Vector3 targetPos, Vector3 direction, float travelTime, Action action = null)
    {
        float timer = 0f;

        while (timer < travelTime)
        {
            transform.position += direction * Time.deltaTime * 20f;
            transform.LookAt(transform.position + direction);

            if (trailVfx)
            {
                trailVfx.transform.rotation = transform.rotation;
                trailVfx.transform.position = transform.position;
            }
            timer += Time.deltaTime;
            yield return null;
        }
        if (trailVfx)
        {
            trailVfx.transform.parent = transform;
            trailVfx.GetComponent<ParticleSystem>().Stop();
        }
        if (poolingSystem)
            poolingSystem.DestroyAPS(gameObject);
        else Destroy(gameObject);
    }

    public virtual void ThrowBomb(Transform owner, Vector3 target, float damage, float bombDelay, ParticleSystem extraVfx = null)
    {
        attackDamage = damage;

        target.y += 0.2f;
        float distance = Vector3.Distance(transform.position, target);

        int jumpCount = 1;
        float jumpPower = 3f;
        float duration = distance / 5f;

        Sequence bombSeq = DOTween.Sequence();
        bombSeq.Append(transform.DOJump(target, jumpPower, jumpCount, duration).SetEase(customCurve));
        bombSeq.Join(transform.DORotate(new Vector3(0, 900, 0), duration / 2, RotateMode.FastBeyond360).SetEase(Ease.Linear));

        Vector3 originalScale = transform.localScale;

        bombSeq.OnComplete(() =>
        {
            Debug.Log("Bomb Sekanc complete");
            /*  Sequence impactSeq = DOTween.Sequence();

              impactSeq.Append(transform.DOScale(originalScale * 1.2f, bombDelay).SetEase(Ease.OutBack));
              impactSeq.Join(transform.DOShakePosition(bombDelay, 0.2f, 15, 100, false).SetDelay(bombDelay / 2f)); // Daha manyak titreme

              impactSeq.OnComplete(() =>
              {*/
            Explode(transform.position);
            //EventManager.OnCamShake(5, IsPlayer());
            transform.localScale = originalScale;
            //  audioManager.Play("skeleton-ulti");
            Taptic.Heavy();
            if (extraVfx != null)
            {
                if (hitSfxName != string.Empty)
                    audioManager.Play(hitSfxName);
                Vector3 pos = transform.position; pos.y += .2f;
                extraVfx.transform.position = pos;
                extraVfx.gameObject.SetActive(true);
                extraVfx.Play();
                GetComponent<MeshRenderer>().enabled = false;
                Destroy(gameObject, .3f);
            }
            if (vfxName != string.Empty)
            {
                GameObject vfx = poolingSystem.InstantiateAPS(vfxName, transform.position);
                //vfx.transform.localScale = Vector3.one * skill.effectRange;
                poolingSystem.DestroyAPS(vfx, 2f);
            }

            if (trailVfx)
            {
                trailVfx.transform.parent = transform;
                trailVfx.GetComponent<ParticleSystem>().Stop();
                trailVfx.gameObject.SetActive(false);
            }
            //  Destroy(gameObject);
            // ýnject yapýcaz poolingSystem.DestroyAPS(gameObject);
            // });
        });
    }
    protected virtual void Explode(Vector3 explosionCenter)
    {

    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<Damageable>(out Damageable enemy))
        {
            AttackToEnemy(enemy); // Base mermi her zaman yok olur (override edilene kadar)
        }
    }

    protected virtual void AttackToEnemy(Damageable target)
    {
        if (target == null || poolingSystem == null)
        {
            Destroy(gameObject);
            return;
        }

        target.TakeDamage(attackDamage, true);

        if (projectileType == ProjectileType.standart)
            if (poolingSystem != null)
            {
                // ... vfx kodlarý ...
                poolingSystem.DestroyAPS(gameObject); // <--- BU KOD BASE SINIFTA KALSIN
            }
            else
            {
                Destroy(gameObject);
            }
    }

    private void OnDestroy()
    {
        if (trailVfx) Destroy(trailVfx.gameObject);
    }
}
public enum ProjectileType
{
    standart,
    highRange,
    infinity
}