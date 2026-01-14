using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class CustomerController : MonoBehaviour
{
    [Header("Bileþenler")]
    public ModularCharacterManager appearanceManager;
    public GameObject purchasedIcon;
    private NavMeshAgent agent;
    private Animator animator;

    [Header("Navigasyon Ayarlarý")]
    public float stopThreshold = 0.05f; // Durma mesafesi
    public float queueThreshold = 0.4f; // Sýra bekleme mesafesi

    // --- YENÝ AYAR: TÝTREME ÖNLEYÝCÝ ---
    // Karakter durduktan sonra, hedef en az bu kadar uzaklaþmazsa tekrar yürümeye baþlamaz.
    private float moveBuffer = 0.15f;
    private bool isMoving = true; // Þu an hareket halinde miyiz?

    private Transform exitPoint;
    private CounterManager targetCounter;
    private Vector3 targetPosition;

    private enum State { Spawning, InQueue, Buying, WalkingToExit }
    private State currentState;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        if (appearanceManager == null) appearanceManager = GetComponent<ModularCharacterManager>();

        if (agent != null)
        {
            agent.stoppingDistance = 0f;
            agent.autoBraking = true;
            agent.angularSpeed = 360f;
            agent.acceleration = 12f;
        }
    }

    public void Initialize(Transform counter, Transform exit, Gender genderPref, SkinType skinPref)
    {
        exitPoint = exit;
        appearanceManager.BuildCharacter(genderPref, skinPref);

        if (agent != null) agent.Warp(transform.position);

        targetCounter = counter.GetComponentInParent<CounterManager>();

        if (targetCounter != null)
        {
            targetCounter.JoinQueue(this);
            currentState = State.InQueue;
            UpdateTarget(targetCounter.GetPositionForIndex(targetCounter.GetQueueIndex(this)));
        }
    }

    public void UpdateQueuePosition(Vector3 newPos)
    {
        if (currentState == State.InQueue)
        {
            UpdateTarget(newPos);
        }
    }

    void UpdateTarget(Vector3 pos)
    {
        // Hedef deðiþti mi?
        if (Vector3.Distance(targetPosition, pos) > 0.01f)
        {
            targetPosition = pos;
            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(targetPosition);
                // Yeni hedef geldiði için kesinlikle yürümeye baþla
                isMoving = true;
                if (animator) animator.SetBool("isWalking", true);
                agent.updateRotation = true;
                agent.isStopped = false;
            }
        }
    }

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        if (currentState == State.InQueue)
        {
            float dist = Vector3.Distance(transform.position, targetPosition);
            int myIndex = targetCounter.GetQueueIndex(this);
            float currentThreshold = (myIndex == 0) ? stopThreshold : queueThreshold;

            // --- STABÝLÝZASYON MANTIÐI ---

            if (isMoving)
            {
                // HAREKET EDERKEN: Hedefe tam varana kadar durma (Threshold)
                if (dist <= currentThreshold)
                {
                    // HEDEFE VARDIK -> DUR
                    isMoving = false;
                    StopMovement();

                    if (myIndex == 0) StartCoroutine(BuyProcess());
                }
            }
            else
            {
                // DURURKEN: Ufak kaymalar için tekrar yürüme!
                // Sadece hedef (Threshold + Buffer) kadar uzaklaþýrsa tekrar yürü.
                if (dist > currentThreshold + moveBuffer)
                {
                    // ÇOK UZAKLAÞTIK -> YÜRÜMEYE BAÞLA
                    isMoving = true;
                    StartMovement();
                }
                else
                {
                    // HALA DURUYORUZ -> YÖNÜ DÜZELT
                    RotateToZero();
                }
            }
        }
        else if (currentState == State.WalkingToExit)
        {
            if (agent.remainingDistance < 1.0f) Destroy(gameObject);
        }
    }

    // Kod tekrarýný önlemek için yardýmcý fonksiyonlar
    void StopMovement()
    {
        if (animator) animator.SetBool("isWalking", false);
        agent.isStopped = true;       // Agent'ý kesin durdur
        agent.velocity = Vector3.zero; // Kaymayý engelle
        agent.updateRotation = false; // Dönmeyi durdur (Biz yöneteceðiz)
    }

    void StartMovement()
    {
        if (animator) animator.SetBool("isWalking", true);
        agent.isStopped = false;
        agent.updateRotation = true; // Agent yola baksýn
    }

    void RotateToZero()
    {
        Quaternion targetRotation = Quaternion.Euler(0, 0, 0);
        if (Quaternion.Angle(transform.rotation, targetRotation) < 1f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
    }

    IEnumerator BuyProcess()
    {
        if (currentState == State.Buying) yield break;

        currentState = State.Buying;
        StopMovement(); // Satýn alýrken kesin dur

        bool transactionSuccess = false;
        while (!transactionSuccess)
        {
            RotateToZero();
            transactionSuccess = targetCounter.TryGiveItem();
            if (!transactionSuccess) yield return new WaitForSeconds(1f);
        }

        if (animator) animator.SetTrigger("Buy");
        if (purchasedIcon != null) purchasedIcon.SetActive(true);
        yield return new WaitForSeconds(1.0f);

        targetCounter.LeaveQueue(this);

        currentState = State.WalkingToExit;
        StartMovement(); // Çýkýþa giderken tekrar hareketlen
        agent.SetDestination(exitPoint.position);
    }
}