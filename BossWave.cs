using UnityEngine;

// คลื่นพลังที่ขยายวงออกจากบอส (ท่า Zone Blast) กันได้ แดชอมตะหลบได้
public class BossWave : MonoBehaviour
{
    float speed, maxRadius, radius;
    float thickness = 0.8f;
    bool hit;
    SpriteRenderer sr;
    PlayerHealth player;

    public static void Spawn(Vector2 pos, float speed, float maxRadius)
    {
        var go = new GameObject("BossWave");
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteTools.Ring();
        sr.sortingOrder = 20;
        sr.color = new Color(0.8f, 0.35f, 1f, 0.9f);

        var w = go.AddComponent<BossWave>();
        w.speed = speed;
        w.maxRadius = maxRadius;
        w.sr = sr;
        w.player = Object.FindAnyObjectByType<PlayerHealth>();
    }

    void Update()
    {
        radius += speed * Time.deltaTime;
        transform.localScale = Vector3.one * radius * 2f;

        Color c = sr.color;
        c.a = 0.9f * Mathf.Clamp01(1f - radius / maxRadius);
        sr.color = c;

        if (!hit && player != null)
        {
            float d = Vector2.Distance(transform.position, player.transform.position);
            if (Mathf.Abs(d - radius) <= thickness)
            {
                hit = true;
                player.TakeHit(false, 1, false);
            }
        }

        if (radius >= maxRadius) Destroy(gameObject);
    }
}