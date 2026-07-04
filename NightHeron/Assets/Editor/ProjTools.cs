using UnityEditor;
using UnityEngine;

public static class ProjTools
{
    [MenuItem("Tools/Night Heron/Test")]
    public static void Test()
    {
        foreach (var transform in Selection.transforms)
        {
            transform.position += new Vector3(Random.value * 0.25f, Random.value * 0.25f);
        }
    }
}