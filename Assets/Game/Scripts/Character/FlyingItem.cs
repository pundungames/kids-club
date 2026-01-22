using UnityEngine;
using System;

/// <summary>
/// Flying animation with speed control
/// </summary>
public class FlyingItem : MonoBehaviour
{
    [Header("Speed Settings")]
    [SerializeField] private float flySpeed = 8f;        // Normal speed
    [SerializeField] private float arcHeight = 1f;       // Arc yüksekliği
    [SerializeField] private float rotationSpeed = 360f; // Dönme hızı

    private bool isFlying = false;
    private Vector3 targetPos;
    private Vector3 startPos;
    private float flightTime;
    private float elapsedTime;
    private Action onComplete;

    public void FlyTo(Vector3 target, Action callback = null)
    {
        if (isFlying)
        {
            Debug.LogWarning("[FlyingItem] Already flying!");
            return;
        }

        targetPos = target;
        startPos = transform.position;
        onComplete = callback;

        // Mesafeye göre süre hesapla
        float distance = Vector3.Distance(startPos, targetPos);
        flightTime = distance / flySpeed; // Speed bazlı

        elapsedTime = 0f;
        isFlying = true;
    }

    void Update()
    {
        if (!isFlying) return;

        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / flightTime);

        // Arc trajectory (parabol)
        Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
        float arc = Mathf.Sin(t * Mathf.PI) * arcHeight;
        currentPos.y += arc;

        transform.position = currentPos;

        // Rotate
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // Tamamlandı
        if (t >= 1f)
        {
            isFlying = false;
            transform.position = targetPos;
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// Hızı değiştir (runtime)
    /// </summary>
    public void SetSpeed(float speed)
    {
        flySpeed = speed;
    }
}