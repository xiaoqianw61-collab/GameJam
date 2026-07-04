using System.Collections;
using System.Collections.Generic;

public static class ListUtil
{
    public static bool IsEmpty(this IList list)
    {
        return list == null || list.Count == 0;
    }
    public static T Random<T>(this IList<T> list)
    {
        return list[UnityEngine.Random.Range(0, list.Count)];
    }
}