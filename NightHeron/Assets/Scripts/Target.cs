using UnityEngine;

/// <summary>
/// 可被便便砸中的目标：行人、建筑、车辆、路牌
/// </summary>
public class Target : MonoBehaviour
{
    public enum TargetType { Person, Building, Car, Sign }
    public TargetType type = TargetType.Person;
    public int baseScore = 100;

    private SpriteRenderer sr;
    private bool pooped;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void GetPooped()
    {
        if (pooped) return;
        pooped = true;

        int points = GetScore();
        GameManager.Instance.OnTargetHit(points);

        // 视觉反馈
        if (sr != null)
        {
            sr.color = new Color(0.4f, 0.25f, 0.1f);
            transform.localScale = Vector3.one * 1.2f;
        }

        StartCoroutine(SplashEffect());
    }

    int GetScore()
    {
        return type switch
        {
            TargetType.Person => 100,
            TargetType.Building => 50,
            TargetType.Car => 150,
            TargetType.Sign => 75,
            _ => baseScore,
        };
    }

    System.Collections.IEnumerator SplashEffect()
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
