using UnityEngine;
using UnityEngine.SceneManagement;

// ทิศเล็งของผู้เล่น (ดาบ / ธนู / แดชถอยหลัง)
//   มือถือ: ตามทิศที่เดินล่าสุด (ถ้ายังไม่เคยเดินจะหันไปทางบอส)
//   คอม   : ใช้ AimTarget เดิม (เล็งบอส/เมาส์)
public static class PlayerAim
{
    static Vector2 facing = Vector2.right;
    static bool hasFacing;
    static int lastFrame = -1;

    const float Threshold = 0.2f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        Reset();
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
    }

    static void OnLoaded(Scene s, LoadSceneMode m) { Reset(); }

    static void Reset()
    {
        facing = Vector2.right;
        hasFacing = false;
        lastFrame = -1;
    }

    static void Refresh(Vector2 from)
    {
        if (lastFrame == Time.frameCount) return;
        lastFrame = Time.frameCount;

        Vector2 m = MobileInput.Move;
        if (m.magnitude > Threshold)
        {
            facing = m.normalized;
            hasFacing = true;
            return;
        }

        // ยังไม่เคยเดินเลย: หันไปทางบอสก่อน
        if (!hasFacing)
        {
            BossAI b = BossAI.Main;
            if (b != null)
            {
                Vector2 d = (Vector2)b.transform.position - from;
                if (d.sqrMagnitude > 0.0001f) facing = d.normalized;
            }
            hasFacing = true;
        }
    }

    // เรียกแบบมีกล้อง: PlayerAim.Direction(pos, cam)
    public static Vector2 Direction(Vector2 from, Camera cam)
    {
        if (!GameSettings.Touch) return AimTarget.Direction(from, cam);

        Refresh(from);
        return facing;
    }

    // เรียกแบบไม่มีกล้อง: PlayerAim.Direction(pos)
    // เรียกแบบไม่มีกล้อง: PlayerAim.Direction(pos)
    public static Vector2 Direction(Vector2 from)
    {
        Camera c = Camera.main;
        if (c == null) c = Object.FindAnyObjectByType<Camera>();
        return Direction(from, c);
    }
}