using UnityEngine;



[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Game/Enemy Data")]
public class EnemyDataSO : ScriptableObject
{
    [Header("预制体")]
    [Tooltip("波次系统生成时使用的敌人预制体。")]
    public GameObject enemyPrefab;

    [Header("基础属性")]
    public EnemyType enemyType = EnemyType.Normal;
    public string enemyName = "Enemy";
    public float baseMaxHealth = 50f;
    public float baseDamage = 10f;
    public float baseMagicPower = 0f;
    public float baseMoveSpeed = 3f;
    public int baseDefanse = 2;
    public float baseMagicResistance = 0f;

    [Header("移动模式")]
    public EnemyMovementMode movementMode = EnemyMovementMode.Chase;
    [Min(0.1f)] public float flybySpeedMultiplier = 2f;
    [Min(1f)] public float flybyDespawnDistance = 12f;
    [Min(0.1f)] public float flybyMaxLifeTime = 6f;

    [Header("攻击")]
    public DamageType attackDamageType = DamageType.Physical;
    public bool canCrit = false;
    [Range(0f, 1f)] public float critRate = 0f;
    public float critMultiplier = 1.5f;
    public StatusEffectDataSO[] onHitStatusEffects;

    [Header("奖励")]
    public int experienceReward = 10;
    public GameObject dropPrefab;
}
