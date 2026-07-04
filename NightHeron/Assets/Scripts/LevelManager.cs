using UnityEngine;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// 关卡管理器：管理关卡元信息、解锁/通关状态、场景加载、进度持久化。
/// 跨场景持久化（DontDestroyOnLoad），由 RuntimeBootstrap 自动创建。
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public int CurrentLevelIndex;
    public int LevelRecord;

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

    public bool IsLevelUnlocked(int level)
    {
        return level - 1 <= LevelRecord;
    }
    public void LoadLevelScene(int level)
    {
        CurrentLevelIndex = level;
        SceneManager.LoadScene(level);
    }
    public void ReturnToMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void Pass()
    {
        LevelRecord++;
        SceneManager.LoadScene(CurrentLevelIndex + 1);
    }
}
