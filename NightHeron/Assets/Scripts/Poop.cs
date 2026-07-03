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
    /// 碰到 Target（Trigger）→ 得分并消失
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        if (alreadyHit) return;

        if (other.CompareTag("Target") || other.CompareTag("Building"))
        {
            if (other.TryGetComponent<Target>(out var target))
            {
                target.GetPooped();
                alreadyHit = true;
                Destroy(gameObject);
            }
        }
    }
}
