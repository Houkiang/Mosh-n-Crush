using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    private const string PauseReasonUpgrade = "UpgradeSelection";

    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private UpgradeCardUI[] cards;

    [Header("Upgrade Pool")]
    [SerializeField] private List<UpgradeDataSO> allUpgrades;

    private readonly Dictionary<UpgradeDataSO, int> selectedUpgradeCounts = new Dictionary<UpgradeDataSO, int>();

    private void Start()
    {
        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }

        if (weaponManager == null)
        {
            weaponManager = FindObjectOfType<WeaponManager>();
        }

        player.OnLevelUp += HandleLevelUp;
        upgradePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.OnLevelUp -= HandleLevelUp;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReleasePause(PauseReasonUpgrade);
        }
    }

    private void HandleLevelUp(int level)
    {
        List<UpgradeDataSO> choices = GetRandomUpgrades(3);
        if (choices.Count == 0)
        {
            if (upgradePanel != null)
            {
                upgradePanel.SetActive(false);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReleasePause(PauseReasonUpgrade);
            }

            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RequestPause(PauseReasonUpgrade);
        }

        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
        }

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
        if (upgrade == null)
        {
            return;
        }

        UpgradeContext context = BuildContext();
        if (!upgrade.CanOffer(context))
        {
            return;
        }

        upgrade.Apply(context);
        RegisterUpgradePick(upgrade);

        upgradePanel.SetActive(false);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReleasePause(PauseReasonUpgrade);
        }
    }

    private List<UpgradeDataSO> GetRandomUpgrades(int count)
    {
        UpgradeContext context = BuildContext();
        List<UpgradeDataSO> validPool = new List<UpgradeDataSO>();

        for (int i = 0; i < allUpgrades.Count; i++)
        {
            UpgradeDataSO upgrade = allUpgrades[i];
            if (upgrade == null || !upgrade.CanOffer(context))
            {
                continue;
            }

            validPool.Add(upgrade);
        }

        List<UpgradeDataSO> results = new List<UpgradeDataSO>();
        count = Mathf.Min(count, validPool.Count);

        for (int i = 0; i < count; i++)
        {
            if (validPool.Count == 0)
            {
                break;
            }

            float totalWeight = 0f;
            for (int j = 0; j < validPool.Count; j++)
            {
                totalWeight += validPool[j].weight;
            }

            float randomPoint = Random.Range(0f, totalWeight);
            UpgradeDataSO selectedItem = null;

            for (int j = 0; j < validPool.Count; j++)
            {
                UpgradeDataSO candidate = validPool[j];
                randomPoint -= candidate.weight;
                if (randomPoint <= 0f)
                {
                    selectedItem = candidate;
                    break;
                }
            }

            if (selectedItem == null)
            {
                selectedItem = validPool[validPool.Count - 1];
            }

            results.Add(selectedItem);
            validPool.Remove(selectedItem);
        }

        return results;
    }

    private UpgradeContext BuildContext()
    {
        return new UpgradeContext(player, weaponManager, selectedUpgradeCounts);
    }

    private void RegisterUpgradePick(UpgradeDataSO upgrade)
    {
        if (upgrade == null)
        {
            return;
        }

        selectedUpgradeCounts.TryGetValue(upgrade, out int currentCount);
        selectedUpgradeCounts[upgrade] = currentCount + 1;
    }
}
