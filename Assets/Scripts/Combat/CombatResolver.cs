using UnityEngine;

public static class CombatResolver
{
    public static DamageResult ResolveDamage(
        DamageContext context,
        float physicalDefense,
        float magicResistance,
        bool isInvincible = false)
    {
        DamageResult result = new DamageResult();

        if (isInvincible || context.BaseDamage <= 0f)
        {
            result.WasBlocked = isInvincible;
            return result;
        }

        float resolvedDamage = context.BaseDamage;
        bool isCritical = context.CanCrit
            && context.CritRate > 0f
            && Random.value <= context.CritRate;

        if (isCritical)
        {
            resolvedDamage *= Mathf.Max(context.CritMultiplier, 1f);
        }

        switch (context.DamageType)
        {
            case DamageType.Physical:
                resolvedDamage = Mathf.Max(resolvedDamage - physicalDefense, 1f);
                break;
            case DamageType.Magical:
                resolvedDamage = Mathf.Max(resolvedDamage - magicResistance, 1f);
                break;
            case DamageType.True:
                break;
        }

        result.FinalDamage = resolvedDamage;
        result.IsCritical = isCritical;
        return result;
    }
}
