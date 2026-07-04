using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DefaultNamespace
{
    public class NpcSpawner : MonoBehaviour
    {
        [SerializeField, LabelText("生成列表")]
        private GameObject[] prefabArr;

        private void Awake()
        {
            if (!prefabArr.IsEmpty())
            {
                Instantiate(prefabArr.Random(), transform.position, Quaternion.identity);
            }
        }
    }
}