using UnityEngine;
using UnityEditor;

/// <summary>
/// 一键从现有 Create*Runtime 方法生成全部 Prefab，存入 Prefabs/ 文件夹。
/// 菜单：Tools / Night Heron / Generate All Prefabs
/// </summary>
public class GeneratePrefabs : EditorWindow
{
    [MenuItem("Tools/Night Heron/Generate All Prefabs")]
    static void Generate()
    {
        string prefabDir = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabDir))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        int count = 0;

        // ── 1. 障碍物 ×9 ──
        string[] obstacleNames = { "roadsign1", "roadsign2", "birdsign", "fruitshop", "popsign", "shop1", "shop2", "shop3", "shopwithtree" };
        for (int i = 0; i < obstacleNames.Length; i++)
        {
            Sprite roadSign = LevelBuilder.GetObstacleSpriteByIndex(i);
            var obstacle = LevelBuilder.CreateObstacleRuntime(Vector2.zero, null, roadSign);
            obstacle.name = "Obstacle_" + obstacleNames[i];
            SavePrefab(obstacle, prefabDir + "/Obstacle_" + obstacleNames[i] + ".prefab");
            count++;
        }

        // ── 2. 路人 ×7（people0 ~ people6，被砸后都变 people0） ──
        Sprite poopedSprite = Resources.Load<Sprite>("people0");
        for (int i = 0; i <= 6; i++)
        {
            Sprite normalSprite = Resources.Load<Sprite>("people" + i);
            var person = LevelBuilder.CreatePersonRuntime(Vector2.zero, null,
                Target.TargetSubType.PersonStationary, false, false, normalSprite, poopedSprite);
            person.name = "Person_" + i;
            SavePrefab(person, prefabDir + "/Person_" + i + ".prefab");
            count++;
        }

        // ── 3. 摩托车 ──
        var motorcycle = LevelBuilder.CreateMotorcycleRuntime(Vector2.zero, null,
            Target.TargetSubType.VehicleStationary, false, false);
        motorcycle.name = "Motorcycle";
        SavePrefab(motorcycle, prefabDir + "/Motorcycle.prefab");
        count++;

        // ── 4. 玩家（夜鹭） ──
        var player = LevelBuilder.CreatePlayerRuntime(Vector3.zero, null, out PlayerBird pb);
        player.name = "Player";
        SavePrefab(player, prefabDir + "/Player.prefab");
        count++;

        // ── 5. 便便 ──
        var poop = PlayerBird.CreatePoopRuntime(Vector3.zero);
        poop.name = "Poop";
        SavePrefab(poop, prefabDir + "/Poop.prefab");
        count++;

        // ── 6. 起点/终点标记 ──
        var marker = LevelBuilder.CreateMarkerRuntime("Marker", Vector3.zero,
            new Color(0.4f, 0.55f, 0.85f));
        marker.name = "Marker";
        SavePrefab(marker, prefabDir + "/Marker.prefab");
        count++;

        AssetDatabase.Refresh();
        Debug.Log($"[GeneratePrefabs] 全部 {count} 个 Prefab 已生成到 Assets/Prefabs/");
    }

    static void SavePrefab(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);
        Debug.Log("  ✅ " + path);
    }
}
