using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
/// <summary>
/// 可被便便砸中的目标：行人 / 交通工具 / 建筑
/// 支持静止/移动/敌对三种行为模式 + 精准命中判定
/// </summary>
public class Target : MonoBehaviour
{
    public enum TargetType { Person, Building, Car, Sign }

    /// <summary>行为子类型</summary>
    public enum TargetSubType
    {
        PersonStationary = 0,   // 静止路人 +5
        PersonMoving = 1,       // 移动路人 +10
        PersonHostile = 2,      // 敌对路人 +15（被砸中额外 -5）
        VehicleStationary = 3,  // 静止摩托车 +10
        VehicleMoving = 4,      // 移动摩托车 +15
        VehicleHostile = 5,     // 敌对摩托车 +20（被砸中关卡重开）
        Building = 6            // 建筑 +50
    }

    /// <summary>命中精度等级</summary>
    public enum HitQuality { Perfect, Good, Emm }

    [Header("美术资源（路人）")]
    public Sprite normalSprite;
    public Sprite poopedSprite;

    [Header("类型")]
    public TargetType type = TargetType.Person;
    public TargetSubType subType = TargetSubType.PersonStationary;

    [Header("移动（仅 Moving 类型生效）")]
    public bool isMoving = false;
    public Vector2 moveDir = Vector2.right;
    public float moveSpeed = 1.5f;
    public float moveRange = 3f;   // 来回移动范围

    [Header("敌对（仅 Hostile 类型生效）")]
    public bool isHostile = false;
    public float attackInterval = 2f;
    public Color hostileTint = new Color(1f, 0.3f, 0.2f, 1f);

    private SpriteRenderer sr;
    private bool pooped;
    private Vector3 originPos;
    private float moveTimer;
    private float attackTimer;
    private int moveDirSign = 1;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        originPos = transform.position;

