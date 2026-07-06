using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MainMenu : MonoBehaviour
{
    [SerializeField, LabelText("开始界面")]
    private GameObject mainMenu;
    [SerializeField, LabelText("开始动画")]
    private VideoPlayer beginVideo;
    [Title("关卡")]
    [SerializeField, LabelText("关卡选择")]
    private GameObject levelSelect;
    [SerializeField, LabelText("关卡锁")]
    private GameObject[] levelLocks;
    
    private enum EState
    {
        MainMenu,
        SelectLevel,
    }
    private EState _state;
    
    public const string PLAY_FIRSTLY_KEY = nameof(PLAY_FIRSTLY_KEY);

    private void Awake()
    {
        _state = EState.MainMenu;
        mainMenu.SetActive(true);
        levelSelect.SetActive(false);
        beginVideo.loopPointReached += OnBeginVideoPlayFinish;
    }
    private void Update()
    {
        switch (_state)
        {
            case EState.MainMenu:
            {
                if (Input.anyKeyDown)
                {
                    _state = EState.SelectLevel;
                    if (PlayerPrefs.GetInt(PLAY_FIRSTLY_KEY) == 0)
                    {
                        PlayerPrefs.SetInt(PLAY_FIRSTLY_KEY, 1);
                        beginVideo.gameObject.SetActive(true);
                    }
                    else
                    {
                        ShowLevelSelect();
                    }
                    
                    SoundManager.Instance?.PlayMainMenuStart();
                }
                break;
            }
        }
    }

    public void SetLevel(int level)
    {
        if (level - 1 > LevelManager.Instance.LevelRecord) return;
        LevelManager.Instance.LoadLevelScene(level);
    }
    
    private void OnBeginVideoPlayFinish(VideoPlayer source)
    {
        ShowLevelSelect();
    }

    private void ShowLevelSelect()
    {
        mainMenu.SetActive(false);
        levelSelect.SetActive(true);
        for (var i = 0; i < levelLocks.Length; i++)
        {
            levelLocks[i].SetActive(i > LevelManager.Instance.LevelRecord);
        }
    }
}