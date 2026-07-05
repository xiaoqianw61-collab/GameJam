using System;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

public class FeelWobble : MonoBehaviour
{
    /// <summary>
    /// 当前效果系数
    /// </summary>
    public float CurEffectFactor => effectFactor;
    
    [SerializeField, LabelText("效果系数")]
    private float effectFactor = 1f;
    
    [Space]
    [SerializeField, LabelText("启动x偏移")]
    private bool openXOffset;
    [SerializeField, LabelText("x偏移系数"), ShowIf(nameof(openXOffset))]
    private float xOffsetFactor = 0.8f;
    
    [Header("摇晃")]
    [SerializeField, LabelText("开启")]
    private bool openRotate = true;
    [SerializeField, LabelText("时间尺度"), ShowIf(nameof(openRotate))]
    private float rotateTimeRate = 4;
    [SerializeField, LabelText("摇晃起始"), ShowIf(nameof(openRotate))]
    private float rotateStart = 0;
    [SerializeField, LabelText("摇晃幅度"), ShowIf(nameof(openRotate))]
    private float rotateRate = 8;
    [SerializeField, LabelText("使用Sin"), ShowIf(nameof(openRotate))]
    private bool rotateUseSin;
    
    [Header("缩放")]
    [SerializeField, LabelText("开启")]
    private bool openScale = true;
    [SerializeField, LabelText("时间尺度"), ShowIf(nameof(openScale))]
    private float scaleTimeRate = 4;
    [SerializeField, LabelText("缩放尺度"), ShowIf(nameof(openScale))]
    private float scaleRate = 0.05f;
    [SerializeField, LabelText("缩放轴开启"), ShowIf(nameof(openScale))]
    private bool3 scaleAxisEnable = new bool3(true, true, true);
    [SerializeField, LabelText("缩放轴系数"), ShowIf(nameof(openScale))]
    private Vector3 scaleAxisFactor = Vector3.one;
    [SerializeField, LabelText("使用Cos"), ShowIf(nameof(openScale))]
    private bool scaleUseCos;
    
    private Transform _transform;
    
    private void Awake()
    {
        _transform = transform;
    }
    private void Update()
    {
        var time = Time.time;
        // 摇晃
        if (openRotate)
        {
            var localEulerAngles = _transform.localEulerAngles;
            var t = time * rotateTimeRate;
            if (openXOffset) t += _transform.position.x * xOffsetFactor;
            var f = rotateUseSin ? Mathf.Sin(t) : Mathf.Cos(t);
            localEulerAngles.z = rotateStart + f * rotateRate * effectFactor;
            _transform.localEulerAngles = localEulerAngles;
        }
        // 缩放
        if (openScale)
        {
            var localScale = _transform.localScale;
            var t = time * scaleTimeRate;
            if (openXOffset) t += _transform.position.x * xOffsetFactor;
            var f = scaleUseCos ? Mathf.Cos(t) : Mathf.Sin(t);
            var step = f * scaleRate;
            if (scaleAxisEnable.x) localScale.x = 1 + step * scaleAxisFactor.x * effectFactor;
            if (scaleAxisEnable.y) localScale.y = 1 + step * scaleAxisFactor.y * effectFactor;
            if (scaleAxisEnable.z) localScale.z = 1 + step * scaleAxisFactor.z * effectFactor;
            _transform.localScale = localScale;
        }
    }
    
    /// <summary>
    /// 设置效果系数
    /// </summary>
    public void SetEffectFactor(float f)
    {
        effectFactor = f;
    }
}