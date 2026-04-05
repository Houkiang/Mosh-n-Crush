using System;
using System.Collections.Generic;
using UnityEngine;

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
    
    // 逻辑暂停来源（例如升级面板）
    private readonly HashSet<string> pauseReasons = new HashSet<string>();

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
        pauseReasons.Clear();
        ChangeState(GameState.Playing);
    }

    public void GameOver()
    {
        if (CurrentState == GameState.GameOver) return;
        pauseReasons.Clear();
        ChangeState(GameState.GameOver);
        Debug.Log($"游戏结束! 得分: {currentScore}");
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        killCount++;
        OnScoreChanged?.Invoke(currentScore);
    }

    public void RequestPause(string reason)
    {
        if (CurrentState == GameState.GameOver) return;
        if (string.IsNullOrWhiteSpace(reason)) reason = "Unknown";

        pauseReasons.Add(reason);
        RefreshPauseState();
    }

    public void ReleasePause(string reason)
    {
        if (CurrentState == GameState.GameOver) return;
        if (string.IsNullOrWhiteSpace(reason)) reason = "Unknown";

        pauseReasons.Remove(reason);
        RefreshPauseState();
    }

    private void RefreshPauseState()
    {
        GameState targetState = pauseReasons.Count > 0 ? GameState.Paused : GameState.Playing;
        if (CurrentState != targetState)
        {
            ChangeState(targetState);
        }
    }

    private void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);
    }
}
