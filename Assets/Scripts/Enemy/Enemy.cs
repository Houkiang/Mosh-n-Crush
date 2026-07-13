using System;
using UnityEngine;

public class Enemy : MonoBehaviour, ICombatant
{
    [Header("核心数据")]
    [SerializeField] private EnemyDataSO enemyData;

    [Header("运行时属性")]
    [SerializeField] private float currentHealth;
    [SerializeField] private float currentDamage;
    [SerializeField] private float currentMoveSpeed;
    [SerializeField] private float currentDefence;
    [SerializeField] private float currentMagicResistance;
    [SerializeField] private int experienceReward;

    private EnemyMovement movement;
    private EnemyFlybyMovement flybyMovement;
    private EnemyHitFlash hitFlash;
    private Transform playerTransform;
    private Collider enemyCollider;
    private bool isDead = false;
    private bool hasBeenRemoved = false;
    private float maxHealth;
    private StatCollection stats;
    private StatusController statusController;

    public Transform CombatTransform => transform;
    public Collider EnemyCollider => enemyCollider;
    public bool IsAlive => !isDead && gameObject.activeSelf;
    public StatCollection Stats => stats;
    public float CurrentDamage => GetAttackPowerForDamageType();
    public float CurrentMoveSpeed => GetStatValue(StatType.MoveSpeed, currentMoveSpeed);
    public Transform PlayerTransform => playerTransform;
    public int ExperienceReward => experienceReward;
    public bool IsDead => isDead;
    public EnemyDataSO EnemyData => enemyData;

    public static event Action<Enemy> OnEnemyKilled;
    public static event Action<Enemy> OnEnemyRemoved;
    public event Action<float, float> OnHealthChanged;

    private void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        flybyMovement = GetComponent<EnemyFlybyMovement>();
        hitFlash = GetComponent<EnemyHitFlash>();
        if (hitFlash == null)
        {
            hitFlash = gameObject.AddComponent<EnemyHitFlash>();
        }

        enemyCollider = GetComponent<Collider>();
        statusController = GetComponent<StatusController>();
        if (statusController == null)
        {
            statusController = gameObject.AddComponent<StatusController>();
        }

