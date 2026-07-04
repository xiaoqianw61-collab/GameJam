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

    private Attackable _attackable;
    private void Awake()
    {
        Instance = this;
        _attackable = GetComponent<Attackable>();
        animate.Completed += OnFlyCompleted;
    }

    public void BeginFly()
    {
        animate.Play();
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
        GameState.Instance.AddScore(reduceScore);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerUtil.Obstacle)
        {
            _isDead = true;
            GameState.Instance.SetGameOver();
        }
    }
    private void OnFlyCompleted()
    {
        if (!_isDead)
        {
            _attackable.SetStartAttack(false);
            GameState.Instance.SetGamePass();
        }
    }
}