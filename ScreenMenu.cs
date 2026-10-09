using UnityEngine;

// หน้าเมนูหลัก / ตาย / ชนะ (วางบนวัตถุในฉาก MainMenu, GameOver, Victory แล้วเลือก Mode)
//   - ปุ่มโลก (ซ้ายบน) = เปลี่ยนภาษา 5 ภาษา   - ปุ่มฟันเฟือง (ข้างปุ่มโลก) = OPTION
//   - พ่อค้าซ้ายจอ (เฉพาะหน้า Title) = ร้านอัพเกรด
public class ScreenMenu : MonoBehaviour
{
    public enum Mode { Title, GameOver, Victory }

    public Mode mode = Mode.Title;
    public string titleText = "BOSS ARENA";
    public Sprite backgroundSprite;                         // ไม่ใส่ก็ได้
    public Color overlayColor = new Color(0f, 0f, 0f, 0.55f);

    enum Page { Main, HowTo, Option, Shop }
    Page page = Page.Main;

    GUIStyle titleSt, subSt, textSt, btnSt;

    static readonly Color[] Confetti = { UIKit.Gold, UIKit.Orange, UIKit.Cyan, UIKit.Green, UIKit.Purple, UIKit.Red };

    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        BossMemory.Load();
        MenuPanels.CloseAll();
        UIKit.CloseLang();

