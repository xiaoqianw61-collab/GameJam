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
    }
    [LabelText("关卡配置")]
    public LevelConfig config;

    [LabelText("分数")]
    public TextMeshProUGUI scoreText;

    public event Action<int> OnGetScore;
    public event Action<int> OnReduceScore;

    private int _score;
    private int _usedAnchorCount;

    private void Awake()
    {
        Instance = this;
        scoreText.text = "分数: 0";
        UIUtil.InitUtil();
    }

    /// <summary>
    /// 增加分数
    /// </summary>
    public void AddScore(int point)
    {
        _score += point;
        scoreText.text = $"分数: {_score}";
        OnGetScore?.Invoke(point);
    }
    /// <summary>
    /// 扣除分数
    /// </summary>
    public void ReduceScore(int point)
    {
        _score -= point;
        scoreText.text = $"分数: {_score}";
        OnReduceScore?.Invoke(point);
    }

    /// <summary>
    /// 开始
    /// </summary>
    public void SetGameStart()
    {
        Hero.Instance.BeginFly();
    }
    /// <summary>
    /// 结束
    /// </summary>
    public void SetGameOver()
    {
        Hero.Instance.StopFly();
        Debug.Log("撞墙了");
    }
    /// <summary>
    /// 结束
    /// </summary>
    public void SetGamePass()
    {
        LevelManager.Instance.Pass();
    }
}