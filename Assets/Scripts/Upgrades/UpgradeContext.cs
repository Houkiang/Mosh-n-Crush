using UnityEngine;

public readonly struct UpgradeContext
{
    public UpgradeContext(Player player, WeaponManager weaponManager)
    {
        Player = player;
        WeaponManager = weaponManager;
    }

    public Player Player { get; }
    public WeaponManager WeaponManager { get; }
    public bool IsValid => Player != null;
}
