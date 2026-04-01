
using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class UpgradeCardUI : MonoBehaviour
{
    [Header("UI 组件")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private Button selectButton;

    private UpgradeDataSO currentData;
    private UpgradeManager manager;

    public void Setup(UpgradeDataSO data, UpgradeManager mgr)
    {
        currentData = data;
        manager = mgr;

        iconImage.sprite = data.icon;
        nameText.text = data.upgradeName;
        descText.text = data.description;
        
        // 清除旧的监听并添加新的
        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnSelect);
    }

    private void OnSelect()
    {
        manager.SelectUpgrade(currentData);
    }
}
