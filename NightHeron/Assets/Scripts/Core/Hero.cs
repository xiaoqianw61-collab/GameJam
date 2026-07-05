using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Splines;

public class Hero : MonoBehaviour
{
    public static Hero Instance;
    
    public bool IsDead => _isDead;
    private bool _isDead;

    [SerializeField, LabelText("运动控制")]
    private SplineAnimate animate;

    private enum EAnimType
    {
        Wait,
        Fly,
    }
    
    private Animator _animator;
    private Attackable _attackable;
    private void Awake()
    {
        Instance = this;
        _animator = GetComponentInChildren<Animator>();
        _attackable = GetComponent<Attackable>();
        _attackable.OnBeginAttack += OnBeginAttack;
        _attackable.OnEndAttack += OnEndAttack;
        animate.Completed += OnFlyCompleted;
    }

    public void BeginFly()
    {
        animate.Play();
        _animator.Play(EAnimType.Fly.ToString(), 0, 0);
        _attackable.SetStartAttack(true);
    }
    public void StopFly()
    {
        animate.Pause();
        _attackable.SetStartAttack(false);
    }

    /// <summary>
    /// 被命中
    /// </summary>
    public void Hit(int reduceScore)
    {
        GameState.Instance.ReduceScore(reduceScore);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerUtil.Obstacle)
        {
            _isDead = true;
            GameState.Instance.SetGameOver();
        }
    }

    private bool _isFlyEnd;
    private int _attackNum;
    private void OnBeginAttack()
    {
        _attackNum++;
    }
    private void OnEndAttack()
    {
        _attackNum--;
        if (_isFlyEnd && _attackNum == 0)
        {
            GameState.Instance.SetGamePass();
        }
    }
    private void OnFlyCompleted()
    {
        if (!_isDead)
        {
            _isFlyEnd = true;
            _attackable.SetStartAttack(false);
        }
    }
}