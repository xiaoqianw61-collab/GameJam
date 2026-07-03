using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class Attackable : MonoBehaviour
{
    [SerializeField, LabelText("攻击间隔")]
    private float attackInterval;
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
        Debug.Log("开始攻击");
        DOVirtual.DelayedCall(hitDelay, Hit, false);
        Instantiate(attackVfx, transform.position, Quaternion.identity);
    }
    private void Hit()
    {
        Debug.Log("造成伤害");
    }
}