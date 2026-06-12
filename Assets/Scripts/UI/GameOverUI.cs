using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("UI 组件")]
    [SerializeField] private GameObject gameOverPanel;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = EnsureCanvasGroup();
        SetPanelVisible(false);
    }

    private void OnEnable()
    {
        Player.OnPlayerDied += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        Player.OnPlayerDied -= HandlePlayerDeath;
    }

    private void HandlePlayerDeath()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }

        SetPanelVisible(true);
    }

    public void OnRestartButtonClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private CanvasGroup EnsureCanvasGroup()
    {
        GameObject target = gameOverPanel != null ? gameOverPanel : gameObject;
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = target.AddComponent<CanvasGroup>();
        }

        return group;
    }

    private void SetPanelVisible(bool visible)
    {
        if (gameOverPanel != null && !gameOverPanel.activeSelf)
        {
            gameOverPanel.SetActive(true);
        }

        if (canvasGroup == null)
        {
            canvasGroup = EnsureCanvasGroup();
        }

        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}
