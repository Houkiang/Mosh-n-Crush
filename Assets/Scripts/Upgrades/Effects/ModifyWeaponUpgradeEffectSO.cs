using UnityEngine;

public enum WeaponUpgradeStat
{
    Damage,
    Cooldown,
    Count,
    Knockback
}

[CreateAssetMenu(menuName = "Game/Upgrades/Effects/Modify Weapon")]
public class ModifyWeaponUpgradeEffectSO : UpgradeEffectSO
{
    [SerializeField] private WeaponDataSO weaponData;
    [SerializeField] private WeaponUpgradeStat stat;
    [SerializeField] private float value;

    public override bool CanApply(UpgradeContext context)
    {
        return base.CanApply(context)
            && context.WeaponManager != null
            && weaponData != null
            && context.WeaponManager.HasWeapon(weaponData);
    }

    public override void Apply(UpgradeContext context)
    {
        WeaponManager weaponManager = context.WeaponManager;
        if (weaponManager == null)
        {
            return;
        }

        switch (stat)
        {
            case WeaponUpgradeStat.Damage:
                weaponManager.UpgradeWeaponDamage(weaponData, value);
                break;
            case WeaponUpgradeStat.Cooldown:
                weaponManager.UpgradeWeaponFireRate(weaponData, value);
                break;
            case WeaponUpgradeStat.Count:
                weaponManager.UpgradeWeaponCount(weaponData, value);
                break;
            case WeaponUpgradeStat.Knockback:
                weaponManager.UpgradeWeaponKnockback(weaponData, value);
                break;
        }
    }
}
