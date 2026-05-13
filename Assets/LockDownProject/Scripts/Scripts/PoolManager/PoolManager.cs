using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [Header("Refs")]
    private bool isExpandable = true;
   
    // �����պ� Ǯ ť
    private readonly Dictionary<GameObject, Queue<GameObject>> prefabToPool
        = new Dictionary<GameObject, Queue<GameObject>>();

    // �ν��Ͻ� �� � ������ Ǯ����
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

    /// <summary>
    /// 프리팹에 대해 count개의 인스턴스를 미리 만들어 풀에 적재한다.
    /// 게임 시작 직후 발사 폭주 시 런타임 Instantiate 비용을 제거하는 워밍업 용도.
    /// 같은 prefab으로 여러 번 호출하면 누적된다.
    /// </summary>
    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;

        if (!prefabToPool.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            prefabToPool.Add(prefab, queue);
        }

        for (int i = 0; i < count; i++)
        {
            GameObject obj = CreateInstance(prefab);
            obj.SetActive(false);
            queue.Enqueue(obj);
        }
    }

    public void Return(GameObject instance)
    {
        if (instance == null) return;

        if (!instanceToPrefab.TryGetValue(instance, out var prefab))
        {
            // Ǯ���� ������ �� �ƴϸ� �׳� Destroy
            Debug.LogWarning("[PoolManager.Return]: unknown instance, Destroy ó��");
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
