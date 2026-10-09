using System.Collections.Generic;
using UnityEngine;

// หน้า HOW TO PLAY (ลากนิ้วเลื่อนได้) และหน้า OPTION
//   PC     : ปรับปุ่มคีย์ได้ (คลิกปุ่ม แล้วกดคีย์/คลิกเมาส์ที่ต้องการ)
//   มือถือ : ลากปุ่ม ATTACK / GUARD / DASH ไปวางตำแหน่งที่ชอบ + ปรับขนาดปุ่ม
// ทุกฟังก์ชันวาดในพิกัดออกแบบ "จอสูง 720" ผู้เรียกต้องตั้ง GUI.matrix = Scale(Screen.height / 720) ไว้ก่อน
public static class MenuPanels
{
    // iOS: Apple ไม่ชอบให้แอปปิดตัวเอง ถ้าจะส่ง App Store ให้ตั้งเป็น false (ปุ่ม QUIT จะซ่อนบน iOS)
    public const bool AllowQuitOnIOS = true;

    public static bool CanQuit { get { return Application.platform != RuntimePlatform.IPhonePlayer || AllowQuitOnIOS; } }

    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // true ระหว่างรอให้ผู้เล่นกดปุ่มใหม่ (PauseMenu จะไม่ปิดเมนูด้วย ESC ในช่วงนี้)
    public static bool Capturing { get { return capture >= 0; } }

    public static void CloseAll()
    {
        capture = -1; dragBtn = -1; tab = 0; scroll = 0f; vel = 0f; dragging = false;
        if (layoutDirty) { ControlLayout.Save(); layoutDirty = false; }
    }

    static int capture = -1, dragBtn = -1, tab;
    static float scroll, vel, lastMouseY, contentH;
    static bool dragging, layoutDirty;
    static Vector2 grab;

    // ================= Styles =================
    static GUIStyle titleSt, tabSt, headSt, bodySt, keySt, rowSt, keyBtnSt, noteSt, ballSt;

    static void Styles()
    {
        if (titleSt != null) return;
        titleSt = UIKit.MakeStyle(44, FontStyle.Bold, TextAnchor.MiddleCenter);
        tabSt = UIKit.MakeStyle(22, FontStyle.Bold, TextAnchor.MiddleCenter);
        headSt = UIKit.MakeStyle(22, FontStyle.Bold, TextAnchor.UpperLeft, false, true);
        bodySt = UIKit.MakeStyle(18, FontStyle.Normal, TextAnchor.UpperLeft, true, true);
        bodySt.normal.textColor = new Color(0.88f, 0.9f, 0.95f);
        keySt = UIKit.MakeStyle(20, FontStyle.Bold, TextAnchor.MiddleCenter, true);
        rowSt = UIKit.MakeStyle(20, FontStyle.Bold, TextAnchor.MiddleLeft);
        keyBtnSt = UIKit.MakeStyle(20, FontStyle.Bold, TextAnchor.MiddleCenter);
        noteSt = UIKit.MakeStyle(18, FontStyle.Normal, TextAnchor.MiddleCenter, true);
        noteSt.normal.textColor = new Color(1f, 1f, 1f, 0.75f);
        ballSt = UIKit.MakeStyle(22, FontStyle.Bold, TextAnchor.MiddleCenter);
    }

    // ================= HOW TO PLAY =================
    class Item { public bool key; public string k, t, b; public Color c; public float y, h; }

    static readonly List<Item> items = new List<Item>();
    static readonly string[] TabKeys = { "h_tab1", "h_tab2", "h_tab3" };
    static int bLoc = -1, bCtl = -1, bTab = -1;
    static float bW;
    static bool bTouch;

    static void AddKey(string k, Color c, string t, string b) { items.Add(new Item { key = true, k = k, c = c, t = t, b = b }); }
    static void AddCard(Color c, string t, string b) { items.Add(new Item { c = c, t = t, b = b }); }

