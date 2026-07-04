using System;
using UnityEngine;

public class Hero : MonoBehaviour
{
    public bool IsDead => _isDead;
    private bool _isDead;

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
}