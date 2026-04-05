using System;
using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("核心数据")]
    [SerializeField] private EnemyDataSO enemyData;

    [Header("运行时属性 (只读/调试)")]
    [SerializeField] private float currentHealth;
    [SerializeField] private float currentDamage;
    [SerializeField] private float currentMoveSpeed;
    [SerializeField] private float currentDefence;
    [SerializeField] private int experienceReward;
    // 组件引用
    private EnemyMovement movement;
    private Transform playerTransform;
    private Collider enemyCollider;
    private bool isDead = false;
    private float maxHealth;//只用来计算比例

    // 公开属性供其他组件访问
    public float CurrentDamage => currentDamage;
    public float CurrentMoveSpeed => currentMoveSpeed;
    public Transform PlayerTransform => playerTransform;
    public int ExperienceReward => experienceReward;
    public bool IsDead => isDead;
    public EnemyDataSO EnemyData => enemyData; 
    public static event Action<Enemy> OnEnemyKilled;
    public event Action<float, float> OnHealthChanged;

    void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        enemyCollider = GetComponent<Collider>();
    }

    void OnEnable()
    {
        isDead = false;
        if(enemyCollider!=null)enemyCollider.enabled = true;
        // 具体的数值初始化由 Spawner 调用 Initialize 触发
    }

    /// <summary>
    /// 初始化入口 (由 Spawner 调用)
    /// </summary>
    public void Initialize(EnemyDataSO data, float gameTime)
    {
        this.enemyData = data;
        
        if (GameManager.Instance != null)
        {
            playerTransform = GameManager.Instance.playerTransform;
        }

        // 数值成长计算
        float timeMultiplier = 1f + (gameTime / 60f) * 0.1f;
        //记录 maxHealth
        maxHealth = data.baseMaxHealth * timeMultiplier;
        currentHealth = maxHealth; // 确保初始血量也是满的
        currentDamage = data.baseDamage * timeMultiplier;
        currentDefence = data.baseDefanse * timeMultiplier;
        currentMoveSpeed = data.baseMoveSpeed;
        experienceReward = (int)(data.experienceReward * timeMultiplier);

        // 通知移动组件重置状态
        if (movement != null)
        {
            movement.ResetState();
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        float effectiveDamage = Mathf.Max(amount - currentDefence, 1);
        currentHealth -= effectiveDamage;

        Vector3 popupPos = transform.position + Vector3.up * 2f; 
        DamageTextManager.Instance.ShowDamage(popupPos, effectiveDamage, false);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // 处理击退请求，转发给移动组件
    public void TakeKnockback(Vector3 sourcePosition, float force, float stunDuration)
    {
        if (isDead || movement == null) return;
        movement.ApplyKnockback(sourcePosition, force, stunDuration);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log($"{enemyData.enemyName} 死亡");
        // 1. 立即禁用碰撞体，防止尸体挡路
        if (enemyCollider != null) enemyCollider.enabled = false;

        // 2. 通知外部（Spawner/Drop/UI/Score）统一处理结算
        // 注意：计分由 GameManager 监听 OnEnemyKilled 统一执行，避免重复加分。
        OnEnemyKilled?.Invoke(this); 

    }

    /// <summary>
    /// 真正的回收方法，等待动画播放完毕后由外部调用
    /// </summary>
    public void Despawn()
    {
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnObject(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
