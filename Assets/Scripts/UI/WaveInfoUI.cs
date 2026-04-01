using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class WaveInfoUI : MonoBehaviour
{
    [Header("UI 组件引用")]
    [SerializeField] private Slider timerSlider;      // 普通波次的时间条
    [SerializeField] private Slider bossHealthSlider; // Boss波次的血条
    [SerializeField] private TextMeshProUGUI waveText; // 显示 "Wave 1/5"
    [SerializeField] private GameObject bossIcon;     

    private Enemy currentBoss; // 持有当前Boss的引用以监听血量

    void Start()
    {
        // 初始化 UI 状态
        timerSlider.gameObject.SetActive(true);
        bossHealthSlider.gameObject.SetActive(false);
        if(bossIcon) bossIcon.SetActive(false);
        
        UpdateWaveText();

        // 订阅 Spawner 事件
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.OnWaveChanged += HandleWaveChanged;
            EnemySpawner.Instance.OnBossSpawned += HandleBossSpawned;
        }
    }

    void OnDestroy()
    {
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.OnWaveChanged -= HandleWaveChanged;
            EnemySpawner.Instance.OnBossSpawned -= HandleBossSpawned;
        }
        
        // 清理 Boss 事件订阅
        if (currentBoss != null)
        {
            currentBoss.OnHealthChanged -= UpdateBossHealthUI;
        }
    }

    void Update()
    {
        if (EnemySpawner.Instance == null) return;

        // 如果是普通波次，每帧更新时间条
        if (EnemySpawner.Instance.CurrentWaveType == WaveType.Normal)
        {
            // 随着时间填满 (0 -> 1)
            timerSlider.value = EnemySpawner.Instance.GetNormalWaveProgress();
        }
    }

    // --- 事件回调 ---

    private void HandleWaveChanged(int newWaveIndex)
    {
        UpdateWaveText();

        // 切换回普通波次模式
        WaveType type = EnemySpawner.Instance.CurrentWaveType;
        
        if (type == WaveType.Normal)
        {
            timerSlider.gameObject.SetActive(true);
            bossHealthSlider.gameObject.SetActive(false);
            if(bossIcon) bossIcon.SetActive(false);
            timerSlider.value = 0;
        }

    }

    private void HandleBossSpawned(Enemy boss)
    {
        // 1. 切换 UI 显示
        timerSlider.gameObject.SetActive(false);
        bossHealthSlider.gameObject.SetActive(true);
        if(bossIcon) bossIcon.SetActive(true);

        // 2. 初始化 Boss 血条
        bossHealthSlider.value = 1f; // 满血

        // 3. 记录 Boss 并订阅血量变化
        // 如果有旧的 Boss 引用，先取消订阅
        if (currentBoss != null)
        {
            currentBoss.OnHealthChanged -= UpdateBossHealthUI;
        }

        currentBoss = boss;
        currentBoss.OnHealthChanged += UpdateBossHealthUI;
    }

    private void UpdateBossHealthUI(float current, float max)
    {
        bossHealthSlider.value = current / max;
        
        // 如果 Boss 死了，可以在这里做一些 UI 效果，

    }

    private void UpdateWaveText()
    {
        if (EnemySpawner.Instance != null)
        {
            waveText.text = $"WAVE {EnemySpawner.Instance.CurrentWaveNumber} / {EnemySpawner.Instance.TotalWaves}";
        }
    }
}
