using UnityEngine;

// ตัวช่วยเล็ง: ล็อกเป้าที่บอสตัวจริงเสมอ ถ้าบอสกำลังตาย/ฟื้นจะเล็งร่างแยกที่ใกล้ที่สุด
// ถ้าไม่มีเป้าเลย ใช้เมาส์แทน
public static class AimTarget
{
    public static Transform Target(Vector2 from)
    {
        BossAI main = BossAI.Main;
        if (main != null && main.IsTargetable) return main.transform;

        Transform best = null;
        float bestSqr = float.MaxValue;

        foreach (var b in BossAI.All)
        {
            if (b == null || !b.IsClone || !b.IsTargetable) continue;
            float d = ((Vector2)b.transform.position - from).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = b.transform; }
        }
        return best;
    }

    public static Vector2 Direction(Vector2 from, Camera cam)
    {
        Transform t = Target(from);
        if (t != null)
        {
            Vector2 d = (Vector2)t.position - from;
            if (d.sqrMagnitude > 0.0001f) return d.normalized;
        }

        if (cam != null)
        {
            Vector3 m = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 d = (Vector2)m - from;
            if (d.sqrMagnitude > 0.0001f) return d.normalized;
        }

        return Vector2.right;
    }
}