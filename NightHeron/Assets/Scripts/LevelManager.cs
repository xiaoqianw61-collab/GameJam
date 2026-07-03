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

    [Serializable]
    public class LevelInfo
    {
        public int index;          // 关卡编号 1~6
        public string sceneName;   // 场景名 "Level1"
        public string displayName; // 显示名 "第一关"
        public bool unlocked;      // 是否已解锁
        public bool completed;     // 是否已通关
    }

    [Header("关卡定义（共 6 关）")]
    public LevelInfo[] levels = new LevelInfo[6];

    /// <summary>当前所在关卡索引，0 表示在菜单</summary>
    public int CurrentLevelIndex { get; private set; }

    /// <summary>已通关关卡数</summary>
    public int CompletedCount
    {
        get
        {
            int c = 0;
            foreach (var l in levels) if (l.completed) c++;
            return c;
        }
    }

    /// <summary>总关卡数</summary>
    public int TotalLevels => levels.Length;

    /// <summary>场景加载后事件</summary>
    public event Action<Scene, LoadSceneMode> OnSceneChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
        InitLevels();
        LoadProgress();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    // ─── 初始化 ───

    void InitLevels()
    {
        for (int i = 0; i < 6; i++)
        {
            levels[i] = new LevelInfo
            {
                index = i + 1,
                sceneName = $"Level{i + 1}",
                displayName = $"第{i + 1}关",
                unlocked = true,  // 【开发阶段】默认全部解锁
                completed = false
            };
        }
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 判断是否关卡场景
        bool isLevelScene = IsLevelScene(scene.name);
        CurrentLevelIndex = isLevelScene ? ParseLevelIndex(scene.name) : 0;

        OnSceneChanged?.Invoke(scene, mode);
    }

    // ─── 场景切换 ───

    /// <summary>加载指定关卡场景</summary>
    public void LoadLevelScene(int levelIndex)
    {
        if (levelIndex < 1 || levelIndex > levels.Length)
        {
            Debug.LogError($"[LevelManager] 无效关卡索引: {levelIndex}");
            return;
        }

        var info = levels[levelIndex - 1];
        if (!info.unlocked)
        {
            Debug.LogWarning($"[LevelManager] 关卡 {levelIndex} 未解锁");
            return;
        }

        CurrentLevelIndex = levelIndex;
        if (!Application.CanStreamedLevelBeLoaded(info.sceneName))
        {
            Debug.LogError($"[LevelManager] 场景 {info.sceneName} 无法加载。请先执行 Tools → Build All Scenes (Night Heron) 生成关卡场景，并确认它已加入 Build Settings。");
            return;
        }

        SceneManager.LoadScene(info.sceneName);
    }

    /// <summary>返回菜单</summary>
    public void ReturnToMenu()
    {
        CurrentLevelIndex = 0;
        SceneManager.LoadScene("MenuScene");
    }

    // ─── 关卡状态 ───

    /// <summary>标记关卡通关，解锁下一关</summary>
    public void CompleteLevel(int levelIndex)
    {
        if (levelIndex < 1 || levelIndex > levels.Length) return;

        var info = levels[levelIndex - 1];
        info.completed = true;

        // 解锁下一关
        if (levelIndex < levels.Length)
        {
            levels[levelIndex].unlocked = true;
        }

        SaveProgress();
    }

    /// <summary>查询关卡是否解锁</summary>
    public bool IsLevelUnlocked(int levelIndex)
    {
        if (levelIndex < 1 || levelIndex > levels.Length) return false;
        return levels[levelIndex - 1].unlocked;
    }

    /// <summary>查询关卡是否通关</summary>
    public bool IsLevelCompleted(int levelIndex)
    {
        if (levelIndex < 1 || levelIndex > levels.Length) return false;
        return levels[levelIndex - 1].completed;
    }

    /// <summary>获取关卡信息</summary>
    public LevelInfo GetLevelInfo(int levelIndex)
    {
        if (levelIndex < 1 || levelIndex > levels.Length) return null;
        return levels[levelIndex - 1];
    }

    /// <summary>强制解锁所有关卡（调试用）</summary>
    public void UnlockAllLevels()
    {
        foreach (var l in levels)
            l.unlocked = true;
        SaveProgress();
    }

    /// <summary>重置所有进度（调试用）</summary>
    public void ResetAllProgress()
    {
        for (int i = 0; i < levels.Length; i++)
        {
            levels[i].unlocked = (i == 0);
            levels[i].completed = false;
        }
        PlayerPrefs.DeleteKey("NightHeron_Progress");
    }

    // ─── 持久化 ───

    void SaveProgress()
    {
        // 用位掩码存储：低 6 位 = 解锁状态，高 6 位 = 通关状态
        int data = 0;
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i].unlocked)  data |= (1 << i);
            if (levels[i].completed) data |= (1 << (i + 8));
        }
        PlayerPrefs.SetInt("NightHeron_Progress", data);
        PlayerPrefs.Save();
    }

    void LoadProgress()
    {
        if (!PlayerPrefs.HasKey("NightHeron_Progress")) return;

        int data = PlayerPrefs.GetInt("NightHeron_Progress");
        for (int i = 0; i < levels.Length; i++)
        {
            levels[i].unlocked  = (data & (1 << i)) != 0;
            levels[i].completed = (data & (1 << (i + 8))) != 0;
        }
    }

    // ─── 工具 ───

    bool IsLevelScene(string sceneName)
    {
        return sceneName.StartsWith("Level");
    }

    int ParseLevelIndex(string sceneName)
    {
        // "Level3" → 3
        string numPart = sceneName.Replace("Level", "");
        return int.TryParse(numPart, out int idx) ? idx : 0;
    }
}
