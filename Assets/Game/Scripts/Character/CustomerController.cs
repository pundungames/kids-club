using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class CustomerController : MonoBehaviour
{
    [Header("Bileþenler")]
    public ModularCharacterManager appearanceManager;
    public GameObject purchasedIcon;
    private NavMeshAgent agent;
    private Animator animator;

    [Header("Navigasyon Ayarlarý")]
    public float stopThreshold = 0.2f;

    private Transform exitPoint;
    private CounterManager targetCounter;
    private Vector3 currentTargetPos;

    public bool isReadyToBuy = false;

    private enum State { Spawning, MovingToQueue, WaitingInQueue, WalkingToExit }
    private State currentState;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        if (appearanceManager == null) appearanceManager = GetComponent<ModularCharacterManager>();

        if (agent != null)
        {
            agent.stoppingDistance = 0f;
            agent.autoBraking = false;
            agent.angularSpeed = 360f;
            agent.acceleration = 20f;
            agent.speed = 3.5f; // Hýzýn 0 olmadýðýndan emin olalým
        }
    }

    public void Initialize(Transform counter, Transform exit, Gender genderPref, SkinType skinPref)
    {
        exitPoint = exit;
        if (appearanceManager != null) appearanceManager.BuildCharacter(genderPref, skinPref);

        // --- 1. NAVMESH FIX ---
        // Agent'ý kapatýp açmak bazen NavMesh'e oturmasýný garanti eder
        if (agent != null)
        {
            agent.enabled = false;
            agent.Warp(transform.position);
            agent.enabled = true;
        }

        // --- 2. MASAYI BULMA (Senin þüphelendiðin kýsým) ---
        targetCounter = counter.GetComponent<CounterManager>();
        if (targetCounter == null) targetCounter = counter.GetComponentInParent<CounterManager>();
        if (targetCounter == null) targetCounter = counter.GetComponentInChildren<CounterManager>();

        if (targetCounter != null)
        {
            // Debug: Masayý bulduðunu konsola yazsýn
            Debug.Log($"Masa Bulundu: {targetCounter.name}. Sýraya giriliyor...");

            targetCounter.JoinQueue(this);

            Vector3 queuePos = targetCounter.GetPositionForIndex(targetCounter.GetQueueIndex(this));
            MoveTo(queuePos);
            currentState = State.MovingToQueue;
        }
        else
        {
            Debug.LogError("KRÝTÝK HATA: Müþteri CounterManager scriptini bulamadý! Spawner'a sürüklediðin objeyi kontrol et.");
        }
    }

    public void UpdateQueuePosition(Vector3 newPos)
    {
        if (currentState == State.WalkingToExit) return;

        // Yükseklik farkýný yok sayarak mesafe ölç (Flat Distance)
        if (GetFlatDistance(transform.position, newPos) > 0.1f)
        {
            MoveTo(newPos);
            currentState = State.MovingToQueue;
            isReadyToBuy = false;
        }
    }

    void MoveTo(Vector3 pos)
    {
        currentTargetPos = pos;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;

            // Hedefe gidilebilir mi kontrol et?
            bool pathFound = agent.SetDestination(currentTargetPos);

            if (!pathFound)
            {
                Debug.LogWarning("NavMesh bu hedefe yol bulamadý! Hedef NavMesh dýþýnda olabilir.");
            }

            if (animator) animator.SetBool("isWalking", true);
        }
    }

    void StopMovement()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }
        if (animator) animator.SetBool("isWalking", false);
    }

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        if (currentState == State.MovingToQueue)
        {
            // --- 3. DÜZELTME: YÜKSEKLÝK FARKINI YOK SAY ---
            // Karakterin kafasý ile yerdeki nokta arasýndaki mesafeyi ölçmemesi için Y'leri sýfýrlýyoruz.
            float dist = GetFlatDistance(transform.position, currentTargetPos);

            if (dist <= stopThreshold)
            {
                StopMovement();
                currentState = State.WaitingInQueue;

                if (targetCounter.GetQueueIndex(this) == 0)
                {
                    isReadyToBuy = true;
                }
            }
            else
            {
                // Ekstra Güvenlik: Hedefte deðiliz ama durmuþuz? (Takýlma Önleyici)
                if (agent.velocity.sqrMagnitude < 0.1f && dist > 1.0f)
                {
                    agent.SetDestination(currentTargetPos);
                }
            }
        }
        else if (currentState == State.WaitingInQueue)
        {
            RotateTowards(targetCounter.transform.position);

            if (GetFlatDistance(transform.position, currentTargetPos) > 0.5f)
            {
                MoveTo(currentTargetPos);
                currentState = State.MovingToQueue;
                isReadyToBuy = false;
            }
        }
        else if (currentState == State.WalkingToExit)
        {
            if (GetFlatDistance(transform.position, exitPoint.position) < 1.5f)
            {
                Destroy(gameObject);
            }
        }
    }

    // Yükseklik farkýný önemsemeyen mesafe ölçer
    float GetFlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0;
        b.y = 0;
        return Vector3.Distance(a, b);
    }

    public void CompletePurchase()
    {
        StartCoroutine(PurchaseRoutine());
    }

    IEnumerator PurchaseRoutine()
    {
        if (animator) animator.SetTrigger("Buy");
        if (purchasedIcon != null) purchasedIcon.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        targetCounter.LeaveQueue(this);

        currentState = State.WalkingToExit;
        MoveTo(exitPoint.position);
    }

    void RotateTowards(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        if (dir == Vector3.zero) return;
        dir.y = 0;
        Quaternion lookRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 5f);
    }
}