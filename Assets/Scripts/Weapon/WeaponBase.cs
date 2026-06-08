using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    public WeaponDataSO weaponData;

    [Header("Runtime Stats")]
    [SerializeField] protected float currentDamage;
    [SerializeField] protected float currentCooldown;
    [SerializeField] protected float currentRange;
    [SerializeField] protected float currentKnockback;
    [SerializeField] protected float currentKnockbackDuration;
    [SerializeField] protected int currentWeaponCount;
    [SerializeField] protected int weaponCount = 1;

    protected float cooldownTimer;
    protected Transform playerTransform;
    protected int enemyLayerMask;
    protected WeaponManager weaponManager;
    protected Player playerStats;

    private readonly List<StatusEffectApplication> runtimeStatusEffects = new List<StatusEffectApplication>();

    public virtual void Initialize(WeaponDataSO data, Transform owner, WeaponManager manager)
    {
        weaponData = data;
        playerTransform = owner;
        weaponManager = manager;
        playerStats = weaponManager.player.GetComponent<Player>();

        currentDamage = data.damage;
        currentCooldown = data.cooldown;
        currentRange = data.attackRange;
        currentWeaponCount = data.weaponCount;
        currentKnockback = data.knockbackForce;
        currentKnockbackDuration = data.knockbackDuration;

        cooldownTimer = Random.Range(0f, currentCooldown);
        enemyLayerMask = LayerMask.GetMask("Enemy");
    }

    protected DamageContext CreateDamageContext(GameObject target = null)
    {
        float critRateBonus = playerStats != null ? playerStats.CritRate : 0f;
        float critDamageBonus = playerStats != null ? playerStats.CritDamage : 0f;

        return new DamageContext
        {
            Source = playerTransform != null ? playerTransform.gameObject : gameObject,
            Target = target,
            BaseDamage = GetDamageAfterPlayer(),
            DamageType = weaponData.damageType,
            CanCrit = weaponData.canCrit,
            CritRate = Mathf.Clamp01(weaponData.critRate + critRateBonus),
            CritMultiplier = Mathf.Max(1f, weaponData.critMultiplier + critDamageBonus),
            KnockbackForce = currentKnockback,
            KnockbackDuration = currentKnockbackDuration,
            StatusEffects = BuildStatusEffects()
        };
    }

    protected virtual void Update()
    {
        if (playerTransform == null)
        {
            return;
        }

        float reduction = playerStats != null ? playerStats.CooldownReduction : 0f;
        float actualCooldown = currentCooldown * (1f - reduction);
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            Attack();
            cooldownTimer = actualCooldown;
        }
    }

    protected float GetDamageAfterPlayer()
    {
        if (playerStats == null)
        {
            return currentDamage;
        }

        float offensiveBonus = weaponData.damageType == DamageType.Magical
            ? playerStats.MagicPower
            : playerStats.Strength;

        return currentDamage + offensiveBonus;
    }

    public void IncreaseDamage(float amount)
    {
        currentDamage += amount;
    }

    public void ReduceCooldown(float amount)
    {
        currentCooldown = Mathf.Max(0.1f, currentCooldown - amount);
    }

    public virtual void IncreaseWeaponCount(int amount)
    {
    }

    public void IncreaseKnockback(float amount)
    {
        currentKnockback *= 1f + amount;
        currentKnockbackDuration *= 1f + amount / 2f;
    }

    public void AddStatusEffect(StatusEffectDataSO statusEffect, int stackCount = 1)
    {
        if (statusEffect == null)
        {
            return;
        }

        runtimeStatusEffects.Add(new StatusEffectApplication
        {
            Data = statusEffect,
            StackCount = Mathf.Max(stackCount, 1)
        });
    }

    private StatusEffectApplication[] BuildStatusEffects()
    {
        StatusEffectApplication[] dataEffects = StatusEffectApplication.FromData(weaponData.onHitStatusEffects);
        int dataCount = dataEffects != null ? dataEffects.Length : 0;
        int runtimeCount = runtimeStatusEffects.Count;
        if (dataCount == 0 && runtimeCount == 0)
        {
            return null;
        }

        StatusEffectApplication[] results = new StatusEffectApplication[dataCount + runtimeCount];
        int index = 0;

        for (int i = 0; i < dataCount; i++)
        {
            results[index++] = dataEffects[i];
        }

        for (int i = 0; i < runtimeCount; i++)
        {
            results[index++] = runtimeStatusEffects[i];
        }

        return results;
    }

    protected abstract void Attack();
}
