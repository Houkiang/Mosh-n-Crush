using System.Collections.Generic;
using UnityEngine;

public readonly struct UpgradeContext
{
    public UpgradeContext(
        Player player,
        WeaponManager weaponManager,
        IReadOnlyDictionary<UpgradeDataSO, int> selectedUpgradeCounts)
    {
        Player = player;
        WeaponManager = weaponManager;
        SelectedUpgradeCounts = selectedUpgradeCounts;
    }

    public Player Player { get; }
    public WeaponManager WeaponManager { get; }
    public IReadOnlyDictionary<UpgradeDataSO, int> SelectedUpgradeCounts { get; }
    public bool IsValid => Player != null;

    public int GetPickCount(UpgradeDataSO upgrade)
    {
        if (upgrade == null || SelectedUpgradeCounts == null)
        {
            return 0;
        }

        return SelectedUpgradeCounts.TryGetValue(upgrade, out int count) ? count : 0;
    }

    public bool HasReachedPickLimit(UpgradeDataSO upgrade, int maxPickCount)
    {
        if (upgrade == null || maxPickCount <= 0)
        {
            return false;
        }

        return GetPickCount(upgrade) >= maxPickCount;
    }
}
