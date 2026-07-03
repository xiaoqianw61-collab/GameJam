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

    private List<Vector3> curvePath;
    private int pathIndex;
    private float poopTimer;
    private SpriteRenderer sr;
    private bool finished;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

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

        poopTimer -= Time.deltaTime;
    }

    void FlyAlongCurve()
    {
        if (pathIndex >= curvePath.Count)
        {
            finished = true;
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

    public void DropPoop()
    {
        if (finished || poopTimer > 0) return;
        poopTimer = poopCooldown;

        Vector3 spawnPos = poopPoint != null ? poopPoint.position : transform.position + Vector3.down * 0.5f;

        if (poopPrefab == null)
        {
            var poop = new GameObject("Poop");
            poop.transform.position = spawnPos;
            poop.tag = "Poop";

            var sr2 = poop.AddComponent<SpriteRenderer>();
            sr2.sprite = CreateCircleSprite(8, Color.white);
            sr2.color = new Color(0.55f, 0.35f, 0.15f);
            sr2.sortingOrder = 8;

            var bc = poop.AddComponent<CircleCollider2D>();
            bc.radius = 0.2f;
            bc.isTrigger = true;

            var rb = poop.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.velocity = Vector2.down * poopSpeed;

            poop.AddComponent<Poop>();
            Destroy(poop, 5f);
        }
        else
        {
            var poop = Instantiate(poopPrefab, spawnPos, Quaternion.identity);
            var rb = poop.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.down * poopSpeed;
        }
    }

    public bool IsFinished() { return finished; }

    Sprite CreateCircleSprite(int radius, Color color)
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
