using UnityEngine;
using UnityEngine.SceneManagement;

// ระบบควบคุมบนมือถือ (สร้างตัวเองอัตโนมัติ)
//   - ครึ่งซ้ายของจอ = จอยสติ๊กลอย
//   - ปุ่ม ATTACK / GUARD / DASH ตำแหน่งและขนาดมาจาก ControlLayout (ผู้เล่นปรับได้ใน OPTION)
//   - ปุ่มหยุดเกมมุมขวาบน (ปุ่ม Back ของ Android ส่งมาเป็น Escape ให้ PauseMenu จัดการอยู่แล้ว)
[DefaultExecutionOrder(-100)]
public class MobileInput : MonoBehaviour
{
    public static Vector2 Move { get; private set; }
    public static bool HasMove => Move.sqrMagnitude > 0.01f;

    public static bool AttackHeld { get; private set; }
    public static bool AttackDown { get; private set; }
    public static bool AttackUp { get; private set; }

    public static bool GuardHeld { get; private set; }
    public static bool GuardDown { get; private set; }
    public static bool GuardUp { get; private set; }

    public static bool DashDown { get; private set; }

    public static bool Active { get; private set; }
    public static bool forceShowInEditor = false;

    const float StickRadius = 90f;
    const float StickZoneFrac = 0.45f;
    const float Deadzone = 0.15f;
    const float HitForgive = 1.15f;

    int stickFinger = -1, attackFinger = -1, guardFinger = -1, dashFinger = -1;
    Vector2 stickOrigin, stickPos;
    bool pendAttackUp, pendGuardUp;

    PlayerHealth ph;
    float nextFind;
    GUIStyle labelStyle;

