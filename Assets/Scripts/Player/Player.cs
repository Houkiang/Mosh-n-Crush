using System;
using UnityEngine;

public class Player : MonoBehaviour, ICombatant
{
    [Header("Config")]
    [SerializeField] private LevelingDataSO levelingData;

    [Header("Base Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float defence = 5f;
    [SerializeField] private float magicResistance = 0f;
    [SerializeField] private float strength = 10f;
    [SerializeField] private float magicPower = 0f;
    [SerializeField] private float healingPower = 0.5f;
    [SerializeField] private float cooldownReduction = 0f;
    [SerializeField] private float critRateBonus = 0f;
    [SerializeField] private float critDamageBonus = 0f;
    [SerializeField] private bool isInvincible = false;

    [Header("Level")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentExperience = 0;
    [SerializeField] private int requiredExperience = 0;

    private const float HealingInterval = 1f;

    private float healingTimer = 0f;
    private StatCollection stats;
    private StatusController statusController;

    public Transform CombatTransform => transform;
    public bool IsAlive => gameObject.activeSelf && currentHealth > 0f;
    public StatCollection Stats => stats;
    public int CurrentLevel => currentLevel;
    public float MaxHealth => GetStatValue(StatType.MaxHealth, maxHealth);
    public float CurrentHealth => currentHealth;
    public float HealingPower => GetStatValue(StatType.HealingPower, healingPower);
    public float Defence => GetStatValue(StatType.PhysicalDefense, defence);
    public float MagicResistance => GetStatValue(StatType.MagicResistance, magicResistance);
    public float Strength => GetStatValue(StatType.PhysicalAttack, strength);
    public float MagicPower => GetStatValue(StatType.MagicPower, magicPower);
    public float CooldownReduction => Mathf.Clamp(GetStatValue(StatType.CooldownReduction, cooldownReduction), 0f, 0.9f);
    public float CritRate => Mathf.Clamp01(GetStatValue(StatType.CritRate, critRateBonus));
    public float CritDamage => Mathf.Max(0f, GetStatValue(StatType.CritDamage, critDamageBonus));

    public Action<float, float> OnHealthChange;
    public Action<int> OnLevelUp;
    public Action<float> OnXpChange;
    public static event Action OnPlayerDied;

    private void Awake()
    {
        EnsureStatsInitialized();
        statusController = GetComponent<StatusController>();
        if (statusController == null)
        {
            statusController = gameObject.AddComponent<StatusController>();
        }

        statusController.Initialize(this);
        currentHealth = Mathf.Clamp(currentHealth, 0f, MaxHealth);
    }

    private void Start()
    {
        requiredExperience = levelingData != null
            ? levelingData.GetRequiredExperience(currentLevel)
            : Mathf.Max(requiredExperience, 1);

        OnHealthChange?.Invoke(currentHealth, MaxHealth);
        OnXpChange?.Invoke(0f);
    }

    private void Update()
    {
        healingTimer -= Time.deltaTime;
        if (healingTimer > 0f)
        {
            return;
        }

        healingTimer = HealingInterval;
        if (currentHealth >= MaxHealth)
        {
            return;
        }

        currentHealth = Mathf.Min(currentHealth + HealingPower, MaxHealth);
        OnHealthChange?.Invoke(currentHealth, MaxHealth);
    }

    public void GainExperience(int amount)
    {
        currentExperience += amount;
        CheckLevelUp();

        float xpProgress = requiredExperience > 0
            ? (float)currentExperience / requiredExperience
            : 0f;
        OnXpChange?.Invoke(xpProgress);
    }

    public void IncreaseMaxHealth(float amount)
    {
        EnsureStatsInitialized();

        stats.AddToBaseValue(StatType.MaxHealth, amount);
        SyncSerializedStatsFromCollection();

        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, MaxHealth);
        OnHealthChange?.Invoke(currentHealth, MaxHealth);
    }

    public void IncreaseHealingPower(float amount)
    {
        AddToBaseStat(StatType.HealingPower, amount);
    }

    public void IncreaseStrength(float amount)
    {
        AddToBaseStat(StatType.PhysicalAttack, amount);
    }

    public void IncreaseMagicPower(float amount)
    {
        AddToBaseStat(StatType.MagicPower, amount);
    }

    public void IncreaseDefence(float amount)
    {
        AddToBaseStat(StatType.PhysicalDefense, amount);
    }

    public void IncreaseMagicResistance(float amount)
    {
        AddToBaseStat(StatType.MagicResistance, amount);
    }

    public void IncreaseCooldownReduction(float amount)
    {
        EnsureStatsInitialized();

        float nextValue = Mathf.Clamp(
            stats.GetBaseValue(StatType.CooldownReduction, cooldownReduction) + amount,
            0f,
            0.9f);

        stats.SetBaseValue(StatType.CooldownReduction, nextValue);
        SyncSerializedStatsFromCollection();
    }

    public void IncreaseCritRate(float amount)
    {
        EnsureStatsInitialized();

        float nextValue = Mathf.Clamp01(
            stats.GetBaseValue(StatType.CritRate, critRateBonus) + amount);

        stats.SetBaseValue(StatType.CritRate, nextValue);
        SyncSerializedStatsFromCollection();
    }

    public void IncreaseCritDamage(float amount)
    {
        AddToBaseStat(StatType.CritDamage, amount);
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
            CritMultiplier = 1f,
            StatusEffects = null
        });
    }

