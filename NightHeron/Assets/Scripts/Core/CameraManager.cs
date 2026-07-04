using System;
using UnityEngine;

[DefaultExecutionOrder(-960)]
public class CameraManager : MonoBehaviour
{
    private static CameraManager _instance;

    public static Camera MainCamera => _instance.mainCamera;
    public static Camera UICamera => _instance.uICamera;
    public static Vector3 MouseWorldPos => _instance._mouseWorldPos;
    
    /// <summary>
    /// 屏幕大小（世界坐标系下）
    /// </summary>
    public static Vector2 ViewSize => _instance._viewSize;
    private Vector2 _viewSize;
    /// <summary>
    /// 一半屏幕大小（世界坐标系下）
    /// </summary>
    public static Vector2 HalfViewSize => _instance._halfViewSize;
    private Vector2 _halfViewSize;
    
    [SerializeField]
    private Camera mainCamera;
    [SerializeField]
    private Camera uICamera;

    private Vector3 _mouseWorldPos;
    private void Awake()
    {
        _instance = this;
    }
    private void Start()
    {
        var ratio = (float) Screen.width / Screen.height;
        var halfWidth = mainCamera.orthographicSize * ratio;
        _halfViewSize = new Vector2(halfWidth, mainCamera.orthographicSize);
        _viewSize = _halfViewSize * 2;
    }

    private void Update()
    {
        _mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        _mouseWorldPos.z = 0;
    }
    
    public static Vector2 WorldPosToUILocalPos(RectTransform parentRoot, Vector3 pos)
    {
        // 点击动画
        var screenPos = MainCamera.WorldToScreenPoint(pos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRoot, screenPos, UICamera, out var fingerLocalPos);
        return fingerLocalPos;
    }
}