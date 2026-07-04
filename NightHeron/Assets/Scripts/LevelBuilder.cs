using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

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

    /// <summary>移动目标占比（0~1），剩余为静止，最后一两个可能为敌对</summary>
    [Range(0f, 1f)]
    public float movingRatio = 0.4f;
    /// <summary>敌对目标最多数量（0 = 不要红色圆点，障碍物代替）</summary>
    public int maxHostileCount = 0;

    [Header("起点/终点")]
    public Vector3 startPos = new Vector3(-9f, 4f, 0f);
    public Vector3 endPos = new Vector3(19f, 4f, 0f);

    [Header("预制体 (Prefab) - 优先使用")]
    [Tooltip("路人预制体（需包含 SpriteRenderer、BoxCollider2D、Target）")]
    public GameObject personPrefab;
    [Tooltip("障碍物/路牌预制体（需包含 SpriteRenderer、BoxCollider2D、Target，tag=Building）")]
    public GameObject obstaclePrefab;
    [Tooltip("摩托车预制体（需包含 SpriteRenderer、CircleCollider2D、Target）")]
    public GameObject motorcyclePrefab;
    [Tooltip("玩家夜鹭预制体（需包含 SpriteRenderer、Rigidbody2D、BoxCollider2D、PlayerBird）")]
    public GameObject playerPrefab;
    [Tooltip("便便预制体（需包含 SpriteRenderer、CircleCollider2D、Rigidbody2D、Poop）")]
    public GameObject poopPrefab;
    [Tooltip("起点/终点标记预制体（需包含 SpriteRenderer）")]
    public GameObject startMarkerPrefab;
    public GameObject endMarkerPrefab;

    private AnchorEditor editor;
    private PlayerBird playerBird;
    private bool gameStarted = false;
    private bool gameEnded = false;
    private Text scoreText;
    private GameObject scoreGO;

    void Awake()
    {
        // 编辑器模式不执行
        if (!Application.isPlaying) return;

        // 如果 Inspector 没挂 Prefab，自动从 Resources/Prefabs 加载默认预制体
        if (personPrefab == null) personPrefab = Resources.Load<GameObject>("Prefabs/Person");
        if (obstaclePrefab == null) obstaclePrefab = Resources.Load<GameObject>("Prefabs/Obstacle");
        if (motorcyclePrefab == null) motorcyclePrefab = Resources.Load<GameObject>("Prefabs/Motorcycle");
        if (playerPrefab == null) playerPrefab = Resources.Load<GameObject>("Prefabs/Player");
        if (poopPrefab == null) poopPrefab = Resources.Load<GameObject>("Prefabs/Poop");
        if (startMarkerPrefab == null) startMarkerPrefab = Resources.Load<GameObject>("Prefabs/StartMarker");
        if (endMarkerPrefab == null) endMarkerPrefab = Resources.Load<GameObject>("Prefabs/EndMarker");

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
            // ─── 清理旧版本烘焙场景中的敌对目标（现在用红色方块障碍物代替） ───
            var allTargets = FindObjectsByType<Target>(FindObjectsSortMode.None);
            foreach (var t in allTargets)
            {
                if (t.isHostile)
                    Destroy(t.gameObject);
            }
            // ─── 更新障碍物：使用新 roadsign 贴图，否则保持红色方块 ───
            var obstacles = GameObject.Find("Obstacles");
            if (obstacles != null)
            {
                foreach (Transform child in obstacles.transform)
                {
                    var sr2 = child.GetComponent<SpriteRenderer>();
                    if (sr2 != null)
                    {
                        Sprite roadSign = GetRandomRoadSignSprite();
                        if (roadSign != null)
                        {
                            sr2.sprite = roadSign;
                            sr2.color = Color.white;
                            float desiredHeight = Random.Range(3f, 4.5f);
                            float spriteHeightUnits = roadSign.texture.height / roadSign.pixelsPerUnit;
                            float scale = desiredHeight / spriteHeightUnits;
                            float aspect = (float)roadSign.texture.width / roadSign.texture.height;
                            child.localScale = new Vector3(scale * aspect, scale, 1);
                        }
                        else
                        {
                            sr2.color = new Color(0.9f, 0.15f, 0.15f); // 红色
                            var scale = child.localScale;
                            child.localScale = new Vector3(Random.Range(0.3f, 0.8f), scale.y, scale.z); // 细
                        }
                    }
                    // 移除旧 Rigidbody2D，碰撞体设为非 Trigger
                    var rb2 = child.GetComponent<Rigidbody2D>();
                    if (rb2 != null) Destroy(rb2);
                    var col = child.GetComponent<BoxCollider2D>();
                    if (col != null) col.isTrigger = false;
                }
            }

        // 必须用 Include 查找，因为 PlayerBird 在烘焙场景中 enabled=false
        // ─── 更新旧路人：使用新 people 贴图，没有则用明显可见的方块 ───
        var poopedSprite = Resources.Load<Sprite>("people0");
        bool hasCustomSprite = poopedSprite != null;
        Debug.Log($"[LevelBuilder] people0 加载: {hasCustomSprite} (提示: PNG 需要在 Unity 中设置 Texture Type 为 Sprite)");

        var targetsParent = GameObject.Find("Targets");
        if (targetsParent != null)
        {
            if (hasCustomSprite)
            {
                // 让路人高度约 2.25 个世界单位（整体放大 1.5x 提升清晰度）
                float desiredHeight = 2.25f;

                foreach (Transform child in targetsParent.transform)
                {
                    var t = child.GetComponent<Target>();
                    if (t == null || t.type != Target.TargetType.Person) continue;

                    Sprite normalSprite = GetRandomPersonSprite();
                    if (normalSprite == null) continue; // 没有可用的普通贴图则跳过
                    float spriteHeightUnits = normalSprite.texture.height / normalSprite.pixelsPerUnit;
                    float scale = desiredHeight / spriteHeightUnits;

                    var sr2 = child.GetComponent<SpriteRenderer>();
                    if (sr2 != null)
                    {
                        sr2.sprite = normalSprite;
                        sr2.color = Color.white;
                    }
                    t.normalSprite = normalSprite;
                    t.poopedSprite = poopedSprite;

                    child.localScale = new Vector3(scale, scale, 1);

                    // 把旧圆形碰撞体换成更适合人体的盒状
                    var oldCircle = child.GetComponent<CircleCollider2D>();
                    if (oldCircle != null) Destroy(oldCircle);
                    var box = child.GetComponent<BoxCollider2D>();
                    if (box == null)
                    {
                        box = child.gameObject.AddComponent<BoxCollider2D>();
                        box.isTrigger = true;
                    }
                    box.size = new Vector2(0.75f / scale, 1.8f / scale);
                    box.offset = Vector2.zero;

                }
            }
            else
                {
                    // 没有自定义贴图：用 0.9×2.25 的彩色方块，确保可见
                    foreach (Transform child in targetsParent.transform)
                    {
                        var t = child.GetComponent<Target>();
                        if (t == null || t.type != Target.TargetType.Person) continue;

                        var sr2 = child.GetComponent<SpriteRenderer>();
                        if (sr2 != null)
                        {
                            sr2.color = t.subType switch
                            {
                                Target.TargetSubType.PersonStationary => new Color(0.3f, 0.75f, 0.4f),  // 绿色
                                Target.TargetSubType.PersonMoving => new Color(0.3f, 0.55f, 0.85f),      // 蓝色
                                Target.TargetSubType.PersonHostile => new Color(0.9f, 0.2f, 0.2f),       // 红色
                                _ => new Color(0.5f, 0.5f, 0.5f)
                            };
                            sr2.sortingOrder = 3;
                        }
                        // 确保尺寸可见
                        child.localScale = new Vector3(0.9f, 2.25f, 1f);
                        // 确保碰撞体是 Trigger
                        var col = child.GetComponent<Collider2D>();
                        if (col != null) col.isTrigger = true;
                    }
                }

                int personCount = 0;
                foreach (Transform child in targetsParent.transform)
                {
                    var t = child.GetComponent<Target>();
                    if (t != null && t.type == Target.TargetType.Person) personCount++;
                }
                Debug.Log($"[LevelBuilder] Targets 下找到 {personCount} 个路人");
            }
            else
            {
                Debug.LogWarning("[LevelBuilder] 场景中未找到 Targets 父对象！");
            }

            if (Camera.main != null)
                Camera.main.allowMSAA = false; // 已烘焙场景也要关闭抗锯齿

            var birds = FindObjectsByType<PlayerBird>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            playerBird = birds.Length > 0 ? birds[0] : null;
            if (playerBird != null)
            {
                // 修正鸟的碰撞体为非 Trigger
                var birdCol = playerBird.GetComponent<BoxCollider2D>();
                if (birdCol != null) birdCol.isTrigger = false;

                // 重新加载高清无模糊的鸟贴图（覆盖烘焙场景中的旧贴图）
                var sr = playerBird.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = LoadExternalBirdSprite();
                    sr.drawMode = SpriteDrawMode.Simple;
                }
            }
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
        cam.allowMSAA = false; // 关闭抗锯齿，避免像素画模糊
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
            Sprite roadSign = GetRandomRoadSignSprite();

            if (obstaclePrefab != null)
            {
                var go = Instantiate(obstaclePrefab, positions[i], Quaternion.identity, parent);
                go.name = "Obstacle_" + i;
                go.tag = "Building";

                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = roadSign ?? sr.sprite;
                    sr.color = Color.white;
                    sr.sortingOrder = 1;
                }

                var target = go.GetComponent<Target>();
                if (target == null) target = go.AddComponent<Target>();
                target.type = Target.TargetType.Building;

                if (roadSign != null)
                {
                    float desiredHeight = Random.Range(3f, 4.5f);
                    float spriteHeightUnits = roadSign.texture.height / roadSign.pixelsPerUnit;
                    float scale = desiredHeight / spriteHeightUnits;
                    float aspect = (float)roadSign.texture.width / roadSign.texture.height;
                    go.transform.localScale = new Vector3(scale * aspect, scale, 1);
                }

                var box = go.GetComponent<BoxCollider2D>();
                if (box == null) box = go.AddComponent<BoxCollider2D>();
                // 碰撞体大小匹配贴图实际尺寸（本地空间）
                float psW = roadSign != null ? roadSign.texture.width / roadSign.pixelsPerUnit : 1f;
                float psH = roadSign != null ? roadSign.texture.height / roadSign.pixelsPerUnit : 1f;
                box.size = new Vector2(psW, psH);
                box.isTrigger = true; // Trigger，由 PlayerBird 像素级检测
            }
            else
            {
                var go = CreateObstacleRuntime(positions[i], parent, roadSign);
                go.name = "Obstacle_" + i;
            }
        }
    }

    /// <summary>
    /// 运行时创建障碍物（无 Prefab 时使用，也用于编辑器生成 Prefab）。
    /// </summary>
    public static GameObject CreateObstacleRuntime(Vector2 pos, Transform parent, Sprite roadSign)
    {
        var go = new GameObject("Obstacle");
        if (parent != null) go.transform.SetParent(parent);
        go.transform.position = pos;
        go.tag = "Building";

        bool hasSign = roadSign != null;

        var srr = go.AddComponent<SpriteRenderer>();
        srr.sprite = hasSign ? roadSign : CreateRectSprite(32, 32, Color.white);
        srr.color = hasSign ? Color.white : new Color(0.9f, 0.15f, 0.15f);
        srr.sortingOrder = 1;

        if (hasSign)
        {
            float desiredHeight = Random.Range(3f, 4.5f);
            float spriteHeightUnits = roadSign.texture.height / roadSign.pixelsPerUnit;
            float scale = desiredHeight / spriteHeightUnits;
            float aspect = (float)roadSign.texture.width / roadSign.texture.height;
            go.transform.localScale = new Vector3(scale * aspect, scale, 1);
        }
        else
        {
            float w = Random.Range(0.45f, 1.2f);
            float h = Random.Range(1.8f, 5.25f);
            go.transform.localScale = new Vector3(w, h, 1);
        }

        var bcc = go.AddComponent<BoxCollider2D>();
        // 碰撞体大小匹配贴图实际尺寸（本地空间，缩放后覆盖整个贴图）
        float sprW = hasSign ? roadSign.texture.width / roadSign.pixelsPerUnit : 1f;
        float sprH = hasSign ? roadSign.texture.height / roadSign.pixelsPerUnit : 1f;
        bcc.size = new Vector2(sprW, sprH);
        bcc.isTrigger = true; // Trigger，由 PlayerBird 像素级检测

        var target = go.AddComponent<Target>();
        target.type = Target.TargetType.Building;

        return go;
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

    // ─── 目标（静止/移动/敌对 路人/摩托）───
    void CreateTargets()
    {
        var parent = new GameObject("Targets").transform;
        var occupied = new List<Vector2>();

        // 计算移动和敌对数量
        int hostilePersonCount = Mathf.Min(maxHostileCount, Mathf.Max(1, personCount / 4));
        int movingPersonCount = Mathf.RoundToInt((personCount - hostilePersonCount) * movingRatio);
        int stationaryPersonCount = personCount - movingPersonCount - hostilePersonCount;

        int hostileCarCount = Mathf.Min(maxHostileCount, 1);
        int movingCarCount = Mathf.RoundToInt((carCount - hostileCarCount) * movingRatio);
        int stationaryCarCount = carCount - movingCarCount - hostileCarCount;

        // 生成静止路人
        for (int i = 0; i < stationaryPersonCount; i++)
        {
            var pos = GetRandomPos(occupied, -7f, 22f, 0.5f, 8f);
            CreatePerson(pos, parent, Target.TargetSubType.PersonStationary, false, false);
        }

        // 生成移动路人
        for (int i = 0; i < movingPersonCount; i++)
        {
            var pos = GetRandomPos(occupied, -7f, 22f, 0.5f, 8f);
            CreatePerson(pos, parent, Target.TargetSubType.PersonMoving, true, false);
        }

        // 生成敌对路人
        for (int i = 0; i < hostilePersonCount; i++)
        {
            var pos = GetRandomPos(occupied, -7f, 22f, 0.5f, 8f);
            CreatePerson(pos, parent, Target.TargetSubType.PersonHostile, false, true);
        }

        // 生成静止摩托车
        for (int i = 0; i < stationaryCarCount; i++)
        {
            var pos = GetRandomPos(occupied, -7f, 22f, 0.5f, 8f);
            CreateMotorcycle(pos, parent, Target.TargetSubType.VehicleStationary, false, false);
        }

        // 生成移动摩托车
        for (int i = 0; i < movingCarCount; i++)
        {
            var pos = GetRandomPos(occupied, -7f, 22f, 0.5f, 8f);
            CreateMotorcycle(pos, parent, Target.TargetSubType.VehicleMoving, true, false);
        }

        // 生成敌对摩托车
        for (int i = 0; i < hostileCarCount; i++)
        {
            var pos = GetRandomPos(occupied, -7f, 22f, 0.5f, 8f);
            CreateMotorcycle(pos, parent, Target.TargetSubType.VehicleHostile, false, true);
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

    /// <summary>
    /// 运行时创建路人（无 Prefab 时使用，也用于编辑器生成 Prefab）。
    /// </summary>
    public static GameObject CreatePersonRuntime(Vector2 pos, Transform parent,
        Target.TargetSubType subType, bool isMoving, bool isHostile,
        Sprite normalSprite, Sprite poopedSprite)
    {
        var go = new GameObject("Person");
        if (parent != null) go.transform.SetParent(parent);
        go.transform.position = pos;
        go.tag = "Target";

        bool hasCustomSprite = normalSprite != null && poopedSprite != null;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = hasCustomSprite ? normalSprite : CreateCircleSprite(10, Color.white);
        sr.sortingOrder = 2;

        // 没有自定义贴图时才用颜色区分类型
        if (!hasCustomSprite)
        {
            sr.color = subType switch
            {
                Target.TargetSubType.PersonStationary => new Color(0.8f, 0.8f, 0.8f),
                Target.TargetSubType.PersonMoving => new Color(0.5f, 0.7f, 0.9f),
                Target.TargetSubType.PersonHostile => new Color(1f, 0.35f, 0.3f),
                _ => new Color(0.8f, 0.8f, 0.8f)
            };
        }
        else
        {
            sr.color = Color.white;
        }

        // 碰撞体
        if (hasCustomSprite)
        {
            // 让路人高度约 2.25 个世界单位
            float desiredHeight = 2.25f;
            float spriteHeightUnits = normalSprite.texture.height / normalSprite.pixelsPerUnit;
            float scale = desiredHeight / spriteHeightUnits;
            go.transform.localScale = new Vector3(scale, scale, 1);

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.75f / scale, 1.8f / scale);
            box.offset = new Vector2(0, 0);
            box.isTrigger = true;
        }
        else
        {
            var bc = go.AddComponent<CircleCollider2D>();
            bc.radius = 0.525f;
            bc.isTrigger = true;
        }

        var target = go.AddComponent<Target>();
        target.type = Target.TargetType.Person;
        target.subType = subType;
        target.normalSprite = normalSprite;
        target.poopedSprite = poopedSprite;

        ConfigureTargetMovementAndHostile(target, isMoving, isHostile);
        return go;
    }

    /// <summary>把移动/敌对参数设置到 Target 上</summary>
    static void ConfigureTargetMovementAndHostile(Target target, bool isMoving, bool isHostile)
    {
        if (isMoving)
        {
            target.isMoving = true;
            target.moveDir = Random.value > 0.5f ? Vector2.right : Vector2.up;
            target.moveSpeed = Random.Range(1f, 2.5f);
            target.moveRange = Random.Range(1.5f, 4f);
        }
        if (isHostile)
        {
            target.isHostile = true;
            target.attackInterval = Random.Range(1.5f, 3f);
        }
    }

    void CreatePerson(Vector2 pos, Transform parent, Target.TargetSubType subType, bool isMoving, bool isHostile)
    {
        Sprite normalSprite = GetRandomPersonSprite();
        Sprite poopedSprite = GetPoopedPersonSprite();

        if (personPrefab != null)
        {
            var go = Instantiate(personPrefab, pos, Quaternion.identity, parent);
            go.name = "Person";
            go.tag = "Target";

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = normalSprite ?? sr.sprite;
                sr.color = normalSprite != null ? Color.white : sr.color;
                sr.sortingOrder = 2;
            }

            var target = go.GetComponent<Target>();
            if (target == null) target = go.AddComponent<Target>();
            target.type = Target.TargetType.Person;
            target.subType = subType;
            target.normalSprite = normalSprite;
            target.poopedSprite = poopedSprite;

            // 有自定义贴图时统一缩放与碰撞体
            if (normalSprite != null && poopedSprite != null)
            {
                float desiredHeight = 2.25f;
                float spriteHeightUnits = normalSprite.texture.height / normalSprite.pixelsPerUnit;
                float scale = desiredHeight / spriteHeightUnits;
                go.transform.localScale = new Vector3(scale, scale, 1);

                var box = go.GetComponent<BoxCollider2D>();
                if (box == null) box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(0.75f / scale, 1.8f / scale);
                box.offset = Vector2.zero;

                var circle = go.GetComponent<CircleCollider2D>();
                if (circle != null) Destroy(circle);
            }

            ConfigureTargetMovementAndHostile(target, isMoving, isHostile);
        }
        else
        {
            CreatePersonRuntime(pos, parent, subType, isMoving, isHostile, normalSprite, poopedSprite);
        }
    }

    /// <summary>
    /// 运行时创建摩托车（无 Prefab 时使用，也用于编辑器生成 Prefab）。
    /// </summary>
    public static GameObject CreateMotorcycleRuntime(Vector2 pos, Transform parent,
        Target.TargetSubType subType, bool isMoving, bool isHostile)
    {
        var go = new GameObject("Motorcycle");
        if (parent != null) go.transform.SetParent(parent);
        go.transform.position = pos;
        go.tag = "Target";

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite(14, Color.white);
        sr.color = subType switch
        {
            Target.TargetSubType.VehicleStationary => new Color(0.65f, 0.65f, 0.65f),
            Target.TargetSubType.VehicleMoving => new Color(0.4f, 0.5f, 0.75f),
            Target.TargetSubType.VehicleHostile => new Color(0.9f, 0.15f, 0.15f),
            _ => new Color(0.65f, 0.65f, 0.65f)
        };
        sr.sortingOrder = 2;

        var bc = go.AddComponent<CircleCollider2D>();
        bc.radius = 0.55f;
        bc.isTrigger = true;

        var target = go.AddComponent<Target>();
        target.type = Target.TargetType.Car;
        target.subType = subType;

        ConfigureTargetMovementAndHostile(target, isMoving, isHostile);
        return go;
    }

    void CreateMotorcycle(Vector2 pos, Transform parent, Target.TargetSubType subType, bool isMoving, bool isHostile)
    {
        if (motorcyclePrefab != null)
        {
            var go = Instantiate(motorcyclePrefab, pos, Quaternion.identity, parent);
            go.name = "Motorcycle";
            go.tag = "Target";

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = 2;

            var target = go.GetComponent<Target>();
            if (target == null) target = go.AddComponent<Target>();
            target.type = Target.TargetType.Car;
            target.subType = subType;

            ConfigureTargetMovementAndHostile(target, isMoving, isHostile);
        }
        else
        {
            CreateMotorcycleRuntime(pos, parent, subType, isMoving, isHostile);
        }
    }

    // ─── 起点/终点标记（蓝色方块）───
    void CreateStartEndMarkers()
    {
        CreateMarker("StartMarker", startPos, new Color(0.4f, 0.55f, 0.85f), startMarkerPrefab);
        CreateMarker("EndMarker", endPos, new Color(0.4f, 0.55f, 0.85f), endMarkerPrefab);
    }

    void CreateMarker(string name, Vector3 pos, Color color, GameObject prefab)
    {
        if (prefab != null)
        {
            var go = Instantiate(prefab, pos, Quaternion.identity);
            go.name = name;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = color;
                sr.sortingOrder = 5;
            }
            go.transform.localScale = new Vector3(2.25f, 3.75f, 1f);
        }
        else
        {
            CreateMarkerRuntime(name, pos, color);
        }
    }

    /// <summary>
    /// 运行时创建起点/终点标记（无 Prefab 时使用，也用于编辑器生成 Prefab）。
    /// </summary>
    public static GameObject CreateMarkerRuntime(string name, Vector3 pos, Color color)
    {
        var go = new GameObject(name);
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateRectSprite(32, 48, Color.white);
        sr.color = color;
        sr.sortingOrder = 5;

        go.transform.localScale = new Vector3(2.25f, 3.75f, 1f);
        return go;
    }

    // ─── 玩家（夜鹭）停在起点，先不动 ───
    void CreatePlayer()
    {
        if (playerPrefab != null)
        {
            var go = Instantiate(playerPrefab, startPos, Quaternion.identity);
            go.name = "Player";
            go.tag = "Player";

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = LoadExternalBirdSprite();
                sr.sortingOrder = 6;
            }

            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0;
                rb.isKinematic = true;
            }

            var bc = go.GetComponent<BoxCollider2D>();
            if (bc == null) bc = go.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(1.2f, 0.75f);

            var playerBird = go.GetComponent<PlayerBird>();
            if (playerBird == null) playerBird = go.AddComponent<PlayerBird>();

            var poopPoint = go.transform.Find("PoopPoint");
            if (poopPoint == null)
            {
                var poopPointGO = new GameObject("PoopPoint");
                poopPointGO.transform.SetParent(go.transform);
                poopPointGO.transform.localPosition = new Vector3(0, -1.65f, 0);
                poopPoint = poopPointGO.transform;
            }
            playerBird.poopPoint = poopPoint;
            playerBird.poopPrefab = poopPrefab;
            playerBird.flySpeed = 6f;
            playerBird.poopSpeed = 12f;
            playerBird.autoPoop = false; // 编辑阶段不自动拉
            playerBird.enabled = false; // 设计阶段先不动
            this.playerBird = playerBird;
        }
        else
        {
            var go = CreatePlayerRuntime(startPos, poopPrefab, out PlayerBird pb);
            this.playerBird = pb;
        }
    }

    /// <summary>
    /// 运行时创建玩家（无 Prefab 时使用，也用于编辑器生成 Prefab）。
    /// </summary>
    public static GameObject CreatePlayerRuntime(Vector3 startPos, GameObject poopPrefab, out PlayerBird playerBird)
    {
        var go = new GameObject("Player");
        go.transform.position = startPos;
        go.tag = "Player";

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = null; // Prefab 生成时不需要外部贴图；运行时由调用方覆盖
        sr.sortingOrder = 6;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.isKinematic = true;

        var bc = go.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(1.2f, 0.75f);
        // 非 Trigger，由 PlayerBird 手动 Physics2D.OverlapCollider 检测

        var poopPoint = new GameObject("PoopPoint");
        poopPoint.transform.SetParent(go.transform);
        poopPoint.transform.localPosition = new Vector3(0, -1.65f, 0);

        var pb = go.AddComponent<PlayerBird>();
        pb.poopPoint = poopPoint.transform;
        pb.poopPrefab = poopPrefab;
        pb.flySpeed = 6f;
        pb.poopSpeed = 12f;
        pb.autoPoop = false;
        pb.enabled = false;
        playerBird = pb;
        return go;
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

    // ─── 分数 UI ───

    void ShowScoreUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;

        scoreGO = new GameObject("ScoreText");
        scoreGO.transform.SetParent(canvas.transform);

        var rt = scoreGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20, -30);
        rt.sizeDelta = new Vector2(200, 40);

        scoreText = scoreGO.AddComponent<Text>();
        scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                     ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        scoreText.fontSize = 24;
        scoreText.alignment = TextAnchor.MiddleRight;
        scoreText.color = Color.white;
        scoreText.fontStyle = FontStyle.Bold;

        var shadow = scoreGO.AddComponent<Shadow>();
        shadow.effectColor = Color.black;
        shadow.effectDistance = new Vector2(1, -1);

        UpdateScoreDisplay();
    }

    void Update()
    {
        if (!gameStarted || gameEnded) return;
        UpdateScoreDisplay();
    }

    void UpdateScoreDisplay()
    {
        if (scoreText == null || GameManager.Instance == null) return;
        int current = GameManager.Instance.currentScore;
        int max = GameManager.Instance.maxPossibleScore;
        scoreText.text = $"💩 {current} / {max}";
    }

    IEnumerator ShowResultDelayed(bool won)
    {
        yield return new WaitForSeconds(1.5f);
        ShowResultScreen(won);
    }

    /// <summary>显示结算画面</summary>
    void ShowResultScreen(bool won)
    {
        // 停止鸟
        if (playerBird != null)
        {
            playerBird.enabled = false;
            playerBird.autoPoop = false;
        }

        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;

        var panel = new GameObject("ResultPanel");
        panel.transform.SetParent(canvas.transform);
        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.7f);

        // 标题
        CreateResultText(panel.transform, won ? "通关成功！🎉" : "撞到障碍物！💥",
            new Vector2(0, 120), 48,
            won ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.3f, 0.3f));

        if (won)
        {
            int current = GameManager.Instance != null ? GameManager.Instance.currentScore : 0;
            int starCount = GameManager.Instance != null ? GameManager.Instance.GetStarRating() : 0;

            CreateResultText(panel.transform, $"得分: {current}",
                new Vector2(0, 40), 32, Color.white);

            string stars = "";
            for (int i = 0; i < 3; i++)
                stars += i < starCount ? "★" : "☆";
            CreateResultText(panel.transform, stars,
                new Vector2(0, -20), 48, Color.white);
        }
        else
        {
            CreateResultText(panel.transform, "夜鹭撞毁，请重新规划路线",
                new Vector2(0, 40), 24, new Color(0.9f, 0.7f, 0.7f));
        }

        // 按钮
        CreateResultButton(panel.transform, "重新开始", new Vector2(-100, -120),
            () => SceneManager.LoadScene(SceneManager.GetActiveScene().name));

        CreateResultButton(panel.transform, "返回关卡", new Vector2(100, -120),
            () => GameManager.Instance?.ReturnToMenu());
    }

    void CreateResultText(Transform parent, string content, Vector2 pos, int fontSize, Color color)
    {
        var go = new GameObject("ResultText");
        go.transform.SetParent(parent);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(600, 80);

        var txt = go.AddComponent<Text>();
        txt.text = content;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = fontSize;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = color;
    }

    void CreateResultButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction callback)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(150, 50);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 0.3f);

        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(callback);

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform);
        var txtRt = txtGO.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        var txt = txtGO.AddComponent<Text>();
        txt.text = label;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                 ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = 20;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
    }

    /// <summary>
    /// 开始游戏：锁定锚点编辑，鸟开始飞行并自动拉屎
    /// </summary>
    void StartGame()
    {
        if (gameStarted) return;
        gameStarted = true;

        // ─── 重置分数 ───
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetScore();

            // 满分固定 100，星级阈值：20→⭐、60→⭐⭐、90→⭐⭐⭐
            GameManager.Instance.maxPossibleScore = 100;
        }

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

        // ─── 显示分数 UI ───
        ShowScoreUI();

        // 找到玩家鸟，设置路径并启动
        if (playerBird == null)
        {
            var birds = FindObjectsByType<PlayerBird>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            playerBird = birds.Length > 0 ? birds[0] : null;
        }

        if (playerBird != null && editor != null)
        {
            // 鸟和障碍物都是 Trigger，碰撞通过 OnTriggerEnter2D 检测
            var path = editor.GetCurvePath();
            playerBird.SetPath(path);
            playerBird.SetEndPos(endPos);
            playerBird.autoPoop = true;
            playerBird.autoPoopInterval = 0.3f;

            // ─── 通关/失败回调 ───
            playerBird.OnReachEnd = () =>
            {
                if (gameEnded) return;
                gameEnded = true;
                StartCoroutine(ShowResultDelayed(true));
            };
            playerBird.OnHitObstacle = () =>
            {
                if (gameEnded) return;
                gameEnded = true;
                ShowResultScreen(false);
            };

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
    static Sprite CreateRectSprite(int width, int height, Color color)
    {
        var tex = new Texture2D(width, height);
        var pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32);
    }

    static Sprite CreateCircleSprite(int radius, Color color)
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

    /// <summary>随机选择路人普通贴图（people1 ~ people6），任意一张缺失时从剩下的随机</summary>
    Sprite GetRandomPersonSprite()
    {
        var available = new List<Sprite>();
        for (int i = 1; i <= 6; i++)
        {
            var s = Resources.Load<Sprite>("people" + i);
            if (s != null) available.Add(s);
        }
        if (available.Count == 0) return null;
        return available[Random.Range(0, available.Count)];
    }

    /// <summary>路人被砸中后的统一贴图</summary>
    Sprite GetPoopedPersonSprite()
    {
        return Resources.Load<Sprite>("people0");
    }

    /// <summary>7 种障碍物样式</summary>
    static readonly string[] ObstacleSpriteNames = {
        "birdsign", "fruitshop", "popsign",
        "shop1", "shop2", "shop3", "shopwithtree"
    };

    /// <summary>随机选择一个障碍物贴图</summary>
    Sprite GetRandomRoadSignSprite()
    {
        var available = new List<Sprite>();
        foreach (var name in ObstacleSpriteNames)
        {
            var s = Resources.Load<Sprite>(name);
            if (s != null) available.Add(s);
        }
        if (available.Count == 0) return null;
        return available[Random.Range(0, available.Count)];
    }

    /// <summary>获取第 index 个障碍物贴图（0~6），用于生成 Prefab</summary>
    public static Sprite GetObstacleSpriteByIndex(int index)
    {
        if (index < 0 || index >= ObstacleSpriteNames.Length) return null;
        return Resources.Load<Sprite>(ObstacleSpriteNames[index]);
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
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        var flipped = FlipTextureHorizontally(tex);

        float ppu = flipped.width / 3f; // 鸟从 2 单位 → 3 单位（1.5x 放大）
        return Sprite.Create(flipped, new Rect(0, 0, flipped.width, flipped.height),
                             new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
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
        flipped.filterMode = FilterMode.Point;
        flipped.wrapMode = TextureWrapMode.Clamp;
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
