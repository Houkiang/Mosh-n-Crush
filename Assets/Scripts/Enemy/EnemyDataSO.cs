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
    
    [Header("奖励")]
    public int experienceReward = 10;
    public GameObject dropPrefab;   // 未来可以在这里加 dropTable (掉落表)
}
