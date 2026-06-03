using System.Collections.Generic;

public sealed class StatCollection
{
    private readonly StatBlock baseStats = new StatBlock();
    private readonly List<StatModifier> modifiers = new List<StatModifier>();

    public void SetBaseValue(StatType statType, float value)
    {
        baseStats.SetValue(statType, value);
    }

    public void AddToBaseValue(StatType statType, float value)
    {
        baseStats.AddValue(statType, value);
    }

    public float GetBaseValue(StatType statType, float fallback = 0f)
    {
        return baseStats.GetValue(statType, fallback);
    }

    public float GetValue(StatType statType, float fallback = 0f)
    {
        float baseValue = GetBaseValue(statType, fallback);
        float additive = 0f;
        float multiplicative = 1f;

        for (int i = 0; i < modifiers.Count; i++)
        {
            StatModifier modifier = modifiers[i];
            if (modifier.StatType != statType)
            {
                continue;
            }

            if (modifier.Mode == StatModifierMode.Additive)
            {
                additive += modifier.Value;
            }
            else
            {
                multiplicative *= 1f + modifier.Value;
            }
        }

        return (baseValue + additive) * multiplicative;
    }

    public void AddModifier(StatModifier modifier)
    {
        modifiers.Add(modifier);
    }

    public void RemoveModifiersFromSource(object source)
    {
        if (source == null)
        {
            return;
        }

        modifiers.RemoveAll(modifier => modifier.Source == source);
    }

    public void ClearModifiers()
    {
        modifiers.Clear();
    }

    public void Clear()
    {
        baseStats.Clear();
        modifiers.Clear();
    }
}
