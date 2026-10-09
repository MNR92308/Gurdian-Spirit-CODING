using UnityEngine;
using UnityEngine.SceneManagement;

public enum Act { Up = 0, Down = 1, Left = 2, Right = 3, Dash = 4, Attack = 5, Guard = 6, Hud = 7 }

public static class GameSettings
{
    // มือถือที่จอละเอียดเกิน 1080 จะเรนเดอร์ที่ความสูง 1080 (ช่วย FPS มาก) ถ้าสัมผัสเพี้ยนให้ตั้งเป็น false
    public const bool CapMobileResolution = true;
    public const int MaxMobileHeight = 1080;

    public static bool Touch { get { return MobileInput.Active || Application.isMobilePlatform; } }
    public static bool LowFx { get { return Application.isMobilePlatform; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;

        if (!Application.isMobilePlatform) return;

        if (CapMobileResolution)
        {
            int shortSide = Mathf.Min(Screen.width, Screen.height);
            if (shortSide > MaxMobileHeight)
            {
                float k = MaxMobileHeight / (float)shortSide;
                Screen.SetResolution(Mathf.RoundToInt(Screen.width * k), Mathf.RoundToInt(Screen.height * k), true);
            }
        }
        ApplyPerf();
    }

    static void OnLoaded(Scene s, LoadSceneMode m)
    {
        if (Application.isMobilePlatform) ApplyPerf();
    }

    // มือถือ: ล็อก 60 FPS, ปิด VSync, ฟิสิกส์ 60Hz ให้ตรงกับภาพ (กันภาพกระตุก), ปิด Anti-Aliasing
    static void ApplyPerf()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Time.fixedDeltaTime = 1f / 60f;
        Time.maximumDeltaTime = 0.1f;
        QualitySettings.antiAliasing = 0;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
}

// ================= ปุ่มควบคุม (คอม: ปรับคีย์ได้ / มือถือ: ปุ่มบนจอ) =================
// ทุกสคริปต์ผู้เล่นอ่านค่าจากที่นี่ แทน Input.GetMouseButton... / GetAxisRaw...
public static class Controls
{
    static readonly KeyCode[] Def =
    {
        KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.Space, KeyCode.Mouse0, KeyCode.Mouse1, KeyCode.H
    };
    static KeyCode[] keys;

    // เพิ่มทุกครั้งที่เปลี่ยนปุ่ม (ใช้ล้างแคชข้อความในหน้า How to play)
    public static int Version { get; private set; }

    static void Load()
    {
        if (keys != null) return;
        keys = new KeyCode[Def.Length];
        for (int i = 0; i < keys.Length; i++)
            keys[i] = (KeyCode)PlayerPrefs.GetInt("Key" + i, (int)Def[i]);
    }

    public static KeyCode Get(Act a)
    {
        Load();
        return keys[(int)a];
    }

    // ตั้งปุ่ม ถ้าซ้ำกับปุ่มอื่นจะสลับให้ (ไม่มีปุ่มซ้ำกัน)
    public static void Set(Act a, KeyCode k)
    {
        Load();
        int i = (int)a;
        for (int j = 0; j < keys.Length; j++)
            if (j != i && keys[j] == k) keys[j] = keys[i];
        keys[i] = k;
        Save();
    }

    public static void ResetKeys()
    {
        keys = null;
        for (int i = 0; i < Def.Length; i++) PlayerPrefs.DeleteKey("Key" + i);
        PlayerPrefs.Save();
        Version++;
        Load();
    }

    static void Save()
    {
        for (int i = 0; i < keys.Length; i++) PlayerPrefs.SetInt("Key" + i, (int)keys[i]);
        PlayerPrefs.Save();
        Version++;
    }

    // ชื่อปุ่มที่แสดงบนจอ
    public static string Name(KeyCode k)
    {
        switch (k)
        {
            case KeyCode.Mouse0: return Loc.T("key_m0");
            case KeyCode.Mouse1: return Loc.T("key_m1");
            case KeyCode.Mouse2: return Loc.T("key_m2");
            case KeyCode.LeftShift: return "L-SHIFT";
            case KeyCode.RightShift: return "R-SHIFT";
            case KeyCode.LeftControl: return "L-CTRL";
            case KeyCode.RightControl: return "R-CTRL";
            case KeyCode.LeftAlt: return "L-ALT";
            case KeyCode.RightAlt: return "R-ALT";
            case KeyCode.Return: return "ENTER";
        }
        string s = k.ToString();
        if (s.StartsWith("Alpha")) return s.Substring(5);
        if (s.StartsWith("Mouse")) return "MOUSE " + s.Substring(5);
        return s.ToUpper();
    }

