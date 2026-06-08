using UnityEngine;

[CreateAssetMenu(menuName = "Game/Upgrades/Effects/Unlock Weapon")]
public class UnlockWeaponUpgradeEffectSO : UpgradeEffectSO
{
    [SerializeField] private WeaponDataSO weaponData;

    public override bool CanApply(UpgradeContext context)
    {
        return base.CanApply(context)
            && context.WeaponManager != null
            && weaponData != null
            && !context.WeaponManager.HasWeapon(weaponData);
    }

    public override void Apply(UpgradeContext context)
    {
        context.WeaponManager?.AddWeapon(weaponData);
    }
}