        statusController.Initialize(this);
    }

    private void OnEnable()
    {
        isDead = false;
        hasBeenRemoved = false;

        if (enemyCollider != null)
        {
            enemyCollider.enabled = true;
        }

        statusController?.ClearAllEffects();
        movement?.ResetState();
        flybyMovement?.ResetState();
    }

    public void Initialize(EnemyDataSO data, float gameTime)
    {
        enemyData = data;

        if (GameManager.Instance != null)
        {
            playerTransform = GameManager.Instance.playerTransform;
        }

        float timeMultiplier = 1f + (gameTime / 60f) * 0.1f;
        BuildStats(data, timeMultiplier);

        currentHealth = maxHealth;
        experienceReward = Mathf.RoundToInt(data.experienceReward * timeMultiplier);
        ApplyMovementMode();
    }

    public DamageContext CreateDamageContext(GameObject target = null)
    {
        return new DamageContext
        {
            Source = gameObject,
            Target = target,
            BaseDamage = GetAttackPowerForDamageType(),
            DamageType = enemyData.attackDamageType,
            CanCrit = enemyData.canCrit,
            CritRate = Mathf.Clamp01(enemyData.critRate + GetStatValue(StatType.CritRate)),
            CritMultiplier = Mathf.Max(1f, enemyData.critMultiplier + GetStatValue(StatType.CritDamage)),
            KnockbackForce = 0f,
            KnockbackDuration = 0f,
            StatusEffects = StatusEffectApplication.FromData(enemyData.onHitStatusEffects)
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
            CritMultiplier = 1f,
            StatusEffects = null
        });
    }

    public DamageResult ReceiveDamage(DamageContext context)
    {
        DamageResult result = CombatResolver.ResolveDamage(context, stats);
        if (isDead || result.FinalDamage <= 0f)
        {
            return result;
        }

        if (statusController != null)
        {
            float adjustedDamage = statusController.AbsorbIncomingDamage(result.FinalDamage);
            result.AbsorbedDamage = result.FinalDamage - adjustedDamage;
            result.FinalDamage = adjustedDamage;
            result.WasBlocked = result.WasBlocked || result.FinalDamage <= 0f;
        }

        if (result.FinalDamage <= 0f)
        {
            return result;
        }

        currentHealth -= result.FinalDamage;

        Vector3 popupPos = transform.position + Vector3.up * 2f;
        DamageTextManager.Instance.ShowDamage(popupPos, result.FinalDamage, result.IsCritical);
        hitFlash?.PlayFlash();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        statusController?.ApplyFromDamageContext(context);

        if (currentHealth <= 0f)
        {
            Die();
            result.TargetDied = true;
        }

        return result;
    }

    public void TakeKnockback(Vector3 sourcePosition, float force, float stunDuration)
    {
        if (isDead)
        {
            return;
        }

        if (movement != null && movement.enabled)
        {
            movement.ApplyKnockback(sourcePosition, force, stunDuration);
            return;
        }

        if (flybyMovement != null && flybyMovement.enabled)
        {
            flybyMovement.ApplyKnockback(sourcePosition, force, stunDuration);
        }
    }

    public void Despawn()
    {
        if (!hasBeenRemoved)
        {
            hasBeenRemoved = true;
            OnEnemyRemoved?.Invoke(this);
        }

        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnObject(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ConfigureFlyby(Vector3 direction, Vector3 lockedTargetPoint)
    {
        if (enemyData == null || flybyMovement == null)
        {
            return;
        }

        float speed = Mathf.Max(0f, CurrentMoveSpeed * enemyData.flybySpeedMultiplier);
        flybyMovement.Configure(
            direction,
            lockedTargetPoint,
            speed,
            enemyData.flybyDespawnDistance,
            enemyData.flybyMaxLifeTime);
    }

    public void AssignRushGroupController(EnemyRushGroupController controller)
    {
        if (flybyMovement == null)
        {
            return;
        }

        flybyMovement.SetRushGroupController(controller);
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        #if UNITY_EDITOR
        Debug.Log($"{enemyData.enemyName} died");
        #endif
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        OnEnemyKilled?.Invoke(this);
    }

    private void BuildStats(EnemyDataSO data, float timeMultiplier)
    {
        if (stats == null)
        {
            stats = new StatCollection();
        }
        else
        {
            stats.Clear();
        }

        float scaledHealth = data.baseMaxHealth * timeMultiplier;
        float scaledDamage = data.baseDamage * timeMultiplier;
        float scaledMagicPower = data.baseMagicPower > 0f
            ? data.baseMagicPower * timeMultiplier
            : scaledDamage;

        stats.SetBaseValue(StatType.MaxHealth, scaledHealth);
        stats.SetBaseValue(StatType.PhysicalAttack, scaledDamage);
        stats.SetBaseValue(StatType.MagicPower, scaledMagicPower);
        stats.SetBaseValue(StatType.PhysicalDefense, data.baseDefanse * timeMultiplier);
        stats.SetBaseValue(StatType.MagicResistance, data.baseMagicResistance * timeMultiplier);
        stats.SetBaseValue(StatType.MoveSpeed, data.baseMoveSpeed);

        maxHealth = stats.GetValue(StatType.MaxHealth, scaledHealth);
        currentDefence = stats.GetValue(StatType.PhysicalDefense, 0f);
        currentMagicResistance = stats.GetValue(StatType.MagicResistance, 0f);
        currentMoveSpeed = stats.GetValue(StatType.MoveSpeed, data.baseMoveSpeed);
        currentDamage = GetAttackPowerForDamageType();
    }

    private float GetAttackPowerForDamageType()
    {
        StatType attackStat = enemyData != null && enemyData.attackDamageType == DamageType.Magical
            ? StatType.MagicPower
            : StatType.PhysicalAttack;

        return GetStatValue(attackStat);
    }

    private float GetStatValue(StatType statType, float fallback = 0f)
    {
        return stats != null ? stats.GetValue(statType, fallback) : fallback;
    }

    private void ApplyMovementMode()
    {
        if (movement != null)
        {
            movement.enabled = enemyData == null || enemyData.movementMode == EnemyMovementMode.Chase;
            movement.ResetState();
        }

        if (flybyMovement != null)
        {
            flybyMovement.enabled = enemyData != null && enemyData.movementMode == EnemyMovementMode.FlybyRush;
            flybyMovement.ResetState();
        }
    }
}
