using UnityEngine;

/// <summary>
/// 夜鹭的便便 - 留在飞行路线上，离鸟越远越小，步步生粑粑
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Poop : MonoBehaviour
{
    [Header("缩小渐变")]
    public float lifetime = 2.5f;  // 存活时间

    private bool alreadyHit;
    private float elapsed;
    private Vector3 startScale;
    private SpriteRenderer sr;

    void Start()
    {
        startScale = transform.localScale;
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (alreadyHit) return;

        elapsed += Time.deltaTime;
        float t = elapsed / lifetime;

        // 随时间缩小，从全尺寸渐变到 0
        transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

        // 透明度也渐变消失
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 1f - t;
            sr.color = c;
        }

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 碰到 Target（Trigger）→ 计算命中距离，传递得分信息
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        if (alreadyHit) return;

        if (other.CompareTag("Target") || other.CompareTag("Building"))
        {
            if (other.TryGetComponent<Target>(out var target))
            {
                // 传便便位置 + 便便自身半径，让 Target 内部做方向感知判定
                float poopRadius = GetComponent<CircleCollider2D>()?.radius ?? 0.5f;
                int score = target.GetPooped(transform.position, poopRadius);

                // 通知 GameManager 加分
                if (GameManager.Instance != null)
                    GameManager.Instance.AddScore(score);

                alreadyHit = true;
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// 落到地面（非 Trigger）→ 失误
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (alreadyHit) return;

        if (collision.collider.CompareTag("Ground"))
        {
            // GameManager.Instance.OnTargetMiss();
            alreadyHit = true;
            Destroy(gameObject);
        }
    }
}
