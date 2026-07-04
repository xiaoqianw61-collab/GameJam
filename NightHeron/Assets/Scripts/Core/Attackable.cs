using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class Attackable : MonoBehaviour
{
    [SerializeField, LabelText("自动开始攻击")]
    private bool autoStart;
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

    private bool _startAttack;
    
    private float _timer;

    private static Collider2D[] _hits = new Collider2D[30];

    private object _animBinder = new object();
    private ContactFilter2D _filter2D;
    private void Awake()
    {
        _filter2D = new ContactFilter2D().NoFilter();
        _filter2D.SetLayerMask(LayerUtil.Interactable_Mask);
        _filter2D.useTriggers = true;
        if (autoStart)
        {
            _startAttack = true;
        }
    }
    private void OnDisable()
    {
        DOTween.Kill(_animBinder);
    }
    private void Update()
    {
        if (!_startAttack) return;
        _timer += Time.deltaTime;
        if (_timer >= attackInterval)
        {
            _timer -= attackInterval;
            Attack();
        }
    }

    /// <summary>
    /// 设置开始攻击
    /// </summary>
    public void SetStartAttack(bool start)
    {
        _startAttack = start;
    }

    private void Attack()
    {
        DOVirtual.DelayedCall(hitDelay, Hit, false).SetId(_animBinder);
        Instantiate(attackVfx, transform.position, Quaternion.identity);
    }

    private void Hit()
    {
        // 玩家
        if (tag == "Player")
        {
            var count = Physics2D.OverlapCircle(transform.position, hitRadius, _filter2D, _hits);
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
            var count = Physics2D.OverlapCircle(transform.position, hitRadius, _filter2D, _hits);
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