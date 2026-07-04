using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 夜鹭游戏 - 关卡构建器
/// 当前阶段：设计模式
/// 1. 生成俯视场景（起点、终点、障碍、路人、摩托）
/// 2. 添加 AnchorEditor，玩家点击放置锚点设计路径
/// 3. 鸟停留在起点，暂不移动
/// </summary>
public class LevelBuilder : MonoBehaviour
{
    [Header("关卡配置")]
    public int levelIndex = 1;

    [Header("地图")]
    public float mapWidth = 28f;
    public float mapHeight = 14f;

    [Header("目标数量")]
    public int obstacleCount = 5;
    public int personCount = 8;
    public int carCount = 4;

    [Header("起点/终点")]
    public Vector3 startPos = new Vector3(-9f, 4f, 0f);
    public Vector3 endPos = new Vector3(19f, 4f, 0f);

    private AnchorEditor editor;
    private PlayerBird playerBird;
    private bool gameStarted = false;

    void Awake()
    {
        // 编辑器模式不执行
        if (!Application.isPlaying) return;

        // 菜单场景（NightHeronScene / MenuScene）：不生成关卡内容，由 GameManager 控制菜单流程
        var sceneName = gameObject.scene.name;
        if (sceneName == "NightHeronScene" || sceneName == "MenuScene")
        {
            Debug.Log($"[LevelBuilder] 菜单场景 {sceneName}，跳过生成。");
            return;
        }

        // 关卡场景：判断是否已烘焙
        bool alreadyBaked = Camera.main != null && GameObject.Find("Ground") != null;
        if (alreadyBaked)
        {
            // 必须用 Include 查找，因为 PlayerBird 在烘焙场景中 enabled=false
            var birds = FindObjectsByType<PlayerBird>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            playerBird = birds.Length > 0 ? birds[0] : null;
            var editors = FindObjectsByType<AnchorEditor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            editor = editors.Length > 0 ? editors[0] : null;

            // 强制重新绑定按钮监听（烘焙时的序列化回调可能解析失败）
            var confirmBtn = GameObject.Find("ConfirmButton");
            if (confirmBtn != null)
            {
                var btn = confirmBtn.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(StartGame);
                }
            }

            // 恢复 AnchorEditor 的 UI 引用（烘焙时也会丢失）
            if (editor != null)
            {
                var anchorCountGO = GameObject.Find("AnchorCount");
                if (anchorCountGO != null) editor.anchorCountText = anchorCountGO.GetComponent<Text>();

                var stockImages = new List<Image>();
                for (int i = 0; i < 4; i++)
                {
                    var sq = GameObject.Find($"StockSquare_{i}");
                    if (sq != null) stockImages.Add(sq.GetComponent<Image>());
                }
                editor.anchorStockImages = stockImages;
            }

            Debug.Log($"[LevelBuilder] 场景 {sceneName} 已烘焙，playerBird={playerBird != null}, editor={editor != null}, button={confirmBtn != null}");
            return;
        }

