using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random; 
public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;

    [Header("波次配置")]
    public List<WaveConfigSO> waves = new List<WaveConfigSO>();
    private int currentWaveIndex = 0;

    [Header("全局限制")]
    public int maxEnemies = 1000;

    [Header("范围设置")]
    public float minSpawnRadius = 10f;
    public float maxSpawnRadius = 20f;

    [Header("地形适配")]
    public LayerMask groundLayer;
    public float raycastHeight = 50f;
    public float enemyYOffset = 0f;

    // 运行时状态
    private bool initSpawned = false;  
    private float currentWaveTimer;
    private float spawnTimer;
    private int currentActiveEnemyCount = 0;
    
    // Boss状态追踪
    private bool isBossSpawned = false;
    private int activeBossCount = 0;

    // Boss生成事件，将Boss实例传给UI
    public event Action<int> OnWaveChanged;
    public event Action<Enemy> OnBossSpawned;
    
    // 获取当前波次配置的属性，供UI读取
    public int TotalWaves => waves.Count;
    public int CurrentWaveNumber => currentWaveIndex + 1;
    public WaveType CurrentWaveType => currentWaveIndex < waves.Count ? waves[currentWaveIndex].waveType : WaveType.Normal;


    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        currentWaveIndex = 0;
        currentWaveTimer = 0;
        isBossSpawned = false;
        
        // 预热池子（可选：遍历所有波次的所有敌人类型进行预热）
        PrewarmAllEnemies();
    }

    void OnEnable()
    {
        Enemy.OnEnemyKilled += HandleEnemyKilled;
    }

    void OnDisable()
    {
        Enemy.OnEnemyKilled -= HandleEnemyKilled;
    }

    void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (currentWaveIndex >= waves.Count) return; // 所有波次结束（游戏胜利逻辑可放在这）

        WaveConfigSO currentWave = waves[currentWaveIndex];

        // 处理波次逻辑
        if (currentWave.waveType == WaveType.Normal)
        {
            HandleNormalWave(currentWave);
        }
        else if (currentWave.waveType == WaveType.Boss)
        {
            HandleBossWave(currentWave);
        }
    }

    // --- 波次逻辑 ---
    // 供UI调用的辅助方法：获取普通波次的时间进度 (0.0 ~ 1.0)
    public float GetNormalWaveProgress()
    {
        if (currentWaveIndex >= waves.Count) return 1f;
        var wave = waves[currentWaveIndex];
        if (wave.waveType != WaveType.Normal) return 0f;
        
        return Mathf.Clamp01(currentWaveTimer / wave.waveDuration);
    }

    void HandleNormalWave(WaveConfigSO wave)
    {
        // 1. 计时
        currentWaveTimer += Time.deltaTime;
        // 2. 处理初始生成
        if(initSpawned == false)
        {
            HandleInitNormalSpawn(wave.initialSpawnCount, wave);
            initSpawned = true;
        }

        // 3. 生成怪物
        HandleSpawnTimer(wave);

        // 4. 检查波次结束
        if (currentWaveTimer >= wave.waveDuration)
        {
            NextWave();
        }
    }

    void HandleBossWave(WaveConfigSO wave)
    {
        // 1. 如果Boss还没生成，立即生成
        if (!isBossSpawned)
        {
            SpawnBoss(wave.bossUnit);
            isBossSpawned = true;
        }

        // 2. Boss波次依然可以生成小怪骚扰玩家（如果配置了enemies列表）
        if (wave.enemies.Count > 0)
        {
            if(!initSpawned)
            {
                initSpawned = true;
                HandleInitNormalSpawn(wave.initialSpawnCount, wave);
            }
            
            
            HandleSpawnTimer(wave);
        }

        // 3. 检查波次结束：由 HandleEnemyKilled 中的 activeBossCount 决定
        if (isBossSpawned && activeBossCount <= 0)
        {
            NextWave();
        }
    }
    void HandleInitNormalSpawn(int count, WaveConfigSO wave)
    {
        for (int i = 0; i < count; i++)
        {
            if (currentActiveEnemyCount >= maxEnemies) break;
            SpawnWeightedEnemy(wave);
        }
    }
    void HandleSpawnTimer(WaveConfigSO wave)
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= wave.spawnInterval && currentActiveEnemyCount < maxEnemies)
        {
            spawnTimer = 0f;
            SpawnWeightedEnemy(wave);
        }
    }

    void NextWave()
    {
        currentWaveIndex++;
        currentWaveTimer = 0f;
        spawnTimer = 0f;
        isBossSpawned = false;
        OnWaveChanged?.Invoke(currentWaveIndex);
        Debug.Log($"进入第 {currentWaveIndex + 1} 波");
        
        // 这里可以发送UI事件通知玩家波次刷新
    }

    // --- 生成核心逻辑 ---

    void SpawnBoss(EnemyDataSO bossData)
    {
        if (bossData == null) return;
        
        // 强制生成Boss，哪怕超过最大数量限制
        if(TrySpawnEnemy(bossData, out Enemy enemy))
        {
            activeBossCount++; 
            
            // [新增] 通知UI Boss生成了，并把 Boss 脚本传过去
            OnBossSpawned?.Invoke(enemy);
        }
    }

    void SpawnWeightedEnemy(WaveConfigSO wave)
    {
        if (wave.enemies.Count == 0) return;

        // 权重随机算法
        int totalWeight = 0;
        foreach (var entry in wave.enemies) totalWeight += entry.weight;

        int randomValue = Random.Range(0, totalWeight);
        EnemyDataSO selectedEnemy = null;

        foreach (var entry in wave.enemies)
        {
            if (randomValue < entry.weight)
            {
                selectedEnemy = entry.enemyData;
                break;
            }
            randomValue -= entry.weight;
        }

        if (selectedEnemy != null)
        {
            TrySpawnEnemy(selectedEnemy, out _);
        }
    }

    bool TrySpawnEnemy(EnemyDataSO data, out Enemy spawnedEnemy)
    {
        spawnedEnemy = null;
        if (data.enemyPrefab == null) return false;

        if (TryGetSpawnPosition(out Vector3 spawnPos))
        {
            GameObject enemyObj = PoolManager.Instance.GetObject(
                data.enemyPrefab, 
                spawnPos, 
                Quaternion.identity
            );

            Enemy enemy = enemyObj.GetComponent<Enemy>();
            if (enemy != null)
            {
                // 传入当前波次索引或游戏时间，用于计算成长
                enemy.Initialize(data, GameManager.Instance.gameTime);
                

            }
            
            currentActiveEnemyCount++;
            spawnedEnemy = enemy;
            return true;
        }
        return false;
    }

    // --- 事件处理 ---

    private void HandleEnemyKilled(Enemy enemy)
    {
        currentActiveEnemyCount--;
        if (currentActiveEnemyCount < 0) currentActiveEnemyCount = 0;

        // 检查是否是当前波次的Boss
        if (currentWaveIndex < waves.Count)
        {
            WaveConfigSO currentWave = waves[currentWaveIndex];
            if (currentWave.waveType == WaveType.Boss && isBossSpawned)
            {
                // 判断死的是不是Boss
                if (enemy.EnemyData.enemyType == EnemyType.Boss)
                {
                    activeBossCount--;
                    if (activeBossCount < 0) activeBossCount = 0;
                }
            }
        }
    }

    // --- 辅助方法 ---
    
    bool TryGetSpawnPosition(out Vector3 position)
    {
        position = Vector3.zero;
        if (GameManager.Instance.playerTransform == null) return false;
        
        Vector3 playerPos = GameManager.Instance.playerTransform.position;
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float randomDist = Random.Range(minSpawnRadius, maxSpawnRadius);
        Vector3 randomOffset = new Vector3(randomDir.x, 0, randomDir.y) * randomDist;

        Vector3 rayOrigin = new Vector3(playerPos.x + randomOffset.x, playerPos.y + raycastHeight, playerPos.z + randomOffset.z);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastHeight * 2, groundLayer))
        {
            position = hit.point;
            position.y += enemyYOffset;
            return true;
        }
        return false;
    }

    void PrewarmAllEnemies()
    {
        // 简单的去重预热逻辑
        HashSet<EnemyDataSO> allTypes = new HashSet<EnemyDataSO>();
        foreach(var wave in waves)
        {
            if(wave.bossUnit != null) allTypes.Add(wave.bossUnit);
            foreach(var entry in wave.enemies) allTypes.Add(entry.enemyData);
        }

        foreach(var data in allTypes)
        {
            if(data.enemyPrefab != null) 
                PoolManager.Instance.PreparePool(data.enemyPrefab, 10);
        }
    }
}
