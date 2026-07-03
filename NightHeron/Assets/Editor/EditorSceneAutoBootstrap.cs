using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 编辑器启动时确保项目配置正确：
/// 1. 场景存在 → 打开 MenuScene
/// 2. Build Settings 包含所有场景（Menu + Level1~6）
/// 3. TagManager 包含所需 Tag
/// </summary>
[InitializeOnLoad]
public static class EditorSceneAutoBootstrap
{
    static readonly string MenuScenePath = Path.Combine("Assets", "Scenes", "MenuScene.unity");
    static readonly string[] LevelPaths = {
        Path.Combine("Assets", "Scenes", "Level1.unity"),
        Path.Combine("Assets", "Scenes", "Level2.unity"),
        Path.Combine("Assets", "Scenes", "Level3.unity"),
        Path.Combine("Assets", "Scenes", "Level4.unity"),
        Path.Combine("Assets", "Scenes", "Level5.unity"),
        Path.Combine("Assets", "Scenes", "Level6.unity"),
    };

    static readonly string[] RequiredTags = {
        "Player", "Target", "Building", "Ground", "Poop"
    };

    static EditorSceneAutoBootstrap()
    {
        EditorApplication.delayCall += EnsureProjectConfig;
    }

    static void EnsureProjectConfig()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        EnsureTags();

        // 优先打开 MenuScene；如果不存在则打开 NightHeronScene（兼容旧版）再提示创建
        if (!EnsureMenuSceneOpen())
        {
            Debug.LogWarning("[Night Heron] MenuScene not found. Please run Tools > Build All Scenes to create all scenes.");
        }

        EnsureBuildSettings();
    }

    static void EnsureTags()
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        List<string> existingTags = new List<string>();
        for (int i = 0; i < tagsProp.arraySize; i++)
            existingTags.Add(tagsProp.GetArrayElementAtIndex(i).stringValue);

        bool needsApply = false;
        foreach (string tag in RequiredTags)
        {
            if (!existingTags.Contains(tag))
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                needsApply = true;
            }
        }

        if (needsApply)
        {
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Night Heron] Registered tags: " + string.Join(", ", RequiredTags));
        }
    }

    static bool EnsureMenuSceneOpen()
    {
        string fullPath = Path.Combine(Application.dataPath, "..", MenuScenePath);
        if (File.Exists(fullPath))
        {
            var active = EditorSceneManager.GetActiveScene();
            if (!active.IsValid() || active.name != "MenuScene")
            {
                EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            }
            return true;
        }

        // 回退：兼容旧版 NightHeronScene
        string oldPath = Path.Combine(Application.dataPath, "..", "Assets", "Scenes", "NightHeronScene.unity");
        if (File.Exists(oldPath))
        {
            EditorSceneManager.OpenScene(oldPath, OpenSceneMode.Single);
            Debug.Log("[Night Heron] Using legacy NightHeronScene. Consider running Tools > Build All Scenes.");
            return false;
        }

        return false;
    }

    static void EnsureBuildSettings()
    {
        List<EditorBuildSettingsScene> currentScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        bool hasMenu = currentScenes.Exists(s => s.path == MenuScenePath);
        if (!hasMenu)
        {
            currentScenes.Insert(0, new EditorBuildSettingsScene(MenuScenePath, true));
        }

        // 检查关卡场景
        bool anyMissing = false;
        foreach (string lp in LevelPaths)
        {
            if (!currentScenes.Exists(s => s.path == lp))
            {
                currentScenes.Add(new EditorBuildSettingsScene(lp, true));
                anyMissing = true;
            }
        }

        if (!hasMenu || anyMissing)
        {
            EditorBuildSettings.scenes = currentScenes.ToArray();
            Debug.Log("[Night Heron] Build Settings updated with all scenes.");
        }
    }
}
