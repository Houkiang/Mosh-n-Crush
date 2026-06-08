using UnityEngine;

[CreateAssetMenu(menuName = "Game/Upgrades/Weapon Upgrade")]
public class WeaponUpgradeSO : UpgradeDataSO
{
    public enum UpgradeType
    {
        Damage,
        FireRate,
        Count,
        Knockback
    }

    [Header("Legacy Weapon Upgrade")]
    public UpgradeType upgradeType;
    public WeaponDataSO weaponDataSO;
    public float value;

    public override bool CanOffer(UpgradeContext context)
    {
        return base.CanOffer(context)
            && context.WeaponManager != null
            && weaponDataSO != null
            && context.WeaponManager.HasWeapon(weaponDataSO);
    }

    public override void Apply(UpgradeContext context)
    {
        WeaponManager weaponManager = context.WeaponManager;
        if (weaponManager == null)
        {
            return;
        }

        switch (upgradeType)
        {
            case UpgradeType.Damage:
                weaponManager.UpgradeWeaponDamage(weaponDataSO, value);
                break;
            case UpgradeType.FireRate:
                weaponManager.UpgradeWeaponFireRate(weaponDataSO, value);
                break;
            case UpgradeType.Count:
                weaponManager.UpgradeWeaponCount(weaponDataSO, value);
                break;
            case UpgradeType.Knockback:
                weaponManager.UpgradeWeaponKnockback(weaponDataSO, value);
                break;
        }
    }
}
