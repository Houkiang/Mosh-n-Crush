using System;

public enum StatModifierMode
{
    Additive,
    Multiplicative
}

public readonly struct StatModifier
{
    public StatModifier(StatType statType, float value, StatModifierMode mode, object source = null)
    {
        StatType = statType;
        Value = value;
        Mode = mode;
        Source = source;
    }

    public StatType StatType { get; }
    public float Value { get; }
    public StatModifierMode Mode { get; }
    public object Source { get; }
}
