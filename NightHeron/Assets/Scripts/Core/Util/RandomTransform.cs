using System;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomTransform : MonoBehaviour
{
    public bool randomPos;
    [ShowIf(nameof(randomPos))]
    public Vector2 posBound;
    
    public bool randomAngle;
    [ShowIf(nameof(randomAngle))]
    public Vector2 angleBound;
    
    public bool randomScale;
    [ShowIf(nameof(randomScale))]
    public Vector2 scaleBound;

    private void Awake()
    {
        if (randomPos)
        {
            transform.localPosition = posBound.Random() * Random.insideUnitCircle;
        }
        if (randomAngle)
        {
            transform.localEulerAngles = new Vector3(0, 0, angleBound.Random());
        }
        if (randomScale)
        {
            var s = scaleBound.Random();
            transform.localScale = new Vector3(s, s, s);
        }
    }
}
