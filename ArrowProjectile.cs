using System.Collections.Generic;
using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    enum ArrowState { Flying, Dropped, Stuck }

    const float CastRadius = 0.12f;
    const float EmbedDepth = 0.25f;

    static readonly List<ArrowProjectile> All = new List<ArrowProjectile>();

    public static void ClearAll()
    {
        for (int i = All.Count - 1; i >= 0; i--)
            if (All[i] != null) Destroy(All[i].gameObject);
        All.Clear();
    }

    // ธนูที่กำลังบินเข้าหาตำแหน่งนี้ (ให้บอสตรวจว่าจะฟันทิ้งไหม)
    public static ArrowProjectile FindIncoming(Vector2 target, float range)
    {
        foreach (var a in All)
        {
            if (a == null || !a.IsFlying) continue;

            Vector2 to = target - (Vector2)a.transform.position;
            float dist = to.magnitude;
            if (dist > range) continue;
            if (Vector2.Dot(a.dir, to / Mathf.Max(0.0001f, dist)) < 0.6f) continue;

            return a;
        }
        return null;
    }

    public static ArrowProjectile FindFlyingWithin(Vector2 pos, float radius)
    {
        foreach (var a in All)
        {
            if (a == null || !a.IsFlying) continue;
            if (Vector2.Distance(pos, a.transform.position) <= radius) return a;
        }
        return null;
    }

    PlayerArrow owner;
    SpriteRenderer sr;
    Vector2 dir;
    float travelled;
    float droppedAt;
    float nextTrail;
    ArrowState state = ArrowState.Flying;
    BossAI stuckBoss;

    public bool IsFlying => state == ArrowState.Flying;

    public void Init(PlayerArrow arrowOwner, Vector2 direction)
    {
        owner = arrowOwner;
        dir = direction.normalized;
        sr = GetComponent<SpriteRenderer>();
        All.Add(this);
    }

    void OnDestroy()
    {
        All.Remove(this);
    }

    void Update()
    {
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }

        switch (state)
        {
            case ArrowState.Flying:
                Fly();
                break;

            case ArrowState.Dropped:
                if (sr != null)
                    sr.color = new Color(1f, 1f, 1f, 0.7f + 0.3f * Mathf.Sin(Time.time * 8f));

                if (Time.time - droppedAt >= owner.pickupDelay &&
                    Vector2.Distance(transform.position, owner.transform.position) <= owner.pickupRadius)
                {
                    Collect();
                }
                break;

            case ArrowState.Stuck:
                break;
        }
    }

    void Fly()
    {
        float step = owner.arrowSpeed * Time.deltaTime;
        float dist = Mathf.Min(step, owner.maxDistance - travelled);
        Vector2 pos = transform.position;

        RaycastHit2D[] hits = Physics2D.CircleCastAll(pos, CastRadius, dir, dist);

        foreach (var h in hits)
        {
            Collider2D c = h.collider;
            if (c == null || c.isTrigger) continue;
            if (c.GetComponentInParent<PlayerHealth>() != null) continue;

            Vector2 hitPoint = h.fraction <= 0f ? pos : h.point;

            BossAI boss = c.GetComponentInParent<BossAI>();
            if (boss != null)
            {
                if (!boss.IsTargetable) continue;               // บอสกำลังตาย/ฟื้น ทะลุไป
                boss.ReceiveHit(Mathf.Max(0, owner.arrowDamage), HitType.Arrow);
                StickTo(boss, hitPoint);
                return;
            }

            Drop(hitPoint - dir * 0.15f);
            return;
        }

        transform.position = pos + dir * dist;
        travelled += dist;

        // เส้นแสงและประกายตามหลังลูกธนู
        if (Time.time >= nextTrail)
        {
            nextTrail = Time.time + 0.025f;
            Vector2 pp = transform.position;
            FXPlus.Beam(pp, pp - dir * 1.3f, new Color(0.7f, 0.95f, 1f, 0.55f), 0.08f, 0.14f);
            FX.Burst(pp - dir * 0.4f, new Color(0.8f, 1f, 1f, 0.8f), 1, 0.4f, 0.25f, 0.12f);
        }

        if (travelled >= owner.maxDistance)
            Drop(transform.position);
    }

    void StickTo(BossAI boss, Vector2 hitPoint)
    {
        state = ArrowState.Stuck;
        stuckBoss = boss;

        transform.position = hitPoint + dir * EmbedDepth;
        transform.SetParent(boss.transform, true);

        FXPlus.Flare(hitPoint, new Color(0.8f, 1f, 1f, 0.9f), 1.6f, 0.2f);
        FX.Ring(hitPoint, new Color(0.8f, 1f, 1f, 0.8f), 0.1f, 0.9f, 0.2f);
    }

    void Drop(Vector2 point)
    {
        state = ArrowState.Dropped;
        transform.position = point;
        droppedAt = Time.time;
    }

    // บอสฟันธนูทิ้ง: ธนูหาย และกลับมาให้ผู้เล่นหลังหน่วงเวลา
    public void DeflectByBoss()
    {
        if (owner != null) owner.OnArrowDestroyed(this);
        Destroy(gameObject);
    }

    public void TryCollectBySlash(Vector2 center, float radius, Collider2D[] hits)
    {
        if (state == ArrowState.Flying) return;

        bool hitStuckBoss = false;

        if (state == ArrowState.Stuck && stuckBoss != null && hits != null)
        {
            foreach (var h in hits)
            {
                if (h != null && h.GetComponentInParent<BossAI>() == stuckBoss)
                {
                    hitStuckBoss = true;
                    break;
                }
            }
        }

        float distToArrow = Vector2.Distance(transform.position, center);

        if (hitStuckBoss || distToArrow <= radius + owner.stuckPickupRadius)
            Collect();
    }

    void Collect()
    {
        FX.Ring(transform.position, new Color(1f, 0.9f, 0.4f, 0.8f), 0.2f, 1.1f, 0.25f);
        if (owner != null) owner.OnArrowCollected(this);
        Destroy(gameObject);
    }
}