    // สร้างรายการข้อความ + คำนวณความสูงใหม่ เฉพาะตอนเปลี่ยนภาษา/ปุ่ม/แท็บ/ขนาด
    static void Rebuild(float w)
    {
        items.Clear();
        bool touch = GameSettings.Touch;

        if (tab == 0)
        {
            if (touch) AddKey(Loc.T("k_drag"), UIKit.Purple, Loc.T("h_move_t"), Loc.T("h_move_mb"));
            else
                AddKey(Controls.Name(Controls.Get(Act.Up)) + " " + Controls.Name(Controls.Get(Act.Left)) + " " +
                       Controls.Name(Controls.Get(Act.Down)) + " " + Controls.Name(Controls.Get(Act.Right)),
                       UIKit.Purple, Loc.T("h_move_t"), Loc.T("h_move_pc"));

            string atk = touch ? Loc.T("btn_attack") : Controls.Name(Controls.Get(Act.Attack));
            string grd = touch ? Loc.T("btn_guard") : Controls.Name(Controls.Get(Act.Guard));
            string dsh = touch ? Loc.T("btn_dash") : Controls.Name(Controls.Get(Act.Dash));

            AddKey(atk, UIKit.Blue, Loc.T("h_atk_t"), Loc.T("h_atk"));
            AddKey(Loc.T("k_hold") + "\n" + atk, UIKit.Orange, Loc.T("h_chg_t"), Loc.T("h_chg"));
            AddKey(grd, UIKit.Cyan, Loc.T("h_grd_t"), Loc.T(touch ? "h_grd_mb" : "h_grd_pc"));
            AddKey(dsh, UIKit.Green, Loc.T("h_dash_t"), Loc.T("h_dash"));
            AddCard(UIKit.Gold, Loc.T("h_arrow_t"), Loc.T("h_arrow"));
        }
        else if (tab == 1)
        {
            AddCard(UIKit.Gold, Loc.T("b1_t"), Loc.T("b1_b"));
            AddCard(UIKit.Blue, Loc.T("b2_t"), Loc.T("b2_b"));
            AddCard(UIKit.Orange, Loc.T("b3_t"), Loc.T("b3_b"));
            AddCard(UIKit.Red, Loc.T("b4_t"), Loc.T("b4_b"));
            AddCard(UIKit.Purple, Loc.T("b5_t"), Loc.T("b5_b"));
            AddCard(UIKit.Cyan, Loc.T("b6_t"), Loc.T("b6_b", Loc.T("reset_mem")));
            AddCard(UIKit.Green, Loc.T("b7_t"), Loc.T("b7_b"));
        }
        else
        {
            AddCard(UIKit.Gold, Loc.T("t1_t"), Loc.T("t1_b"));
            AddCard(UIKit.Red, Loc.T("t2_t"), Loc.T("t2_b"));
            AddCard(UIKit.Orange, Loc.T("t3_t"), Loc.T("t3_b"));
            AddCard(UIKit.Orange, Loc.T("t4_t"), Loc.T("t4_b"));
            AddCard(UIKit.Gold, Loc.T("t5_t"), Loc.T("t5_b"));
            AddCard(UIKit.Purple, Loc.T("t6_t"), Loc.T("t6_b"));
            AddCard(UIKit.Cyan, Loc.T("t7_t"), Loc.T("t7_b"));
            AddCard(UIKit.Green, Loc.T("t8_t"), Loc.T("t8_b"));
        }

        float y = 0f;
        for (int i = 0; i < items.Count; i++)
        {
            Item it = items[i];
            if (it.key) it.h = Mathf.Max(74f, 40f + bodySt.CalcHeight(new GUIContent(it.b), w - 230f) + 10f);
            else it.h = 44f + bodySt.CalcHeight(new GUIContent(it.b), w - 40f) + 12f;
            it.y = y;
            y += it.h + 10f;
        }
        contentH = y + 4f;
    }

    // ลากนิ้ว/เมาส์เพื่อเลื่อน + ล้อเมาส์ + แรงเฉื่อย
    static void HandleScroll(Rect area)
    {
        int id = GUIUtility.GetControlID(FocusType.Passive);
        Event e = Event.current;
        float max = Mathf.Max(0f, contentH - area.height);

        switch (e.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (e.button == 0 && area.Contains(e.mousePosition))
                {
                    GUIUtility.hotControl = id;
                    dragging = true;
                    lastMouseY = e.mousePosition.y;
                    vel = 0f;
                    e.Use();
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == id)
                {
                    float dy = e.mousePosition.y - lastMouseY;
                    lastMouseY = e.mousePosition.y;
                    scroll -= dy;
                    vel = Mathf.Lerp(vel, -dy / Mathf.Max(0.001f, Time.unscaledDeltaTime), 0.4f);
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == id)
                {
                    GUIUtility.hotControl = 0;
                    dragging = false;
                    e.Use();
                }
                break;

            case EventType.ScrollWheel:
                if (area.Contains(e.mousePosition))
                {
                    scroll += e.delta.y * 30f;
                    vel = 0f;
                    e.Use();
                }
                break;

            case EventType.Repaint:
                if (!dragging && Mathf.Abs(vel) > 2f)
                {
                    float dt = Time.unscaledDeltaTime;
                    scroll += vel * dt;
                    vel *= Mathf.Exp(-5f * dt);
                }
                break;
        }

        scroll = Mathf.Clamp(scroll, 0f, max);
        if (scroll <= 0f || scroll >= max) vel = 0f;
    }