        Debug.Log($"[LevelBuilder] 场景 {sceneName} 未烘焙，开始生成关卡内容。");
        GenerateLevel();
    }
    public void GenerateLevel()
    {
        CreateCamera();
        CreateGround();
        CreateObstacles();
        CreateTargets();
        CreateStartEndMarkers();
        CreatePlayer();
        CreateAnchorEditor();
        CreateUI();
    }

    // ─── 相机 ───
    void CreateCamera()
    {
        if (Camera.main == null)
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
        }
        Camera cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 9f;
        cam.backgroundColor = new Color(0.25f, 0.45f, 0.2f);
        cam.transform.position = new Vector3(5f, 4f, -10f);
    }

    // ─── 地面（地图底图）───
    void CreateGround()
    {
        var ground = new GameObject("Ground");
        ground.tag = "Ground";
        ground.transform.position = new Vector3(5f, 4f, 0);

        var sr = ground.AddComponent<SpriteRenderer>();
        sr.sprite = LoadMapSprite();
        sr.color = Color.white;
        sr.sortingOrder = -10;

        // 让地图填满相机视野（32 × 18 世界单位）
        if (sr.sprite != null)
        {
            Vector3 size = sr.sprite.bounds.size;
            ground.transform.localScale = new Vector3(32f / size.x, 18f / size.y, 1f);
        }
        else
        {
            // fallback：绿色草坪
            sr.sprite = CreateRectSprite(900, 320, Color.white);
            sr.color = new Color(0.45f, 0.65f, 0.3f);
            ground.transform.localScale = Vector3.one;
        }

        var bc = ground.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(32f, 18f);
    }

    // ─── 障碍物（路牌/箱子）───
    void CreateObstacles()
    {
        var parent = new GameObject("Obstacles").transform;

        // 每关不同的障碍物布局
        Vector2[] positions = GetObstaclePositions(levelIndex);

        for (int i = 0; i < Mathf.Min(obstacleCount, positions.Length); i++)
        {
            var go = new GameObject("Obstacle_" + i);
            go.transform.SetParent(parent);
            go.transform.position = positions[i];
            go.tag = "Building";

            var srr = go.AddComponent<SpriteRenderer>();
            srr.sprite = CreateRectSprite(32, 32, Color.white);
            srr.color = new Color(0.7f, 0.7f, 0.7f);
            srr.sortingOrder = 1;

            float w = Random.Range(1.2f, 2.5f);
            float h = Random.Range(1.2f, 3.5f);
            go.transform.localScale = new Vector3(w, h, 1);

            var bcc = go.AddComponent<BoxCollider2D>();
            bcc.size = new Vector2(1f, 1f);

            var target = go.AddComponent<Target>();
            target.type = Target.TargetType.Building;
        }
    }

    /// <summary>每关不同的障碍物位置</summary>
    Vector2[] GetObstaclePositions(int level)
    {
        switch (level)
        {
            case 1: return new Vector2[] { new(-3f, 3f), new(3f, 6f), new(8f, 2.5f) };
            case 2: return new Vector2[] { new(-2f, 5f), new(5f, 1.5f), new(10f, 4f), new(14f, 6f) };
            case 3: return new Vector2[] { new(-4f, 2f), new(1f, 3.5f), new(7f, 6f), new(13f, 2f), new(17f, 5f) };
            case 4: return new Vector2[] { new(-5f, 4f), new(0f, 1f), new(6f, 3f), new(12f, 5.5f), new(18f, 1.5f) };
            case 5: return new Vector2[] { new(-3f, 6f), new(2f, 2f), new(8f, 5f), new(15f, 3.5f), new(19f, 6.5f) };
            case 6: return new Vector2[] { new(-4f, 1.5f), new(3f, 5f), new(9f, 1f), new(14f, 4f), new(20f, 2.5f) };
            default: return new Vector2[] { new(-3f, 3f), new(3f, 6f), new(8f, 2.5f), new(14f, 5f), new(17f, 2f) };
        }
    }

    // ─── 目标（静止路人/摩托）───
    void CreateTargets()
    {
        var parent = new GameObject("Targets").transform;
        var occupied = new List<Vector2>();

        for (int i = 0; i < personCount; i++)
        {
            var pos = GetRandomPos(occupied, -7f, 22f, 0.5f, 8f);
            CreatePerson(pos, parent);
        }

        for (int i = 0; i < carCount; i++)
        {
            var pos = GetRandomPos(occupied, -7f, 22f, 0.5f, 8f);
            CreateMotorcycle(pos, parent);
        }

    }

    Vector2 GetRandomPos(List<Vector2> occupied, float xMin, float xMax, float yMin, float yMax)
    {
        for (int a = 0; a < 50; a++)
        {
            var pos = new Vector2(Random.Range(xMin, xMax), Random.Range(yMin, yMax));
            bool blocked = false;
            foreach (var p in occupied)
            {
                if (Vector2.Distance(pos, p) < 1.5f) { blocked = true; break; }
            }
            if (!blocked) { occupied.Add(pos); return pos; }
        }
        return new Vector2(Random.Range(xMin, xMax), Random.Range(yMin, yMax));
    }

    void CreatePerson(Vector2 pos, Transform parent)
    {
        var go = new GameObject("Person");
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.tag = "Target";

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite(10, Color.white);
        sr.color = new Color(0.8f, 0.8f, 0.8f); // 浅灰圆
        sr.sortingOrder = 2;

        var bc = go.AddComponent<CircleCollider2D>();
        bc.radius = 0.35f;
        bc.isTrigger = true;

        go.AddComponent<Target>().type = Target.TargetType.Person;
    }

    void CreateMotorcycle(Vector2 pos, Transform parent)
    {
        var go = new GameObject("Motorcycle");
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.tag = "Target";

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite(14, Color.white);
        sr.color = new Color(0.65f, 0.65f, 0.65f); // 深灰大圆
        sr.sortingOrder = 2;

        var bc = go.AddComponent<CircleCollider2D>();
        bc.radius = 0.55f;
        bc.isTrigger = true;

        go.AddComponent<Target>().type = Target.TargetType.Car;
    }

    // ─── 起点/终点标记（蓝色方块）───
    void CreateStartEndMarkers()
    {
        CreateMarker("StartMarker", startPos, new Color(0.4f, 0.55f, 0.85f));
        CreateMarker("EndMarker", endPos, new Color(0.4f, 0.55f, 0.85f));
    }

    void CreateMarker(string name, Vector3 pos, Color color)
    {
        var go = new GameObject(name);
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateRectSprite(32, 48, Color.white);
        sr.color = color;
        sr.sortingOrder = 5;

        go.transform.localScale = new Vector3(1.5f, 2.5f, 1f);
    }

    // ─── 玩家（夜鹭）停在起点，先不动 ───
    void CreatePlayer()
    {
        var go = new GameObject("Player");
        go.transform.position = startPos;
        go.tag = "Player";

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadExternalBirdSprite();
        sr.sortingOrder = 6;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.isKinematic = true;

        var bc = go.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(0.8f, 0.5f);
        bc.isTrigger = true;

        var poopPoint = new GameObject("PoopPoint");
        poopPoint.transform.SetParent(go.transform);
        poopPoint.transform.localPosition = new Vector3(0, -1.1f, 0);

        var playerBird = go.AddComponent<PlayerBird>();
        playerBird.poopPoint = poopPoint.transform;
        playerBird.flySpeed = 6f;
        playerBird.poopSpeed = 12f;
        playerBird.autoPoop = false; // 编辑阶段不自动拉
        playerBird.enabled = false; // 设计阶段先不动
        this.playerBird = playerBird;
    }

    // ─── 锚点编辑器 ───
    void CreateAnchorEditor()
    {
        var go = new GameObject("AnchorEditor");
        editor = go.AddComponent<AnchorEditor>();
        editor.startPos = startPos;
        editor.endPos = endPos;
        editor.isEditing = true;
    }

    // ─── UI ───
    void CreateUI()
    {
        // 必须有 EventSystem，否则 Button 点击无效
        // EventSystem 由 GameManager 跨场景持久化创建，LevelBuilder 不再创建
        // if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        // {
        //     var esGO = new GameObject("EventSystem");
        //     esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        //     esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        // }

        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var instrText = CreateUIText("Instruction",
            "左键空白：放锚点 | 左键锚点：选中/激活手柄 | 拖蓝点：调曲线 | 退格：撤销",
            new Vector2(0, -8), 14, TextAnchor.UpperCenter, canvasGO.transform);
        var instrRt = instrText.GetComponent<RectTransform>();
        instrRt.anchorMin = new Vector2(0.5f, 1f);
        instrRt.anchorMax = new Vector2(0.5f, 1f);
        instrRt.sizeDelta = new Vector2(700, 30);

        var anchorText = CreateUIText("AnchorCount", "锚点: 0/4  选中后拖蓝色手柄调曲线",
            new Vector2(0, -34), 18, TextAnchor.UpperCenter, canvasGO.transform);
        var anchorRt = anchorText.GetComponent<RectTransform>();
        anchorRt.anchorMin = new Vector2(0.5f, 1f);
        anchorRt.anchorMax = new Vector2(0.5f, 1f);
        anchorRt.sizeDelta = new Vector2(500, 40);

        // 把锚点数量文本传给 AnchorEditor
        var editor = FindAnyObjectByType<AnchorEditor>();
        if (editor != null) editor.anchorCountText = anchorText.GetComponent<Text>();

        // ─── 锚点库存面板（底部）───
        CreateAnchorStockUI(canvasGO.transform, editor);

        // ─── 确认按钮（右侧中间）───
        CreateConfirmButton(canvasGO.transform);
    }

    void CreateAnchorStockUI(Transform canvas, AnchorEditor editor)
    {
        var panel = new GameObject("AnchorStockPanel");
        panel.transform.SetParent(canvas);

        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0, 40);
        rt.sizeDelta = new Vector2(160, 45);

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.9f, 0.85f, 0.75f);

        // 标签
        var label = CreateUIText("AnchorStockLabel", "锚点个数",
            new Vector2(0, 11), 14, TextAnchor.MiddleCenter, panel.transform);
        label.GetComponent<RectTransform>().sizeDelta = new Vector2(140, 20);

        // 4 个黑色方块
        var stockImages = new List<Image>();
        for (int i = 0; i < 4; i++)
        {
            var sq = new GameObject("StockSquare_" + i);
            sq.transform.SetParent(panel.transform);

            var srt = sq.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 0.5f);
            srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = new Vector2((i - 1.5f) * 28, -8);
            srt.sizeDelta = new Vector2(20, 20);

            var img = sq.AddComponent<Image>();
            img.color = Color.black;
            stockImages.Add(img);
        }

        if (editor != null) editor.anchorStockImages = stockImages;
    }

    /// <summary>
    /// 确认按钮：锁定编辑，鸟开始沿玩家画的路线飞行，并持续拉屎
    /// </summary>
    void CreateConfirmButton(Transform canvas)
    {
        var btnGO = new GameObject("ConfirmButton");
        btnGO.transform.SetParent(canvas);

        var btnRt = btnGO.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(1f, 0f);
        btnRt.anchorMax = new Vector2(1f, 0f);
        btnRt.anchoredPosition = new Vector2(-80, 40);
        btnRt.sizeDelta = new Vector2(120, 50);

        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.7f, 0.3f);

        var btn = btnGO.AddComponent<Button>();

        // 按钮文字
        var btnText = CreateUIText("ConfirmBtnText", "开始拉屎！",
            Vector2.zero, 20, TextAnchor.MiddleCenter, btnGO.transform);
        var btnTextRt = btnText.GetComponent<RectTransform>();
        btnTextRt.anchorMin = Vector2.zero;
        btnTextRt.anchorMax = Vector2.one;
        btnTextRt.offsetMin = Vector2.zero;
        btnTextRt.offsetMax = Vector2.zero;
        btnTextRt.anchoredPosition = Vector2.zero;

        btn.onClick.AddListener(StartGame);
    }

    /// <summary>
    /// 开始游戏：锁定锚点编辑，鸟开始飞行并自动拉屎
    /// </summary>
    void StartGame()
    {
        if (gameStarted) return;
        gameStarted = true;

        // 锁定编辑
        if (editor != null)
            editor.isEditing = false;

        // 隐藏确认按钮
        var confirmBtn = GameObject.Find("ConfirmButton");
        if (confirmBtn != null) confirmBtn.SetActive(false);

        // 隐藏操作说明
        var instr = GameObject.Find("Instruction");
        if (instr != null) instr.SetActive(false);

        var anchorText = GameObject.Find("AnchorCount");
        if (anchorText != null) anchorText.SetActive(false);

        // 隐藏锚点库存面板
        var stockPanel = GameObject.Find("AnchorStockPanel");
        if (stockPanel != null) stockPanel.SetActive(false);

        // 隐藏所有锚点可视化
        if (editor != null)
        {
            foreach (var g in editor.gameObject.scene.GetRootGameObjects())
            {
                if (g.name.StartsWith("Anchor_") || g.name.StartsWith("Handle"))
                    g.SetActive(false);
            }
        }

        // 找到玩家鸟，设置路径并启动
        if (playerBird == null)
        {
            var birds = FindObjectsByType<PlayerBird>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            playerBird = birds.Length > 0 ? birds[0] : null;
        }

        if (playerBird != null && editor != null)
        {
            var path = editor.GetCurvePath();
            playerBird.SetPath(path);
            playerBird.autoPoop = true;
            playerBird.autoPoopInterval = 0.3f;
            playerBird.enabled = true;
        }
    }

    GameObject CreateUIText(string name, string text, Vector2 pos, int fontSize,
        TextAnchor alignment, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(400, 60);

        var t = go.AddComponent<Text>();
        t.text = text;
        t.fontSize = fontSize;
        t.alignment = alignment;
        t.color = Color.white;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
              ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.raycastTarget = false;

        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = Color.black;
        shadow.effectDistance = new Vector2(2, -2);

        return go;
    }

    // ─── 精灵工具 ───
    Sprite CreateRectSprite(int width, int height, Color color)
    {
        var tex = new Texture2D(width, height);
        var pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32);
    }

    Sprite CreateCircleSprite(int radius, Color color)
    {
        int size = radius * 2;
        var tex = new Texture2D(size, size);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - radius, dy = y - radius;
                pixels[y * size + x] = (dx * dx + dy * dy <= radius * radius)
                    ? Color.white : Color.clear;
            }
        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
    }

    Sprite LoadExternalBirdSprite()
    {
        string path = Application.dataPath + "/Sprites/NightHeron.png";
        if (!File.Exists(path))
        {
            Debug.LogWarning("NightHeron.png not found, using fallback.");
            return CreateBirdSprite();
        }

        byte[] bytes = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();

        var flipped = FlipTextureHorizontally(tex);

        float ppu = flipped.width / 2f;
        return Sprite.Create(flipped, new Rect(0, 0, flipped.width, flipped.height),
                             new Vector2(0.5f, 0.5f), ppu);
    }

    Sprite LoadMapSprite()
    {
        string path = Application.dataPath + "/Sprites/Map.png";
        if (!File.Exists(path))
        {
            Debug.LogWarning("Map.png not found, using fallback ground. Please save the map image to Assets/Sprites/Map.png");
            return null;
        }

        byte[] bytes = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();

        float ppu = tex.width / 32f;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                             new Vector2(0.5f, 0.5f), ppu);
    }

    Texture2D FlipTextureHorizontally(Texture2D original)
    {
        int w = original.width;
        int h = original.height;
        var flipped = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                flipped.SetPixel(x, y, original.GetPixel(w - 1 - x, y));
        flipped.Apply();
        return flipped;
    }

    Sprite CreateBirdSprite()
    {
        int w = 64, h = 32;
        var tex = new Texture2D(w, h);
        var pixels = new Color[w * h];
        Color body = new Color(0.45f, 0.5f, 0.6f);

        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        void SetPx(int x, int y, Color c) { if (x >= 0 && x < w && y >= 0 && y < h) pixels[y * w + x] = c; }
        void Fill(int x0, int y0, int x1, int y1, Color c)
        { for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) SetPx(x, y, c); }

        Fill(20, 12, 47, 21, body);
        Fill(44, 18, 55, 25, body);
        Fill(54, 20, 61, 22, Color.yellow);
        Fill(24, 14, 41, 19, new Color(0.35f, 0.4f, 0.5f));
        Fill(8, 16, 21, 19, body);
        SetPx(50, 22, Color.white); SetPx(50, 23, Color.white);
        SetPx(51, 22, Color.white); SetPx(51, 23, Color.white);
        SetPx(51, 22, Color.black);

        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32);
    }
}
