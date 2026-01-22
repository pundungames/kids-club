using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using MilkFarm;
using Zenject;
using TMPro;

[RequireComponent(typeof(NavMeshAgent))]
public class CustomerController : MonoBehaviour
{
    [Inject] private CustomerManager customerManager;

    [Header("Bileşenler")]
    public ModularCharacterManager appearanceManager;
    public GameObject purchasedIcon;
    private NavMeshAgent agent;
    private Animator animator;

    [Header("Navigasyon")]
    public float stopThreshold = 0.2f;

    [Header("UI")]
    [SerializeField] private GameObject packageTextParent;
    [SerializeField] private TextMeshProUGUI packageCountText;

    private Transform exitPoint;
    private Vector3 currentTargetPos;
    public bool isReadyToBuy = false;

    private enum State { Spawning, MovingToQueue, WaitingInQueue, WalkingToExit }
    private State currentState;

    private int requestedBottles;
    private int givenBottles; // Kaç şişe verildi

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
            agent.speed = 3.5f;
        }
    }

    public void Initialize(Transform exit, Gender genderPref, SkinType skinPref)
    {
        exitPoint = exit;
        if (appearanceManager != null) appearanceManager.BuildCharacter(genderPref, skinPref);

        if (agent != null)
        {
            agent.enabled = false;
            agent.Warp(transform.position);
            agent.enabled = true;
        }

        if (customerManager != null)
        {
            customerManager.JoinQueue(this);
            Vector3 queuePos = customerManager.GetPositionForIndex(customerManager.GetQueueIndex(this));
            MoveTo(queuePos);
            currentState = State.MovingToQueue;
        }
        else
        {
            Debug.LogError("[CustomerController] CustomerManager inject edilmedi!");
        }
    }

    /// <summary>
    /// Talep ayarla (başlangıç)
    /// </summary>
    public void SetRequestedBottles(int bottles)
    {
        requestedBottles = bottles;
        givenBottles = 0; // Başlangıçta 0
        UpdateUI();
    }

    /// <summary>
    /// Bir şişe verildi
    /// </summary>
    public void GiveBottle()
    {
        givenBottles++;
        UpdateUI();
    }

    /// <summary>
    /// Tamamlandı mı?
    /// </summary>
    public bool IsOrderComplete()
    {
        return givenBottles >= requestedBottles;
    }

    /// <summary>
    /// Kaç şişe daha gerekiyor?
    /// </summary>
    public int GetRemainingBottles()
    {
        return requestedBottles - givenBottles;
    }

    private void UpdateUI()
    {
        if (packageCountText != null)
        {
            // 4/0 → 4/1 → 4/2 → 4/3 → 4/4
            packageCountText.text = $"{requestedBottles}/{givenBottles}";
        }

        if (IsOrderComplete())
        {
            if (packageTextParent != null)
            {
                packageTextParent.SetActive(false);
            }
        }
        else
        {
            if (packageTextParent != null)
            {
                packageTextParent.SetActive(true);
            }
        }
    }

    public void UpdateQueuePosition(Vector3 newPos)
    {
        if (currentState == State.WalkingToExit) return;
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
            agent.SetDestination(currentTargetPos);
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
            float dist = GetFlatDistance(transform.position, currentTargetPos);
            if (dist <= stopThreshold)
            {
                StopMovement();
                currentState = State.WaitingInQueue;
                if (customerManager != null && customerManager.GetQueueIndex(this) == 0)
                {
                    isReadyToBuy = true;
                    Debug.Log("[CustomerController] İlk sıradayım, hazırım!");
                }
            }
            else if (agent.velocity.sqrMagnitude < 0.1f && dist > 1.0f)
            {
                agent.SetDestination(currentTargetPos);
            }
        }
        else if (currentState == State.WaitingInQueue)
        {
            if (customerManager != null && customerManager.queueStartPoint != null)
            {
                RotateTowards(customerManager.queueStartPoint.position);
            }

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

        if (packageTextParent != null)
        {
            packageTextParent.SetActive(false);
        }

        yield return new WaitForSeconds(0.8f);

        if (customerManager != null) customerManager.LeaveQueue(this);
        currentState = State.WalkingToExit;
        if (exitPoint != null) MoveTo(exitPoint.position);
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