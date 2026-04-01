using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum GameState { Menu, Playing, Paused, GameOver }
    public GameState CurrentState { get; private set; }

    [Header("核心引用")]
    public Transform playerTransform; // 敌人会自动读取这个

    [Header("游戏数据")]
    public float gameTime = 0f;
    public int currentScore = 0;
    public int killCount = 0;

    // 事件通知
    public event Action<GameState> OnGameStateChanged;
    public event Action<int> OnScoreChanged;

    void Awake()
    {
        Application.targetFrameRate = 60;
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        CurrentState = GameState.Menu;
    }

    void Start()
    {
        // 自动寻找玩家 Tag (防止 Inspector 丢失引用)
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
            else Debug.LogError("GameManager: 场景中找不到 Tag 为 Player 的物体！");
        }
        
        StartGame();
    }

    void Update()
    {
        
        if (CurrentState == GameState.Playing)
        {
            gameTime += Time.deltaTime;
        }
        Debug.Log("当前分数: " + currentScore);
    }
    void OnEnable()
    {
        Enemy.OnEnemyKilled += HandleScore;
    }

    void OnDisable()
    {
        Enemy.OnEnemyKilled -= HandleScore;
    }

    private void HandleScore(Enemy enemy)
    {

        if (enemy != null) 
        {

            AddScore(enemy.ExperienceReward); 

        }
    }
    public void StartGame()
    {
        gameTime = 0f;
        currentScore = 0;
        killCount = 0;
        ChangeState(GameState.Playing);
    }

    public void GameOver()
    {
        if (CurrentState == GameState.GameOver) return;
        ChangeState(GameState.GameOver);
        Debug.Log($"游戏结束! 得分: {currentScore}");
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        killCount++;
        OnScoreChanged?.Invoke(currentScore);
    }

    private void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);
        Time.timeScale = (newState == GameState.Paused || newState == GameState.GameOver) ? 0f : 1f;
    }
}
