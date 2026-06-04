using System;
using UnityEngine;

[Serializable]
public struct StatusStatModifierData
{
    public StatType statType;
    public float value;
    public StatModifierMode mode;
}

[CreateAssetMenu(fileName = "NewStatusEffect", menuName = "Game/Status Effect")]
public class StatusEffectDataSO : ScriptableObject
{
    [Header("Identity")]
    public string effectName = "Status Effect";
    public StatusEffectType effectType = StatusEffectType.Burning;

    [Header("Duration")]
    [Min(0f)] public float duration = 3f;
    public bool canStack = false;
    [Min(1)] public int maxStacks = 1;
    public bool refreshDurationOnReapply = true;

    [Header("Tick Damage")]
    [Min(0f)] public float tickInterval = 1f;
    [Min(0f)] public float tickDamage = 0f;
    public DamageType tickDamageType = DamageType.Magical;

    [Header("Shield")]
    [Min(0f)] public float shieldAmount = 0f;

    [Header("Stat Modifiers")]
    public StatusStatModifierData[] statModifiers;
}
