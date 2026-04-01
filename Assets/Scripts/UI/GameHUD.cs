using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class GameHUD : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private Player player;
    [SerializeField] private Slider expSlider;
    [SerializeField] private TextMeshProUGUI levelText; 
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI frameRateText; // 显示3帧的平均帧率
    [Header("平均帧数设置")]
    [Tooltip("每多少帧计算一次平均帧率")]
    public int frameCountForAverage = 5;
    // 私有变量
    private float timeAccumulator;     // 累计时间（秒）
    private int frameAccumulator;      // 累计帧数
    void Awake()
    {
        if (player == null)
            player = FindObjectOfType<Player>();
    }

    void Start()
    {
        if (player != null)
        {
            player.OnXpChange += UpdateExpBar;
            player.OnLevelUp += UpdateLevelText;
            GameManager.Instance.OnScoreChanged += UpdateScoreText;
        }
        ResetAccumulator();
    }

    void OnDestroy()
    {
        if (player != null)
        {
            player.OnXpChange -= UpdateExpBar;
            player.OnLevelUp -= UpdateLevelText;
            GameManager.Instance.OnScoreChanged -= UpdateScoreText;
        }
    }
    void Update()
    {
        // 累加当前帧的耗时（使用 unscaledDeltaTime 避免受时间缩放影响）
        timeAccumulator += Time.unscaledDeltaTime;
        frameAccumulator++;

        // 当累计帧数达到设定值时，计算并更新显示
        if (frameAccumulator >= frameCountForAverage)
        {
            // 计算平均帧率：总帧数 / 总时间
            float averageFPS = frameAccumulator / timeAccumulator;

            // 更新 UI 文本，保留两位小数
            if (frameRateText != null)
                frameRateText.text = $"AVGFPS: {averageFPS:F2}";

            // 重置累计器，开始下一轮统计
            ResetAccumulator();
        }
    }
    private void UpdateScoreText(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    private void ResetAccumulator()
    {
        timeAccumulator = 0f;
        frameAccumulator = 0;
    }

    private void UpdateExpBar(float progress)
    {
        if (expSlider != null)
        {
            expSlider.value = progress;
        }
    }

    private void UpdateLevelText(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"Lv.{level}";
        }
    }
}