        if (mode == Mode.Title) UpgradeData.DiscardRun();   // ออกมาหน้าเมนูกลางด่าน = ทิ้งเหรียญของรอบนั้น
    }

    void Update()
    {
        // ESC / ปุ่ม Back ของ Android: ปิดเมนูภาษา → กลับหน้าหลัก
        if (Input.GetKeyDown(KeyCode.Escape) && !MenuPanels.Capturing)
        {
            if (UIKit.LangOpen) UIKit.CloseLang();
            else if (page != Page.Main) Back();
        }
    }

    void Back()
    {
        page = Page.Main;
        MenuPanels.CloseAll();
    }

    void MakeStyles()
    {
        if (titleSt != null) return;
        titleSt = UIKit.MakeStyle(72, FontStyle.Bold, TextAnchor.MiddleCenter);
        subSt = UIKit.MakeStyle(30, FontStyle.Bold, TextAnchor.MiddleCenter);
        textSt = UIKit.MakeStyle(26, FontStyle.Bold, TextAnchor.MiddleCenter);
        btnSt = UIKit.MakeStyle(28, FontStyle.Bold, TextAnchor.MiddleCenter);
    }

    bool B(float cx, float y, string label, Color c)
    {
        return UIKit.Button(new Rect(cx - 210f, y, 420f, 58f), label, c, btnSt);
    }

    void OnGUI()
    {
        MakeStyles();

        float s = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float W = Screen.width / s, cx = W / 2f;

        Rect sa = Screen.safeArea;
        float left = sa.xMin / s + 16f, top = (Screen.height - sa.yMax) / s + 16f;
        Rect globe = new Rect(left, top, 64f, 64f);
        Rect gear = new Rect(left + 78f, top, 64f, 64f);

        // 1) เมนูภาษาต้องดักอีเวนต์ก่อนวาดอย่างอื่น  2) วาด UI  3) LangDraw เป็นอันสุดท้าย (อยู่บนสุด)
        UIKit.LangEvents(globe);

        Color accent = mode == Mode.GameOver ? UIKit.Red : (mode == Mode.Victory ? UIKit.Gold : UIKit.Orange);
        DrawBackground(W, accent);

        if (mode == Mode.Title && page == Page.HowTo)
        {
            if (MenuPanels.DrawHowTo(cx, W)) Back();
        }
        else if (mode == Mode.Title && page == Page.Option)
        {
            if (MenuPanels.DrawOption(cx, W)) Back();
        }
        else if (mode == Mode.Title && page == Page.Shop)
        {
            if (ShopUI.DrawShop(cx, W)) Back();
        }
        else
        {
            switch (mode)
            {
                case Mode.Title: DrawTitle(cx, W); break;
                case Mode.GameOver: DrawGameOver(cx, W); break;
                case Mode.Victory: DrawVictory(cx, W); break;
            }

            if (mode == Mode.Title)
            {
                ShopUI.DrawCoins(W);
                if (ShopUI.DrawNpc(W))
                {
                    page = Page.Shop;
                    MenuPanels.CloseAll();
                }

                if (UIKit.IconButton(gear, UIKit.Gear, UIKit.Gray))
                {
                    page = Page.Option;
                    MenuPanels.CloseAll();
                }
            }
        }

        UIKit.LangDraw(globe);
        GUI.matrix = Matrix4x4.identity;
    }

    // ================= พื้นหลัง =================
    void DrawBackground(float W, Color accent)
    {
        if (Event.current.type != EventType.Repaint) return;

        Rect full = new Rect(0f, 0f, W, 720f);

        if (backgroundSprite != null)
        {
            Texture2D tex = backgroundSprite.texture;
            Rect r = backgroundSprite.textureRect;
            Rect uv = new Rect(r.x / tex.width, r.y / tex.height, r.width / tex.width, r.height / tex.height);
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(full, tex, uv);
        }
        else
        {
            UIKit.Fill(full, new Color(0.03f, 0.035f, 0.08f, 1f));
            UIKit.Tex(full, UIKit.Grad, new Color(0.10f, 0.12f, 0.26f, 1f));
        }
        UIKit.Fill(full, overlayColor);

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.6f);
        UIKit.Tex(new Rect(0f, 720f - 300f, W, 300f), UIKit.Fade, new Color(accent.r, accent.g, accent.b, 0.30f + 0.12f * pulse));

        if (mode == Mode.GameOver) UIKit.Fill(full, new Color(0.35f, 0f, 0f, 0.22f));

        if (mode == Mode.Victory) DrawConfetti(W);
        else DrawEmbers(W, accent);

        UIKit.Tex(full, UIKit.Vignette, new Color(0f, 0f, 0f, 0.85f));
    }

    void DrawEmbers(float W, Color accent)
    {
        int n = GameSettings.LowFx ? 14 : 30;
        float t = Time.unscaledTime;

        for (int i = 0; i < n; i++)
        {
            float sp = 18f + (i * 37 % 29);
            float ph = i * 0.6180339f;
            float y = 760f - ((t * sp + ph * 900f) % 800f);
            float x = (ph * 977f) % W + Mathf.Sin(t * 0.8f + i) * 22f;
            float a = Mathf.Clamp01(y / 220f) * 0.6f;
            float r = 1.5f + (i % 4);
            UIKit.DrawCircle(new Vector2(x, y), r, new Color(accent.r, Mathf.Lerp(accent.g, 0.85f, 0.4f), 0.25f, a));
        }
    }

    void DrawConfetti(float W)
    {
        int n = GameSettings.LowFx ? 24 : 48;
        float t = Time.unscaledTime;

        for (int i = 0; i < n; i++)
        {
            float sp = 70f + (i % 5) * 25f;
            float y = ((t * sp + i * 53f) % 780f) - 30f;
            float x = (i * 97.3f) % W + Mathf.Sin(t * 2f + i) * 18f;
            float w = 6f + Mathf.Abs(Mathf.Sin(t * 5f + i)) * 6f;
            Color c = Confetti[i % Confetti.Length];
            UIKit.Fill(new Rect(x, y, w, 8f), new Color(c.r, c.g, c.b, 0.85f));
        }
    }

    // ================= หน้าเมนูหลัก =================
    void DrawTitle(float cx, float W)
    {
        float glow = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2f);

        UIKit.ShadowLabel(new Rect(0f, 44f, W, 100f), titleText, titleSt, Color.Lerp(Color.white, UIKit.Gold, 0.3f + 0.3f * glow));

        // HARDCORE BOSS ADAPTATION AI
        UIKit.ShadowLabel(new Rect(0f, 142f, W, 44f), Loc.T("subtitle"), subSt, Color.Lerp(UIKit.Gold, UIKit.Cyan, glow));

        UIKit.Fill(new Rect(cx - 220f, 192f, 440f, 3f), new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, 0.55f + 0.3f * glow));

        float y = 222f;

        if (GameProgress.HasProgress)
        {
            if (B(cx, y, Loc.T("continue", GameProgress.Stage), UIKit.Green))
                GameScenes.Load(GameScenes.Arena);
            y += 70f;
        }

        if (B(cx, y, Loc.T("new_game"), UIKit.Blue))
        {
            GameProgress.ResetAll();          // ไม่ลบเหรียญและอัพเกรด
            GameScenes.Load(GameScenes.Arena);
        }
        y += 70f;

        if (B(cx, y, Loc.T("how_to"), UIKit.Cyan))
        {
            page = Page.HowTo;
            MenuPanels.CloseAll();
        }
        y += 70f;

        if (B(cx, y, Loc.T("reset_mem"), UIKit.Purple))
        {
            BossMemory.ResetAll();
            BossAI.ResetProfile();
        }
        y += 70f;

        if (MenuPanels.CanQuit && B(cx, y, Loc.T("quit"), UIKit.Red))
            MenuPanels.Quit();
    }

    // ================= YOU DIED =================
    void DrawGameOver(float cx, float W)
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);

        UIKit.ShadowLabel(new Rect(0f, 120f, W, 110f), Loc.T("you_died"), titleSt, Color.Lerp(new Color(0.8f, 0.1f, 0.08f), new Color(1f, 0.3f, 0.22f), pulse));
        UIKit.Fill(new Rect(cx - 200f, 236f, 400f, 3f), new Color(UIKit.Red.r, UIKit.Red.g, UIKit.Red.b, 0.7f));
        UIKit.ShadowLabel(new Rect(0f, 250f, W, 44f), Loc.T("stage_line", GameProgress.Stage, GameProgress.TotalStages), textSt, Color.white);

        if (B(cx, 340f, Loc.T("retry"), UIKit.Orange)) GameScenes.Load(GameScenes.Arena);
        if (B(cx, 414f, Loc.T("main_menu"), UIKit.Blue)) GameScenes.Load(GameScenes.Menu);
    }

    // ================= VICTORY =================
    void DrawVictory(float cx, float W)
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.5f);

        UIKit.ShadowLabel(new Rect(0f, 90f, W, 110f), Loc.T("victory"), titleSt, Color.Lerp(UIKit.Gold, Color.white, pulse * 0.5f));
        UIKit.Fill(new Rect(cx - 200f, 206f, 400f, 3f), new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, 0.8f));

        UIKit.ShadowLabel(new Rect(0f, 222f, W, 40f), Loc.T("all_cleared", GameProgress.TotalStages), textSt, Color.white);
        UIKit.ShadowLabel(new Rect(0f, 262f, W, 40f), Loc.T("armor_line", GameProgress.ArmorLevel, PlayerHealth.MaxArmor), textSt, UIKit.Gold);
        UIKit.ShadowLabel(new Rect(0f, 302f, W, 40f), Loc.T("boss_deaths", BossMemory.TotalDeaths), textSt, new Color(1f, 0.55f, 0.45f));

        if (B(cx, 392f, Loc.T("play_again"), UIKit.Green))
        {
            GameProgress.ResetAll();
            GameScenes.Load(GameScenes.Arena);
        }
        if (B(cx, 466f, Loc.T("main_menu"), UIKit.Blue)) GameScenes.Load(GameScenes.Menu);
    }
}