using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EnemyDamageText : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI lockText;
    [SerializeField] float yValue = 2f;
    private Transform mainCameraTransform;
    private void Start()
    {
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (mainCameraTransform == null) return;

        Vector3 directionToCamera = mainCameraTransform.position - transform.position;

        if (directionToCamera == Vector3.zero) return;
        Quaternion targetRotation = Quaternion.LookRotation(-directionToCamera);
        //  targetRotation.y = 0;
        //  targetRotation.z = 0;
        transform.rotation = targetRotation;
    }

    public void SetTextAnimation(string text)
    {
        lockText.text = text;
        transform.DOMoveY(transform.position.y + yValue, .7f).OnComplete(() => Destroy(gameObject));
        lockText.DOFade(0, .7f);
    }
}
