using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏管理器 - 夜鹭便便大作战
/// </summary>
[DefaultExecutionOrder(-999)]
public class GameState : MonoBehaviour
{
    public static GameState Instance;
    
    [Serializable]
    public class LevelConfig
    {
        [LabelText("限制锚点数")]
        public int anchorCount;
        [LabelText("目标分数")]
        public int[] targetScores = { 2, 4, 6 };
    }
    [LabelText("关卡配置")]
    public LevelConfig config;
    
    public enum EGamePhase
    {
        Prepare,
        Playing,
        Settle,
    }
    public EGamePhase Phase => _phase;
    private EGamePhase _phase;

    /// <summary>
    /// 分数
    /// </summary>
    public int Score => _score;
    private int _score;
    
    /// <summary>
    /// 游戏结果
    /// </summary>
    public bool GameResult => _gameResult;
    private bool _gameResult;

    public event Action<int> OnGetScore;
    public event Action<int> OnReduceScore;
    
    public event Action OnGameStart;
    public event Action<bool> OnGameFinish;

    private void Awake()
    {
        Instance = this;
        UIUtil.InitUtil();
        _phase = EGamePhase.Prepare;
    }

    /// <summary>
    /// 增加分数
    /// </summary>
    public void AddScore(int point)
    {
        _score += point;
        OnGetScore?.Invoke(point);
    }
    /// <summary>
    /// 扣除分数
    /// </summary>
    public void ReduceScore(int point)
    {
        _score -= point;
        OnReduceScore?.Invoke(point);
    }

    /// <summary>
    /// 开始
    /// </summary>
    public void SetGameStart()
    {
        _phase = EGamePhase.Playing;
        Hero.Instance.BeginFly();
        OnGameStart?.Invoke();
    }
    /// <summary>
    /// 失败
    /// </summary>
    public void SetGameOver()
    {
        _phase = EGamePhase.Settle;
        _gameResult = false;
        Hero.Instance.StopFly();
        OnGameFinish?.Invoke(false);
    }
    /// <summary>
    /// 通关
    /// </summary>
    public void SetGamePass()
    {
        _phase = EGamePhase.Settle;
        _gameResult = true;
        LevelManager.Instance.LevelPass();
        OnGameFinish?.Invoke(true);
    }

    /// <summary>
    /// 计算分数
    /// </summary>
    public int CalculateStar()
    {
        for (var i = 0; i < config.targetScores.Length; i++)
        {
            var score = config.targetScores[i];
            if (_score < score)
            {
                return i;
            }
        }
        return config.targetScores.Length;
    }
}