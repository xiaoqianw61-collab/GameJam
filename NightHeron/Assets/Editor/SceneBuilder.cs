using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.IO;

/// <summary>
/// 一键创建所有游戏场景：
/// 1. 读取原始 NightHeronScene
/// 2. 调用 LevelBuilder.GenerateLevel() 把内容烘焙进场景
/// 3. 生成 6 个独立关卡场景（内容可独立编辑）
/// 4. 创建 MenuScene 并更新 Build Settings
/// </summary>
public class SceneBuilder
{
    [MenuItem("Tools/Build All Scenes (Night Heron)")]
    public static void BuildAllScenes()
    {
        if (!Directory.Exists("Assets/Scenes"))
            Directory.CreateDirectory("Assets/Scenes");

        CreateMenuScene();
        BakeLevelScenes();
        UpdateBuildSettings();

        Debug.Log("[Night Heron] All scenes created! MenuScene + Level1~Level6");
    }

    static void CreateMenuScene()
    {
        string path = "Assets/Scenes/MenuScene.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, path, true);
        Debug.Log("  Created: MenuScene");
    }

    /// <summary>
    /// 从原始 NightHeronScene 烘焙生成 6 个关卡场景
    /// 每个场景都是独立 .unity 文件，可直接在编辑器里修改
    /// </summary>
    static void BakeLevelScenes()
    {
        string sourceScene = "Assets/Scenes/NightHeronScene.unity";
        if (!File.Exists(sourceScene))
        {
            Debug.LogError($"[SceneBuilder] 找不到源场景: {sourceScene}");
            return;
        }

        // 1. 打开原始场景
        var scene1 = EditorSceneManager.OpenScene(sourceScene, OpenSceneMode.Single);

        // 动态添加 LevelBuilder（厨房水槽场景本身不需要常驻这个组件）
        var builder = Object.FindAnyObjectByType<LevelBuilder>();
        if (builder == null)
        {
            var go = new GameObject("LevelBuilder");
            builder = go.AddComponent<LevelBuilder>();
            Debug.Log("[SceneBuilder] 动态创建 LevelBuilder 用于烘焙。");
        }

        // 删除可能已烘焙的旧内容（防止重复生成）
        ClearGeneratedContent();

        // 生成所有关卡内容（相机、地面、障碍、路人、摩托、玩家、锚点编辑器、UI）
        builder.GenerateLevel();

        // Level1 使用原始配置（levelIndex 默认/0 会走 default 分支，保持原始布局）
        string level1Path = "Assets/Scenes/Level1.unity";
        EditorSceneManager.SaveScene(scene1, level1Path, true);
        AssetDatabase.Refresh();
        Debug.Log("  Baked: Level1");

        // 2. 复制 Level1 生成 Level2~Level6（初始内容相同，方便后续独立编辑）
        for (int i = 2; i <= 6; i++)
        {
            CopyLevelScene(i, level1Path);
        }

        // 3. 还原 NightHeronScene 为干净状态（移除烘焙过程中生成的动态内容）
        EditorSceneManager.OpenScene(sourceScene, OpenSceneMode.Single);
        ClearGeneratedContent();
        var dynamicBuilder = Object.FindAnyObjectByType<LevelBuilder>();
        if (dynamicBuilder != null) Object.DestroyImmediate(dynamicBuilder.gameObject);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), sourceScene, true);
        Debug.Log("  NightHeronScene 已还原。");
    }

    /// <summary>
    /// 复制 Level1 为 LevelN，并修改 LevelBuilder.levelIndex 做标记
    /// </summary>
    static void CopyLevelScene(int levelIndex, string sourcePath)
    {
        string destPath = $"Assets/Scenes/Level{levelIndex}.unity";

        // 删除旧文件，避免 AssetDatabase 冲突
        if (File.Exists(destPath)) AssetDatabase.DeleteAsset(destPath);

        AssetDatabase.CopyAsset(sourcePath, destPath);
        AssetDatabase.Refresh();

        // 打开复制的场景，设置关卡编号
        var scene = EditorSceneManager.OpenScene(destPath, OpenSceneMode.Single);
        var builder = Object.FindAnyObjectByType<LevelBuilder>();
        if (builder != null)
        {
            builder.levelIndex = levelIndex;
            EditorUtility.SetDirty(builder);
        }

        EditorSceneManager.SaveScene(scene, destPath, true);
        Debug.Log($"  Copied: Level{levelIndex}");
    }

    /// <summary>
    /// 删除可能由 GenerateLevel 生成的旧对象，防止重复
    /// </summary>
    static void ClearGeneratedContent()
    {
        string[] names = { "Main Camera", "MenuCamera", "Ground", "Obstacles", "Targets",
                           "StartMarker", "EndMarker", "Player", "AnchorEditor", "Canvas",
                           "PathLine", "EventSystem" };
        foreach (var root in Object.FindObjectsOfType<GameObject>())
        {
            if (root.transform.parent == null && System.Array.IndexOf(names, root.name) >= 0)
            {
                Object.DestroyImmediate(root);
            }
        }
    }

    static void UpdateBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();

        // MenuScene 必须在 Build Settings 索引 0
        scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/MenuScene.unity", true));

        for (int i = 1; i <= 6; i++)
        {
            scenes.Add(new EditorBuildSettingsScene($"Assets/Scenes/Level{i}.unity", true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("  Build Settings updated: MenuScene (index 0) + Level1~6");
    }
}
