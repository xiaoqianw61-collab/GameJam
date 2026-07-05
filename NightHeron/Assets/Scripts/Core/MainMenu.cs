using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField, LabelText("开始界面")]
    private GameObject mainMenu;
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

    private int _level;

    private void Awake()
    {
        _state = EState.MainMenu;
        mainMenu.SetActive(true);
        levelSelect.SetActive(false);
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
                    mainMenu.SetActive(false);
                    levelSelect.SetActive(true);
                    for (var i = 0; i < levelLocks.Length; i++)
                    {
                        levelLocks[i].SetActive(i > LevelManager.Instance.LevelRecord);
                    }
                    SoundManager.Instance?.PlayMainMenuStart();
                }
                break;
            }
        }
    }

    public void SetLevel(int level)
    {
        if (_state != EState.SelectLevel || level - 1 > LevelManager.Instance.LevelRecord) return;
        LevelManager.Instance.LoadLevelScene(level);
    }
}