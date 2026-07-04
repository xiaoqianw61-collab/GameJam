using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 夜鹭 - 沿贝塞尔曲线飞行，玩家控制投便便
/// </summary>
public class PlayerBird : MonoBehaviour
{
    [Header("飞行")]
    public float flySpeed = 5f;
    public float poopCooldown = 0.25f;

    [Header("投便便")]
    public GameObject poopPrefab;
    public Transform poopPoint;
    public float poopSpeed = 10f;

    [Header("自动拉屎")]
    public bool autoPoop = true;
    public float autoPoopInterval = 0.3f;

    private List<Vector3> curvePath;
    private int pathIndex;
    private float poopTimer;
    private float autoPoopTimer;
    private SpriteRenderer sr;
    private bool finished;
    private bool hasWon;
    private Vector3 endPos;
    private Collider2D birdCollider;
    private Collider2D[] overlapResults = new Collider2D[8];
    private ContactFilter2D obstacleFilter;

    /// <summary>到达终点事件</summary>
    public System.Action OnReachEnd;

    /// <summary>撞到障碍物事件</summary>
    public System.Action OnHitObstacle;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        birdCollider = GetComponent<BoxCollider2D>();
        obstacleFilter = new ContactFilter2D();
        obstacleFilter.useTriggers = true; // 障碍物是 Trigger，像素级检测
        obstacleFilter.useLayerMask = false;
    }

    /// <summary>设置终点位置（用于通关检测）</summary>
    public void SetEndPos(Vector3 end) { endPos = end; }

    public void SetPath(List<Vector3> path)
    {
        curvePath = path;
        if (path.Count > 0)
            transform.position = path[0];
        pathIndex = 0;
        finished = false;
    }

    void Update()
    {
        if (finished || curvePath == null || curvePath.Count == 0) return;

        FlyAlongCurve();

        // 每帧手动检测是否撞到障碍物（比 OnTriggerEnter2D 更可靠）
        if (!hasWon && !finished)
            CheckObstacleCollision();

        poopTimer -= Time.deltaTime;

        // 自动连续拉屎（不受手动冷却限制）
        if (autoPoop)
        {
            autoPoopTimer -= Time.deltaTime;
            if (autoPoopTimer <= 0)
            {
                DropPoop(true);
                autoPoopTimer = autoPoopInterval;
            }
        }

        // 飞到终点就停住
    }

    void FlyAlongCurve()
    {
        if (pathIndex >= curvePath.Count)
        {
            finished = true;
            if (!hasWon)
            {
                hasWon = true;
                OnReachEnd?.Invoke();
            }
            return;
        }

        Vector3 target = curvePath[pathIndex];
        transform.position = Vector3.MoveTowards(transform.position, target, flySpeed * Time.deltaTime);

        // 朝向
        Vector3 dir = (target - transform.position).normalized;
        if (Mathf.Abs(dir.x) > 0.01f) sr.flipX = dir.x < 0;

        if (Vector3.Distance(transform.position, target) < 0.05f)
        {
            pathIndex++;
        }
    }

    /// <summary>撞到障碍物 → 失败</summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasWon) return;

        if (collision.collider.CompareTag("Building"))
        {
            finished = true;
            autoPoop = false;
            OnHitObstacle?.Invoke();
        }
    }

    /// <summary>每帧手动检测是否与障碍物重叠（不依赖 Unity 碰撞事件）</summary>
    void CheckObstacleCollision()
    {
        int count = Physics2D.OverlapCollider(birdCollider, obstacleFilter, overlapResults);
        for (int i = 0; i < count; i++)
        {
            if (overlapResults[i].CompareTag("Building"))
            {
                finished = true;
                autoPoop = false;
                OnHitObstacle?.Invoke();
                return;
            }
            if (overlapResults[i].CompareTag("Target") &&
                overlapResults[i].TryGetComponent<Target>(out var t) && t.isHostile)
            {
                finished = true;
                autoPoop = false;
                OnHitObstacle?.Invoke();
                return;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasWon || finished) return;

        if (other.CompareTag("Building"))
        {
            finished = true;
            autoPoop = false;
            OnHitObstacle?.Invoke();
        }
        else if (other.CompareTag("Target") && other.TryGetComponent<Target>(out var t) && t.isHostile)
        {
            finished = true;
            autoPoop = false;
            OnHitObstacle?.Invoke();
        }
    }

    public void DropPoop(bool bypassCooldown = false)
    {
        if (finished) return;
        if (!bypassCooldown && poopTimer > 0) return;
        poopTimer = poopCooldown;

        // 粑粑直接出生在鸟的位置（飞行路径上），不下落，留在原地缩小
        Vector3 spawnPos = transform.position;

        if (poopPrefab == null)
        {
            CreatePoopRuntime(spawnPos);
        }
        else
        {
            var poop = Instantiate(poopPrefab, spawnPos, Quaternion.identity);
            var rb = poop.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
        }
    }

    /// <summary>
    /// 运行时创建便便（无 Prefab 时使用，也用于编辑器生成 Prefab）。
    /// </summary>
    public static GameObject CreatePoopRuntime(Vector3 pos)
    {
        var poop = new GameObject("Poop");
        poop.transform.position = pos;
        poop.tag = "Poop";

        var sr2 = poop.AddComponent<SpriteRenderer>();
        sr2.sprite = CreateCircleSprite(16, Color.white);
        sr2.color = new Color(0.55f, 0.35f, 0.15f);
        sr2.sortingOrder = 8;

        var bc = poop.AddComponent<CircleCollider2D>();
        bc.radius = 0.525f;
        bc.isTrigger = true;

        var rb = poop.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.bodyType = RigidbodyType2D.Kinematic;

        var p = poop.AddComponent<Poop>();
        p.lifetime = 2.5f;

        return poop;
    }

    public bool IsFinished() { return finished; }

    static Sprite CreateCircleSprite(int radius, Color color)
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
