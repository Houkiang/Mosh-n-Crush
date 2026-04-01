using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class Player : MonoBehaviour,IDamageable
{
        [Header("配置引用")]
    [SerializeField] private LevelingDataSO levelingData;
        [Header("属性设置")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float defence = 5f;
    [SerializeField] private float strength = 10f;
    [SerializeField] private float healingPower = 0.5f;
    [SerializeField] private float cooldownReduction = 0f;
    [SerializeField] private bool isInvincible = false;
        [Header("经验与等级")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentExperience = 0;
    [SerializeField] private int requiredExperience=0;

    private float healingTimer = 0f;
    private float healingInterval = 1f; // 每秒回血一次
    public int CurrentLevel => currentLevel;
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float HealingPower => healingPower;
    public float Defence => defence;        
    public float Strength => strength;
    public float CooldownReduction => cooldownReduction;

    
    public Action<float, float> OnHealthChange; // 参数是当前血量和最大血量
    // 升级事件
    public Action<int> OnLevelUp; 
    public Action<float> OnXpChange; // 参数是经验百分比 0-1，用于UI进度条
    public static event Action OnPlayerDied;

        // --- 经验系统 ---

    public void GainExperience(int amount)
    {
        currentExperience += amount;
        
        // 检查是否升级
        CheckLevelUp();

        // 更新UI
        float xpProgress = (float)currentExperience / requiredExperience;
        OnXpChange?.Invoke(xpProgress);
    }

    private void CheckLevelUp()
    {
        //Debug.Log($"当前经验: {currentExperience} / {requiredExperience}");
        while (currentExperience >= requiredExperience)
        {
            currentExperience -= requiredExperience;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentLevel++;
        
        // 获取下一级所需经验
        if (levelingData != null)
        {
            requiredExperience = levelingData.GetRequiredExperience(currentLevel);
        }
        else
        {
            requiredExperience = Mathf.CeilToInt(requiredExperience * 1.2f); // 默认保底逻辑
        }

        //Debug.Log($"升级了！当前等级: {currentLevel}");
        
        // 触发事件 
        OnLevelUp?.Invoke(currentLevel);
        

    }
    
    public void IncreaseMaxHealth(float amount)
    {
        maxHealth += amount;
        currentHealth += amount; // 增加上限同时也回血
        OnHealthChange?.Invoke(currentHealth, maxHealth); // 更新血条
    }
    public void IncreaseHealingPower(float amount)
    {
        healingPower += amount;
    }
    public void IncreaseStrength(float amount)
    {
        strength += amount;
    }

    public void IncreaseDefence(float amount)
    {
        defence += amount;
    }

    public void IncreaseCooldownReduction(float amount)
    {
        cooldownReduction += amount;
        cooldownReduction = Mathf.Clamp(cooldownReduction, 0f, 0.9f); // 最大90%冷却缩减
    }

    public void TakeDamage(float damage)
    {
        // 如果处于无敌状态，直接忽略伤害
        if (isInvincible) return;

        float effectiveDamage = Mathf.Max(damage - defence, 1);
        currentHealth -= effectiveDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        //Debug.Log($"玩家受到 {effectiveDamage} 伤害");
        OnHealthChange?.Invoke(currentHealth, maxHealth);
        if (currentHealth <= 0)
        {
            Die();
        }

    }

    // --- 接口实现：受到击退 ---
    public void TakeKnockback(Vector3 sourcePosition, float force, float stunDuration)
    {
        // 玩家暂时不处理击退效果
    }

    private void Die()
    {
        Debug.Log("玩家死亡！游戏结束");
        // 弹出结算界面
        OnPlayerDied?.Invoke();
        gameObject.SetActive(false); 
    }



    // Start is called before the first frame update
    void Start()
    {
        requiredExperience=levelingData.GetRequiredExperience(currentLevel);
        OnHealthChange?.Invoke(currentHealth, maxHealth);
        // 初始化经验条
        OnXpChange?.Invoke(0f);

    }

    // Update is called once per frame
    void Update()
    {
        healingTimer -=Time.deltaTime;
        if(healingTimer<=0f)
        {
            healingTimer=healingInterval;
            if(currentHealth<maxHealth)
            {
                currentHealth=Math.Min(currentHealth+healingPower,maxHealth);
                OnHealthChange?.Invoke(currentHealth, maxHealth);
            }
        }

    }
}
