using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    public WeaponDataSO weaponData; // 当前武器的数据引用

    // 运行时属性 (因为游戏里可能有升级系统改变这些值，所以不直接用 SO 的值)
    [Header("运行时属性,勿修改")]
    [SerializeField] protected float currentDamage;
    [SerializeField] protected float currentCooldown;
    [SerializeField] protected float currentRange;
    [SerializeField] protected float currentKnockback;
    [SerializeField] protected float currentKnockbackDuration;
    [SerializeField] protected int currentWeaponCount;
    [SerializeField] protected int weaponCount=1 ; // 初始数量
    protected float cooldownTimer;
    protected Transform playerTransform;
    protected int enemyLayerMask; // 缓存层级掩码，提高性能
    protected WeaponManager weaponManager;
    protected Player playerStats;

    public virtual void Initialize(WeaponDataSO data, Transform owner, WeaponManager manager)
    {
        weaponData = data;
        playerTransform = owner;
        weaponManager = manager;
        playerStats = weaponManager.player.GetComponent<Player>();
        // 初始化运行时数值
        currentDamage = data.damage;
        currentCooldown = data.cooldown;
        currentRange = data.attackRange;
        currentWeaponCount = data.weaponCount;
        currentKnockback = data.knockbackForce;
        currentKnockbackDuration = data.knockbackDuration;

        // 重置计时器
        cooldownTimer = Random.Range(0f,currentCooldown); 

        // 自动获取 Enemy 层级 
        enemyLayerMask = LayerMask.GetMask("Enemy");
    }

    protected virtual void Update()
    {
        if (playerTransform == null) return;
        // 计算实际冷却时间
        float reduction = (playerStats != null) ? playerStats.CooldownReduction : 0f;
        // 公式：原冷却 * (1 - 缩减比例)
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
        float baseDmg = currentDamage;
        float strengthBonus = 0f;

        // 安全检查
        if (playerStats != null)
        {
            strengthBonus = playerStats.Strength;
        }


        return baseDmg + strengthBonus;

    }
    public void IncreaseDamage(float amount)
    {
        currentDamage += amount;
    }
    public void ReduceCooldown(float amount)
    {
        currentCooldown = Mathf.Max(0.1f, currentCooldown - amount); // 最小冷却时间0.1秒
    }
    public virtual void IncreaseWeaponCount(int amount)
    {
        //只有ShieldWeapon会重写这个方法
    }
    public void IncreaseKnockback(float amount)
    {
        currentKnockback *= (1+amount);
        currentKnockbackDuration *= (1+amount/2);
    }

    // 子类必须实现这个
    protected abstract void Attack();
}
