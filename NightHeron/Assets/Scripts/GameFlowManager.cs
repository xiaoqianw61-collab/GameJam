using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 游戏流程管理：开始界面 → 关卡地图 → 进入游戏
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    private GameObject menuCanvas;
    private bool isStartScreen;  // 当前是否在开始界面（用于监听任意键）
    private bool canSkip;         // 防止 Start 的第一帧就触发按键

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        DontDestroyOnLoad(gameObject);

        // 必须有 EventSystem，否则 UI 按钮点不了
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }
    }

    void Start()
    {
        ShowStartScreen();
        canSkip = false;
        // 延迟一帧，防止 Start 里 Input.anyKeyDown 误触发
        Invoke(nameof(EnableSkip), 0.3f);
    }

    void EnableSkip() { canSkip = true; }

    void Update()
    {
        if (canSkip && isStartScreen && Input.anyKeyDown)
        {
            // 忽略鼠标按键，只响应键盘/手柄
            if (!Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1) && !Input.GetMouseButtonDown(2))
            {
                ShowLevelMap();
            }
        }
    }

    // ─── 开始界面 ───
    void ShowStartScreen()
    {
        isStartScreen = true;
        ClearMenu();
        menuCanvas = CreateCanvas("StartScreenCanvas");

        // 背景图：居中完整显示
        CreateBackgroundImage("StartScreen", menuCanvas.transform);

        // 提示文字：按任意键继续（底部闪烁）
        var hint = CreateUIText("Hint", "按 任 意 键 继 续",
            new Vector2(0, -Screen.height * 0.35f), 28,
            new Color(0.15f, 0.15f, 0.15f, 0.9f), TextAnchor.MiddleCenter, menuCanvas.transform);
        hint.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.1f);
        hint.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.1f);
        hint.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
    }

    // ─── 关卡地图 ───
    void ShowLevelMap()
    {
        isStartScreen = false;
        ClearMenu();
        menuCanvas = CreateCanvas("LevelMapCanvas");
        var canvas = menuCanvas.GetComponent<Canvas>();
        float w = Screen.width;
        float h = Screen.height;

        // 背景图：居中完整显示
        CreateBackgroundImage("MapScreen", menuCanvas.transform);

        // 6 个关卡透明点击区域，按百分比定位（调整这些坐标对准你地图上的关卡位置）
        var nodePositions = new Vector2[]
        {
            new Vector2(0.15f, 0.18f), // 1
            new Vector2(0.28f, 0.48f), // 2
            new Vector2(0.48f, 0.55f), // 3
            new Vector2(0.72f, 0.72f), // 4
            new Vector2(0.68f, 0.35f), // 5
            new Vector2(0.88f, 0.20f), // 6
        };

        // 创建透明可点击区域（不画圆圈、不画连线，点击地图对应位置即可）
        for (int i = 0; i < nodePositions.Length; i++)
        {
            int level = i + 1;
            Vector2 screenPos = new Vector2(nodePositions[i].x * w, nodePositions[i].y * h);
            CreateInvisibleHotspot(level, screenPos, menuCanvas.transform);
        }
    }

    // ─── 进入游戏 ───
    void StartLevel(int levelIndex)
    {
        ClearMenu();

        // 使用场景里已有的 LevelBuilder，或创建一个
        var builder = FindAnyObjectByType<LevelBuilder>();
        if (builder == null)
        {
            var lbGO = new GameObject("LevelBuilder");
            builder = lbGO.AddComponent<LevelBuilder>();
        }

        builder.GenerateLevel();
        // 后续可以把 levelIndex 传给 LevelBuilder，做不同关卡布局
    }

    // ─── 通用工具 ───
    void ClearMenu()
    {
        if (menuCanvas != null) Destroy(menuCanvas);
    }

    GameObject CreateCanvas(string name)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        // 不自动缩放，我们手动用 Screen.width/height 计算位置
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    /// <summary>
    /// 获取可用字体，兼容不同 Unity/Tuanjie 版本
    /// </summary>
    Font GetSafeFont()
    {
        // Tuanjie 1.9.3 只支持 LegacyRuntime.ttf，Arial.ttf 会抛异常
        try
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null) return font;
        }
        catch { }

        try
        {
            var font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font != null) return font;
        }
        catch { }

        // 兜底：使用系统字体
        return Font.CreateDynamicFontFromOSFont("Arial", 14);
    }

    /// <summary>
    /// 创建全屏背景图片，保持宽高比完整显示，不裁切不拉伸
    /// </summary>
    GameObject CreateBackgroundImage(string resourceName, Transform parent)
    {
        var bg = new GameObject(resourceName + "Background");
        bg.transform.SetParent(parent, false);

        var rt = bg.AddComponent<RectTransform>();
        // 锚点居中，手动算尺寸
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        var img = bg.AddComponent<Image>();
        var tex = Resources.Load<Texture2D>(resourceName);
        if (tex != null)
        {
            img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

            float sw = Screen.width;
            float sh = Screen.height;
            float imgRatio = (float)tex.width / tex.height;   // 图片比例
            float screenRatio = sw / sh;                       // 屏幕比例

            Vector2 displaySize;
            if (imgRatio > screenRatio)
            {
                // 图片比屏幕更宽 → 宽度撑满，高度按比例
                displaySize = new Vector2(sw, sw / imgRatio);
            }
            else
            {
                // 图片比屏幕更高 → 高度撑满，宽度按比例
                displaySize = new Vector2(sh * imgRatio, sh);
            }
            rt.sizeDelta = displaySize;
        }
        img.color = Color.white;

        // 放最底层
        bg.transform.SetAsFirstSibling();
        return bg;
    }

    GameObject CreateUIText(string name, string content, Vector2 anchoredPos, int fontSize, Color color, TextAnchor anchor, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.text = content;
        text.font = GetSafeFont();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(800, 120);
        return go;
    }





    /// <summary>
    /// 创建完全透明的可点击热区，覆盖在地图图片上方
    /// </summary>
    void CreateInvisibleHotspot(int level, Vector2 screenPos, Transform parent)
    {
        var btnGO = new GameObject("Hotspot_L" + level);
        btnGO.transform.SetParent(parent, false);

        var rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.anchoredPosition = screenPos;
        rt.sizeDelta = new Vector2(120, 120); // 大一点好点

        // 全透明 Image（必须有 Image 才能接收点击）
        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0); // 完全透明

        var btn = btnGO.AddComponent<Button>();
        int idx = level;
        btn.onClick.AddListener(() => StartLevel(idx));
    }

}
