using UnityEngine;

// สถานะของผู้เล่นที่บอสกำหนดได้: ชะงัก (Stun) และถูกผลัก (Knockback)
public static class PlayerStatus
{
    static float stunUntil;
    static Vector2 knockVel;
    static float knockStart, knockEnd;

    public static bool IsStunned => Time.time < stunUntil;
    public static bool IsKnocked => Time.time < knockEnd;

    public static void Reset()
    {
        stunUntil = 0f;
        knockEnd = 0f;
    }

    public static void Stun(float duration)
    {
        stunUntil = Mathf.Max(stunUntil, Time.time + duration);
    }

    public static void Knockback(Vector2 velocity, float duration)
    {
        knockVel = velocity;
        knockStart = Time.time;
        knockEnd = Time.time + duration;
    }

    public static Vector2 KnockVelocity()
    {
        float k = Mathf.Clamp01((Time.time - knockStart) / Mathf.Max(0.01f, knockEnd - knockStart));
        return knockVel * (1f - k);
    }
}