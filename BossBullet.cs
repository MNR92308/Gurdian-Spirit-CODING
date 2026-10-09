using UnityEngine;

public class BossBullet : MonoBehaviour
{
    const float Radius = 0.2f;

    BossAI owner;
    SpriteRenderer sr;
    Vector2 dir;
    float speed;
    float travelled;
    bool reflected;

    public static void Spawn(Vector2 pos, Vector2 direction, float speed, BossAI owner)
    {
        var go = new GameObject("BossBullet");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.55f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteTools.SoftCircle();
        sr.color = new Color(1f, 0.35f, 0.1f, 1f);
        sr.sortingOrder = 7;

        var b = go.AddComponent<BossBullet>();
        b.owner = owner;
        b.dir = direction.normalized;
        b.speed = speed;
        b.sr = sr;
    }

    void Update()
    {
        float step = speed * Time.deltaTime;
        Vector2 pos = transform.position;

        RaycastHit2D[] hits = Physics2D.CircleCastAll(pos, Radius, dir, step);

        foreach (var h in hits)
        {
            Collider2D c = h.collider;
            if (c == null || c.isTrigger) continue;

            PlayerHealth ph = c.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                if (reflected) continue;

                HitResult r = ph.TakeHit(false, 1, false);
                if (r == HitResult.Parried)
                {
                    Reflect();
                    return;
                }
                Kill();
                return;
            }

            BossAI boss = c.GetComponentInParent<BossAI>();
            if (boss != null)
            {
                if (!reflected || !boss.IsTargetable) continue;   // กระสุนของบอสทะลุตัวบอส
                boss.ReceiveHit(2, HitType.Reflected);
                Kill();
                return;
            }

            Kill();   // กำแพง
            return;
        }

        transform.position = pos + dir * step;
        travelled += step;
        if (travelled > 25f) Destroy(gameObject);
    }

    // Parry สำเร็จ: สะท้อนกลับไปหาบอส
    void Reflect()
    {
        reflected = true;
        speed *= 1.4f;
        sr.color = new Color(1f, 0.9f, 0.2f, 1f);

        if (owner != null && owner.IsTargetable)
            dir = ((Vector2)owner.transform.position - (Vector2)transform.position).normalized;
        else
            dir = -dir;

        FX.Burst(transform.position, new Color(1f, 0.9f, 0.3f), 8, 5f, 0.3f, 0.12f);
    }

    void Kill()
    {
        FX.Burst(transform.position, sr != null ? sr.color : Color.white, 5, 3f, 0.25f, 0.1f);
        Destroy(gameObject);
    }
}