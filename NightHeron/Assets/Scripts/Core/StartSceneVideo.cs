using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 挂在 levelSelect 上，当面板激活时自动播放全屏开场视频，播完自动销毁。
/// </summary>
public class StartSceneVideo : MonoBehaviour
{
    [SerializeField, Tooltip("开场视频文件")]
    private VideoClip videoClip;

    private void OnEnable()
    {
        if (videoClip == null)
        {
            Debug.LogWarning("[StartSceneVideo] videoClip 未赋值，请在 Inspector 拖入视频文件");
            return;
        }

        // 创建全屏 Canvas
        var canvasGo = new GameObject("StartVideoCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        DontDestroyOnLoad(canvasGo);

        // RawImage 作为视频显示层
        var rawImageGo = new GameObject("VideoImage", typeof(RawImage), typeof(VideoPlayer), typeof(AudioSource));
        rawImageGo.transform.SetParent(canvasGo.transform);
        var rt = rawImageGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // RawImage 初始透明，视频准备好后再显示
        var rawImage = rawImageGo.GetComponent<RawImage>();
        rawImage.color = Color.clear;

        // VideoPlayer
        var player = rawImageGo.GetComponent<VideoPlayer>();
        player.clip = videoClip;
        player.playOnAwake = false;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = new RenderTexture(Screen.width, Screen.height, 0);
        rawImage.texture = player.targetTexture;

        // AudioSource
        var audio = rawImageGo.GetComponent<AudioSource>();
        player.audioOutputMode = VideoAudioOutputMode.AudioSource;
        player.SetTargetAudioSource(0, audio);

        // 先 Prepare，准备好后再 Play，避免黑屏
        player.prepareCompleted += _ =>
        {
            rawImage.color = Color.white;
            player.Play();
        };

        // 播完销毁
        player.loopPointReached += _ =>
        {
            Destroy(canvasGo);
            enabled = false;
        };

        player.Prepare();
    }
}
