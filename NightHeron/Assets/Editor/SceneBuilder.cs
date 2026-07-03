using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class SceneBuilder
{
    [MenuItem("Tools/Build Night Heron Scene")]
    public static void BuildNightHeronScene()
    {
        if (!System.IO.Directory.Exists("Assets/Scenes"))
        {
            System.IO.Directory.CreateDirectory("Assets/Scenes");
        }

        string scenePath = "Assets/Scenes/NightHeronScene.unity";

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var levelBuilderGO = new GameObject("LevelBuilder");
        var levelBuilder = levelBuilderGO.AddComponent<LevelBuilder>();
        levelBuilder.mapWidth = 28f;
        levelBuilder.mapHeight = 18f;
        levelBuilder.personCount = 15;
        levelBuilder.obstacleCount = 6;
        levelBuilder.carCount = 5;
        levelBuilder.personCount = 15;

        EditorSceneManager.SaveScene(scene, scenePath, true);

        Debug.Log("Night Heron scene created at: " + scenePath);
        Debug.Log("Gameplay: 1) Click to place anchors  2) Select anchor and drag blue handles to adjust curve  3) ENTER to start  4) SPACE/J/Click to drop poop!");
    }
}
