using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    private const string PauseReasonUpgrade = "UpgradeSelection";

    [Header("引用")]
    [SerializeField] private Player player;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private GameObject upgradePanel; // 整个UI面板
    [SerializeField] private UpgradeCardUI[] cards;   // 面板里的3个卡片

    [Header("数据库")]
    [SerializeField] private List<UpgradeDataSO> allUpgrades; // 所有的增益池

    private void Start()
    {
        if (player == null) player = FindObjectOfType<Player>();
        if (weaponManager == null) weaponManager = FindObjectOfType<WeaponManager>();

        player.OnLevelUp += HandleLevelUp;
        upgradePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (player != null) player.OnLevelUp -= HandleLevelUp;
        if (GameManager.Instance != null) GameManager.Instance.ReleasePause(PauseReasonUpgrade);
    }

    private void HandleLevelUp(int level)
    {
        // 逻辑暂停（不使用全局 Time.timeScale）
        if (GameManager.Instance != null) GameManager.Instance.RequestPause(PauseReasonUpgrade);
        upgradePanel.SetActive(true);

        //  获取随机增益
        List<UpgradeDataSO> choices = GetRandomUpgrades(3);

        //  填充UI
        for (int i = 0; i < cards.Length; i++)
        {
            if (i < choices.Count)
            {
                cards[i].gameObject.SetActive(true);
                cards[i].Setup(choices[i], this);
            }
            else
            {
                cards[i].gameObject.SetActive(false);
            }
        }
    }

    public void SelectUpgrade(UpgradeDataSO upgrade)
    {
        //  应用增益
        upgrade.Apply(player);

        // 恢复逻辑
        upgradePanel.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.ReleasePause(PauseReasonUpgrade);
    }

    // 筛选并随机抽取
    private List<UpgradeDataSO> GetRandomUpgrades(int count)
    {
        // 1. 准备一个临时池子，先筛选出所有合法的增益
        List<UpgradeDataSO> validPool = new List<UpgradeDataSO>();
        
        foreach (var upgrade in allUpgrades)
        {
            // 检查武器是否已拥有
            if (upgrade is WeaponUnlockSO weaponUnlock&& weaponManager.HasWeapon(weaponUnlock.weaponDataSO))
                continue;
            if(upgrade is WeaponUpgradeSO weaponUpgradeSO&& !weaponManager.HasWeapon(weaponUpgradeSO.weaponDataSO))
                continue;
            validPool.Add(upgrade);
        }

        List<UpgradeDataSO> results = new List<UpgradeDataSO>();
        
        // 防止请求数量超过池子总数
        count = Mathf.Min(count, validPool.Count);

        // 2. 循环抽取 'count' 次
        for (int i = 0; i < count; i++)
        {
            if (validPool.Count == 0) break;


            //  计算当前池子的总权重
            float totalWeight = 0;
            foreach (var item in validPool)
            {
                totalWeight += item.weight;
            }

            //  生成随机数
            float randomPoint = UnityEngine.Random.Range(0, totalWeight);

            //  遍历寻找命中目标
            UpgradeDataSO selectedItem = null;
            
            foreach (var item in validPool)
            {
                randomPoint -= item.weight;
                if (randomPoint <= 0)
                {
                    selectedItem = item;
                    break;
                }
            }

            // 万一浮点数误差导致没选中，选最后一个
            if (selectedItem == null) selectedItem = validPool[validPool.Count - 1];



            //  添加到结果并从池子移除（确保不重复选中同一个）
            results.Add(selectedItem);
            validPool.Remove(selectedItem);
        }

        return results;
 
    }
}