    static readonly string[] LabelKeys = { "btn_attack", "btn_guard", "btn_dash" };
    static Color[] cols;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindAnyObjectByType<MobileInput>() != null) return;
        var go = new GameObject("MobileInput");
        DontDestroyOnLoad(go);
        go.AddComponent<MobileInput>();
    }

    static bool DetectTouchDevice()
    {
        if (Application.isMobilePlatform) return true;
#if UNITY_EDITOR
        return Input.touchSupported || forceShowInEditor;
#else
        return false;
#endif
    }

    void Awake()
    {
        Move = Vector2.zero;
        AttackHeld = AttackDown = AttackUp = false;
        GuardHeld = GuardDown = GuardUp = false;
        DashDown = false;

        Active = DetectTouchDevice();
        if (!Active) { enabled = false; return; }

        Input.multiTouchEnabled = true;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        ph = null;
        nextFind = 0f;
        ReleaseAll(false);
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) ReleaseAll(true);
    }

    void ReleaseAll(bool raiseUp)
    {
        if (raiseUp)
        {
            if (attackFinger >= 0) pendAttackUp = true;
            if (guardFinger >= 0) pendGuardUp = true;
        }
        stickFinger = attackFinger = guardFinger = dashFinger = -1;
        AttackHeld = false;
        GuardHeld = false;
        Move = Vector2.zero;
    }

    bool PlayerAlive() { return ph != null && !ph.IsDead; }

    public static Rect PauseRect(float u)
    {
        Rect sa = Screen.safeArea;
        return new Rect(sa.xMax - 80f * u, (Screen.height - sa.yMax) + 14f * u, 64f * u, 64f * u);
    }
    void Update()
    {
        AttackDown = AttackUp = GuardDown = GuardUp = DashDown = false;
        if (pendAttackUp) { AttackUp = true; pendAttackUp = false; }
        if (pendGuardUp) { GuardUp = true; pendGuardUp = false; }

        if (ph == null && Time.unscaledTime >= nextFind)
        {
            ph = FindAnyObjectByType<PlayerHealth>();
            nextFind = Time.unscaledTime + 0.5f;
        }

        bool paused = PauseMenu.Paused;

        // หน้าเมนู และตอนหยุดเกม: ให้แตะเป็นเมาส์ (เมนู IMGUI ใช้งานได้) / ตอนเล่น: ปิด
        bool wantSim = ph == null || paused;
        if (Input.simulateMouseWithTouches != wantSim) Input.simulateMouseWithTouches = wantSim;

        if (!PlayerAlive() || paused)
        {
            ReleaseAll(paused);
            return;
        }

        float u = Screen.height / 720f;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            Vector2 p = new Vector2(t.position.x, Screen.height - t.position.y);

            switch (t.phase)
            {
                case TouchPhase.Began: OnBegan(t.fingerId, p, u); break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary: OnHeld(t.fingerId, p, u); break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled: OnEnded(t.fingerId); break;
            }
            if (PauseMenu.Paused) return;      // เพิ่งกดปุ่มหยุดเกม
        }

        if (attackFinger >= 0 && !FingerExists(attackFinger)) OnEnded(attackFinger);
        if (guardFinger >= 0 && !FingerExists(guardFinger)) OnEnded(guardFinger);
        if (dashFinger >= 0 && !FingerExists(dashFinger)) OnEnded(dashFinger);
        if (stickFinger >= 0 && !FingerExists(stickFinger)) OnEnded(stickFinger);

        if (stickFinger >= 0)
        {
            Vector2 d = (stickPos - stickOrigin) / (StickRadius * u);
            if (d.magnitude < Deadzone) Move = Vector2.zero;
            else Move = Vector2.ClampMagnitude(new Vector2(d.x, -d.y), 1f);
        }
        else Move = Vector2.zero;
    }

    static bool FingerExists(int id)
    {
        for (int i = 0; i < Input.touchCount; i++)
            if (Input.GetTouch(i).fingerId == id) return true;
        return false;
    }

    bool TryPress(int id, Vector2 p, float u)
    {
        if (attackFinger < 0 && InCircle(p, ControlLayout.Center(ControlLayout.Attack, u), ControlLayout.Radius(ControlLayout.Attack) * u * HitForgive))
        {
            attackFinger = id; AttackHeld = true; AttackDown = true;
            return true;
        }
        if (guardFinger < 0 && InCircle(p, ControlLayout.Center(ControlLayout.Guard, u), ControlLayout.Radius(ControlLayout.Guard) * u * HitForgive))
        {
            guardFinger = id; GuardHeld = true; GuardDown = true;
            return true;
        }
        if (dashFinger < 0 && InCircle(p, ControlLayout.Center(ControlLayout.Dash, u), ControlLayout.Radius(ControlLayout.Dash) * u * HitForgive))
        {
            dashFinger = id; DashDown = true;
            return true;
        }
        return false;
    }

    void OnBegan(int id, Vector2 p, float u)
    {
        if (PauseRect(u).Contains(p)) { PauseMenu.Open(); return; }
        if (TryPress(id, p, u)) return;

        Rect sa = Screen.safeArea;
        bool inZone = p.x < sa.xMin + sa.width * StickZoneFrac;
        bool onHud = BossEvolutionHUD.HeaderRect.Contains(p);
        if (stickFinger < 0 && inZone && !onHud)
        {
            stickFinger = id;
            stickOrigin = p;
            stickPos = p;
        }
    }

    void OnHeld(int id, Vector2 p, float u)
    {
        if (id == stickFinger)
        {
            stickPos = p;
            Vector2 d = p - stickOrigin;
            float max = StickRadius * u;
            if (d.magnitude > max) stickOrigin = p - d.normalized * max;
            return;
        }
        if (id != attackFinger && id != guardFinger && id != dashFinger) TryPress(id, p, u);
    }

    void OnEnded(int id)
    {
        if (id == stickFinger) { stickFinger = -1; Move = Vector2.zero; }
        if (id == attackFinger) { attackFinger = -1; AttackHeld = false; AttackUp = true; }
        if (id == guardFinger) { guardFinger = -1; GuardHeld = false; GuardUp = true; }
        if (id == dashFinger) { dashFinger = -1; }
    }

    static bool InCircle(Vector2 p, Vector2 c, float r) { return (p - c).sqrMagnitude <= r * r; }

    // ================= วาด =================
    void OnGUI()
    {
        if (!Active || Event.current.type != EventType.Repaint) return;
        if (!PlayerAlive() || PauseMenu.Paused) return;

        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.identity;
        GUI.depth = -100;
        Color old = GUI.color;

        float u = Screen.height / 720f;
        if (labelStyle == null) labelStyle = UIKit.MakeStyle(20, FontStyle.Bold, TextAnchor.MiddleCenter);
        if (cols == null) cols = new[] { UIKit.Blue, UIKit.Cyan, UIKit.Green };

        // จอยสติ๊ก
        if (stickFinger >= 0)
        {
            float r = StickRadius * u;
            UIKit.DrawCircle(stickOrigin, r, new Color(1f, 1f, 1f, 0.18f));
            UIKit.DrawCircle(stickOrigin, r - 3f * u, new Color(0.05f, 0.06f, 0.1f, 0.35f));
            Vector2 knob = stickOrigin + Vector2.ClampMagnitude(stickPos - stickOrigin, r);
            UIKit.DrawCircle(knob, r * 0.42f, new Color(1f, 1f, 1f, 0.55f));
        }
        else
        {
            Rect sa = Screen.safeArea;
            Vector2 hint = new Vector2(sa.xMin + 170f * u, Screen.height - sa.yMin - 170f * u);
            float r = StickRadius * u * 0.8f;
            UIKit.DrawCircle(hint, r, new Color(1f, 1f, 1f, 0.08f));
            UIKit.DrawCircle(hint, r - 2f * u, new Color(0.05f, 0.06f, 0.1f, 0.15f));
        }

        // ปุ่มโจมตี / การ์ด / แดช
        bool[] pressed = { AttackHeld, GuardHeld, dashFinger >= 0 };
        for (int i = 0; i < 3; i++)
        {
            float r = ControlLayout.Radius(i) * u;
            labelStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(r * 0.28f));
            UIKit.DrawRoundButton(ControlLayout.Center(i, u), r, 3f * u, Loc.T(LabelKeys[i]), pressed[i], cols[i], labelStyle);
        }

        // ปุ่มหยุดเกม
        Rect pr = PauseRect(u);
        UIKit.DrawCircle(pr.center, pr.width / 2f + 2f * u, new Color(1f, 1f, 1f, 0.35f));
        UIKit.DrawCircle(pr.center, pr.width / 2f, new Color(0.05f, 0.06f, 0.1f, 0.65f));
        UIKit.DrawPauseIcon(pr, Color.white);

        GUI.color = old;
        GUI.matrix = oldMatrix;
    }
}