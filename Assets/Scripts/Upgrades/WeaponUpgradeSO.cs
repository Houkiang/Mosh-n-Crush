using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
// 4.武器升级数据
[CreateAssetMenu(menuName = "Game/Upgrades/Weapon Upgrade")]
public class WeaponUpgradeSO : UpgradeDataSO
{
    public enum UpgradeType { Damage, FireRate,Count,Knockback }
    [Header("武器升级数据")]
    public UpgradeType upgradeType;
    public WeaponDataSO weaponDataSO;
    public float value; 



    public override void Apply(Player player)
    {
        switch (upgradeType)
        {
            case UpgradeType.Damage:
                player.GetComponentInChildren<WeaponManager>()?.UpgradeWeaponDamage(weaponDataSO, value);
                break;
            case UpgradeType.FireRate:
                player.GetComponentInChildren<WeaponManager>()?.UpgradeWeaponFireRate(weaponDataSO, value);
                break;
            case UpgradeType.Count:
                player.GetComponentInChildren<WeaponManager>()?.UpgradeWeaponCount(weaponDataSO, value);
                break;
            case UpgradeType.Knockback:
                player.GetComponentInChildren<WeaponManager>()?.UpgradeWeaponKnockback(weaponDataSO, value);
                break;
            // case UpgradeType.Scale:
            //     player.GetComponentInChildren<WeaponManager>()?.UpgradeWeaponScale(weaponDataSO, value);
            //     break;
            //变大暂未实现
        }
        Debug.Log($"应用武器增益: {upgradeName}");
    }
}
