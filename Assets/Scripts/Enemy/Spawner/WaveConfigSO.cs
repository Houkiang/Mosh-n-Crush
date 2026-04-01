
using UnityEngine;
using System.Collections.Generic;

public enum WaveType
{
    Normal, // 时间驱动：时间到了进下一波
    Boss    // 击杀驱动：Boss死了进下一波
}

[System.Serializable]
public struct EnemySpawnEntry
{
    public EnemyDataSO enemyData;
    [Range(1, 100)] public int weight; // 权重，权重越高出现概率越大
}

[CreateAssetMenu(fileName = "NewWaveConfig", menuName = "Game/Wave Config")]
public class WaveConfigSO : ScriptableObject
{
    [Header("波次类型")]
    public WaveType waveType;
    
    [Header("通用设置")]
    public int initialSpawnCount = 5; // 初始生成多少个敌人
    public float spawnInterval = 1f; // 这一波怪物的生成间隔

    [Header("普通波次设置 (Normal)")]
    public float waveDuration = 60f; // 这一波持续多久

    [Header("Boss波次设置 (Boss)")]
    // 如果是Boss波，必须击杀才能过关
    public EnemyDataSO bossUnit; 

    [Header("敌人生成列表 (权重)")]
    public List<EnemySpawnEntry> enemies = new List<EnemySpawnEntry>();
}
