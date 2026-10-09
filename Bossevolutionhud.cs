using System.Collections.Generic;
using UnityEngine;

// กล่องอธิบายบอส มุมซ้ายบน แบบ Dropdown พับเก็บได้ (ข้อความทั้งหมดมาจาก Loc 5 ภาษา)
//   - คลิก/แตะที่แถบหัวข้อเพื่อย่อ/ขยาย หรือกดปุ่ม Boss info box ที่ตั้งใน OPTION
//   - จำสถานะย่อ/ขยายไว้ แม้เปลี่ยนด่านหรือปิดเกมแล้วเปิดใหม่
//   - บนมือถือ: ขยายตามขนาดจอ (ปรับที่ mobileScale) และหลบ Safe Area
public class BossEvolutionHUD : MonoBehaviour
{
    public float left = 12f;
    public float top = 12f;
    public float width = 330f;
    [Range(0f, 1f)] public float backgroundAlpha = 0.42f;

    [Header("Mobile")]
    public float mobileScale = 1.4f;

    // พื้นที่แถบหัวข้อเป็นพิกเซลจริง (พิกัด GUI: y ลง) ให้ MobileInput ใช้กันจอยสติ๊กแย่งการแตะ
    public static Rect HeaderRect { get; private set; }

    const string PrefKey = "BossHUD_Collapsed";

    static readonly Color[] FormColor =
    {
        new Color(0.8f, 0.9f, 1f), new Color(1f, 0.7f, 0.3f), new Color(1f, 0.35f, 0.3f)
    };

    GUIStyle head, line, hint;
    bool collapsed;
    int styleLoc = -1;

    void Awake() { collapsed = PlayerPrefs.GetInt(PrefKey, 0) == 1; }
    void OnDestroy() { HeaderRect = Rect.zero; }

    void Layout(out float s, out Vector2 origin)
    {
        s = 1f;
        origin = Vector2.zero;
        if (MobileInput.Active)
        {
            s = Screen.height / 720f * mobileScale;
            Rect sa = Screen.safeArea;
            origin = new Vector2(sa.xMin, Screen.height - sa.yMax);
        }
    }

