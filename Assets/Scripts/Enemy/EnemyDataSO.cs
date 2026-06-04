using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Game/Enemy Data")]
public class EnemyDataSO : ScriptableObject
{
    [Header("Prefab")]
    [Tooltip("Enemy prefab spawned by the wave system.")]
    public GameObject enemyPrefab;

    [Header("Base Stats")]
    public EnemyType enemyType = EnemyType.Normal;
    public string enemyName = "Enemy";
    public float baseMaxHealth = 50f;
    public float baseDamage = 10f;
    public float baseMagicPower = 0f;
    public float baseMoveSpeed = 3f;
    public int baseDefanse = 2;
    public float baseMagicResistance = 0f;

    [Header("Attack")]
    public DamageType attackDamageType = DamageType.Physical;
    public bool canCrit = false;
    [Range(0f, 1f)] public float critRate = 0f;
    public float critMultiplier = 1.5f;
    public StatusEffectDataSO[] onHitStatusEffects;

    [Header("Rewards")]
    public int experienceReward = 10;
    public GameObject dropPrefab;
}
