using UnityEngine;

[CreateAssetMenu(fileName = "LevelingData", menuName = "Game/Leveling Data")]
public class LevelingDataSO : ScriptableObject
{
    [Header("升级配置")]
    public AnimationCurve experienceCurve; // 曲线：X轴=等级, Y轴=所需经验
    public int maxLevel = 100;

    /// <summary>
    /// 获取升到下一级所需的经验值
    /// </summary>
    public int GetRequiredExperience(int currentLevel)
    {

        return Mathf.RoundToInt(experienceCurve.Evaluate(currentLevel));
    }
}
