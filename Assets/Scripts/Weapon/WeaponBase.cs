using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    public WeaponDataSO weaponData;

    [Header("运行时属性(勿直接修改)")]
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
            KnockbackDuration = currentKnockbackDuration
        };
    }

    protected virtual void Update()
    {
        if (playerTransform == null) return;

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
        currentKnockback *= 1 + amount;
        currentKnockbackDuration *= 1 + amount / 2f;
    }

    protected abstract void Attack();
}