    static void DrawItems(float w, float viewH)
    {
        for (int i = 0; i < items.Count; i++)
        {
            Item it = items[i];
            float y = it.y - scroll;
            if (y + it.h < 0f || y > viewH) continue;

            UIKit.Fill(new Rect(0f, y, w, it.h), new Color(1f, 1f, 1f, 0.05f));

            if (it.key)
            {
                float bodyW = w - 230f;
                Rect k = new Rect(12f, y + it.h / 2f - 28f, 190f, 56f);
                UIKit.Fill(new Rect(k.x - 2f, k.y - 2f, k.width + 4f, k.height + 4f), new Color(1f, 1f, 1f, 0.35f));
                UIKit.Fill(k, it.c);
                GUI.Label(k, it.k, keySt);

                headSt.normal.textColor = Color.white;
                GUI.Label(new Rect(222f, y + 6f, bodyW, 30f), it.t, headSt);
                GUI.Label(new Rect(222f, y + 38f, bodyW, it.h - 40f), it.b, bodySt);
            }
            else
            {
                UIKit.Fill(new Rect(0f, y, 6f, it.h), it.c);
                headSt.normal.textColor = it.c;
                GUI.Label(new Rect(20f, y + 6f, w - 40f, 32f), it.t, headSt);
                GUI.Label(new Rect(20f, y + 40f, w - 40f, it.h - 44f), it.b, bodySt);
            }
        }

        // แถบเลื่อนด้านขวา
        if (contentH > viewH)
        {
            float th = Mathf.Max(30f, viewH * viewH / contentH);
            float ty = scroll / (contentH - viewH) * (viewH - th);
            UIKit.Fill(new Rect(w + 6f, 0f, 5f, viewH), new Color(1f, 1f, 1f, 0.08f));
            UIKit.Fill(new Rect(w + 6f, ty, 5f, th), new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, 0.85f));
        }
    }

    // คืน true เมื่อกดปุ่ม BACK
    public static bool DrawHowTo(float cx, float W)
    {
        Styles();
        float pw = Mathf.Min(940f, W - 40f);
        Rect p = new Rect(cx - pw / 2f, 20f, pw, 680f);
        UIKit.Panel(p, UIKit.Gold, Loc.T("how_to"), titleSt);

        float gap = 12f, tw = (pw - 60f - gap * 2f) / 3f;
        for (int i = 0; i < 3; i++)
        {
            Rect r = new Rect(p.x + 30f + i * (tw + gap), p.y + 86f, tw, 44f);
            if (UIKit.Button(r, Loc.T(TabKeys[i]), tab == i ? UIKit.Orange : UIKit.Gray, tabSt))
            {
                tab = i; scroll = 0f; vel = 0f;
            }
        }

        Rect area = new Rect(p.x + 20f, p.y + 140f, pw - 40f, 460f);
        float w = area.width - 14f;

        if (bLoc != Loc.Version || bCtl != Controls.Version || bTab != tab || bTouch != GameSettings.Touch || !Mathf.Approximately(bW, w))
        {
            bLoc = Loc.Version; bCtl = Controls.Version; bTab = tab; bTouch = GameSettings.Touch; bW = w;
            Rebuild(w);
        }

        HandleScroll(area);

        GUI.BeginGroup(area);
        if (Event.current.type == EventType.Repaint) DrawItems(w, area.height);
        GUI.EndGroup();

        return UIKit.Button(new Rect(cx - 130f, p.yMax - 64f, 260f, 50f), Loc.T("back"), UIKit.Blue, tabSt);
    }

    // ================= OPTION =================
    static readonly string[] ActKeys = { "act_up", "act_down", "act_left", "act_right", "act_dash", "act_attack", "act_guard", "act_hud" };
    static readonly Act[] LeftCol = { Act.Up, Act.Down, Act.Left, Act.Right };
    static readonly Act[] RightCol = { Act.Dash, Act.Attack, Act.Guard, Act.Hud };
    static readonly string[] BtnKeys = { "btn_attack", "btn_guard", "btn_dash" };
    static readonly Color[] BtnCols = { UIKit.Blue, UIKit.Cyan, UIKit.Green };
    static readonly Vector2[] centers = new Vector2[3];

    // คืน true เมื่อกดปุ่ม BACK
    public static bool DrawOption(float cx, float W)
    {
        Styles();
        bool back = GameSettings.Touch ? OptionTouch(W) : OptionKeys(cx, W);
        if (back) CloseAll();
        return back;
    }

    // ---------- PC: ปรับปุ่มคีย์ ----------
    static bool OptionKeys(float cx, float W)
    {
        float pw = Mathf.Min(940f, W - 40f);
        Rect p = new Rect(cx - pw / 2f, 20f, pw, 680f);
        UIKit.Panel(p, UIKit.Blue, Loc.T("option"), titleSt);

        float colW = (pw - 60f) / 2f;
        KeyColumn(LeftCol, p.x + 30f, p.y + 100f, colW, Loc.T("opt_move"));
        KeyColumn(RightCol, p.x + 30f + colW, p.y + 100f, colW, null);

        GUI.Label(new Rect(p.x + 40f, p.y + 430f, pw - 80f, 80f), Loc.T("opt_note"), noteSt);

        float by = p.yMax - 70f;
        if (UIKit.Button(new Rect(cx - 280f, by, 260f, 52f), Loc.T("opt_reset"), UIKit.Orange, tabSt))
        {
            capture = -1;
            Controls.ResetKeys();
        }
        return UIKit.Button(new Rect(cx + 20f, by, 260f, 52f), Loc.T("back"), UIKit.Blue, tabSt);
    }

    static void KeyColumn(Act[] acts, float x, float y0, float colW, string header)
    {
        if (header != null) UIKit.ShadowLabel(new Rect(x, y0, colW, 34f), header, rowSt, UIKit.Gold);
        Event e = Event.current;

        for (int i = 0; i < acts.Length; i++)
        {
            Act a = acts[i];
            float y = y0 + 44f + i * 66f;

            UIKit.Fill(new Rect(x - 6f, y - 4f, colW - 10f, 54f), new Color(1f, 1f, 1f, 0.05f));
            GUI.Label(new Rect(x + 6f, y, colW - 236f, 46f), Loc.T(ActKeys[(int)a]), rowSt);

            Rect kr = new Rect(x + colW - 226f, y, 200f, 46f);
            bool cap = capture == (int)a;

            // ต้องเช็กก่อนวาดปุ่ม เพื่อให้ปุ่มไม่แย่งอีเวนต์ไปก่อน
            if (cap)
            {
                if (e.type == EventType.KeyDown && e.keyCode != KeyCode.None)
                {
                    if (e.keyCode != KeyCode.Escape) Controls.Set(a, e.keyCode);
                    capture = -1;
                    e.Use();
                }
                else if (e.type == EventType.MouseDown)
                {
                    // คลิกซ้ายต้องคลิกที่ปุ่มนี้เอง (กันไปเผลอผูกคลิกซ้ายตอนกดปุ่มอื่น) ปุ่มเมาส์อื่นผูกได้เลย
                    if (e.button != 0 || kr.Contains(e.mousePosition)) Controls.Set(a, KeyCode.Mouse0 + e.button);
                    capture = -1;
                    e.Use();
                }
            }

            string label = cap ? Loc.T("opt_press") : Controls.Name(Controls.Get(a));
            if (UIKit.Button(kr, label, cap ? UIKit.Orange : UIKit.Gray, keyBtnSt)) capture = (int)a;
        }
    }

    // ---------- มือถือ: ย้าย/ปรับขนาดปุ่มบนจอ ----------
    static bool OptionTouch(float W)
    {
        float s = Screen.height / 720f;
        Rect sa = Screen.safeArea;
        float saL = sa.xMin / s, saR = sa.xMax / s, saT = (Screen.height - sa.yMax) / s, saB = (Screen.height - sa.yMin) / s;
        Event e = Event.current;

        UIKit.Fill(new Rect(0f, 0f, W, 720f), new Color(0.03f, 0.04f, 0.08f, 0.97f));

        Color fr = new Color(1f, 1f, 1f, 0.10f);
        UIKit.Fill(new Rect(saL, saT, saR - saL, 2f), fr);
        UIKit.Fill(new Rect(saL, saB - 2f, saR - saL, 2f), fr);
        UIKit.Fill(new Rect(saL, saT, 2f, saB - saT), fr);
        UIKit.Fill(new Rect(saR - 2f, saT, 2f, saB - saT), fr);

        // วงจอยสติ๊ก (ของจริงลอยตามนิ้ว ตรงนี้แค่บอกตำแหน่งโซนเดิน)
        Vector2 hint = new Vector2(saL + 170f, saB - 170f);
        UIKit.DrawCircle(hint, 72f, new Color(1f, 1f, 1f, 0.08f));
        GUI.Label(new Rect(hint.x - 70f, hint.y - 20f, 140f, 40f), Loc.T("opt_move"), noteSt);

        UIKit.ShadowLabel(new Rect(0f, saT + 8f, W, 60f), Loc.T("option"), titleSt, UIKit.Gold);
        UIKit.ShadowLabel(new Rect(saL + 40f, saT + 70f, W - saL - 300f, 60f), Loc.T("opt_drag"), noteSt, new Color(1f, 1f, 1f, 0.85f));

        for (int i = 0; i < 3; i++)
        {
            Vector2 o = ControlLayout.Offset(i);
            centers[i] = new Vector2(saR - o.x, saB - o.y);
        }

        // ลากปุ่ม
        int id = GUIUtility.GetControlID(FocusType.Passive);
        switch (e.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (e.button == 0)
                {
                    for (int i = 2; i >= 0; i--)
                    {
                        float r = ControlLayout.Radius(i) * 1.1f;
                        if ((e.mousePosition - centers[i]).sqrMagnitude <= r * r)
                        {
                            dragBtn = i;
                            grab = centers[i] - e.mousePosition;
                            GUIUtility.hotControl = id;
                            e.Use();
                            break;
                        }
                    }
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == id && dragBtn >= 0)
                {
                    float r = ControlLayout.Radius(dragBtn);
                    Vector2 np = e.mousePosition + grab;
                    np.x = Mathf.Clamp(np.x, saL + r + 8f, saR - r - 8f);
                    np.y = Mathf.Clamp(np.y, saT + r + 8f, saB - r - 8f);
                    ControlLayout.SetOffset(dragBtn, new Vector2(saR - np.x, saB - np.y));
                    layoutDirty = true;
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == id)
                {
                    GUIUtility.hotControl = 0;
                    dragBtn = -1;
                    e.Use();
                }
                break;
        }

        for (int i = 0; i < 3; i++)
        {
            float r = ControlLayout.Radius(i);
            ballSt.fontSize = Mathf.Max(10, Mathf.RoundToInt(r * 0.28f));
            UIKit.DrawRoundButton(centers[i], r, 3f, Loc.T(BtnKeys[i]), dragBtn == i, BtnCols[i], ballSt);
        }

        // ปรับขนาดปุ่ม
        float sx = saL + 60f;
        UIKit.ShadowLabel(new Rect(sx, 270f, 340f, 36f), Loc.T("opt_size") + "  " + Mathf.RoundToInt(ControlLayout.Scale * 100f) + "%", rowSt, Color.white);
        float ns = UIKit.Slider(new Rect(sx, 326f, 320f, 30f), ControlLayout.Scale, 0.7f, 1.4f);
        if (!Mathf.Approximately(ns, ControlLayout.Scale))
        {
            ControlLayout.Scale = ns;
            ClampAll(saL, saR, saT, saB);
            layoutDirty = true;
        }

        // บันทึกเมื่อปล่อยนิ้วแล้ว (ไม่เซฟรัวทุกเฟรม)
        if (layoutDirty && GUIUtility.hotControl == 0 && e.type == EventType.Repaint)
        {
            ControlLayout.Save();
            layoutDirty = false;
        }

        if (UIKit.Button(new Rect(saR - 250f, saT + 14f, 230f, 52f), Loc.T("opt_reset"), UIKit.Orange, tabSt))
        {
            ControlLayout.Reset();
            layoutDirty = false;
        }
        bool back = UIKit.Button(new Rect(saR - 250f, saT + 76f, 230f, 52f), Loc.T("back"), UIKit.Blue, tabSt);
        if (back) { ControlLayout.Save(); layoutDirty = false; }
        return back;
    }

    static void ClampAll(float saL, float saR, float saT, float saB)
    {
        for (int i = 0; i < 3; i++)
        {
            float r = ControlLayout.Radius(i);
            Vector2 o = ControlLayout.Offset(i);
            Vector2 c = new Vector2(saR - o.x, saB - o.y);
            c.x = Mathf.Clamp(c.x, saL + r + 8f, saR - r - 8f);
            c.y = Mathf.Clamp(c.y, saT + r + 8f, saB - r - 8f);
            ControlLayout.SetOffset(i, new Vector2(saR - c.x, saB - c.y));
        }
    }
}