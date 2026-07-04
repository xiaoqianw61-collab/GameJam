using System;
using Sirenix.OdinInspector;
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

    public event Action<int> OnGetScore;
    public event Action<int> OnReduceScore;

    private int _score;
    private int _usedAnchorCount;

    private void Awake()
    {
        Instance = this;
        UIUtil.InitUtil();
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

    public void SetGameOver()
    {
        Debug.Log("撞墙了");
    }
}