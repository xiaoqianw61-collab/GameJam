using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class Npc : MonoBehaviour
{
    [SerializeField, LabelText("分数")]
    private int score;
    [SerializeField, LabelText("被命中颜色")]
    private Color beHitColor;
    [Title("位移")]
    [SerializeField, LabelText("路线节点")]
    private Transform pointWayRoot;
    [SerializeField, LabelText("移动时间")]
    private float moveTime;

    private Vector3[] _posArr;
    private Collider2D _collider;
    private SpriteRenderer _renderer;
    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _renderer = GetComponent<SpriteRenderer>();
    }
    private void Start()
    {
        if (pointWayRoot != null && pointWayRoot.childCount > 0)
        {
            _posArr = new Vector3[pointWayRoot.childCount + 2];
            _posArr[0] = transform.position;
            for (var i = 0; i < pointWayRoot.childCount; i++)
            {
                var point = pointWayRoot.GetChild(i).position;
                _posArr[i + 1] = point;
            }
            _posArr[_posArr.Length - 1] = transform.position;
            transform.DOPath(_posArr, moveTime, PathType.Linear, PathMode.TopDown2D).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear);
        }
    }

    /// <summary>
    /// 命中
    /// </summary>
    public void Hit()
    {
        _collider.enabled = false;
        GameState.Instance.AddScore(score);
        _renderer.color = beHitColor;
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