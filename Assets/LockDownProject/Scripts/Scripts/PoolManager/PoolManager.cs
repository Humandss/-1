using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [Header("Refs")]
    private bool isExpandable = true;
   
    // 프리팹별 풀 큐
    private readonly Dictionary<GameObject, Queue<GameObject>> prefabToPool
        = new Dictionary<GameObject, Queue<GameObject>>();

    // 인스턴스 → 어떤 프리팹 풀인지
    private readonly Dictionary<GameObject, GameObject> instanceToPrefab
        = new Dictionary<GameObject, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    private GameObject CreateInstance(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab);
        instanceToPrefab[obj] = prefab;
        /*
 
        var pooled = obj.GetComponent<PooledObject>();
        if (pooled == null)
            pooled = obj.AddComponent<PooledObject>();
        */
        return obj;
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("[PoolManager.Spawn]: prefab is null");
            return null;
        }

        if (!prefabToPool.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            prefabToPool.Add(prefab, queue);
        }

        GameObject obj;
        if (queue.Count > 0)
        {
            obj = queue.Dequeue();
        }
        else
        {
            if (!isExpandable)
            {
                Debug.LogWarning($"Pool for {prefab.name} is empty and not expandable");
                return null;
            }

            obj = CreateInstance(prefab);
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);

        return obj;
    }

    public void Return(GameObject instance)
    {
        if (instance == null) return;

        if (!instanceToPrefab.TryGetValue(instance, out var prefab))
        {
            // 풀에서 생성된 게 아니면 그냥 Destroy
            Debug.LogWarning("[PoolManager.Return]: unknown instance, Destroy 처리");
            Destroy(instance);
            return;
        }

        if (!prefabToPool.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            prefabToPool.Add(prefab, queue);
        }

        instance.SetActive(false);
        queue.Enqueue(instance);
    }
}
