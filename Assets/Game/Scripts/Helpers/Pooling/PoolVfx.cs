using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolVfx : MonoBehaviour
{
    PoolingSystem poolingSystem;
    [SerializeField] float time = 2f;
    private void Awake()
    {
        poolingSystem = transform.parent.GetComponent<PoolingSystem>();
    }
    private void OnEnable()
    {
        StartCoroutine(Delay());
    }
    IEnumerator Delay()
    {
        yield return new WaitForSeconds(time);
        if (poolingSystem)
            poolingSystem.DestroyAPS(gameObject);
    }
}
