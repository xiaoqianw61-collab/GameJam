using UnityEngine;

/// <summary>
/// 运行时装配：进入 Play 模式后自动创建 GameFlowManager 和 Camera。
/// GameFlowManager 负责管理开始界面 → 关卡地图 → 进入游戏。
/// Camera 用于消除 Game 视图"No cameras rendering"的提示。
/// </summary>
public class RuntimeBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBuildGameFlow()
    {
        EnsureCameraExists();

        if (FindAnyObjectByType<GameFlowManager>() == null)
        {
            var go = new GameObject("GameFlowManager");
            go.AddComponent<GameFlowManager>();
        }
    }

    static void EnsureCameraExists()
    {
        var cam = Camera.main;
        if (cam == null)
            cam = FindAnyObjectByType<Camera>();

        if (cam != null) return;

        var go = new GameObject("MenuCamera");
        cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cam.orthographicSize = 10f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;
        cam.cullingMask = 0; // 不渲染任何层级，只用于消除"No cameras rendering"
        // 注意：不设置 MainCamera tag，避免和 LevelBuilder 冲突
    }
}
// trigger-recompile-001
