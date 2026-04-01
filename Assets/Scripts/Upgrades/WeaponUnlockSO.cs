using UnityEngine;

// 3. 武器获取类：处理获得新武器
[CreateAssetMenu(menuName = "Game/Upgrades/Weapon Unlock")]
public class WeaponUnlockSO : UpgradeDataSO
{
    [Header("武器数据")]
    public WeaponDataSO weaponDataSO;


    public override void Apply(Player player)
    {
        WeaponManager weaponManager = player.GetComponentInChildren<WeaponManager>();
        if (weaponManager != null)
        {
            weaponManager.AddWeapon(weaponDataSO);
            Debug.Log($"获得新武器: {upgradeName}");
        }  
        else
        {
            Debug.LogError("WeaponManager 组件未找到，无法添加武器！");
        } 

    }
}
