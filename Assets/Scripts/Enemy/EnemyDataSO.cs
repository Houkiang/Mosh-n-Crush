using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Game/Enemy Data")]
public class EnemyDataSO : ScriptableObject
{
    [Header("资源配置")]
    [Tooltip("该敌人对应的预制体")]
    public GameObject enemyPrefab;

    [Header("基础属性")]
    public EnemyType enemyType = EnemyType.Normal;
    public string enemyName = "Enemy";
    public float baseMaxHealth = 50f;
    public float baseDamage = 10f;
    public float baseMoveSpeed = 3f;
    public int baseDefanse = 2;
    public float baseMagicResistance = 0f;

    [Header("攻击设置")]
    public DamageType attackDamageType = DamageType.Physical;
    public bool canCrit = false;
    [Range(0f, 1f)] public float critRate = 0f;
    public float critMultiplier = 1.5f;

    [Header("奖励")]
    public int experienceReward = 10;
    public GameObject dropPrefab;
}
