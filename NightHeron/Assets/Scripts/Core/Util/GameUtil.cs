using UnityEngine;

public static class GameUtil
{
    public static float Random(this Vector2 bound)
    {
        return UnityEngine.Random.Range(bound.x, bound.y);
    }
}