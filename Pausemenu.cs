using UnityEngine;
using UnityEngine.SceneManagement;

// เมนูหยุดเกมชั่วคราว (สร้างตัวเองอัตโนมัติ ไม่ต้องวางในฉาก)
//   PC     : กด ESC
//   มือถือ : ปุ่มหยุดมุมขวาบน / ปุ่ม Back ของ Android (ส่งมาเป็น Escape) / ออกจากแอปชั่วคราว
// มี RESUME / OPTION / HOW TO PLAY / EXIT และปุ่มโลกเปลี่ยนภาษามุมซ้ายบน
public class PauseMenu : MonoBehaviour
{
    public static bool Paused { get; private set; }

    static PauseMenu inst;

    enum Page { Main, Option, HowTo, Exit }
    Page page;
    PlayerHealth ph;
    float nextFind;
    GUIStyle titleSt, textSt, btnSt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Paused = false;
        inst = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (inst != null || FindAnyObjectByType<PauseMenu>() != null) return;
        var go = new GameObject("PauseMenu");
        DontDestroyOnLoad(go);
        inst = go.AddComponent<PauseMenu>();
    }

    public static void Open() { if (inst != null) inst.DoOpen(); }
    public static void Close() { if (inst != null) inst.DoClose(); }

    void Awake() { if (inst == null) inst = this; }
    void OnEnable() { SceneManager.sceneLoaded += OnLoaded; }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        if (Paused) DoClose();
    }

    void OnLoaded(Scene s, LoadSceneMode m)
    {
        ph = null;
        nextFind = 0f;
        if (Paused) DoClose();
    }

    void DoOpen()
    {
        if (Paused) return;
        Paused = true;
        page = Page.Main;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        var sw = FindAnyObjectByType<PlayerSword>();
        if (sw != null) sw.CancelCharge();

        MenuPanels.CloseAll();
        UIKit.CloseLang();
    }

    void DoClose()
    {
        bool was = Paused;
        Paused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        MenuPanels.CloseAll();
        UIKit.CloseLang();
        if (was) Input.ResetInputAxes();
    }

    void Update()
    {
        if (ph == null && Time.unscaledTime >= nextFind)
        {
            ph = FindAnyObjectByType<PlayerHealth>();
            nextFind = Time.unscaledTime + 0.5f;
        }

        // ไม่มีผู้เล่น (หน้าเมนู / ฉากอื่น) = ไม่มีเมนูหยุด
        if (ph == null)
        {
            if (Paused) DoClose();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) && !MenuPanels.Capturing)
        {
            if (!Paused) { if (!ph.IsDead) DoOpen(); }
            else Back();
        }
    }

    void Back()
    {
        if (UIKit.LangOpen) { UIKit.CloseLang(); return; }

        if (page == Page.Main) DoClose();
        else
        {
            page = Page.Main;
            MenuPanels.CloseAll();
        }
    }

    void MakeStyles()
    {
        if (titleSt != null) return;
        titleSt = UIKit.MakeStyle(44, FontStyle.Bold, TextAnchor.MiddleCenter);
        textSt = UIKit.MakeStyle(28, FontStyle.Bold, TextAnchor.MiddleCenter, true);
        btnSt = UIKit.MakeStyle(28, FontStyle.Bold, TextAnchor.MiddleCenter);
    }

    void OnGUI()
    {
        if (!Paused) return;
        GUI.depth = -200;
        MakeStyles();

        float s = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float W = Screen.width / s, cx = W / 2f;

        Rect sa = Screen.safeArea;
        Rect globe = new Rect(sa.xMin / s + 16f, (Screen.height - sa.yMax) / s + 16f, 64f, 64f);

        UIKit.LangEvents(globe);
        UIKit.Fill(new Rect(0f, 0f, W, 720f), new Color(0f, 0f, 0.02f, 0.68f));

        switch (page)
        {
            case Page.Main: DrawMain(cx); break;
            case Page.Option: if (MenuPanels.DrawOption(cx, W)) Back(); break;
            case Page.HowTo: if (MenuPanels.DrawHowTo(cx, W)) Back(); break;
            case Page.Exit: DrawExit(cx); break;
        }

        UIKit.LangDraw(globe);

        // กันคลิกทะลุไปสคริปต์อื่นที่อยู่ด้านหลังเมนู
        Event e = Event.current;
        if (e.isMouse || e.type == EventType.ScrollWheel) e.Use();

        GUI.matrix = Matrix4x4.identity;
    }

    bool B(float cx, float y, string label, Color c)
    {
        return UIKit.Button(new Rect(cx - 210f, y, 420f, 60f), label, c, btnSt);
    }

    void DrawMain(float cx)
    {
        Rect p = new Rect(cx - 260f, 130f, 520f, 460f);
        UIKit.Panel(p, UIKit.Gold, Loc.T("paused"), titleSt);

        float y = p.y + 96f;
        if (B(cx, y, Loc.T("resume"), UIKit.Green)) DoClose();
        y += 74f;
        if (B(cx, y, Loc.T("option"), UIKit.Blue)) { page = Page.Option; MenuPanels.CloseAll(); }
        y += 74f;
        if (B(cx, y, Loc.T("how_to"), UIKit.Cyan)) { page = Page.HowTo; MenuPanels.CloseAll(); }
        y += 74f;
        if (B(cx, y, Loc.T("exit"), UIKit.Red)) page = Page.Exit;
    }

    void DrawExit(float cx)
    {
        Rect p = new Rect(cx - 260f, 120f, 520f, 480f);
        UIKit.Panel(p, UIKit.Red, Loc.T("exit"), titleSt);

        UIKit.ShadowLabel(new Rect(p.x + 20f, p.y + 84f, p.width - 40f, 70f), Loc.T("leave_q"), textSt, Color.white);

        float y = p.y + 170f;
        if (B(cx, y, Loc.T("main_menu"), UIKit.Orange))
        {
            DoClose();
            GameScenes.Load(GameScenes.Menu);
            return;
        }
        y += 74f;

        if (MenuPanels.CanQuit)
        {
            if (B(cx, y, Loc.T("quit_game"), UIKit.Red))
            {
                DoClose();
                MenuPanels.Quit();
                return;
            }
            y += 74f;
        }

        if (B(cx, y, Loc.T("cancel"), UIKit.Gray)) page = Page.Main;
    }
}