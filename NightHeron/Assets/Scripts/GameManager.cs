using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏管理器 - 夜鹭便便大作战
/// </summary>
[DefaultExecutionOrder(-999)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Serializable]
    public class LevelConfig
    {
        [LabelText("限制锚点数")]
        public int anchorCount;
    }
    [LabelText("关卡配置")]
    public LevelConfig Config;

    public event Action OnAnchorChanged;
    public event Action<int> OnGetScore;

    private int _score;
    private int _usedAnchorCount;

    private void Awake()
    {
        
    }

    /// <summary>
    /// 剩余锚点数量
    /// </summary>
    public int GetRemainingAnchorCount()
    {
        return Mathf.Max(Config.anchorCount - _usedAnchorCount, 0);
    }

    /// <summary>
    /// 可否增加锚点
    /// </summary>
    public bool CanAddAnchor()
    {
        return _usedAnchorCount < Config.anchorCount;
    }
    /// <summary>
    /// 增加锚点
    /// </summary>
    public bool AddAnchor()
    {
        if (!CanAddAnchor()) return false;
        _usedAnchorCount++;
        OnAnchorChanged?.Invoke();
        return true;
    }
    /// <summary>
    /// 移除锚点
    /// </summary>
    public void DeleteAnchor()
    {
        if (_usedAnchorCount == 0) return;
        _usedAnchorCount--;
        OnAnchorChanged?.Invoke();
    }

    /// <summary>
    /// 增加分数
    /// </summary>
    public void AddScore(int point)
    {
        _score += point;
        OnGetScore?.Invoke(point);
    }
}
