using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

// 1. 抽象基类：所有增益的模板
public abstract class UpgradeDataSO : ScriptableObject
{
    [Header("UI 显示")]
    public string upgradeName;
    public Sprite icon;
    [TextArea] public string description;

    [Header("随机权重")]
    [Tooltip("权重越高，出现的概率越大。例如：普通=100，稀有=10")]
    [Range(1, 1000)]
    public int weight = 100; // 默认权重
    // 核心逻辑：应用增益
    public abstract void Apply(Player player);
}
