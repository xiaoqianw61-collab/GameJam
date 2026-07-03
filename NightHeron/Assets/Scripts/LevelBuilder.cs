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
    [Header("地图")]
    public float mapWidth = 28f;
    public float mapHeight = 14f;

    [Header("目标数量")]
    public int obstacleCount = 5;
    public int personCount = 8;
    public int carCount = 4;

    [Header("兼容编辑器工具")]
    public int buildingCount = 5;
    public int signCount = 3;

    [Header("起点/终点")]
    public Vector3 startPos = new Vector3(-9f, 4f, 0f);
    public Vector3 endPos = new Vector3(19f, 4f, 0f);

    void Awake()
    {
        GenerateLevel();
    }

    void GenerateLevel()
    {
        CreateCamera();
        CreateGround();
        CreateObstacles();
        CreateTargets();
        CreateStartEndMarkers();
        CreatePlayer();
        CreateAnchorEditor();
        CreateGameManager();
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

        Vector2[] positions = {
            new(-3f, 3f), new(3f, 6f), new(8f, 2.5f),
            new(14f, 5f), new(17f, 2f)
        };

        for (int i = 0; i < Mathf.Min(obstacleCount, positions.Length); i++)
        {
            var go = new GameObject("Obstacle_" + i);
            go.transform.SetParent(parent);
            go.transform.position = positions[i];
            go.tag = "Building";

            var srr = go.AddComponent<SpriteRenderer>();
            srr.sprite = CreateRectSprite(32, 32, Color.white);
            srr.color = new Color(0.7f, 0.7f, 0.7f); // 浅灰方块
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

        // var gm = FindAnyObjectByType<GameManager>();
        // if (gm != null) gm.SetTotalTargets(personCount + carCount + obstacleCount);
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
        playerBird.enabled = false; // 设计阶段先不动
    }

    // ─── 锚点编辑器 ───
    void CreateAnchorEditor()
    {
        var go = new GameObject("AnchorEditor");
        var editor = go.AddComponent<AnchorEditor>();
        editor.startPos = startPos;
        editor.endPos = endPos;
    }

    // ─── GameManager ───
    void CreateGameManager()
    {
        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
    }

    // ─── UI ───
    void CreateUI()
    {
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var scoreText = CreateUIText("Score", "Score: 0",
            new Vector2(16, -12), 24, TextAnchor.UpperLeft, canvasGO.transform);

        var comboText = CreateUIText("Combo", "",
            new Vector2(16, -44), 28, TextAnchor.UpperLeft, canvasGO.transform);

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

        var panel = new GameObject("GameOverPanel");
        panel.transform.SetParent(canvasGO.transform);
        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero; panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero; panelRt.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);
        panel.SetActive(false);

        var finalText = CreateUIText("FinalText", "",
            Vector2.zero, 36, TextAnchor.MiddleCenter, panel.transform);

        var gm = FindAnyObjectByType<GameManager>();
        if (gm != null)
        {
            // gm.scoreText = scoreText.GetComponent<Text>();
            // gm.comboText = comboText.GetComponent<Text>();
            // gm.instructionText = instrText.GetComponent<Text>();
            // gm.gameOverPanel = panel;
            // gm.finalScoreText = finalText.GetComponent<Text>();
        }

        // 把锚点数量文本传给 AnchorEditor
        var editor = FindAnyObjectByType<AnchorEditor>();
        if (editor != null) editor.anchorCountText = anchorText.GetComponent<Text>();

        // ─── 锚点库存面板（底部）───
        CreateAnchorStockUI(canvasGO.transform, editor);
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