    // ---------- สถานะหยุดเกม ----------
    static bool Paused { get { return PauseMenu.Paused; } }

    // ---------- อ่านปุ่ม (คอม + มือถือรวมกัน) ----------
    public static bool AttackDown { get { return !Paused && (Input.GetKeyDown(Get(Act.Attack)) || MobileInput.AttackDown); } }
    public static bool AttackUp { get { return Input.GetKeyUp(Get(Act.Attack)) || MobileInput.AttackUp; } }
    public static bool GuardDown { get { return !Paused && (Input.GetKeyDown(Get(Act.Guard)) || MobileInput.GuardDown); } }
    public static bool GuardUp { get { return Input.GetKeyUp(Get(Act.Guard)) || MobileInput.GuardUp; } }
    public static bool DashDown { get { return !Paused && (Input.GetKeyDown(Get(Act.Dash)) || MobileInput.DashDown); } }
    public static bool HudDown { get { return !Paused && Input.GetKeyDown(Get(Act.Hud)); } }

    // ทิศเดิน: จอยสติ๊กมือถือ (อนาล็อก) หรือคีย์บอร์ด (ลูกศรใช้ได้เสมอเป็นปุ่มสำรอง)
    public static Vector2 Move()
    {
        if (MobileInput.HasMove) return Vector2.ClampMagnitude(MobileInput.Move, 1f);

        Load();
        float x = 0f, y = 0f;
        if (Input.GetKey(keys[(int)Act.Right]) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Input.GetKey(keys[(int)Act.Left]) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        if (Input.GetKey(keys[(int)Act.Up]) || Input.GetKey(KeyCode.UpArrow)) y += 1f;
        if (Input.GetKey(keys[(int)Act.Down]) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;

        Vector2 v = new Vector2(x, y);
        return v.sqrMagnitude > 0f ? v.normalized : v;
    }

    public static bool IsMoving { get { return Move().sqrMagnitude > 0.01f; } }
}

// ================= ตำแหน่ง/ขนาดปุ่มบนจอมือถือ (ผู้เล่นย้ายได้ในหน้า OPTION) =================
public static class ControlLayout
{
    public const int Attack = 0, Guard = 1, Dash = 2;

    // ระยะจากขอบขวา/ล่างของ Safe Area (หน่วยออกแบบ: จอสูง 720)
    static readonly Vector2[] Def = { new Vector2(150f, 150f), new Vector2(330f, 110f), new Vector2(280f, 250f) };
    static readonly float[] BaseR = { 80f, 58f, 58f };

    static Vector2[] off;
    static float scale = 1f;

    static void Load()
    {
        if (off != null) return;
        off = new Vector2[3];
        for (int i = 0; i < 3; i++)
            off[i] = new Vector2(PlayerPrefs.GetFloat("BtnX" + i, Def[i].x), PlayerPrefs.GetFloat("BtnY" + i, Def[i].y));
        scale = PlayerPrefs.GetFloat("BtnScale", 1f);
    }

    public static Vector2 Offset(int i) { Load(); return off[i]; }
    public static void SetOffset(int i, Vector2 v) { Load(); off[i] = v; }

    public static float Scale
    {
        get { Load(); return scale; }
        set { Load(); scale = Mathf.Clamp(value, 0.7f, 1.4f); }
    }

    public static float Radius(int i) { Load(); return BaseR[i] * scale; }

    public static void Save()
    {
        Load();
        for (int i = 0; i < 3; i++)
        {
            PlayerPrefs.SetFloat("BtnX" + i, off[i].x);
            PlayerPrefs.SetFloat("BtnY" + i, off[i].y);
        }
        PlayerPrefs.SetFloat("BtnScale", scale);
        PlayerPrefs.Save();
    }

    public static void Reset()
    {
        off = null;
        for (int i = 0; i < 3; i++) { PlayerPrefs.DeleteKey("BtnX" + i); PlayerPrefs.DeleteKey("BtnY" + i); }
        PlayerPrefs.DeleteKey("BtnScale");
        PlayerPrefs.Save();
        Load();
    }

    // ตำแหน่งกลางปุ่มเป็นพิกเซลจริง (พิกัด GUI: y ลง) อิงขอบขวาล่างของ Safe Area
    public static Vector2 Center(int i, float u)
    {
        Load();
        Rect sa = Screen.safeArea;
        return new Vector2(sa.xMax - off[i].x * u, Screen.height - sa.yMin - off[i].y * u);
    }
}