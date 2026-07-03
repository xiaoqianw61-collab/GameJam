using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 编辑器启动时确保项目配置正确：
/// 1. 场景存在 → 打开 NightHeronScene
/// 2. Build Settings 包含场景
/// 3. TagManager 包含所需 Tag
/// </summary>
[InitializeOnLoad]
public static class EditorSceneAutoBootstrap
{
    // static readonly string ScenePath = Path.Combine("Assets", "Scenes", "NightHeronScene.unity");
    //
    // static readonly string[] RequiredTags = {
    //     "Player", "Target", "Building", "Ground", "Poop"
    // };
    //
    // static EditorSceneAutoBootstrap()
    // {
    //     EditorApplication.delayCall += EnsureProjectConfig;
    // }
    //
    // static void EnsureProjectConfig()
    // {
    //     if (EditorApplication.isPlayingOrWillChangePlaymode) return;
    //
    //     EnsureTags();
    //     EnsureScene();
    //     EnsureBuildSettings();
    // }
    //
    // static void EnsureTags()
    // {
    //     // 动态读取 TagManager 并补全缺失的 Tag
    //     SerializedObject tagManager = new SerializedObject(
    //         AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
    //
    //     SerializedProperty tagsProp = tagManager.FindProperty("tags");
    //     List<string> existingTags = new List<string>();
    //     for (int i = 0; i < tagsProp.arraySize; i++)
    //         existingTags.Add(tagsProp.GetArrayElementAtIndex(i).stringValue);
    //
    //     bool needsApply = false;
    //     foreach (string tag in RequiredTags)
    //     {
    //         if (!existingTags.Contains(tag))
    //         {
    //             tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
    //             tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
    //             needsApply = true;
    //         }
    //     }
    //
    //     if (needsApply)
    //     {
    //         tagManager.ApplyModifiedPropertiesWithoutUndo();
    //         Debug.Log("[Night Heron] Registered missing tags: " + string.Join(", ", RequiredTags));
    //     }
    // }
    //
    // static void EnsureScene()
    // {
    //     string fullPath = Path.Combine(Application.dataPath, "..", ScenePath);
    //     if (File.Exists(fullPath))
    //     {
    //         var active = EditorSceneManager.GetActiveScene();
    //         if (!active.IsValid() || active.name != "NightHeronScene")
    //         {
    //             EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    //         }
    //         return;
    //     }
    //
    //     // 场景不存在 → 创建
    //     var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    //     var go = new GameObject("LevelBuilder");
    //     go.AddComponent<LevelBuilder>();
    //
    //     string sceneDir = Path.Combine(Application.dataPath, "Scenes");
    //     if (!Directory.Exists(sceneDir)) Directory.CreateDirectory(sceneDir);
    //
    //     EditorSceneManager.SaveScene(scene, ScenePath, true);
    //     Debug.Log("[Night Heron] Created scene: " + ScenePath);
    // }
    //
    // static void EnsureBuildSettings()
    // {
    //     List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
    //     bool found = scenes.Exists(s => s.path == ScenePath);
    //
    //     if (!found)
    //     {
    //         scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
    //         EditorBuildSettings.scenes = scenes.ToArray();
    //         Debug.Log("[Night Heron] Added scene to Build Settings: " + ScenePath);
    //     }
    // }
}