    public DamageResult ReceiveDamage(DamageContext context)
    {
        EnsureStatsInitialized();

        DamageResult result = CombatResolver.ResolveDamage(context, stats, isInvincible);
        if (result.FinalDamage <= 0f)
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

        currentHealth = Mathf.Clamp(currentHealth - result.FinalDamage, 0f, MaxHealth);
        OnHealthChange?.Invoke(currentHealth, MaxHealth);

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
    }

    private void CheckLevelUp()
    {
        if (requiredExperience <= 0)
        {
            return;
        }

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
            requiredExperience = Mathf.CeilToInt(Mathf.Max(requiredExperience, 1) * 1.2f);
        }

        OnLevelUp?.Invoke(currentLevel);
    }

    private void Die()
    {
        Debug.Log("Player died.");
        OnPlayerDied?.Invoke();
        gameObject.SetActive(false);
    }

    private void EnsureStatsInitialized()
    {
        if (stats != null)
        {
            return;
        }

        stats = new StatCollection();
        stats.SetBaseValue(StatType.MaxHealth, maxHealth);
        stats.SetBaseValue(StatType.PhysicalDefense, defence);
        stats.SetBaseValue(StatType.MagicResistance, magicResistance);
        stats.SetBaseValue(StatType.PhysicalAttack, strength);
        stats.SetBaseValue(StatType.MagicPower, magicPower);
        stats.SetBaseValue(StatType.HealingPower, healingPower);
        stats.SetBaseValue(StatType.CooldownReduction, Mathf.Clamp(cooldownReduction, 0f, 0.9f));
        stats.SetBaseValue(StatType.CritRate, Mathf.Clamp01(critRateBonus));
        stats.SetBaseValue(StatType.CritDamage, Mathf.Max(0f, critDamageBonus));

        SyncSerializedStatsFromCollection();
    }

    private void AddToBaseStat(StatType statType, float amount)
    {
        EnsureStatsInitialized();
        stats.AddToBaseValue(statType, amount);
        SyncSerializedStatsFromCollection();
    }

    private float GetStatValue(StatType statType, float fallback = 0f)
    {
        EnsureStatsInitialized();
        return stats.GetValue(statType, fallback);
    }

    private void SyncSerializedStatsFromCollection()
    {
        maxHealth = stats.GetValue(StatType.MaxHealth, maxHealth);
        defence = stats.GetValue(StatType.PhysicalDefense, defence);
        magicResistance = stats.GetValue(StatType.MagicResistance, magicResistance);
        strength = stats.GetValue(StatType.PhysicalAttack, strength);
        magicPower = stats.GetValue(StatType.MagicPower, magicPower);
        healingPower = stats.GetValue(StatType.HealingPower, healingPower);
        cooldownReduction = Mathf.Clamp(stats.GetValue(StatType.CooldownReduction, cooldownReduction), 0f, 0.9f);
        critRateBonus = Mathf.Clamp01(stats.GetValue(StatType.CritRate, critRateBonus));
        critDamageBonus = Mathf.Max(0f, stats.GetValue(StatType.CritDamage, critDamageBonus));
    }
}
