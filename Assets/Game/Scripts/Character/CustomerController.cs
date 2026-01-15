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
    public float stopThreshold = 0.2f; // Hedefe ne kadar yaklaþýnca dursun?

    private Transform exitPoint;
    private CounterManager targetCounter;
    private Vector3 currentTargetPos;

    // Müþteri satýn almaya hazýr mý? (Sýranýn en baþýnda ve durmuþ vaziyette)
    public bool isReadyToBuy = false;

    // Durumlar
    private enum State { Spawning, MovingToQueue, WaitingInQueue, WalkingToExit }
    private State currentState;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        if (appearanceManager == null) appearanceManager = GetComponent<ModularCharacterManager>();

        if (agent != null)
        {
            agent.stoppingDistance = 0f; // Durmayý biz kodla yapacaðýz
            agent.autoBraking = false;
            agent.angularSpeed = 360f;
            agent.acceleration = 20f;
        }
    }

    public void Initialize(Transform counter, Transform exit, Gender genderPref, SkinType skinPref)
    {
        exitPoint = exit;
        if (appearanceManager != null) appearanceManager.BuildCharacter(genderPref, skinPref);

        // NavMesh Fix (Zemine oturt)
        if (agent != null) { agent.Warp(transform.position); agent.enabled = true; }

        // CounterManager'ý bul (Parent/Child aramalý)
        targetCounter = counter.GetComponent<CounterManager>();
        if (targetCounter == null) targetCounter = counter.GetComponentInParent<CounterManager>();
        if (targetCounter == null) targetCounter = counter.GetComponentInChildren<CounterManager>();

        if (targetCounter != null)
        {
            targetCounter.JoinQueue(this);

            // Ýlk hedefi al
            Vector3 queuePos = targetCounter.GetPositionForIndex(targetCounter.GetQueueIndex(this));
            MoveTo(queuePos);
            currentState = State.MovingToQueue;
        }
    }

    // --- SIRA ÝLERLEME SÝSTEMÝ ---
    public void UpdateQueuePosition(Vector3 newPos)
    {
        // Eðer zaten çýkýþa gidiyorsa sýrayý takma
        if (currentState == State.WalkingToExit) return;

        // Yeni pozisyon ile þu anki yerim arasýndaki mesafe kayda deðer mi?
        if (Vector3.Distance(transform.position, newPos) > 0.1f)
        {
            // Evet, ilerlemem lazým -> Yürüme Moduna Geç
            MoveTo(newPos);
            currentState = State.MovingToQueue;
            isReadyToBuy = false;
        }
    }

    // Yürüme Emri
    void MoveTo(Vector3 pos)
    {
        currentTargetPos = pos;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(currentTargetPos);

            // Animasyon: YÜRÜ
            if (animator) animator.SetBool("isWalking", true);
        }
    }

    // Durma Emri
    void StopMovement()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero; // Kaymayý engelle
            agent.ResetPath(); // Hedefi unut
        }

        // Animasyon: DUR (IDLE)
        if (animator) animator.SetBool("isWalking", false);
    }

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        // --- DURUM 1: SIRADAKÝ YERÝNE GÝDÝYOR ---
        if (currentState == State.MovingToQueue)
        {
            float dist = Vector3.Distance(transform.position, currentTargetPos);

            // Hedefe vardýk mý?
            if (dist <= stopThreshold)
            {
                StopMovement(); // Hemen dur ve animasyonu kes
                currentState = State.WaitingInQueue;

                // Eðer sýranýn en baþýndaysak -> Satýn almaya hazýr ol
                if (targetCounter.GetQueueIndex(this) == 0)
                {
                    isReadyToBuy = true;
                }
            }
        }
        // --- DURUM 2: SIRADA BEKLÝYOR (IDLE) ---
        else if (currentState == State.WaitingInQueue)
        {
            // Beklerken yüzümüz masaya dönük olsun
            RotateTowards(targetCounter.transform.position);

            // Bazen fiziksel itilmeler olur, yerinden çok kaydýysa düzelt
            if (Vector3.Distance(transform.position, currentTargetPos) > 0.5f)
            {
                MoveTo(currentTargetPos); // Tekrar yürü
                currentState = State.MovingToQueue;
                isReadyToBuy = false;
            }
        }
        // --- DURUM 3: ÇIKIÞA GÝDÝYOR ---
        else if (currentState == State.WalkingToExit)
        {
            if (Vector3.Distance(transform.position, exitPoint.position) < 1.5f)
            {
                Destroy(gameObject); // Oyundan sil
            }
        }
    }

    // --- BU FONKSÝYONU MASA (CounterManager) ÇAÐIRACAK ---
    public void CompletePurchase()
    {
        StartCoroutine(PurchaseRoutine());
    }

    IEnumerator PurchaseRoutine()
    {
        // Satýn alma animasyonu (Varsa)
        if (animator) animator.SetTrigger("Buy");
        if (purchasedIcon != null) purchasedIcon.SetActive(true);

        yield return new WaitForSeconds(0.5f); // Animasyon süresi

        // Masaya haber ver: "Ben gidiyorum, sýradan düþ"
        targetCounter.LeaveQueue(this);

        // Çýkýþ moduna geç
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