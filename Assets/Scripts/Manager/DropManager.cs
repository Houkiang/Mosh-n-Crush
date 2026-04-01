using UnityEngine;

public class DropManager : MonoBehaviour
{
    public static DropManager Instance;

    [Header("配置")]
    public GameObject xpOrbPrefab; // 蓝色小球预制体
    public float dropYOffset = 1f; // 防止球卡在地里

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // 预热对象池
        if (xpOrbPrefab != null && PoolManager.Instance != null)
        {
            PoolManager.Instance.PreparePool(xpOrbPrefab, 50);
        }
    }

    void OnEnable()
    {
        // 监听怪物死亡事件
        Enemy.OnEnemyKilled += HandleEnemyDrop;
    }

    void OnDisable()
    {
        Enemy.OnEnemyKilled -= HandleEnemyDrop;
    }

    private void HandleEnemyDrop(Enemy enemy)
    {
        //Debug.Log("DropManager: 处理怪物掉落");
        if (xpOrbPrefab == null || enemy == null) return;
        //Debug.Log("DropManager: 生成经验球");
        // 1. 获取生成位置
        Vector3 spawnPos = enemy.transform.position;
        spawnPos.y = dropYOffset + enemy.transform.position.y; // 修正高度

        // 2. 从池中取出球
        GameObject orbObj = PoolManager.Instance.GetObject(xpOrbPrefab, spawnPos, Quaternion.identity);

        // 3. 初始化球的经验值
        ExperienceOrb orbScript = orbObj.GetComponent<ExperienceOrb>();
        if (orbScript != null)
        {
            // 使用怪物数据里配置的经验值
            int xpAmount = enemy.ExperienceReward;
            orbScript.Initialize(xpAmount);
        }
    }
}
