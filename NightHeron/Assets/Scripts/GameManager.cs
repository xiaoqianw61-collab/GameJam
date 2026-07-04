using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏管理器：UI流程 + 锚点库存 + 跨场景持久化
/// 场景加载委托给 LevelManager，GameManager 负责界面和库存
/// MenuScene → 开始界面 → 关卡地图 → LevelManager.LoadLevelScene()
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("锚点库存")]
    public int maxAnchors = 4;
    private int remainingAnchors;

    public int RemainingAnchors => remainingAnchors;
    public int UsedAnchors => maxAnchors - remainingAnchors;

    /// <summary>当前关卡索引（从 LevelManager 同步）</summary>
    public int CurrentLevel => LevelManager.Instance != null ? LevelManager.Instance.CurrentLevelIndex : 0;

    // ─── 评分 ───
    [Header("评分")]
    public int currentScore = 0;
    /// <summary>本关理论满分（所有目标中心命中 ×1.5）</summary>
    public int maxPossibleScore = 0;

    private GameObject menuCanvas;
    private bool isStartScreen;
    private bool canSkip;
    private bool showLevelMapOnMenuLoad; // 返回菜单时直接显示关卡地图

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // 强制高分辨率渲染
        // Editor 中最大化 Game View 窗口 + Scale 1x 获得最佳效果
        Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
        Application.targetFrameRate = 60;
        QualitySettings.antiAliasing = 0; // 像素画不需要抗锯齿

        DontDestroyOnLoad(gameObject);
        ResetAnchorStock();
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(esGO);
        }
    }

    void OnEnable()
    {
        // OnEnable 比 Start 更早且更可靠（即使通过 AddComponent 运行时创建也一定会触发）
        TryShowMenuUI();
    }

    void Start()
    {
        // 兜底：如果 OnEnable 时场景还没完全加载好，Start 再试一次
        TryShowMenuUI();
    }

    /// <summary>检查是否在菜单场景，是则显示开始界面</summary>
    public void TryShowMenuUI()
    {
        if (!IsMenuScene()) return;
        if (isStartScreen && menuCanvas != null) return; // 已经显示了

        ShowStartScreen();
        canSkip = false;
        Invoke(nameof(EnableSkip), 0.3f);
        Debug.Log("[GameManager] 显示开始界面（按任意键继续）");
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ─── 场景加载回调 ───

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsMenuScene(scene.name))
        {
            EnsureMenuCamera(); // 保证菜单场景一定有相机，避免 "No cameras rendering"
            isStartScreen = true;
            ClearMenu();

            if (showLevelMapOnMenuLoad)
            {
                ShowLevelMap();              // 从关卡返回 → 直接到关卡选择
                showLevelMapOnMenuLoad = false;
            }
            else
            {
                ShowStartScreen();           // 首次启动 → 开始界面
            }

            canSkip = false;
            Invoke(nameof(EnableSkip), 0.3f);
            return;
        }

        // 关卡场景：内容已通过 SceneBuilder 烘焙在场景里，GameManager 只重置库存和状态
        isStartScreen = false;
        ClearMenu();
        ResetAnchorStock();

        var builder = FindAnyObjectByType<LevelBuilder>();
        if (builder == null)
        {
            Debug.LogWarning($"[GameManager] 场景 {scene.name} 中没有 LevelBuilder，假设为手工编辑关卡。");
        }
    }

    bool IsMenuScene() => IsMenuScene(SceneManager.GetActiveScene().name);
    bool IsMenuScene(string name) => name == "MenuScene" || name == "NightHeronScene";

    void EnableSkip() { canSkip = true; }

    void Update()
    {
        if (canSkip && isStartScreen && Input.anyKeyDown)
        {
            if (!Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1) && !Input.GetMouseButtonDown(2))
                ShowLevelMap();
        }

        // 关卡地图：键盘 1~6 直接进关卡
        if (!isStartScreen && IsMenuScene() && LevelManager.Instance != null)
        {
            for (int i = 1; i <= 6; i++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + i)))
                    RequestEnterLevel(i);
            }
        }

        // 关卡内：ESC 返回菜单
        if (!isStartScreen && CurrentLevel > 0 && Input.GetKeyDown(KeyCode.Escape))
            ReturnToMenu();
    }

    // ─── 锚点库存 ───

    public void ResetAnchorStock() { remainingAnchors = maxAnchors; }
    public bool TryUseAnchor()
    {
        if (remainingAnchors <= 0) return false;
        remainingAnchors--;
        return true;
    }
    public void ReturnAnchor()
    {
        if (remainingAnchors < maxAnchors) remainingAnchors++;
    }
    public bool CanPlaceAnchor() => remainingAnchors > 0;

    // ─── 开始界面 ───

    void ShowStartScreen()
    {
        isStartScreen = true;
        ClearMenu();
        menuCanvas = CreateCanvas("StartScreenCanvas");

        CreateBackgroundImage("StartScreen", menuCanvas.transform);

        var hint = CreateUIText("Hint", "点击任意键开始拉屎（划掉）游戏",
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
        float w = Screen.width, h = Screen.height;

        CreateBackgroundImage("MapScreen", menuCanvas.transform);

        var nodePositions = new Vector2[]
        {
            new Vector2(0.15f, 0.18f), new Vector2(0.28f, 0.48f),
            new Vector2(0.48f, 0.55f), new Vector2(0.72f, 0.72f),
            new Vector2(0.68f, 0.35f), new Vector2(0.88f, 0.20f),
        };

        for (int i = 0; i < nodePositions.Length; i++)
        {
            int level = i + 1;
            Vector2 screenPos = new Vector2(nodePositions[i].x * w, nodePositions[i].y * h);
            CreateLevelMarker(level, screenPos, menuCanvas.transform);
        }
    }

    // ─── 关卡进入 / 返回 → 委托给 LevelManager ───

    public void RequestEnterLevel(int levelIndex)
    {
        if (LevelManager.Instance == null)
        {
            Debug.LogError("[GameManager] LevelManager 不存在！");
            return;
        }

        if (!LevelManager.Instance.IsLevelUnlocked(levelIndex))
        {
            Debug.LogWarning($"[GameManager] 关卡 {levelIndex} 未解锁");
            return;
        }

        ClearMenu();
        LevelManager.Instance.LoadLevelScene(levelIndex);
    }

    public void ReturnToMenu()
    {
        showLevelMapOnMenuLoad = true; // 从关卡返回时直接显示关卡地图
        if (LevelManager.Instance != null)
            LevelManager.Instance.ReturnToMenu();
        else
            SceneManager.LoadScene("MenuScene");
    }

    void EnsureMenuCamera()
    {
        if (Camera.main != null) return;
        if (FindAnyObjectByType<Camera>() != null) return;

        var camGO = new GameObject("MenuCamera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cam.orthographicSize = 10f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;
        cam.cullingMask = ~0; // 渲染所有层，避免 "No cameras rendering"
        cam.depth = -100;
    }

    /// <summary>加分（由 Poop → Target 命中时调用）</summary>
    public void AddScore(int points)
    {
        currentScore += points;
        if (currentScore < 0) currentScore = 0;
    }

    /// <summary>重置本关分数（进关卡 / 重开时调用）</summary>
    public void ResetScore()
    {
        currentScore = 0;
        maxPossibleScore = 0;
    }

    /// <summary>
    /// 星级评定：
    /// 满分 = 所有目标都被中心命中
    /// 三星：≥90% 满分
    /// 二星：60%~90%
    /// 一星：20%~60%
    /// 零星：&lt;20%
    /// </summary>
    public int GetStarRating()
    {
        if (maxPossibleScore <= 0) return 0;
        float ratio = (float)currentScore / maxPossibleScore;
        if (ratio >= 0.9f) return 3;
        if (ratio >= 0.6f) return 2;
        if (ratio >= 0.2f) return 1;
        return 0;
    }

    /// <summary>被敌对摩托车打中 → 关卡重开</summary>
    public void OnHitHostileVehicle()
    {
        Debug.Log("[GameManager] 被敌对摩托车击中！关卡重新开始。");
        StartCoroutine(RestartLevelDelayed());
    }

    System.Collections.IEnumerator RestartLevelDelayed()
    {
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ─── 通用 UI 工具 ───

    void ClearMenu() { if (menuCanvas != null) Destroy(menuCanvas); }

    GameObject CreateCanvas(string name)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    Font GetSafeFont()
    {
        try { var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); if (f) return f; } catch { }
        try { var f = Resources.GetBuiltinResource<Font>("Arial.ttf"); if (f) return f; } catch { }
        return Font.CreateDynamicFontFromOSFont("Arial", 14);
    }

    GameObject CreateBackgroundImage(string resourceName, Transform parent)
    {
        var bg = new GameObject(resourceName + "Background");
        bg.transform.SetParent(parent, false);
        var rt = bg.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        var img = bg.AddComponent<Image>();
        var tex = Resources.Load<Texture2D>(resourceName);
        if (tex != null)
        {
            img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            float sw = Screen.width, sh = Screen.height;
            float ir = (float)tex.width / tex.height, sr = sw / sh;
            rt.sizeDelta = ir > sr ? new Vector2(sw, sw / ir) : new Vector2(sh * ir, sh);
        }
        img.color = Color.white;
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

    void CreateLevelMarker(int level, Vector2 screenPos, Transform parent)
    {
        var btnGO = new GameObject("Marker_L" + level);
        btnGO.transform.SetParent(parent, false);
        var rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.anchoredPosition = screenPos;
        rt.sizeDelta = new Vector2(100, 100);

        // 蓝色圆底 + 白边，与地图蓝白色调和谐
        var img = btnGO.AddComponent<Image>();
        img.sprite = CreateCircleSprite(50, new Color(0.36f, 0.60f, 0.84f, 0.92f), Color.white, 5);
        img.color = Color.white;
        img.type = Image.Type.Simple;

        // 关卡编号
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(btnGO.transform, false);
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = labelRT.anchorMax = new Vector2(0.5f, 0.5f);
        labelRT.anchoredPosition = Vector2.zero;
        labelRT.sizeDelta = new Vector2(100, 100);

        var label = labelGO.AddComponent<Text>();
        label.text = level.ToString();
        label.font = GetSafeFont();
        label.fontSize = 48;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;

        var btn = btnGO.AddComponent<Button>();
        int idx = level;
        btn.onClick.AddListener(() => RequestEnterLevel(idx));
    }

    /// <summary>
    /// 生成带白边的实心圆 Sprite，用于关卡标记。
    /// </summary>
    static Sprite CreateCircleSprite(int radius, Color fillColor, Color borderColor, int borderWidth)
    {
        int size = radius * 2;
        var tex = new Texture2D(size, size);
        float r = radius;
        float rInner = r - borderWidth;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - radius;
                float dy = y - radius;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist <= rInner)
                    tex.SetPixel(x, y, fillColor);
                else if (dist <= r)
                    tex.SetPixel(x, y, borderColor);
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
