using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;

    [Header("波次配置")]
    public List<WaveConfigSO> waves = new List<WaveConfigSO>();

    [Header("全局限制")]
    public int maxEnemies = 1000;

    [Header("环形刷怪范围")]
    public float minSpawnRadius = 10f;
    public float maxSpawnRadius = 20f;

    [Header("地形适配")]
    public LayerMask groundLayer;
    public float raycastHeight = 50f;
    public float enemyYOffset = 0f;

    private int currentWaveIndex;
    private bool initSpawned;
    private float currentWaveTimer;
    private float spawnTimer;
    private float rushSpawnTimer;
    private int currentActiveEnemyCount;
    private bool isBossSpawned;
    private int activeBossCount;

    public event Action<int> OnWaveChanged;
    public event Action<Enemy> OnBossSpawned;

    public int TotalWaves => waves.Count;
    public int CurrentWaveNumber => currentWaveIndex + 1;
    public WaveType CurrentWaveType => currentWaveIndex < waves.Count ? waves[currentWaveIndex].waveType : WaveType.Normal;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        currentWaveIndex = 0;
        currentWaveTimer = 0f;
        spawnTimer = 0f;
        rushSpawnTimer = 0f;
        initSpawned = false;
        isBossSpawned = false;
        activeBossCount = 0;
        currentActiveEnemyCount = 0;

        PrewarmAllEnemies();
    }

    private void OnEnable()
    {
        Enemy.OnEnemyKilled += HandleEnemyKilled;
        Enemy.OnEnemyRemoved += HandleEnemyRemoved;
    }

    private void OnDisable()
    {
        Enemy.OnEnemyKilled -= HandleEnemyKilled;
        Enemy.OnEnemyRemoved -= HandleEnemyRemoved;
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            return;
        }

        if (currentWaveIndex >= waves.Count)
        {
            return;
        }

        WaveConfigSO currentWave = waves[currentWaveIndex];
        switch (currentWave.waveType)
        {
            case WaveType.Normal:
                HandleNormalWave(currentWave);
                break;
            case WaveType.Boss:
                HandleBossWave(currentWave);
                break;
        }
    }

    public float GetNormalWaveProgress()
    {
        if (currentWaveIndex >= waves.Count)
        {
            return 1f;
        }

        WaveConfigSO wave = waves[currentWaveIndex];
        if (wave.waveType != WaveType.Normal)
        {
            return 0f;
        }

        return Mathf.Clamp01(currentWaveTimer / wave.waveDuration);
    }

    private void HandleNormalWave(WaveConfigSO wave)
    {
        currentWaveTimer += Time.deltaTime;

        if (!initSpawned)
        {
            HandleInitNormalSpawn(wave.initialSpawnCount, wave);
            initSpawned = true;
        }

        HandleSpawnTimer(wave);
        HandleRushGroupSpawn(wave);

        if (currentWaveTimer >= wave.waveDuration)
        {
            NextWave();
        }
    }

    private void HandleBossWave(WaveConfigSO wave)
    {
        currentWaveTimer += Time.deltaTime;

        if (!isBossSpawned)
        {
            SpawnBoss(wave.bossUnit);
            isBossSpawned = true;
        }

        if (wave.enemies.Count > 0)
        {
            if (!initSpawned)
            {
                HandleInitNormalSpawn(wave.initialSpawnCount, wave);
                initSpawned = true;
            }

            HandleSpawnTimer(wave);
        }

        HandleRushGroupSpawn(wave);

        if (isBossSpawned && activeBossCount <= 0)
        {
            NextWave();
        }
    }

    private void HandleInitNormalSpawn(int count, WaveConfigSO wave)
    {
        for (int i = 0; i < count; i++)
        {
            if (currentActiveEnemyCount >= maxEnemies)
            {
                break;
            }

            SpawnWeightedEnemy(wave);
        }
    }

    private void HandleSpawnTimer(WaveConfigSO wave)
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer < wave.spawnInterval || currentActiveEnemyCount >= maxEnemies)
        {
            return;
        }

        spawnTimer = 0f;
        SpawnWeightedEnemy(wave);
    }

    private void HandleRushGroupSpawn(WaveConfigSO wave)
    {
        if (!wave.enableRushGroup || wave.rushEnemyData == null)
        {
            return;
        }

        if (wave.rushEnemyData.movementMode != EnemyMovementMode.FlybyRush)
        {
            return;
        }

        if (currentWaveTimer < wave.rushStartDelay)
        {
            return;
        }

        rushSpawnTimer += Time.deltaTime;
        if (rushSpawnTimer < wave.rushSpawnInterval)
        {
            return;
        }

        rushSpawnTimer = 0f;
        SpawnRushGroup(wave);
    }

    private void NextWave()
    {
        currentWaveIndex++;
        currentWaveTimer = 0f;
        spawnTimer = 0f;
        rushSpawnTimer = 0f;
        initSpawned = false;
        isBossSpawned = false;
        activeBossCount = 0;
        OnWaveChanged?.Invoke(currentWaveIndex);
    }

    private void SpawnBoss(EnemyDataSO bossData)
    {
        if (bossData == null)
        {
            return;
        }

        if (TrySpawnEnemy(bossData, out Enemy enemy))
        {
            activeBossCount++;
            OnBossSpawned?.Invoke(enemy);
        }
    }

    private void SpawnWeightedEnemy(WaveConfigSO wave)
    {
        if (wave.enemies.Count == 0)
        {
            return;
        }

        int totalWeight = 0;
        for (int i = 0; i < wave.enemies.Count; i++)
        {
            totalWeight += wave.enemies[i].weight;
        }

        int randomValue = Random.Range(0, totalWeight);
        EnemyDataSO selectedEnemy = null;

        for (int i = 0; i < wave.enemies.Count; i++)
        {
            EnemySpawnEntry entry = wave.enemies[i];
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

    private void SpawnRushGroup(WaveConfigSO wave)
    {
        if (GameManager.Instance == null || GameManager.Instance.playerTransform == null)
        {
            return;
        }

        Transform player = GameManager.Instance.playerTransform;
        Vector3 playerPosition = player.position;

        Vector2 randomDir2D = Random.insideUnitCircle.normalized;
        if (randomDir2D.sqrMagnitude <= 0.0001f)
        {
            randomDir2D = Vector2.right;
        }

        Vector3 fromPlayerDirection = new Vector3(randomDir2D.x, 0f, randomDir2D.y).normalized;
        Vector3 groupCenter = playerPosition + fromPlayerDirection * wave.rushSpawnDistance;
        Vector3 moveDirection = (playerPosition - groupCenter).normalized;
        Vector3 sideDirection = Vector3.Cross(Vector3.up, moveDirection).normalized;
        int spawnCount = Mathf.Max(1, wave.rushGroupCount);
        float spacing = Mathf.Max(0.1f, wave.rushFormationSpacing);
        float clusterRadius = Mathf.Max(spacing, spacing * Mathf.Sqrt(spawnCount) * 0.75f);
        List<Vector2> localOffsets = new List<Vector2>(spawnCount);
        GameObject rushGroupRoot = new GameObject($"RushGroup_{wave.name}");
        rushGroupRoot.transform.SetParent(transform);
        EnemyRushGroupController rushGroupController = rushGroupRoot.AddComponent<EnemyRushGroupController>();
        rushGroupController.Initialize(
            wave.rushCohesionWeight,
            wave.rushSeparationWeight,
            wave.rushCohesionRadius,
            wave.rushSeparationRadius);
        int spawnedCount = 0;

        for (int i = 0; i < spawnCount; i++)
        {
            if (currentActiveEnemyCount >= maxEnemies)
            {
                break;
            }

            Vector2 localOffset = GetRushClusterOffset(localOffsets, clusterRadius, spacing);
            localOffsets.Add(localOffset);
            Vector3 spawnPosition =
                groupCenter
                + sideDirection * localOffset.x
                + moveDirection * localOffset.y;
            Quaternion rotation = Quaternion.LookRotation(moveDirection, Vector3.up);

            if (TrySpawnEnemyAtPosition(
                wave.rushEnemyData,
                spawnPosition,
                rotation,
                moveDirection,
                playerPosition,
                out Enemy enemy))
            {
                enemy.AssignRushGroupController(rushGroupController);
                spawnedCount++;
            }
        }

        if (spawnedCount <= 0)
        {
            Destroy(rushGroupRoot);
        }
    }

    private Vector2 GetRushClusterOffset(List<Vector2> existingOffsets, float clusterRadius, float minSpacing)
    {
        if (existingOffsets == null || existingOffsets.Count == 0)
        {
            return Random.insideUnitCircle * (clusterRadius * 0.35f);
        }

        float minSpacingSqr = minSpacing * minSpacing;
        Vector2 bestCandidate = Vector2.zero;
        float bestNearestDistanceSqr = -1f;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector2 candidate = Random.insideUnitCircle * clusterRadius;
            candidate.y *= 0.8f;

            float nearestDistanceSqr = float.MaxValue;
            for (int i = 0; i < existingOffsets.Count; i++)
            {
                float sqrDistance = (candidate - existingOffsets[i]).sqrMagnitude;
                if (sqrDistance < nearestDistanceSqr)
                {
                    nearestDistanceSqr = sqrDistance;
                }
            }

            if (nearestDistanceSqr >= minSpacingSqr)
            {
                return candidate;
            }

            if (nearestDistanceSqr > bestNearestDistanceSqr)
            {
                bestNearestDistanceSqr = nearestDistanceSqr;
                bestCandidate = candidate;
            }
        }

        return bestCandidate;
    }

    private bool TrySpawnEnemy(EnemyDataSO data, out Enemy spawnedEnemy)
    {
        spawnedEnemy = null;
        if (data == null || data.enemyPrefab == null)
        {
            return false;
        }

        if (!TryGetSpawnPosition(out Vector3 spawnPos))
        {
            return false;
        }

        return TrySpawnEnemyAtPosition(data, spawnPos, Quaternion.identity, null, null, out spawnedEnemy);
    }

    private bool TrySpawnEnemyAtPosition(
        EnemyDataSO data,
        Vector3 desiredPosition,
        Quaternion rotation,
        Vector3? flybyDirection,
        Vector3? flybyTargetPoint,
        out Enemy spawnedEnemy)
    {
        spawnedEnemy = null;
        if (data == null || data.enemyPrefab == null)
        {
            return false;
        }

        if (!TryProjectSpawnPosition(desiredPosition, out Vector3 groundedPosition))
        {
            return false;
        }

        GameObject enemyObj = PoolManager.Instance.GetObject(data.enemyPrefab, groundedPosition, rotation);
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy == null)
        {
            return false;
        }

        enemy.Initialize(data, GameManager.Instance.gameTime);

        if (data.movementMode == EnemyMovementMode.FlybyRush && flybyDirection.HasValue && flybyTargetPoint.HasValue)
        {
            enemy.ConfigureFlyby(flybyDirection.Value, flybyTargetPoint.Value);
        }

        currentActiveEnemyCount++;
        spawnedEnemy = enemy;
        return true;
    }

    private void HandleEnemyKilled(Enemy enemy)
    {
        if (currentWaveIndex >= waves.Count || enemy == null)
        {
            return;
        }

        WaveConfigSO currentWave = waves[currentWaveIndex];
        if (currentWave.waveType == WaveType.Boss && isBossSpawned && enemy.EnemyData.enemyType == EnemyType.Boss)
        {
            activeBossCount--;
            if (activeBossCount < 0)
            {
                activeBossCount = 0;
            }
        }
    }

    private void HandleEnemyRemoved(Enemy enemy)
    {
        currentActiveEnemyCount--;
        if (currentActiveEnemyCount < 0)
        {
            currentActiveEnemyCount = 0;
        }
    }

    private bool TryGetSpawnPosition(out Vector3 position)
    {
        position = Vector3.zero;
        if (GameManager.Instance == null || GameManager.Instance.playerTransform == null)
        {
            return false;
        }

        Vector3 playerPos = GameManager.Instance.playerTransform.position;
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float randomDist = Random.Range(minSpawnRadius, maxSpawnRadius);
        Vector3 randomOffset = new Vector3(randomDir.x, 0f, randomDir.y) * randomDist;

        Vector3 desiredPosition = new Vector3(playerPos.x + randomOffset.x, playerPos.y, playerPos.z + randomOffset.z);
        return TryProjectSpawnPosition(desiredPosition, out position);
    }

    private bool TryProjectSpawnPosition(Vector3 desiredPosition, out Vector3 position)
    {
        position = Vector3.zero;
        Vector3 rayOrigin = new Vector3(desiredPosition.x, desiredPosition.y + raycastHeight, desiredPosition.z);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundLayer))
        {
            position = hit.point;
            position.y += enemyYOffset;
            return true;
        }

        return false;
    }

    private void PrewarmAllEnemies()
    {
        HashSet<EnemyDataSO> allTypes = new HashSet<EnemyDataSO>();
        for (int i = 0; i < waves.Count; i++)
        {
            WaveConfigSO wave = waves[i];
            if (wave.bossUnit != null)
            {
                allTypes.Add(wave.bossUnit);
            }

            for (int j = 0; j < wave.enemies.Count; j++)
            {
                if (wave.enemies[j].enemyData != null)
                {
                    allTypes.Add(wave.enemies[j].enemyData);
                }
            }

            if (wave.rushEnemyData != null)
            {
                allTypes.Add(wave.rushEnemyData);
            }
        }

        foreach (EnemyDataSO data in allTypes)
        {
            if (data != null && data.enemyPrefab != null)
            {
                PoolManager.Instance.PreparePool(data.enemyPrefab, 10);
            }
        }
    }
}
