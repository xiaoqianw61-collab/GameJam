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
    
    private bool _beginFly;
    private bool _isFlyEnd;
    private int _attackNum;
    private Vector3 _lastPos;
    
    private Animator _animator;
    private SpriteRenderer _renderer;
    private Attackable _attackable;
    private void Awake()
    {
        Instance = this;
        _animator = GetComponentInChildren<Animator>();
        _renderer = GetComponentInChildren<SpriteRenderer>();
        _attackable = GetComponent<Attackable>();
        _attackable.OnBeginAttack += OnBeginAttack;
        _attackable.OnEndAttack += OnEndAttack;
        animate.Completed += OnFlyCompleted;
        _lastPos = transform.position;
    }
    private void Update()
    {
        if (_beginFly)
        {
            var delta = transform.position - _lastPos;
            _renderer.flipX = delta.x < 0;
            _lastPos = transform.position;
        }
    }

    public void BeginFly()
    {
        _beginFly = true;
        animate.Play();
        _animator.Play(EAnimType.Fly.ToString(), 0, 0);
        _attackable.SetStartAttack(true);
    }
    public void StopFly()
    {
        _beginFly = false;
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
            SoundManager.Instance?.StopSFX();
            GameState.Instance.SetGameOver();
        }
    }

    private void OnBeginAttack()
    {
        _attackNum++;
        SoundManager.Instance?.PlayPoopDrop();
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
            SoundManager.Instance?.StopSFX();
        }
    }
}