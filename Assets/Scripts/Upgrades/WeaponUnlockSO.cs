using UnityEngine;

[CreateAssetMenu(menuName = "Game/Upgrades/Weapon Unlock")]
public class WeaponUnlockSO : UpgradeDataSO
{
    [Header("Weapon Data")]
    public WeaponDataSO weaponDataSO;

    public override bool CanOffer(UpgradeContext context)
    {
        return base.CanOffer(context)
            && context.WeaponManager != null
            && weaponDataSO != null
            && !context.WeaponManager.HasWeapon(weaponDataSO);
    }

    public override void Apply(UpgradeContext context)
    {
        context.WeaponManager?.AddWeapon(weaponDataSO);
    }
}
