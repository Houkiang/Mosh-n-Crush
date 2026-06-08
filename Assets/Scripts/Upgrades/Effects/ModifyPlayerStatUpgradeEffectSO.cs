using UnityEngine;

[CreateAssetMenu(menuName = "Game/Upgrades/Effects/Modify Player Stat")]
public class ModifyPlayerStatUpgradeEffectSO : UpgradeEffectSO
{
    [SerializeField] private StatType statType;
    [SerializeField] private float value;

    public override void Apply(UpgradeContext context)
    {
        Player player = context.Player;
        if (player == null)
        {
            return;
        }

        switch (statType)
        {
            case StatType.MaxHealth:
                player.IncreaseMaxHealth(value);
                break;
            case StatType.PhysicalAttack:
                player.IncreaseStrength(value);
                break;
            case StatType.PhysicalDefense:
                player.IncreaseDefence(value);
                break;
            case StatType.CooldownReduction:
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
    }
}