    void Update()
    {
        if (Controls.HudDown) Toggle();
        UpdateHeaderRect();

        // มือถือ: ตรวจการแตะเอง (ในฉากสู้ระบบปิดการจำลองเมาส์จากการแตะไว้)
        if (MobileInput.Active && !PauseMenu.Paused)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase != TouchPhase.Began) continue;
                Vector2 p = new Vector2(t.position.x, Screen.height - t.position.y);
                if (HeaderRect.Contains(p)) Toggle();
            }
        }
    }

    void UpdateHeaderRect()
    {
        if (BossAI.Main == null) { HeaderRect = Rect.zero; return; }
        float s; Vector2 o;
        Layout(out s, out o);
        const float pad = 8f;
        HeaderRect = new Rect(o.x + left * s - pad, o.y + top * s - pad, width * s + pad * 2f, 26f * s + pad * 2f);
    }

    void Toggle()
    {
        collapsed = !collapsed;
        PlayerPrefs.SetInt(PrefKey, collapsed ? 1 : 0);
    }

    // สร้าง Style ใหม่เมื่อเปลี่ยนภาษา (ฟอนต์ภาษาจีน/ญี่ปุ่น/เกาหลีอาจต่างกัน)
    void MakeStyles()
    {
        if (head != null && styleLoc == Loc.Version) return;
        styleLoc = Loc.Version;

        head = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, richText = true };
        line = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true, richText = true };
        line.normal.textColor = new Color(0.92f, 0.94f, 1f);
        hint = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleRight };
        hint.normal.textColor = new Color(1f, 1f, 1f, 0.55f);

        if (Loc.UIFont != null) { head.font = Loc.UIFont; line.font = Loc.UIFont; hint.font = Loc.UIFont; }
    }

    void OnGUI()
    {
        BossAI boss = BossAI.Main;
        if (boss == null) return;
        MakeStyles();

        float s; Vector2 o;
        Layout(out s, out o);

        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(new Vector3(o.x, o.y, 0f), Quaternion.identity, new Vector3(s, s, 1f));
        DrawBox(boss);
        GUI.matrix = oldMatrix;
    }

    void DrawBox(BossAI boss)
    {
        Event e = Event.current;
        Rect headerRect = new Rect(left, top, width, 26f);

        if (!MobileInput.Active && e.type == EventType.MouseDown && e.button == 0 && headerRect.Contains(e.mousePosition))
        {
            Toggle();
            e.Use();
        }

        if (e.type != EventType.Repaint) return;

        int evo = Mathf.Clamp(boss.Evolution, 0, 2);
        Color old = GUI.color;
        string arrow = collapsed ? "[+] " : "[-] ";

        if (collapsed)
        {
            DrawHeader(headerRect, evo, arrow, true);
            GUI.color = old;
            return;
        }

        int phase = Mathf.Clamp(boss.Phase, 0, 2);
        var lines = new List<string>();
        lines.Add(Loc.T("hud_info" + (evo + 1)));
        lines.Add("<b>" + Loc.T("hud_mood_l") + "</b> " + Loc.T("hud_mood" + (phase + 1)));

        int revivesLeft = Mathf.Max(0, boss.maxRevives - boss.Evolution);
        lines.Add(revivesLeft > 0
            ? "<b>" + Loc.T("hud_next") + "</b> " + Loc.T("hud_next_b", revivesLeft)
            : "<b>" + Loc.T("hud_last") + "</b>");

        var learned = new List<string>();
        if (BossMemory.Guard > 0.5f) learned.Add(Loc.T("hud_l_guard"));
        if (BossMemory.Dash > 0.5f) learned.Add(Loc.T("hud_l_dash"));
        if (BossMemory.Range > 0.5f) learned.Add(Loc.T("hud_l_range"));
        if (BossMemory.Aggro > 0.5f) learned.Add(Loc.T("hud_l_aggro"));
        lines.Add("<b>" + Loc.T("hud_learned") + "</b> " + (learned.Count == 0 ? Loc.T("hud_watching") : learned[0]));
        for (int i = 1; i < learned.Count; i++) lines.Add(learned[i]);

        string live = LiveHint(boss);
        if (live != null) lines.Add("<b>" + Loc.T("hud_now") + "</b> " + live);

        float textW = width - 18f;
        float bodyH = 0f;
        foreach (string s in lines) bodyH += line.CalcHeight(new GUIContent(s), textW) + 1f;
        float totalH = 26f + bodyH + 6f;

        GUI.color = new Color(0f, 0f, 0f, backgroundAlpha);
        GUI.DrawTexture(new Rect(left, top, width, totalH), Texture2D.whiteTexture);
        GUI.color = FormColor[evo];
        GUI.DrawTexture(new Rect(left, top, 3f, totalH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        DrawHeader(headerRect, evo, arrow, false);

        float y = top + 26f;
        foreach (string s in lines)
        {
            float lh = line.CalcHeight(new GUIContent(s), textW);
            GUI.Label(new Rect(left + 8f, y, textW, lh), s, line);
            y += lh + 1f;
        }
        GUI.color = old;
    }

    void DrawHeader(Rect r, int evo, string arrow, bool collapsedMode)
    {
        bool hover = !MobileInput.Active && r.Contains(Event.current.mousePosition);

        if (collapsedMode)
        {
            GUI.color = new Color(0f, 0f, 0f, backgroundAlpha);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = FormColor[evo];
            GUI.DrawTexture(new Rect(r.x, r.y, 3f, r.height), Texture2D.whiteTexture);
        }
        if (hover)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.1f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
        }
        GUI.color = Color.white;

        head.normal.textColor = FormColor[evo];
        GUI.Label(new Rect(r.x + 8f, r.y + 2f, r.width - 60f, 22f), arrow + Loc.T("hud_form" + (evo + 1)), head);

        if (!MobileInput.Active)
            GUI.Label(new Rect(r.x, r.y + 2f, r.width - 8f, 22f), "[" + Controls.Name(Controls.Get(Act.Hud)) + "]", hint);
    }

    static string C(string hex, string text) { return "<color=#" + hex + ">" + text + "</color>"; }

    static string LiveHint(BossAI b)
    {
        switch (b.State)
        {
            case BossState.Staggered: return C("ffe44d", Loc.T("hud_stagger"));
            case BossState.CounterWarn: return C("ffb060", Loc.T("hud_cwarn"));
            case BossState.CounterStance: return C("66e0ff", Loc.T("hud_cstance"));
            case BossState.CounterDodge: return C("c090ff", Loc.T("hud_cdodge"));
            case BossState.Deflect: return Loc.T("hud_deflect");
            case BossState.Dead:
            case BossState.Reviving: return C("ff8060", Loc.T("hud_rising"));
        }

        if (b.State == BossState.Windup || b.State == BossState.Attack)
        {
            switch (b.CurrentMove)
            {
                case BossMove.ChargeSlash: return C("ff6060", Loc.T("hud_charge"));
                case BossMove.GroundSlam: return C("ff9050", Loc.T("hud_slam"));
                case BossMove.GuardBreak: return C("ffa040", Loc.T("hud_gbreak"));
                case BossMove.HyperTrade: return C("ffd040", Loc.T("hud_trade"));
                case BossMove.BackStab: return C("c090ff", Loc.T("hud_bstab"));
                case BossMove.Interrupt: return Loc.T("hud_interrupt");
                case BossMove.ZoneBlast: return Loc.T("hud_zone");
                case BossMove.Shoot: return Loc.T("hud_shoot");
                case BossMove.Clone: return Loc.T("hud_clone");
            }
        }

        if (b.IsRoaring) return Loc.T("hud_roar");
        return null;
    }
}