using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class Npc : MonoBehaviour
{
    [SerializeField, LabelText("分数")]
    private int score;
    [SerializeField, LabelText("被命中图片")]
    private Sprite beHitSprite;
    [Title("位移")]
    [SerializeField, LabelText("路线节点")]
    private Transform pointWayRoot;
    [SerializeField, LabelText("移动速度")]
    private float moveSpeed;
    [SerializeField, LabelText("摇晃")]
    private FeelWobble wobble;

    private readonly object _animBinder = new object();

    private Vector3 _lastPos;
    
    private bool _isMoving;
    private Vector3[] _posArr;
    private Collider2D _collider;
    private SpriteRenderer _renderer;
    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _renderer = GetComponent<SpriteRenderer>();
        GameState.Instance.RegisterNpc();
    }
    private void Start()
    {
        if (pointWayRoot != null && pointWayRoot.childCount > 0)
        {
            _isMoving = true;
            _posArr = new Vector3[pointWayRoot.childCount + 2];
            _posArr[0] = transform.position;
            for (var i = 0; i < pointWayRoot.childCount; i++)
            {
                var point = pointWayRoot.GetChild(i).position;
                _posArr[i + 1] = point;
            }
            _posArr[_posArr.Length - 1] = transform.position;
            transform.DOPath(_posArr, moveSpeed, PathType.Linear, PathMode.TopDown2D).SetSpeedBased().SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetId(_animBinder);
            _lastPos = transform.position;
        }
    }
    private void OnDisable()
    {
        StopAnim();
    }
    private void Update()
    {
        if (_isMoving)
        {
            var delta = transform.position - _lastPos;
            _renderer.flipX = delta.x < 0;
            _lastPos = transform.position;
        }
    }

    private void StopAnim()
    {
        if (_isMoving)
        {
            _isMoving = false;
            DOTween.Kill(_animBinder);
        }
        wobble.SetEffectFactor(0);
    }

    /// <summary>
    /// 命中
    /// </summary>
    public void Hit()
    {
        _collider.enabled = false;
        StopAnim();
        GameState.Instance.AddScore(score);
        _renderer.sprite = beHitSprite;
    }

#if UNITY_EDITOR
    
    private void OnDrawGizmos()
    {
        if (pointWayRoot == null || pointWayRoot.childCount == 0 || Application.isPlaying) return;
        Gizmos.DrawLine(transform.position, pointWayRoot.GetChild(0).position);
        for (var i = 0; i < pointWayRoot.childCount; i++)
        {
            var begin = pointWayRoot.GetChild(i).position;
            var to = i == pointWayRoot.childCount - 1 ? transform.position : pointWayRoot.GetChild(i + 1).position;
            Gizmos.DrawLine(begin, to);
        }
    }
    
#endif
}