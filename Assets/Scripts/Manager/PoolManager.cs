using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    // 字典：Key是预制体(Prefab)，Value是该预制体的对象池队列
    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();
    
    // 字典：Key是实例化的物体(Instance)，Value是它原本所属的预制体(Prefab)
    // 用于在回收时知道这个物体该回到哪个队列里
    private Dictionary<GameObject, GameObject> objectLookup = new Dictionary<GameObject, GameObject>();

    private Transform poolRoot;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            poolRoot = new GameObject("--- Pool Root ---").transform;
            poolRoot.SetParent(transform);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 预热对象池
    /// </summary>
    public void PreparePool(GameObject prefab, int size)
    {
        if (!poolDictionary.ContainsKey(prefab))
        {
            CreatePool(prefab);
        }

        for (int i = 0; i < size; i++)
        {
            GameObject obj = CreateNewObject(prefab);
            obj.SetActive(false);
            poolDictionary[prefab].Enqueue(obj);
        }
    }

    /// <summary>
    /// 从池中获取对象
    /// </summary>
    public GameObject GetObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(prefab))
        {
            CreatePool(prefab);
        }

        GameObject obj;

        if (poolDictionary[prefab].Count > 0)
        {
            obj = poolDictionary[prefab].Dequeue();
        }
        else
        {
            obj = CreateNewObject(prefab);
        }

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        return obj;
    }

    /// <summary>
    /// 回收对象
    /// </summary>
    public void ReturnObject(GameObject obj)
    {
        if (objectLookup.TryGetValue(obj, out GameObject prefab))
        {
            obj.SetActive(false);
            poolDictionary[prefab].Enqueue(obj);
        }
        else
        {
            Debug.LogWarning($"尝试回收一个非对象池创建的物体: {obj.name}，已直接销毁。");
            Destroy(obj);
        }
    }

    // --- 内部辅助方法 ---

    private void CreatePool(GameObject prefab)
    {
        poolDictionary.Add(prefab, new Queue<GameObject>());
    }

    private GameObject CreateNewObject(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, poolRoot);
        obj.name = prefab.name; // 去掉 (Clone) 后缀
        objectLookup.Add(obj, prefab); // 记录
        return obj;
    }
}
