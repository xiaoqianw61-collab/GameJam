using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class GameUI : MonoBehaviour
    {
        [SerializeField, LabelText("关卡")]
        private TextMeshProUGUI levelText;
        [Title("准备阶段")]
        [SerializeField, LabelText("开始按钮")]
        private GameObject startBtn;
        [SerializeField, LabelText("开始灰化按钮")]
        private GameObject startGrayBtn;
        // [Title("游玩中")]
        [Title("结算")]
        [SerializeField, LabelText("结算节点")]
        private GameObject settleObj;
        [SerializeField, LabelText("结算通关")]
        private GameObject settleCompleteObj;
        [SerializeField, LabelText("结算失败")]
        private GameObject settleFailObj;
        [SerializeField, LabelText("结算星星")]
        private GameObject[] stars;

        private readonly object _animBinder = new object();
        
        private void Awake()
        {
            GameState.Instance.OnGameStart += OnGameStart;
            GameState.Instance.OnGameFinish += OnGameFinish;
            
            startBtn.SetActive(true);
            var btn = startBtn.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(StartPoop);
            startGrayBtn.SetActive(false);
            settleObj.SetActive(false);
            levelText.text = $"第{LevelManager.Instance.CurrentLevelIndex}关";
        }
        private void OnDisable()
        {
            DOTween.Kill(_animBinder);
        }

        private void OnGameStart()
        {
            startBtn.SetActive(false);
            startGrayBtn.SetActive(true);
        }
        private void OnGameFinish(bool result)
        {
            settleObj.SetActive(true);
            settleCompleteObj.SetActive(result);
            settleFailObj.SetActive(!result);
            if (result)
            {
                var starCount = GameState.Instance.CalculateStar();
                for (var i = 0; i < stars.Length; i++)
                {
                    var star = stars[i];
                    star.gameObject.SetActive(i < starCount);
                    if (i < starCount)
                    {
                        star.transform.DOScale(1, 0.3f).SetEase(Ease.OutBack).From(0).SetDelay(i * 0.2f + 0.2f).SetId(_animBinder)
                            .OnStart(() => SoundManager.Instance?.PlayStarShow());
                    }
                }
            }
        }

        public void StartPoop()
        {
            GameState.Instance.SetGameStart();
        }
        
        public void Restart()
        {
            LevelManager.Instance.Restart();
        }
        public void NextLevel()
        {
            LevelManager.Instance.LoadNextLevel();
        }
        public void ReturnMenu()
        {
            LevelManager.Instance.ReturnToMenu();
        }
    }
}