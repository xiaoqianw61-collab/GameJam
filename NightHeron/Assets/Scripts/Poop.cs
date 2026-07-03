using UnityEngine;

/// <summary>
/// 夜鹭的便便 - 下落砸中目标得分，落地消失
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Poop : MonoBehaviour
{
    private bool alreadyHit;

    /// <summary>
    /// 碰到 Target（Trigger）→ 得分
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

    /// <summary>
    /// 落到地面（非 Trigger）→ 失误
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (alreadyHit) return;

        if (collision.collider.CompareTag("Ground"))
        {
            GameManager.Instance.OnTargetMiss();
            alreadyHit = true;
            Destroy(gameObject);
        }
    }
}
