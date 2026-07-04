using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UIUtil
{
    private static PointerEventData _pointerEventData;
    private static List<RaycastResult> _raycastResults;

    public static void InitUtil()
    {
        _pointerEventData = new PointerEventData(EventSystem.current);
        _raycastResults = new List<RaycastResult>();
    }
    
    /// <summary>
    /// 屏幕坐标处是否有ui
    /// </summary>
    public static bool IsOverlapUI(Vector2 screenPos)
    {
        _pointerEventData.position = _pointerEventData.pressPosition = screenPos;
        EventSystem.current.RaycastAll(_pointerEventData, _raycastResults);
        return _raycastResults.Count > 0;
    }
    /// <summary>
    /// 尝试获取屏幕坐标处的ui
    /// </summary>
    public static bool TryGetOverlapUI(Vector2 screenPos, out GameObject ui)
    {
        _pointerEventData.position = _pointerEventData.pressPosition = screenPos;
        EventSystem.current.RaycastAll(_pointerEventData, _raycastResults);
        if (_raycastResults.Count > 0)
        {
            ui = _raycastResults[0].gameObject;
            return true;
        }
        ui = default;
        return false;
    }
}