using UnityEngine;

/// <summary>
/// 运行时装配：如果当前场景里没有 LevelBuilder，进入 Play 模式后自动创建一个。
/// 这样即使编辑器里没生成，点 Play 也能直接玩。
/// </summary>
public class RuntimeBootstrap : MonoBehaviour
{
    // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    // static void AutoBuildLevel()
    // {
    //     if (FindAnyObjectByType<LevelBuilder>() == null)
    //     {
    //         var go = new GameObject("LevelBuilder");
    //         go.AddComponent<LevelBuilder>();
    //     }
    // }
}
// trigger-recompile-001
