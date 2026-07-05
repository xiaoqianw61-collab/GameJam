using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    public class UIAnchorHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Action _dragCb;
        public void SetDragCb(Action dragCb)
        {
            _dragCb = dragCb;
        }
        
        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            SoundManager.Instance?.PlayAnchorAdjust();
        }
        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            _dragCb?.Invoke();
        }
        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            
        }
    }
}