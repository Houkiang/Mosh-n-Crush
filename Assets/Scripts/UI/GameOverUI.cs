using UnityEngine;
using UnityEngine.SceneManagement; 

public class GameOverUI : MonoBehaviour
{
    [Header("UI 组件")]
    [SerializeField] private GameObject gameOverPanel;

    void OnEnable()
    {
        // 订阅玩家死亡事件
        Player.OnPlayerDied += HandlePlayerDeath;
    }

    void OnDisable()
    {
        // 取消订阅，防止内存泄漏
        Player.OnPlayerDied -= HandlePlayerDeath;
    }

    private void HandlePlayerDeath()
    {
        // 1. 显示失败界面
        gameOverPanel.SetActive(true);

        // 2. 暂停游戏时间 
        Time.timeScale = 0f;
    }

    // 绑定到按钮的 OnClick 事件上
    public void OnRestartButtonClicked()
    {
        // 1. 恢复时间流速 
        Time.timeScale = 1f;

        // 2. 重载当前场景
        // 获取当前活动场景的名字并重新加载
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
