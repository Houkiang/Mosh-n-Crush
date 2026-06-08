using UnityEngine;

[CreateAssetMenu(menuName = "Game/Upgrades/Effects/Add Weapon Status Effect")]
public class AddWeaponStatusEffectUpgradeEffectSO : UpgradeEffectSO
{
    [SerializeField] private WeaponDataSO weaponData;
    [SerializeField] private StatusEffectDataSO statusEffect;
    [SerializeField] private int stackCount = 1;

    public override bool CanApply(UpgradeContext context)
    {
        return base.CanApply(context)
            && context.WeaponManager != null
            && weaponData != null
            && statusEffect != null
            && context.WeaponManager.HasWeapon(weaponData);
    }

    public override void Apply(UpgradeContext context)
    {
        context.WeaponManager?.AddWeaponStatusEffect(weaponData, statusEffect, stackCount);
    }
}
