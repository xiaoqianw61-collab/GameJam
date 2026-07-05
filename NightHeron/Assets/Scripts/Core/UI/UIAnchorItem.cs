using System;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    public class UIAnchorItem : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField, LabelText("曲柄连接")]
        private RectTransform handleLink;
        [SerializeField, LabelText("曲柄")]
        private UIAnchorHandle[] handles;
        [Space]
        [SerializeField, LabelText("选中标识")]
        private GameObject selectedSign;
        [SerializeField, LabelText("拖选标识")]
        private GameObject darggingSign;
        
        /// <summary>
        /// 当前索引
        /// </summary>
        public int curIndex;

        private Action<int> _selectCb;
        
        private bool _isSelected;
        private bool _isDragging;

        private RectTransform _parent;
        private void Awake()
        {
            _parent = (RectTransform) transform.parent;
            selectedSign.SetActive(false);
            darggingSign.SetActive(false);
            handleLink.gameObject.SetActive(false);
            for (var i = 0; i < handles.Length; i++)
            {
                var index = i;
                handles[i].SetDragCb(() => OnDragCb(index));
            }
        }

        private void OnDragCb(int index)
        {
            var newPos = CameraManager.MouseWorldPos;
            AnchorManager.Instance.SetAnchorTangent(curIndex, newPos, index == 0);
            RefreshHandle();
        }

        public void SetIndex(int index, Action<int> selectCb)
        {
            curIndex = index;
            _selectCb = selectCb;
        }
        public void SetSelect(bool select)
        {
            _isSelected = select;
            selectedSign.SetActive(select);
            handleLink.gameObject.SetActive(select);
            if (select)
            {
                RefreshHandle();
            }
        }

        private void RefreshHandle()
        {
            var anchorInfo = AnchorManager.Instance.GetAnchorInfo(curIndex, out var angle);
            var originPos = CameraManager.WorldPosToUILocalPos(_parent, Vector3.zero);
            var deltaPos = CameraManager.WorldPosToUILocalPos(_parent, Vector3.zero + new Vector3(anchorInfo.TangentIn.z, 0, 0));
            // 长度x2，-90度偏移
            handleLink.sizeDelta = new Vector2(11, Mathf.Abs(deltaPos.x - originPos.x) * 2 * 1.5f);
            handleLink.localEulerAngles = new Vector3(0, 0, angle - 90);
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            _selectCb?.Invoke(curIndex);
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (_isSelected)
            {
                _isDragging = true;
                darggingSign.SetActive(true);

                UpdatePos();
            }
        }
        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            UpdatePos();
        }
        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            darggingSign.SetActive(false);
            UpdatePos();
        }

        private void UpdatePos()
        {
            var newPos = CameraManager.MouseWorldPos;
            transform.position = newPos;
            AnchorManager.Instance.SetAnchorPos(curIndex, newPos);
        }
    }
}