// ============================================================================
// POOLING SYSTEM - WITH CATEGORY SYSTEM (Like AudioManager)
// ✅ Category-based organization
// ✅ Easy to manage VFX, Projectiles, etc.
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class PoolingSystem : Singleton<PoolingSystem>
{
    [Header("Pool Categories")]
    [SerializeField] List<PoolCategory> categories = new List<PoolCategory>();

    public int DefaultCount = 10;

    [HideInInspector] public Vector3 initScale;

    // ===== CATEGORY SYSTEM =====

    [Serializable]
    public class PoolCategory
    {
        public string categoryName;
        public List<SourceObjects> sourceObjects = new List<SourceObjects>();
    }

    private void Awake()
    {
        // DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitilizePool();
    }

    // ===== INITIALIZE POOL =====

    public void InitilizePool()
    {
        InitilizeGameObjects();
    }

    private void InitilizeGameObjects()
    {
        foreach (var category in categories)
        {
            foreach (var sourceObj in category.sourceObjects)
            {
                if (sourceObj.ID == "BossArrow" && PlayerPrefs.GetInt("_level") != 16) continue;

                int copyNumber = DefaultCount;
                if (sourceObj.MinNumberOfObject != 0)
                    copyNumber = sourceObj.MinNumberOfObject;

                for (int j = 0; j < copyNumber; j++)
                {
                    GameObject go = Instantiate(sourceObj.SourcePrefab, transform);
                    go.SetActive(false);
                    if (sourceObj.AutoDestroy)
                        go.AddComponent<PoolObject>();

                    sourceObj.clones.Add(go);
                }

                Debug.Log($"✅ Pool initialized: {category.categoryName}/{sourceObj.ID} ({copyNumber} objects)");
            }
        }
    }

    // ===== INSTANTIATE =====

    public GameObject InstantiateAPS(string Id)
    {
        foreach (var category in categories)
        {
            foreach (var sourceObj in category.sourceObjects)
            {
                if (string.Equals(sourceObj.ID, Id))
                {
                    for (int a = sourceObj.clones.Count - 1; a >= 0; a--)
                    {
                        if (!sourceObj.clones[a])
                        {
                            sourceObj.clones.RemoveAt(a);
                            continue;
                        }
                        if (!sourceObj.clones[a].activeInHierarchy)
                        {
                            if (sourceObj.clones[a].TryGetComponent<NavMeshAgent>(out NavMeshAgent agent))
                                agent.enabled = false;
                            sourceObj.clones[a].SetActive(true);

                            IPoolable poolable = sourceObj.clones[a].GetComponent<IPoolable>();
                            if (poolable != null)
                                poolable.Initilize();

                            return sourceObj.clones[a];
                        }
                    }

                    if (sourceObj.AllowGrow)
                    {
                        GameObject go = Instantiate(sourceObj.SourcePrefab, transform);
                        sourceObj.clones.Add(go);
                        IPoolable poolable = go.GetComponent<IPoolable>();
                        if (poolable != null)
                            poolable.Initilize();

                        if (sourceObj.AutoDestroy)
                            go.AddComponent<PoolObject>();
                        return go;
                    }
                }
            }
        }

        Debug.LogWarning($"⚠️ Pool object not found: {Id}");
        return null;
    }

    public GameObject InstantiateAPS(string iD, Vector3 position)
    {
        GameObject go = InstantiateAPS(iD);
        if (go)
        {
            go.transform.position = position;
            return go;
        }
        else
            return null;
    }

    public GameObject InstantiateAPS(string iD, Vector3 position, Quaternion rotation)
    {
        GameObject go = InstantiateAPS(iD);
        if (go)
        {
            go.transform.position = position;
            go.transform.rotation = rotation;
            return go;
        }
        else
            return null;
    }

    public GameObject InstantiateAPS(GameObject sourcePrefab)
    {
        foreach (var category in categories)
        {
            foreach (var sourceObj in category.sourceObjects)
            {
                if (ReferenceEquals(sourceObj.SourcePrefab, sourcePrefab))
                {
                    for (int j = 0; j < sourceObj.clones.Count; j++)
                    {
                        if (!sourceObj.clones[j].activeInHierarchy)
                        {
                            sourceObj.clones[j].SetActive(true);
                            return sourceObj.clones[j];
                        }
                    }
                    if (sourceObj.AllowGrow)
                    {
                        GameObject go = Instantiate(sourceObj.SourcePrefab, transform);
                        sourceObj.clones.Add(go);
                        return go;
                    }
                }
            }
        }
        return null;
    }

    public GameObject InstantiateAPS(GameObject sourcePrefab, Vector3 position)
    {
        GameObject go = InstantiateAPS(sourcePrefab);
        if (go)
        {
            go.transform.position = position;
            return go;
        }
        else
            return null;
    }

    // ===== DESTROY =====

    public void DestroyAPS(GameObject clone)
    {
        clone.transform.position = transform.position;
        clone.transform.rotation = transform.rotation;
        if (clone.TryGetComponent<PoolObject>(out var poolObject))
        {
            clone.transform.localScale = poolObject.initScale;
        }
        clone.transform.SetParent(transform);

        IPoolable poolable = clone.GetComponent<IPoolable>();
        if (poolable != null)
            poolable.Dispose();
        clone.SetActive(false);
    }

    public void DestroyAPS(GameObject clone, float waitTime)
    {
        StartCoroutine(DestroyAPSCo(clone, waitTime));
    }

    IEnumerator DestroyAPSCo(GameObject clone, float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        DestroyAPS(clone);
    }

    // ===== HELPER: GET CATEGORY =====

    public PoolCategory GetCategory(string categoryName)
    {
        return categories.Find(c => c.categoryName == categoryName);
    }

    public List<SourceObjects> GetCategoryObjects(string categoryName)
    {
        var category = GetCategory(categoryName);
        return category?.sourceObjects ?? new List<SourceObjects>();
    }
}