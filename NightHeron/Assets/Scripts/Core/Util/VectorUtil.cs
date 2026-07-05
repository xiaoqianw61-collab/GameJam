using UnityEngine;

public static class VectorUtil
{
    /// <summary>
    /// 向量旋转(2d)
    /// </summary>
    public static Vector3 VectorRotate(Vector3 vec, float angle)
    {
        var radians = angle * Mathf.Deg2Rad;
        var cos = Mathf.Cos(radians);
        var sin = Mathf.Sin(radians);
        return new Vector3
        {
            x = vec.x * cos - vec.y * sin,
            y = vec.x * sin + vec.y * cos,
        };
    }
}
