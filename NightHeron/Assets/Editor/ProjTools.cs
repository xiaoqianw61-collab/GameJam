using DefaultNamespace;
using UnityEditor;
using UnityEngine;

public static class ProjTools
{
    [MenuItem("Tools/Night Heron/Test")]
    public static void Test()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Npc/NpcSpawner.prefab");
        var index = 0;
        foreach (var transform in Selection.transforms)
        {
            var obj = (GameObject) PrefabUtility.InstantiatePrefab(prefab);
            obj.name = index.ToString();
            obj.transform.position = transform.position;
            index++;
        }
    }
}