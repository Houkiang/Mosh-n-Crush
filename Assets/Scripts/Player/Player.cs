using System;
using UnityEngine;

public class Player : MonoBehaviour, ICombatant
{
    [Header("配置引用")]
    [SerializeField] private LevelingDataSO levelingData;

    [Header("属性设置")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float defence = 5f;
    [SerializeField] private float magicResistance = 0f;
    [SerializeField] private float strength = 10f;
    [SerializeField] private float healingPower = 0.5f;
    [SerializeField] private float cooldownReduction = 0f;
    [SerializeField] private bool isInvincible = false;

    [Header("经验与等级")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentExperience = 0;
    [SerializeField] private int requiredExperience = 0;

    private float healingTimer = 0f;
    private float healingInterval = 1f;

    public Transform CombatTransform => transform;
    public bool IsAlive => gameObject.activeSelf && currentHealth > 0f;
    public int CurrentLevel => currentLevel;
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float HealingPower => healingPower;
    public float Defence => defence;
    public float MagicResistance => magicResistance;
    public float Strength => strength;
    public float CooldownReduction => cooldownReduction;

    public Action<float, float> OnHealthChange;
    public Action<int> OnLevelUp;
    public Action<float> OnXpChange;
    public static event Action OnPlayerDied;

    public void GainExperience(int amount)
    {
        currentExperience += amount;
        CheckLevelUp();

        float xpProgress = requiredExperience > 0
            ? (float)currentExperience / requiredExperience
            : 0f;
        OnXpChange?.Invoke(xpProgress);
    }

    private void CheckLevelUp()
    {
        while (currentExperience >= requiredExperience)
        {
            currentExperience -= requiredExperience;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentLevel++;

        if (levelingData != null)
        {
            requiredExperience = levelingData.GetRequiredExperience(currentLevel);
        }
        else
        {
            requiredExperience = Mathf.CeilToInt(requiredExperience * 1.2f);
        }

        OnLevelUp?.Invoke(currentLevel);
    }

    public void IncreaseMaxHealth(float amount)
    {
        maxHealth += amount;
        currentHealth += amount;
        OnHealthChange?.Invoke(currentHealth, maxHealth);
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

    public void IncreaseMagicResistance(float amount)
    {
        magicResistance += amount;
    }

    public void IncreaseCooldownReduction(float amount)
    {
        cooldownReduction += amount;
        cooldownReduction = Mathf.Clamp(cooldownReduction, 0f, 0.9f);
    }

    public void TakeDamage(float damage)
    {
        ReceiveDamage(new DamageContext
        {
            Source = null,
            Target = gameObject,
            BaseDamage = damage,
            DamageType = DamageType.Physical,
            CanCrit = false,
            CritRate = 0f,
            CritMultiplier = 1f
        });
    }

    public DamageResult ReceiveDamage(DamageContext context)
    {
        DamageResult result = CombatResolver.ResolveDamage(context, defence, magicResistance, isInvincible);
        if (result.FinalDamage <= 0f)
        {
            return result;
        }

        currentHealth -= result.FinalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHealthChange?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
            result.TargetDied = true;
        }

        return result;
    }

    public void TakeKnockback(Vector3 sourcePosition, float force, float stunDuration)
    {
    }

    private void Die()
    {
        Debug.Log("玩家死亡！游戏结束");
        OnPlayerDied?.Invoke();
        gameObject.SetActive(false);
    }

    void Start()
    {
        requiredExperience = levelingData.GetRequiredExperience(currentLevel);
        OnHealthChange?.Invoke(currentHealth, maxHealth);
        OnXpChange?.Invoke(0f);
    }

    void Update()
    {
        healingTimer -= Time.deltaTime;
        if (healingTimer <= 0f)
        {
            healingTimer = healingInterval;
            if (currentHealth < maxHealth)
            {
                currentHealth = Math.Min(currentHealth + healingPower, maxHealth);
                OnHealthChange?.Invoke(currentHealth, maxHealth);
            }
        }
    }
}
