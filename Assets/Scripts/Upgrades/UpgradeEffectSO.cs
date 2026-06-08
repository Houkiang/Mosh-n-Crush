using UnityEngine;

public abstract class UpgradeEffectSO : ScriptableObject
{
    public virtual bool CanApply(UpgradeContext context)
    {
        return context.IsValid;
    }

    public abstract void Apply(UpgradeContext context);
}
