using UnityEngine;

// ตัวช่วยวาด UI (IMGUI): ปุ่มสวยๆ ไอคอนโลก/ฟันเฟืองที่วาดด้วยโค้ด dropdown ภาษา slider
public static class UIKit
{
    public static readonly Color Gold = new Color(1f, 0.8f, 0.25f);
    public static readonly Color Blue = new Color(0.25f, 0.5f, 0.95f);
    public static readonly Color Orange = new Color(0.95f, 0.5f, 0.12f);
    public static readonly Color Cyan = new Color(0.15f, 0.7f, 0.8f);
    public static readonly Color Green = new Color(0.25f, 0.7f, 0.35f);
    public static readonly Color Red = new Color(0.9f, 0.2f, 0.18f);
    public static readonly Color Purple = new Color(0.65f, 0.35f, 0.95f);
    public static readonly Color Gray = new Color(0.35f, 0.38f, 0.46f);

    static Texture2D circle, globe, gear, grad, fade, vignette;

    // ================= Texture ที่สร้างด้วยโค้ด =================
    static Texture2D Make(int size, System.Func<float, float, float> alpha)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color32[size * size];
        float c = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float a = Mathf.Clamp01(alpha((x - c) / c, (y - c) / c));
                px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    static float L(float dist, float w) { return Mathf.Clamp01((w - dist) / (w * 0.5f)); }

    public static Texture2D Circle
    {
        get
        {
            if (circle == null) circle = Make(128, (x, y) => (1f - Mathf.Sqrt(x * x + y * y)) * 64f);
            return circle;
        }
    }

    // ไอคอนโลก: วงกลม + เส้นศูนย์สูตร/ละติจูด/เส้นเมริเดียน
    public static Texture2D Globe
    {
        get
        {
            if (globe == null) globe = Make(128, (x, y) =>
            {
                float d = Mathf.Sqrt(x * x + y * y);
                if (d > 1f) return 0f;
                const float R = 0.9f, w = 0.05f;
                float a = Mathf.Clamp01((R - d) * 40f) * 0.22f;
                float line = L(Mathf.Abs(d - R), w);
                if (d < R)
                {
                    float half = Mathf.Sqrt(Mathf.Max(0f, R * R - y * y));
                    line = Mathf.Max(line, L(Mathf.Abs(y), w));
                    line = Mathf.Max(line, L(Mathf.Abs(Mathf.Abs(y) - 0.5f * R), w));
                    line = Mathf.Max(line, L(Mathf.Abs(x), w));
                    line = Mathf.Max(line, L(Mathf.Abs(Mathf.Abs(x) - 0.5f * half), w));
                }
                return Mathf.Max(a, line);
            });
            return globe;
        }
    }

    // ไอคอนฟันเฟือง 8 ฟัน
    public static Texture2D Gear
    {
        get
        {
            if (gear == null) gear = Make(128, (x, y) =>
            {
                float d = Mathf.Sqrt(x * x + y * y);
                float ang = Mathf.Atan2(y, x);
                float t = Mathf.Clamp01(Mathf.Sin(ang * 8f) * 2.2f + 0.5f);
                float outer = Mathf.Lerp(0.70f, 0.95f, t);
                return Mathf.Clamp01((outer - d) * 40f) * Mathf.Clamp01((d - 0.28f) * 40f);
            });
            return gear;
        }
    }

    // ไล่เฉด (บนสว่าง → ล่างเข้ม) ใช้คูณกับสีปุ่ม
    public static Texture2D Grad
    {
        get
        {
            if (grad == null)
            {
                grad = new Texture2D(1, 64, TextureFormat.RGBA32, false);
                grad.wrapMode = TextureWrapMode.Clamp;
                for (int y = 0; y < 64; y++)
                {
                    float v = Mathf.Lerp(0.62f, 1f, y / 63f);
                    grad.SetPixel(0, y, new Color(v, v, v, 1f));
                }
                grad.Apply();
            }
            return grad;
        }
    }

    // ไล่ความโปร่งใส (ล่างทึบ → บนโปร่ง) ใช้ทำแสงเรืองด้านล่างจอ
    public static Texture2D Fade
    {
        get
        {
            if (fade == null)
            {
                fade = new Texture2D(1, 64, TextureFormat.RGBA32, false);
                fade.wrapMode = TextureWrapMode.Clamp;
                for (int y = 0; y < 64; y++) fade.SetPixel(0, y, new Color(1f, 1f, 1f, 1f - y / 63f));
                fade.Apply();
            }
            return fade;
        }
    }

    // เงามืดขอบจอแบบวงกลม
    public static Texture2D Vignette
    {
        get
        {
            if (vignette == null) vignette = Make(128, (x, y) =>
                Mathf.SmoothStep(0f, 1f, (Mathf.Sqrt(x * x + y * y) - 0.55f) / 0.6f));
            return vignette;
        }
    }

    // ================= ตัวช่วยวาด =================
    public static void Fill(Rect r, Color c)
    {
        Color o = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = o;
    }

