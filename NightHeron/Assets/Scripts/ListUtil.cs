using System.Collections;

public static class ListUtil
{
    public static bool IsEmpty(this IList list)
    {
        return list == null || list.Count == 0;
    }
}