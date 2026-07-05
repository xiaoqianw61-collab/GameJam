using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Sirenix.OdinInspector;

/// <summary>
/// 关卡管理器：管理关卡元信息、解锁/通关状态、场景加载、进度持久化。
/// 跨场景持久化（DontDestroyOnLoad），由 RuntimeBootstrap 自动创建。
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [LabelText("当前关卡索引")]
    public int CurrentLevelIndex;
    [LabelText("最高记录")]
    public int LevelRecord
    {
        get => _levelRecord;
        set
        {
            _levelRecord = value;
            PlayerPrefs.SetInt("LEVEL_RECORD", value);
        }
    }
    private int _levelRecord;
    [LabelText("最高关卡")]
    public int MaxLevel = 6;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this);
        }
    }

    /// <summary>
    /// 是否已经通关全部
    /// </summary>
    public bool IsCompleteAll()
    {
        return LevelRecord >= MaxLevel;
    }
    /// <summary>
    /// 关卡是否已解锁
    /// </summary>
    public bool IsLevelUnlocked(int level)
    {
        return level - 1 <= LevelRecord;
    }
    
    /// <summary>
    /// 加载关卡
    /// </summary>
    /// <param name="level"></param>
    public void LoadLevelScene(int level)
    {
        CurrentLevelIndex = level;
        SceneManager.LoadScene(level);
    }
    /// <summary>
    /// 重来
    /// </summary>
    public void Restart()
    {
        SceneManager.LoadScene(CurrentLevelIndex);
    }
    /// <summary>
    /// 返回主菜单
    /// </summary>
    public void ReturnToMenu()
    {
        SceneManager.LoadScene(0);
    }

    /// <summary>
    /// 加载下一关
    /// </summary>
    public void LoadNextLevel()
    {
        LoadLevelScene(CurrentLevelIndex + 1);
    }
    /// <summary>
    /// 加载下一关
    /// </summary>
    public void LevelPass()
    {
        LevelRecord++;
    }
}
