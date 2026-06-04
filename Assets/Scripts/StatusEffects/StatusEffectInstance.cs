using UnityEngine;

public sealed class StatusEffectInstance
{
    private readonly StatusEffectDataSO data;
    private readonly GameObject source;
    private readonly object modifierSource;

    private float remainingDuration;
    private float tickTimer;
    private int stacks;
    private float remainingShield;

    public StatusEffectInstance(StatusEffectDataSO effectData, GameObject effectSource, int initialStacks = 1)
    {
        data = effectData;
        source = effectSource;
        modifierSource = this;
        remainingDuration = effectData != null ? effectData.duration : 0f;
        tickTimer = effectData != null ? effectData.tickInterval : 0f;
        stacks = Mathf.Clamp(initialStacks, 1, effectData != null ? Mathf.Max(effectData.maxStacks, 1) : 1);
        remainingShield = effectData != null ? effectData.shieldAmount * stacks : 0f;
    }

    public StatusEffectDataSO Data => data;
    public GameObject Source => source;
    public int Stacks => stacks;
    public float RemainingDuration => remainingDuration;
    public bool IsExpired => data == null || remainingDuration <= 0f || (data.effectType == StatusEffectType.Shield && remainingShield <= 0f);

    public void Reapply(int additionalStacks)
    {
        if (data == null)
        {
            return;
        }

        int appliedStacks = Mathf.Max(additionalStacks, 1);
        if (data.canStack)
        {
            int previousStacks = stacks;
            stacks = Mathf.Clamp(stacks + appliedStacks, 1, Mathf.Max(data.maxStacks, 1));
            int gainedStacks = stacks - previousStacks;
            if (data.effectType == StatusEffectType.Shield && gainedStacks > 0)
            {
                remainingShield += data.shieldAmount * gainedStacks;
            }
        }
        else if (data.effectType == StatusEffectType.Shield)
        {
            remainingShield = Mathf.Max(remainingShield, data.shieldAmount);
        }

        if (data.refreshDurationOnReapply)
        {
            remainingDuration = data.duration;
        }
    }

    public void Update(float deltaTime, StatusController controller)
    {
        if (data == null || controller == null)
        {
            return;
        }

        remainingDuration -= deltaTime;
        if (data.tickDamage <= 0f || data.tickInterval <= 0f)
        {
            return;
        }

        tickTimer -= deltaTime;
        while (tickTimer <= 0f && remainingDuration > 0f)
        {
            controller.ApplyTickDamage(this);
            tickTimer += data.tickInterval;
        }
    }

    public float AbsorbDamage(float damage)
    {
        if (data == null || data.effectType != StatusEffectType.Shield || remainingShield <= 0f || damage <= 0f)
        {
            return damage;
        }

        float absorbed = Mathf.Min(damage, remainingShield);
        remainingShield -= absorbed;
        return damage - absorbed;
    }

    public void ApplyStatModifiers(StatCollection stats)
    {
        if (data == null || stats == null || data.statModifiers == null)
        {
            return;
        }

        for (int i = 0; i < data.statModifiers.Length; i++)
        {
            StatusStatModifierData modifier = data.statModifiers[i];
            float scaledValue = modifier.value * stacks;
            stats.AddModifier(new StatModifier(modifier.statType, scaledValue, modifier.mode, modifierSource));
        }
    }

    public void RemoveStatModifiers(StatCollection stats)
    {
        if (stats == null)
        {
            return;
        }

        stats.RemoveModifiersFromSource(modifierSource);
    }
}
