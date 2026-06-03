using UnityEngine;

[CreateAssetMenu(menuName = "Game/Upgrades/Stat Upgrade")]
public class StatUpgradeSO : UpgradeDataSO
{
    public enum StatType
    {
        MaxHealth,
        Strength,
        Defence,
        Cooldown,
        HealingPower,
        MagicPower,
        MagicResistance,
        CritRate,
        CritDamage
    }

    [Header("Stat Settings")]
    public StatType statType;
    public float value;

    public override void Apply(Player player)
    {
        switch (statType)
        {
            case StatType.MaxHealth:
                player.IncreaseMaxHealth(value);
                break;
            case StatType.Strength:
                player.IncreaseStrength(value);
                break;
            case StatType.Defence:
                player.IncreaseDefence(value);
                break;
            case StatType.Cooldown:
                player.IncreaseCooldownReduction(value);
                break;
            case StatType.HealingPower:
                player.IncreaseHealingPower(value);
                break;
            case StatType.MagicPower:
                player.IncreaseMagicPower(value);
                break;
            case StatType.MagicResistance:
                player.IncreaseMagicResistance(value);
                break;
            case StatType.CritRate:
                player.IncreaseCritRate(value);
                break;
            case StatType.CritDamage:
                player.IncreaseCritDamage(value);
                break;
        }

        Debug.Log($"Applied stat upgrade: {upgradeName}");
    }
}
