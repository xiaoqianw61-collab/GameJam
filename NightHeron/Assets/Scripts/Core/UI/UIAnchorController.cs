using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
    public class UIAnchorController : MonoBehaviour
    {
        [SerializeField, LabelText("锚点预制体")]
        private GameObject anchorPrefab;
        [SerializeField, LabelText("剩余锚点预制体")]
        private GameObject remainAnchorPrefab;
        [SerializeField, LabelText("剩余锚点生成位置")]
        private Transform remainAnchorSpawnRoot;

        private List<UIAnchorItem> _anchorItems;
        private List<GameObject> _remainAnchors;

        private int _selectIndex = -1;

        private void Awake()
        {
            _anchorItems = new List<UIAnchorItem>();
            _remainAnchors = new List<GameObject>();
            for (int i = 0; i < GameState.Instance.config.anchorCount; i++)
            {
                var obj = Instantiate(remainAnchorPrefab, remainAnchorSpawnRoot);
                _remainAnchors.Add(obj);
            }
            
            AnchorManager.Instance.OnAnchorNumChanged += OnAnchorNumChanged;
        }
        private void OnDestroy()
        {
            AnchorManager.Instance.OnAnchorNumChanged -= OnAnchorNumChanged;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0) && !UIUtil.IsOverlapUI(Input.mousePosition))
            {
                OnSelectCb(-1);
            }
        }

        private void OnAnchorNumChanged(int newIndex)
        {
            _selectIndex = newIndex;
            // 重新生成
            for (int i = 0; i < _anchorItems.Count; i++)
            {
                Destroy(_anchorItems[i].gameObject);
            }
            _anchorItems.Clear();
            for (var i = 1; i < AnchorManager.Instance.AllAnchor.Count - 1; i++)
            {
                var knot = AnchorManager.Instance.AllAnchor[i];
                var anchor = Instantiate(anchorPrefab, knot.Position, Quaternion.identity, transform);
                var anchorItem = anchor.GetComponent<UIAnchorItem>();
                anchorItem.SetIndex(i, OnSelectCb);
                anchorItem.SetSelect(false);
                _anchorItems.Add(anchorItem);
            }
            // 刷新剩余
            var remainingAnchorCount = AnchorManager.Instance.RemainingAnchorCount;
            for (var i = 0; i < _remainAnchors.Count; i++)
            {
                _remainAnchors[i].SetActive(i < remainingAnchorCount);
            }
        }

        private void OnSelectCb(int index)
        {
            if (index == _selectIndex)
            {
                _selectIndex = -1;
            }
            else
            {
                _selectIndex = index;
            }
            for (var i = 0; i < _anchorItems.Count; i++)
            {
                var anchorItem = _anchorItems[i];
                anchorItem.SetSelect(index == anchorItem.curIndex);
            }
        }
    }
}