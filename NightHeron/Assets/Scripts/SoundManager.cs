using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// 音频管理器：统一管理背景音乐和音效播放
/// 挂载到场景中的 Audio Manager 空对象上使用
/// 需要在同一个 GameObject 上挂两个 AudioSource 组件
/// 自动根据场景切换 BGM：菜单用 menuBGM，关卡用 gameBGM，通关用 successBGM
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("默认音量")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;

    [Header("三首场景 BGM（拖入对应音频）")]
    public AudioClip menuBGM;                    // 开始界面 / 关卡选择
    public AudioClip gameBGM;                    // 游戏进行中
    public AudioClip successBGM;                 // 通关成功

    [Header("音效（可选拖入）")]
    public AudioClip btnClickClip;              // 按钮点击
    public AudioClip anchorPlaceClip;           // 放置锚点
    public AudioClip anchorRemoveClip;          // 撤销锚点
    public AudioClip birdFlyClip;               // 夜鹭飞行（循环）
    public AudioClip poopDropClip;              // 拉便便
    public AudioClip hitPerfectClip;            // Perfect 命中
    public AudioClip hitGoodClip;               // Good 命中
    public AudioClip hitEmmClip;                // Emm 命中
    public AudioClip hitBuildingClip;           // 命中建筑
    public AudioClip obstacleHitClip;           // 撞到障碍物
    public AudioClip levelClearClip;            // 通关音效
    public AudioClip gameOverClip;              // 失败音效

    private AudioSource bgmSource;   // 背景音乐专用
    private AudioSource sfxSource;   // 音效专用
    private Coroutine resultCheckRoutine; // 通关检测协程

    void Awake()
    {
        // 单例
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 获取或创建两个 AudioSource
        AudioSource[] sources = GetComponents<AudioSource>();

        if (sources.Length >= 2)
        {
            bgmSource = sources[0];
            sfxSource = sources[1];
        }
        else if (sources.Length == 1)
        {
            bgmSource = sources[0];
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        // 配置 BGM 播放器：循环播放
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = bgmVolume;

        // 配置 SFX 播放器：一次性
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = sfxVolume;

        // 监听场景加载，自动切换 BGM
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // 首次启动，播放菜单 BGM
        PlayBGM(menuBGM);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>场景加载后自动切换 BGM</summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MenuScene")
        {
            PlayBGM(menuBGM);
            return;
        }

        // 进入关卡 → 游戏 BGM，并开始检测通关
        PlayBGM(gameBGM);
        if (resultCheckRoutine != null)
            StopCoroutine(resultCheckRoutine);
        resultCheckRoutine = StartCoroutine(CheckResultPanel());
    }

    /// <summary>每 0.5 秒检测是否出现了通关面板</summary>
    IEnumerator CheckResultPanel()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);

            var panel = GameObject.Find("ResultPanel");
            if (panel == null) continue;

            // 检查面板内是否有 "通关成功" 文字
            var texts = panel.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var t in texts)
            {
                if (t.text.Contains("通关成功"))
                {
                    PlayBGM(successBGM);
                    yield break; // 检测到后停止
                }
            }
        }
    }

    // ==================== 公开接口 ====================

    /// <summary>播放一次音效</summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    /// <summary>切换背景音乐（自动循环）</summary>
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return; // 同一首不重启

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    /// <summary>停止背景音乐</summary>
    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    /// <summary>暂停背景音乐</summary>
    public void PauseBGM()
    {
        if (bgmSource != null) bgmSource.Pause();
    }

    /// <summary>恢复背景音乐</summary>
    public void ResumeBGM()
    {
        if (bgmSource != null) bgmSource.UnPause();
    }

    /// <summary>设置音效音量</summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null) sfxSource.volume = sfxVolume;
    }

    /// <summary>设置背景音乐音量</summary>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null) bgmSource.volume = bgmVolume;
    }

    // ==================== 便捷方法（直接用 Inspector 里拖好的 Clip） ====================

    public void PlayBtnClick()       => PlaySFX(btnClickClip);
    public void PlayAnchorPlace()    => PlaySFX(anchorPlaceClip);
    public void PlayAnchorRemove()   => PlaySFX(anchorRemoveClip);
    public void PlayBirdFly()        => PlaySFX(birdFlyClip);
    public void PlayPoopDrop()       => PlaySFX(poopDropClip);
    public void PlayHitPerfect()     => PlaySFX(hitPerfectClip);
    public void PlayHitGood()        => PlaySFX(hitGoodClip);
    public void PlayHitEmm()         => PlaySFX(hitEmmClip);
    public void PlayHitBuilding()    => PlaySFX(hitBuildingClip);
    public void PlayObstacleHit()    => PlaySFX(obstacleHitClip);
    public void PlayLevelClear()     => PlaySFX(levelClearClip);
    public void PlayGameOver()       => PlaySFX(gameOverClip);
}
