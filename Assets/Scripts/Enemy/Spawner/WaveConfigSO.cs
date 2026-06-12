using System.Collections.Generic;
using UnityEngine;

public enum WaveType
{
    Normal,
    Boss
}

[System.Serializable]
public struct EnemySpawnEntry
{
    public EnemyDataSO enemyData;
    [Range(1, 100)] public int weight;
}

[CreateAssetMenu(fileName = "NewWaveConfig", menuName = "Game/Wave Config")]
public class WaveConfigSO : ScriptableObject
{
    [Header("波次类型")]
    public WaveType waveType;

    [Header("通用设置")]
    public int initialSpawnCount = 5;
    public float spawnInterval = 1f;

    [Header("普通波设置")]
    public float waveDuration = 60f;

    [Header("Boss 波设置")]
    public EnemyDataSO bossUnit;

    [Header("普通敌人列表")]
    public List<EnemySpawnEntry> enemies = new List<EnemySpawnEntry>();

    [Header("掠过敌群")]
    public bool enableRushGroup = false;
    public EnemyDataSO rushEnemyData;
    [Min(1)] public int rushGroupCount = 6;
    [Min(0.1f)] public float rushSpawnInterval = 8f;
    [Min(0f)] public float rushStartDelay = 3f;
    [Min(1f)] public float rushSpawnDistance = 18f;
    [Min(0.1f)] public float rushFormationSpacing = 1.5f;
    [Min(0f)] public float rushCohesionWeight = 1.2f;
    [Min(0f)] public float rushSeparationWeight = 2.4f;
    [Min(0.1f)] public float rushCohesionRadius = 2f;
    [Min(0.1f)] public float rushSeparationRadius = 1.25f;
}
