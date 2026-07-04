using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class Attackable : MonoBehaviour
{
    [SerializeField, LabelText("命中扣分(如果是npc)")]
    private int hitReduceScore = 5;
    
    [Title("攻击参数")]
    [SerializeField, LabelText("攻击间隔")]
    private float attackInterval;
    [SerializeField, LabelText("命中半径")]
    private float hitRadius = 1.5f;
    [SerializeField, LabelText("命中延迟")]
    private float hitDelay;
    [SerializeField, LabelText("攻击特效")]
    private GameObject attackVfx;

    private float _timer;
    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= attackInterval)
        {
            _timer -= attackInterval;
            Attack();
        }
    }

    private void Attack()
    {
        DOVirtual.DelayedCall(hitDelay, Hit, false);
        Instantiate(attackVfx, transform.position, Quaternion.identity);
    }

    private static Collider2D[] _hits = new Collider2D[30];
    private void Hit()
    {
        // 玩家
        if (tag == "Player")
        {
            var count = Physics2D.OverlapCircleNonAlloc(transform.position, hitRadius, _hits, LayerUtil.Interactable);
            for (int i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (hit.TryGetComponent(out Npc npc))
                {
                    npc.Hit();
                }
            }
        }
        // 目标
        else if (tag == "Target" || tag == "Building")
        {
            var count = Physics2D.OverlapCircleNonAlloc(transform.position, hitRadius, _hits, LayerUtil.Interactable);
            for (int i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (hit.TryGetComponent(out Hero hero))
                {
                    hero.Hit(hitReduceScore);
                }
            }
        }
    }

#if UNITY_EDITOR
    
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
    
#endif
}