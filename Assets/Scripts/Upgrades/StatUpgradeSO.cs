
using UnityEngine;

// 2. 属性增强类：处理数值
[CreateAssetMenu(menuName = "Game/Upgrades/Stat Upgrade")]
public class StatUpgradeSO : UpgradeDataSO
{
    public enum StatType { MaxHealth, Strength, Defence, Cooldown, HealingPower }
    
    [Header("属性设置")]
    public StatType statType;
    public float value; // 增加的数值

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
        }
        Debug.Log($"应用属性增益: {upgradeName}");
    }
}
