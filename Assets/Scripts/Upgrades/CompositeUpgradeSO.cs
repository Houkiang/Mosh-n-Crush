using UnityEngine;

[CreateAssetMenu(menuName = "Game/Upgrades/Composite Upgrade")]
public class CompositeUpgradeSO : UpgradeDataSO
{
    [Header("Effects")]
    [SerializeField] private UpgradeEffectSO[] effects;

    public override bool CanOffer(UpgradeContext context)
    {
        if (!base.CanOffer(context))
        {
            return false;
        }

        if (effects == null || effects.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < effects.Length; i++)
        {
            UpgradeEffectSO effect = effects[i];
            if (effect == null || !effect.CanApply(context))
            {
                return false;
            }
        }

        return true;
    }

    public override void Apply(UpgradeContext context)
    {
        if (effects == null)
        {
            return;
        }

        for (int i = 0; i < effects.Length; i++)
        {
            UpgradeEffectSO effect = effects[i];
            if (effect == null)
            {
                continue;
            }

            effect.Apply(context);
        }
    }
}