        if (sr != null)
        {
            // 有自定义贴图则使用自定义贴图，否则使用默认圆圈
            if (normalSprite != null)
                sr.sprite = normalSprite;

            // 敌对单位红色闪烁（仅没有自定义贴图时）
            if (isHostile && normalSprite == null)
            {
                sr.color = hostileTint;
                StartCoroutine(HostileFlash());
            }
        }
    }

    void Update()
    {
        if (pooped || !isMoving) return;

        // 来回移动
        moveTimer += Time.deltaTime * moveSpeed * moveDirSign;
        transform.position = originPos + (Vector3)(moveDir.normalized * moveTimer);

        if (Mathf.Abs(moveTimer) >= moveRange)
        {
            moveDirSign *= -1;
            moveTimer = Mathf.Clamp(moveTimer, -moveRange, moveRange);
        }
    }

    IEnumerator HostileFlash()
    {
        while (!pooped && isHostile && sr != null)
        {
            sr.color = Color.Lerp(hostileTint, Color.white, Mathf.PingPong(Time.time * 3f, 1f));
            yield return null;
        }
    }

    /// <summary>被便便命中（由 Poop.cs 调用，传入便便位置与便便半径用于精准判定）</summary>
    public int GetPooped(Vector3 poopPos, float poopRadius)
    {
        if (pooped) return 0;
        pooped = true;

        // ─── 精准度判定 ───
        // 归一化距离：0 = 正中心命中，1 = 碰撞体边缘刚好碰到
        float hitDistance = Vector2.Distance(transform.position, poopPos);
        float effectiveRadius = GetEffectiveRadiusInDirection(poopPos);
        float maxDist = effectiveRadius + poopRadius;
        float normalizedDist = maxDist > 0.001f ? hitDistance / maxDist : 0f;

        HitQuality quality;
        float multiplier;

        if (normalizedDist < 0.65f)            // 便便中心落在 65% 范围 → 精准命中
        {
            quality = HitQuality.Perfect;
            multiplier = 1.5f;
        }
        else if (normalizedDist < 0.92f)       // 落在 92% 范围 → 不错
        {
            quality = HitQuality.Good;
            multiplier = 1.2f;
        }
        else                                   // 只有最边缘蹭到才算 Emmm
        {
            quality = HitQuality.Emm;
            multiplier = 1.0f;
        }

        int baseScore = GetBaseScore();
        int finalScore = Mathf.RoundToInt(baseScore * multiplier);

        // 敌对路人：被砸中额外 -5
        if (subType == TargetSubType.PersonHostile)
            finalScore -= 5;

        // 敌对摩托车：被砸中触发关卡重开
        if (subType == TargetSubType.VehicleHostile)
        {
            ShowHitText("💀 完蛋!", finalScore, Color.red);
            StartCoroutine(SplashEffect());
            GameManager.Instance?.OnHitHostileVehicle();
            return finalScore;
        }

        // ─── 视觉反馈 ───
        if (sr != null)
        {
            if (poopedSprite != null)
                sr.sprite = poopedSprite; // 被砸中后换成狼狈版贴图
            else
            {
                sr.color = new Color(0.4f, 0.25f, 0.1f);
                transform.localScale = Vector3.one * 1.2f;
            }
        }

        // 命中特效 + 飘字
        Color labelColor = quality switch
        {
            HitQuality.Perfect => new Color(1f, 0.85f, 0.1f),
            HitQuality.Good => new Color(0.3f, 0.9f, 0.3f),
            _ => new Color(0.6f, 0.6f, 0.6f)
        };
        ShowHitEffect(quality, finalScore, labelColor);

        StartCoroutine(SplashEffect());
        return finalScore;
    }

    /// <summary>获取基础分数</summary>
    public int GetBaseScore()
    {
        return subType switch
        {
            TargetSubType.PersonStationary => 5,
            TargetSubType.PersonMoving => 10,
            TargetSubType.PersonHostile => 15,
            TargetSubType.VehicleStationary => 10,
            TargetSubType.VehicleMoving => 15,
            TargetSubType.VehicleHostile => 20,
            TargetSubType.Building => 50,
            _ => 0
        };
    }

    /// <summary>获取碰撞体在命中方向上的有效半径（用于精准判定）</summary>
    float GetEffectiveRadiusInDirection(Vector3 poopPos)
    {
        Vector2 dir = (poopPos - transform.position).normalized;

        var bc = GetComponent<BoxCollider2D>();
        if (bc != null)
        {
            float halfW = bc.size.x * transform.localScale.x * 0.5f;
            float halfH = bc.size.y * transform.localScale.y * 0.5f;
            float absX = Mathf.Abs(dir.x);
            float absY = Mathf.Abs(dir.y);
            // 从中心到盒子边缘在该方向上的距离
            if (absX < 0.0001f) return halfH;
            if (absY < 0.0001f) return halfW;
            float t = Mathf.Min(halfW / absX, halfH / absY);
            return t;
        }

        var cc = GetComponent<CircleCollider2D>();
        if (cc != null)
            return cc.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);

        return 0.5f; // 兜底
    }

    /// <summary>获取碰撞体半径（用于编辑器等不需要方向的场景）</summary>
    float GetColliderRadius()
    {
        var cc = GetComponent<CircleCollider2D>();
        if (cc != null)
            return cc.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);

        var bc = GetComponent<BoxCollider2D>();
        if (bc != null)
        {
            float w = bc.size.x * transform.localScale.x * 0.5f;
            float h = bc.size.y * transform.localScale.y * 0.5f;
            return Mathf.Min(w, h);
        }

        return 0.5f; // 兜底
    }

    /// <summary>飘字效果：命中时在目标上方弹出文字</summary>
    void ShowHitText(string label, int score, Color color)
    {
        var go = new GameObject("HitText");
        go.transform.position = transform.position + Vector3.up * 0.8f;

        var txt = go.AddComponent<TextMesh>();
        // 使用旧版 TextMesh（3D 文字，不需要 Canvas）
        txt.text = $"{label}\n+{score}";
        txt.fontSize = 32;
        txt.color = color;
        txt.alignment = TextAlignment.Center;
        txt.anchor = TextAnchor.MiddleCenter;
        txt.characterSize = 0.15f;
        txt.fontStyle = FontStyle.Bold;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 20;

        StartCoroutine(FloatAndFadeHitText(go, txt));
    }

    IEnumerator FloatAndFadeHitText(GameObject go, TextMesh txt)
    {
        float duration = 1.2f;
        float elapsed = 0;
        Vector3 startPos = go.transform.position;

        while (elapsed < duration && go != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            go.transform.position = startPos + Vector3.up * t * 1.5f;

            if (txt != null)
            {
                Color c = txt.color;
                c.a = 1f - t;
                txt.color = c;
            }
            yield return null;
        }

        if (go != null) Destroy(go);
    }

    /// <summary>命中图片特效：根据精准度弹出 Perfect / Good Job / Emmm 图片 + 分数</summary>
    void ShowHitEffect(HitQuality quality, int score, Color scoreColor)
    {
        string imageName = quality switch
        {
            HitQuality.Perfect => "perfect",
            HitQuality.Good => "goodjob",
            _ => "emmm"
        };

        // 父节点
        var root = new GameObject("HitEffect");
        root.transform.position = transform.position + Vector3.up * 1.8f;

        // 命中图片
        Sprite hitSprite = LoadHitSprite(imageName);
        SpriteRenderer sr = null;
        if (hitSprite != null)
        {
            sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = hitSprite;
            sr.color = Color.white;
            sr.sortingOrder = 25;
        }

        // 分数文字（在图片下方）
        var txtGo = new GameObject("HitScoreText");
        txtGo.transform.SetParent(root.transform);
        txtGo.transform.localPosition = new Vector3(0, -0.6f, 0);

        var txt = txtGo.AddComponent<TextMesh>();
        txt.text = $"+{score}";
        txt.fontSize = 32;
        txt.color = scoreColor;
        txt.alignment = TextAlignment.Center;
        txt.anchor = TextAnchor.MiddleCenter;
        txt.characterSize = 0.18f;
        txt.fontStyle = FontStyle.Bold;

        var mr = txtGo.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 26;

        StartCoroutine(AnimateHitEffect(root, sr, txt));
    }

    Sprite LoadHitSprite(string name)
    {
        // 优先用 Resources.Load（打包后可用），失败再回退到文件读取（Editor）
        Texture2D tex = Resources.Load<Texture2D>(name);
        if (tex == null)
        {
            string path = Application.dataPath + "/Resources/" + name + ".png";
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[Target] 命中特效图片缺失: {path}");
                return null;
            }
            byte[] bytes = File.ReadAllBytes(path);
            tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
        }

        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();

        // 1080px -> 2 世界单位，合适大小
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                             new Vector2(0.5f, 0.5f), 540f, 0, SpriteMeshType.FullRect);
    }

    IEnumerator AnimateHitEffect(GameObject root, SpriteRenderer sr, TextMesh txt)
    {
        float duration = 1.2f;
        float elapsed = 0;
        Vector3 startPos = root.transform.position;
        Color spriteColor = sr != null ? sr.color : Color.white;
        Color textColor = txt != null ? txt.color : Color.white;

        while (elapsed < duration && root != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 向上飘
            float up = Mathf.Sin(t * Mathf.PI * 0.5f) * 0.8f;
            root.transform.position = startPos + Vector3.up * up;

            // 缩放：弹一下
            float scale;
            if (t < 0.15f)
                scale = Mathf.Lerp(0.5f, 1.1f, t / 0.15f);
            else if (t < 0.4f)
                scale = Mathf.Lerp(1.1f, 1.0f, (t - 0.15f) / 0.25f);
            else
                scale = Mathf.Lerp(1.0f, 0.9f, (t - 0.4f) / 0.6f);
            root.transform.localScale = Vector3.one * scale;

            // 淡出
            float alpha = 1f - t * t;
            if (sr != null) sr.color = new Color(spriteColor.r, spriteColor.g, spriteColor.b, alpha);
            if (txt != null) txt.color = new Color(textColor.r, textColor.g, textColor.b, alpha);

            yield return null;
        }

        if (root != null) Destroy(root);
    }

    int GetScore()
    {
        return type switch
        {
            TargetType.Person => 100,
            TargetType.Building => 50,
            TargetType.Car => 150,
            TargetType.Sign => 75,
            _ => 0,
        };
    }

    IEnumerator SplashEffect()
    {
        for (int i = 0; i < 5; i++)
        {
            var splash = new GameObject("Splash");
            splash.transform.position = transform.position + (Vector3)(Random.insideUnitCircle * 0.5f);

            var sr2 = splash.AddComponent<SpriteRenderer>();
            sr2.sprite = CreateTinyCircle(4);
            sr2.color = new Color(0.5f, 0.3f, 0.1f, 0.8f);
            sr2.sortingOrder = 9;

            var rb = splash.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0.5f;
            rb.velocity = Random.insideUnitCircle * 3f;

            Destroy(splash, 1.5f);
            yield return new WaitForSeconds(0.05f);
        }
    }

    Sprite CreateTinyCircle(int radius)
    {
        int size = radius * 2;
        var tex = new Texture2D(size, size);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - radius, dy = y - radius;
                if (dx * dx + dy * dy <= radius * radius)
                    tex.SetPixel(x, y, Color.white);
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
    }
}
