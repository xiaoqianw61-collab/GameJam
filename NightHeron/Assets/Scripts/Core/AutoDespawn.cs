using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DefaultNamespace
{
    public class AutoDespawn : MonoBehaviour
    {
        [SerializeField, LabelText("销毁时间")]
        private float time;
        
        private void Start()
        {
            Destroy(gameObject, time);
        }
    }
}