using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 一键生成游戏对象预制体。
/// 生成到 Assets/Prefabs（便于在编辑器里拖拽引用）和 Assets/Resources/Prefabs（便于运行时 Resources.Load）。
/// 生成后可在 Unity 编辑器中继续调整 Prefab 上的组件，后续修改会自动同步所有实例。
/// </summary>
public class PrefabGenerator
{
    const string PrefabsDir = "Assets/Prefabs";
    const string ResourcesPrefabsDir = "Assets/Resources/Prefabs";

    [MenuItem("Tools/Night Heron/Generate Prefabs")]
    public static void GeneratePrefabs()
    {
        EnsureDirectory(PrefabsDir);
        EnsureDirectory(ResourcesPrefabsDir);

        // 先生成 Poop，因为 Player 需要引用它
        GameObject poopPrefab = GeneratePoopPrefab();

        GeneratePersonPrefab();
        GenerateObstaclePrefab();
        GenerateMotorcyclePrefab();
        GeneratePlayerPrefab(poopPrefab);
        GenerateMarkerPrefab("StartMarker", new Color(0.4f, 0.55f, 0.85f));
        GenerateMarkerPrefab("EndMarker", new Color(0.4f, 0.55f, 0.85f));

        AssetDatabase.Refresh();
        Debug.Log("[PrefabGenerator] 所有预制体已生成到 Assets/Prefabs 和 Assets/Resources/Prefabs");
    }

    static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    static GameObject GeneratePersonPrefab()
    {
        Sprite normal = Resources.Load<Sprite>("people1");
        Sprite pooped = Resources.Load<Sprite>("people0");

        var go = LevelBuilder.CreatePersonRuntime(
            Vector2.zero, null,
            Target.TargetSubType.PersonStationary, false, false,
            normal, pooped);

        return SaveAsPrefab(go, "Person");
    }

    static GameObject GenerateObstaclePrefab()
    {
        Sprite roadSign = Resources.Load<Sprite>("roadsign1");
        var go = LevelBuilder.CreateObstacleRuntime(Vector2.zero, null, roadSign);
        go.name = "Obstacle";
        return SaveAsPrefab(go, "Obstacle");
    }

    static GameObject GenerateMotorcyclePrefab()
    {
        var go = LevelBuilder.CreateMotorcycleRuntime(
            Vector2.zero, null,
            Target.TargetSubType.VehicleStationary, false, false);
        return SaveAsPrefab(go, "Motorcycle");
    }

    static GameObject GeneratePlayerPrefab(GameObject poopPrefab)
    {
        var go = LevelBuilder.CreatePlayerRuntime(Vector3.zero, poopPrefab, out PlayerBird _);
        go.name = "Player";
        return SaveAsPrefab(go, "Player");
    }

    static GameObject GeneratePoopPrefab()
    {
        var go = PlayerBird.CreatePoopRuntime(Vector3.zero);
        return SaveAsPrefab(go, "Poop");
    }

    static GameObject GenerateMarkerPrefab(string name, Color color)
    {
        var go = LevelBuilder.CreateMarkerRuntime(name, Vector3.zero, color);
        return SaveAsPrefab(go, name);
    }

    static GameObject SaveAsPrefab(GameObject go, string prefabName)
    {
        string path1 = $"{PrefabsDir}/{prefabName}.prefab";
        string path2 = $"{ResourcesPrefabsDir}/{prefabName}.prefab";

        // 确保对象没有父级，避免保存时包含不相关的层级
        go.transform.SetParent(null);

        GameObject prefab1 = PrefabUtility.SaveAsPrefabAsset(go, path1);
        PrefabUtility.SaveAsPrefabAsset(go, path2);

        Object.DestroyImmediate(go);
        return prefab1;
    }
}
