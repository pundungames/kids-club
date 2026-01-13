using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class CustomerController : MonoBehaviour
{
    [Header("Ayarlar")]
    public ModularCharacterManager appearanceManager;
    public Transform iconPivot;
    public GameObject purchasedIcon;

    [Header("Navigasyon")]
    public float interactionDistance = 0.5f; // Yakýn mesafe
    private NavMeshAgent agent;
    private Animator animator;
    private Transform exitPoint;

    // Gideceðimiz masanýn scripti
    private CounterManager targetCounter;

    private enum State { Spawning, InQueue, Buying, WalkingToExit }
    private State currentState;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        if (appearanceManager == null) appearanceManager = GetComponent<ModularCharacterManager>();
        if (purchasedIcon) purchasedIcon.SetActive(false);
    }

    public void Initialize(Transform counter, Transform exit, Gender genderPref, SkinType skinPref)
    {
        exitPoint = exit;
        appearanceManager.BuildCharacter(genderPref, skinPref);

        if (agent != null) agent.Warp(transform.position);

        // Masadaki yöneticiyi bul ve sýraya gir
        targetCounter = counter.GetComponentInParent<CounterManager>();

        if (targetCounter != null)
        {
            targetCounter.JoinQueue(this); // Sýraya ismini yazdýr
            currentState = State.InQueue;

            // Sýradaki yerine doðru yürü
            GoToQueuePosition();
        }
        else
        {
            // Masa yoksa direkt çýk (Hata önlemi)
            currentState = State.WalkingToExit;
            agent.SetDestination(exitPoint.position);
        }
    }

    // Masadan "Ýlerle" emri gelince bu çalýþýr
    public void UpdateQueuePosition(Vector3 newPos)
    {
        if (currentState == State.InQueue)
        {
            agent.SetDestination(newPos);
            if (animator) animator.SetBool("isWalking", true);
        }
    }

    void GoToQueuePosition()
    {
        Vector3 myPos = targetCounter.GetMyPosition(this);
        agent.SetDestination(myPos);
        if (animator) animator.SetBool("isWalking", true);
    }

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh || !agent.isActiveAndEnabled) return;

        if (currentState == State.InQueue)
        {
            // Hedefe vardým mý?
            if (!agent.pathPending && agent.remainingDistance < interactionDistance)
            {
                // Vardým ama sýra bende mi?
                int myIndex = targetCounter.GetQueueIndex(this);

                if (myIndex == 0) // EVET! En öndeyim.
                {
                    StartCoroutine(BuyProcess());
                }
                else
                {
                    // HAYIR! Önümde adam var, bekliyorum.
                    if (animator) animator.SetBool("isWalking", false);
                    // (Burada istersen bekleme animasyonu oynatabilirsin)
                }
            }
        }
        else if (currentState == State.WalkingToExit)
        {
            if (!agent.pathPending && agent.remainingDistance < 1.0f)
            {
                Destroy(gameObject);
            }
        }
    }

    IEnumerator BuyProcess()
    {
        currentState = State.Buying; // Artýk sýrada deðil iþlemdeyim
        agent.isStopped = true;
        if (animator) animator.SetBool("isWalking", false);

        bool transactionSuccess = false;

        // Ürün gelene kadar bekle (Sonsuz döngü)
        while (!transactionSuccess)
        {
            transactionSuccess = targetCounter.TryGiveItem();

            if (!transactionSuccess)
            {
                // Ürün yok, bekliyorum...
                // (Ýleride buraya 'Sinirlenme' animasyonu koyabilirsin)
                yield return new WaitForSeconds(1f);
            }
        }

        // Ürünü aldým!
        if (animator) animator.SetTrigger("Buy");
        if (purchasedIcon != null) purchasedIcon.SetActive(true);
        yield return new WaitForSeconds(1.0f);

        // Ýþim bitti, sýradan kaydýmý sil (Arkadakiler ilerlesin)
        targetCounter.LeaveQueue(this);

        // Çýkýþa git
        currentState = State.WalkingToExit;
        agent.isStopped = false;
        agent.SetDestination(exitPoint.position);
        if (animator) animator.SetBool("isWalking", true);
    }
}