using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 运行时装配：场景加载后确保核心管理器存在。
/// GameManager + LevelManager 跨场景持久化（DontDestroyOnLoad），只创建一次。
/// 菜单场景创建纯黑占位相机。
/// </summary>
public class RuntimeBootstrap : MonoBehaviour
{
    // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    // static void AutoBuildGameFlow()
    // {
    //     bool isMenuScene = IsMenuScene(SceneManager.GetActiveScene().name);
    //
    //     // 确保 LevelManager 存在（先创建，GameManager 依赖它）
    //     if (FindAnyObjectByType<LevelManager>() == null)
    //     {
    //         var go = new GameObject("LevelManager");
    //         go.AddComponent<LevelManager>();
    //     }
    //
    //     // 确保 GameManager 存在
    //     if (FindAnyObjectByType<GameManager>() == null)
    //     {
    //         var go = new GameObject("GameManager");
    //         var gm = go.AddComponent<GameManager>();
    //         // 显式触发菜单初始化（Start 可能因 timing 还没调用）
    //         gm.TryShowMenuUI();
    //     }
    //
    //     // 菜单场景：占位相机
    //     if (isMenuScene && Camera.main == null && FindAnyObjectByType<Camera>() == null)
    //     {
    //         var camGO = new GameObject("MenuCamera");
    //         var cam = camGO.AddComponent<Camera>();
    //         cam.clearFlags = CameraClearFlags.SolidColor;
    //         cam.backgroundColor = Color.black;
    //         cam.orthographic = true;
    //         cam.orthographicSize = 10f;
    //         cam.nearClipPlane = 0.3f;
    //         cam.farClipPlane = 1000f;
    //         cam.cullingMask = 0;
    //         cam.depth = -100;
    //     }
    // }
    //
    // static bool IsMenuScene(string sceneName)
    // {
    //     return sceneName == "MenuScene" || sceneName == "NightHeronScene";
    // }
}