    public static void Tex(Rect r, Texture2D t, Color c)
    {
        Color o = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, t);
        GUI.color = o;
    }

    public static void DrawCircle(Vector2 c, float r, Color col)
    {
        Tex(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), Circle, col);
    }

    // ต้องเรียกใน OnGUI เท่านั้น (ใช้ GUI.skin)
    public static GUIStyle MakeStyle(int size, FontStyle fs, TextAnchor anchor, bool wrap = false, bool rich = false)
    {
        var s = new GUIStyle(GUI.skin.label)
        {
            fontSize = size,
            fontStyle = fs,
            alignment = anchor,
            wordWrap = wrap,
            richText = rich,
            clipping = TextClipping.Overflow
        };
        s.normal.textColor = Color.white;
        if (Loc.UIFont != null) s.font = Loc.UIFont;
        return s;
    }

    public static void ShadowLabel(Rect r, string text, GUIStyle st, Color col)
    {
        Color old = st.normal.textColor;
        st.normal.textColor = new Color(0f, 0f, 0f, 0.65f * col.a);
        GUI.Label(new Rect(r.x + 2f, r.y + 3f, r.width, r.height), text, st);
        st.normal.textColor = col;
        GUI.Label(r, text, st);
        st.normal.textColor = old;
    }

    public static void Panel(Rect p, Color accent, string title, GUIStyle ts)
    {
        if (Event.current.type != EventType.Repaint) return;
        Fill(new Rect(p.x - 3f, p.y - 3f, p.width + 6f, p.height + 6f), new Color(accent.r, accent.g, accent.b, 0.9f));
        Fill(p, new Color(0.06f, 0.07f, 0.1f, 0.97f));
        Tex(new Rect(p.x, p.y, p.width, 72f), Grad, new Color(0.13f, 0.15f, 0.22f, 1f));
        ShadowLabel(new Rect(p.x, p.y + 4f, p.width, 64f), title, ts, accent);
    }

    // ================= ปุ่ม =================
    public static bool Button(Rect r, string label, Color col, GUIStyle st, bool enabled = true)
    {
        return Core(r, label, null, col, st, enabled);
    }

    public static bool IconButton(Rect r, Texture2D icon, Color col)
    {
        return Core(r, null, icon, col, null, true);
    }

    static bool Core(Rect r, string label, Texture2D icon, Color col, GUIStyle st, bool enabled)
    {
        int id = GUIUtility.GetControlID(FocusType.Passive);
        Event e = Event.current;
        bool clicked = false;

        switch (e.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (enabled && e.button == 0 && r.Contains(e.mousePosition))
                {
                    GUIUtility.hotControl = id;
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == id)
                {
                    GUIUtility.hotControl = 0;
                    clicked = enabled && r.Contains(e.mousePosition);
                    e.Use();
                }
                break;

            case EventType.Repaint:
                {
                    bool hover = enabled && !Application.isMobilePlatform && r.Contains(e.mousePosition);
                    Paint(r, col, hover, GUIUtility.hotControl == id, enabled, icon, label, st);
                    break;
                }
        }
        return clicked;
    }

    static void Paint(Rect r, Color col, bool hover, bool down, bool enabled, Texture2D icon, string label, GUIStyle st)
    {
        if (!enabled) col = Color.Lerp(col, new Color(0.3f, 0.3f, 0.33f), 0.75f);

        Rect b = r;
        if (down) b.y += 2f;

        Fill(new Rect(r.x + 2f, r.y + 6f, r.width, r.height), new Color(0f, 0f, 0f, 0.45f));
        if (hover) Fill(new Rect(b.x - 7f, b.y - 7f, b.width + 14f, b.height + 14f), new Color(col.r, col.g, col.b, 0.22f));

        Color bd = Color.Lerp(col, Color.white, 0.6f);
        Fill(new Rect(b.x - 2f, b.y - 2f, b.width + 4f, b.height + 4f), new Color(bd.r, bd.g, bd.b, hover ? 1f : 0.8f));

        Color body = down ? Color.Lerp(col, Color.black, 0.25f) : (hover ? Color.Lerp(col, Color.white, 0.15f) : col);
        body.a = 1f;
        Tex(b, Grad, body);
        Fill(new Rect(b.x, b.y, b.width, b.height * 0.45f), new Color(1f, 1f, 1f, 0.12f));

        if (icon != null)
        {
            float p = b.height * 0.17f;
            Tex(new Rect(b.x + p, b.y + p, b.width - 2f * p, b.height - 2f * p), icon, Color.white);
        }
        else if (st != null && label != null)
        {
            ShadowLabel(b, label, st, enabled ? Color.white : new Color(1f, 1f, 1f, 0.55f));
        }
    }

    // ปุ่มกลมบนจอสัมผัส (ใช้ทั้งในเกมและในหน้า Option)
    public static void DrawRoundButton(Vector2 c, float r, float edge, string label, bool pressed, Color col, GUIStyle ls)
    {
        DrawCircle(c, r + edge, new Color(1f, 1f, 1f, pressed ? 0.85f : 0.4f));
        DrawCircle(c, r, new Color(0.05f, 0.06f, 0.1f, 0.6f));
        DrawCircle(c, r - edge, new Color(col.r, col.g, col.b, pressed ? 0.8f : 0.4f));

        Color o = GUI.color;
        GUI.color = Color.white;
        GUI.Label(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), label, ls);
        GUI.color = o;
    }

    public static void DrawPauseIcon(Rect r, Color col)
    {
        float bw = r.width * 0.16f, bh = r.height * 0.5f;
        float y = r.y + (r.height - bh) / 2f;
        Fill(new Rect(r.center.x - bw * 1.6f, y, bw, bh), col);
        Fill(new Rect(r.center.x + bw * 0.6f, y, bw, bh), col);
    }

    // ================= Slider (รองรับนิ้ว) =================
    public static float Slider(Rect r, float v, float min, float max)
    {
        int id = GUIUtility.GetControlID(FocusType.Passive);
        Event e = Event.current;
        Rect hit = new Rect(r.x - 14f, r.y - 14f, r.width + 28f, r.height + 28f);

        switch (e.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (e.button == 0 && hit.Contains(e.mousePosition))
                {
                    GUIUtility.hotControl = id;
                    v = Mathf.Lerp(min, max, Mathf.InverseLerp(r.x, r.xMax, e.mousePosition.x));
                    e.Use();
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == id)
                {
                    v = Mathf.Lerp(min, max, Mathf.InverseLerp(r.x, r.xMax, e.mousePosition.x));
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == id)
                {
                    GUIUtility.hotControl = 0;
                    e.Use();
                }
                break;

            case EventType.Repaint:
                {
                    float t = Mathf.InverseLerp(min, max, v);
                    Fill(new Rect(r.x, r.center.y - 4f, r.width, 8f), new Color(1f, 1f, 1f, 0.15f));
                    Fill(new Rect(r.x, r.center.y - 4f, r.width * t, 8f), Gold);
                    DrawCircle(new Vector2(r.x + r.width * t, r.center.y), 15f, Color.white);
                    DrawCircle(new Vector2(r.x + r.width * t, r.center.y), 10f, Gold);
                    break;
                }
        }
        return Mathf.Clamp(v, min, max);
    }

    // ================= ปุ่มโลก + เมนูเลือกภาษา =================
    // วิธีใช้ใน OnGUI:  LangEvents(rect) ก่อนวาดอะไรทั้งหมด → วาด UI อื่น → LangDraw(rect) เป็นอันสุดท้าย
    // (ตอนเมนูเปิด คลิกที่อื่นจะถูกดักไว้ ไม่ทะลุไปกดปุ่มด้านล่าง)
    static bool langOpen;
    static GUIStyle langStyle;
    const float ItemW = 230f, ItemH = 52f;

    public static bool LangOpen { get { return langOpen; } }
    public static void CloseLang() { langOpen = false; }

    static Rect Item(Rect btn, int i)
    {
        return new Rect(btn.x, btn.yMax + 8f + i * (ItemH + 4f), ItemW, ItemH);
    }

    public static void LangEvents(Rect btn)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            if (langOpen || btn.Contains(e.mousePosition)) e.Use();
        }
        else if (e.type == EventType.MouseUp && e.button == 0)
        {
            if (btn.Contains(e.mousePosition))
            {
                langOpen = !langOpen;
                e.Use();
            }
            else if (langOpen)
            {
                for (int i = 0; i < 5; i++)
                    if (Item(btn, i).Contains(e.mousePosition)) { Loc.Current = (Lang)i; break; }
                langOpen = false;
                e.Use();
            }
        }
    }

    public static void LangDraw(Rect btn)
    {
        if (Event.current.type != EventType.Repaint) return;

        bool hover = !Application.isMobilePlatform && btn.Contains(Event.current.mousePosition);
        Paint(btn, langOpen ? Gold : Blue, hover, false, true, Globe, null, null);
        if (!langOpen) return;

        if (langStyle == null) langStyle = MakeStyle(24, FontStyle.Bold, TextAnchor.MiddleLeft);

        Rect panel = new Rect(btn.x - 4f, btn.yMax + 4f, ItemW + 8f, 5f * (ItemH + 4f) + 12f);
        Fill(new Rect(panel.x - 3f, panel.y - 3f, panel.width + 6f, panel.height + 6f), new Color(Gold.r, Gold.g, Gold.b, 0.9f));
        Fill(panel, new Color(0.06f, 0.07f, 0.1f, 0.98f));

        for (int i = 0; i < 5; i++)
        {
            Rect r = Item(btn, i);
            bool cur = (int)Loc.Current == i;
            bool h = !Application.isMobilePlatform && r.Contains(Event.current.mousePosition);
            Tex(r, Grad, cur ? new Color(0.85f, 0.65f, 0.15f) : (h ? Blue : new Color(0.16f, 0.18f, 0.26f)));
            ShadowLabel(new Rect(r.x + 16f, r.y, r.width - 50f, r.height), Loc.Names[i], langStyle, Color.white);
            if (cur) DrawCircle(new Vector2(r.xMax - 24f, r.center.y), 7f, Color.white);
        }
    }
}