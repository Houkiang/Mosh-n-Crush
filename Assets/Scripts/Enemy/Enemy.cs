using System;
using UnityEngine;

public class Enemy : MonoBehaviour, ICombatant
{
    [Header("核心数据")]
    [SerializeField] private EnemyDataSO enemyData;

    [Header("运行时属性(只读/调试)")]
    [SerializeField] private float currentHealth;
    [SerializeField] private float currentDamage;
    [SerializeField] private float currentMoveSpeed;
    [SerializeField] private float currentDefence;
    [SerializeField] private float currentMagicResistance;
    [SerializeField] private int experienceReward;

    private EnemyMovement movement;
    private Transform playerTransform;
    private Collider enemyCollider;
    private bool isDead = false;
    private float maxHealth;

    public Transform CombatTransform => transform;
    public bool IsAlive => !isDead && gameObject.activeSelf;
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
        if (enemyCollider != null) enemyCollider.enabled = true;
    }

    public void Initialize(EnemyDataSO data, float gameTime)
    {
        enemyData = data;

        if (GameManager.Instance != null)
        {
            playerTransform = GameManager.Instance.playerTransform;
        }

        float timeMultiplier = 1f + (gameTime / 60f) * 0.1f;
        maxHealth = data.baseMaxHealth * timeMultiplier;
        currentHealth = maxHealth;
        currentDamage = data.baseDamage * timeMultiplier;
        currentDefence = data.baseDefanse * timeMultiplier;
        currentMagicResistance = data.baseMagicResistance * timeMultiplier;
        currentMoveSpeed = data.baseMoveSpeed;
        experienceReward = (int)(data.experienceReward * timeMultiplier);

        if (movement != null)
        {
            movement.ResetState();
        }
    }

    public DamageContext CreateDamageContext(GameObject target = null)
    {
        return new DamageContext
        {
            Source = gameObject,
            Target = target,
            BaseDamage = currentDamage,
            DamageType = enemyData.attackDamageType,
            CanCrit = enemyData.canCrit,
            CritRate = enemyData.critRate,
            CritMultiplier = enemyData.critMultiplier,
            KnockbackForce = 0f,
            KnockbackDuration = 0f
        };
    }

    public void TakeDamage(float amount)
    {
        ReceiveDamage(new DamageContext
        {
            Source = null,
            Target = gameObject,
            BaseDamage = amount,
            DamageType = DamageType.Physical,
            CanCrit = false,
            CritRate = 0f,
            CritMultiplier = 1f
        });
    }

    public DamageResult ReceiveDamage(DamageContext context)
    {
        DamageResult result = CombatResolver.ResolveDamage(context, currentDefence, currentMagicResistance);
        if (isDead || result.FinalDamage <= 0f)
        {
            return result;
        }

        currentHealth -= result.FinalDamage;

        Vector3 popupPos = transform.position + Vector3.up * 2f;
        DamageTextManager.Instance.ShowDamage(popupPos, result.FinalDamage, result.IsCritical);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
            result.TargetDied = true;
        }

        return result;
    }

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
        if (enemyCollider != null) enemyCollider.enabled = false;
        OnEnemyKilled?.Invoke(this);
    }

    public void Despawn()
    {
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnObject(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
