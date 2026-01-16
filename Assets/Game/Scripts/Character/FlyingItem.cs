using UnityEngine;
using System; // Action i�in gerekli

public class FlyingItem : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 targetPos;
    private Action onCompleteCallback;
    private bool isFlying = false;

    private float flightDuration = 0.5f;
    private float flightHeight = 2.0f;
    private float timeElapsed = 0f;

    public void FlyTo(Vector3 destination, Action onComplete = null)
    {
        startPos = transform.position;
        targetPos = destination;
        onCompleteCallback = onComplete;
        isFlying = true;
        timeElapsed = 0f;
    }

    void Update()
    {
        if (!isFlying) return;

        timeElapsed += Time.deltaTime;
        float percent = timeElapsed / flightDuration;

        if (percent < 1.0f)
        {
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, percent);
            float height = Mathf.Sin(percent * Mathf.PI) * flightHeight;
            transform.position = currentPos + Vector3.up * height;
        }
        else
        {
            transform.position = targetPos;
            isFlying = false;
            if (onCompleteCallback != null) onCompleteCallback.Invoke();
        }
    }
}