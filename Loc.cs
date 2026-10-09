using System.Collections.Generic;
using UnityEngine;

public enum Lang { EN = 0, TH = 1, ZH = 2, JA = 3, KO = 4 }

// ระบบหลายภาษา: Loc.T("key") หรือ Loc.T("key", arg0, arg1)
public static class Loc
{
    // ภาษาเริ่มต้นเมื่อเครื่องไม่ใช่ 5 ภาษานี้ (เครื่องภาษาไทย/อังกฤษ/จีน/ญี่ปุ่น/เกาหลี จะเลือกให้ตรงอัตโนมัติ)
    public const Lang Fallback = Lang.TH;
    const string PrefKey = "Lang";

    public static readonly string[] Names = { "English", "ไทย", "中文", "日本語", "한국어" };

    static Lang cur;
    static bool loaded;
    static readonly Dictionary<string, string[]> D = new Dictionary<string, string[]>();

    // เพิ่มทุกครั้งที่เปลี่ยนภาษา (ใช้ล้างแคชข้อความ)
    public static int Version { get; private set; }

    public static Lang Current
    {
        get { Load(); return cur; }
        set
        {
            Load();
            if (cur == value) return;
            cur = value;
            Version++;
            PlayerPrefs.SetInt(PrefKey, (int)value);
            PlayerPrefs.Save();
        }
    }

    // ถ้าตัวอักษรไทย/จีน/ญี่ปุ่น/เกาหลีขึ้นเป็นสี่เหลี่ยม ให้ใส่ฟอนต์ไว้ที่ Assets/Resources/UIFont (เช่น Noto Sans)
    static Font font;
    static bool fontTried;
    public static Font UIFont
    {
        get
        {
            if (!fontTried) { fontTried = true; font = Resources.Load<Font>("UIFont"); }
            return font;
        }
    }

    static void Load()
    {
        if (loaded) return;
        loaded = true;
        Build();
        cur = PlayerPrefs.HasKey(PrefKey) ? (Lang)Mathf.Clamp(PlayerPrefs.GetInt(PrefKey), 0, 4) : Detect();
    }

    static Lang Detect()
    {
        switch (Application.systemLanguage)
        {
            case SystemLanguage.English: return Lang.EN;
            case SystemLanguage.Thai: return Lang.TH;
            case SystemLanguage.Chinese:
            case SystemLanguage.ChineseSimplified:
            case SystemLanguage.ChineseTraditional: return Lang.ZH;
            case SystemLanguage.Japanese: return Lang.JA;
            case SystemLanguage.Korean: return Lang.KO;
        }
        return Fallback;
    }

    public static string T(string key)
    {
        Load();
        string[] v;
        if (!D.TryGetValue(key, out v)) return key;
        string s = (int)cur < v.Length ? v[(int)cur] : null;
        return string.IsNullOrEmpty(s) ? v[0] : s;
    }

    public static string T(string key, params object[] args)
    {
        return string.Format(T(key), args);
    }

    static void A(string k, string en, string th, string zh, string ja, string ko)
    {
        D[k] = new[] { en, th, zh, ja, ko };
    }

    static void Build()
    {
        // ---------- เมนู ----------
        A("continue", "CONTINUE  (Stage {0})", "เล่นต่อ  (ด่าน {0})", "继续  (第{0}关)", "コンティニュー  (ステージ{0})", "이어하기  (스테이지 {0})");
        A("new_game", "NEW GAME", "เกมใหม่", "新游戏", "ニューゲーム", "새 게임");
        A("how_to", "HOW TO PLAY", "วิธีเล่น", "玩法说明", "遊び方", "게임 방법");
        A("reset_mem", "RESET BOSS MEMORY", "ล้างความจำบอส", "重置Boss记忆", "ボスの記憶をリセット", "보스 기억 초기화");
        A("quit", "QUIT", "ออกจากเกม", "退出游戏", "ゲーム終了", "게임 종료");
        A("back", "BACK", "กลับ", "返回", "戻る", "뒤로");
        A("option", "OPTION", "OPTION", "OPTION", "OPTION", "OPTION");
        A("subtitle", "HARDCORE BOSS ADAPTATION AI", "HARDCORE BOSS ADAPTATION AI", "HARDCORE BOSS ADAPTATION AI", "HARDCORE BOSS ADAPTATION AI", "HARDCORE BOSS ADAPTATION AI");
        A("resume", "RESUME", "เล่นต่อ", "继续", "再開", "계속하기");
        A("paused", "PAUSED", "หยุดเกมชั่วคราว", "已暂停", "一時停止", "일시정지");
        A("exit", "EXIT", "ออก", "退出", "終了", "나가기");
        A("main_menu", "MAIN MENU", "เมนูหลัก", "主菜单", "メインメニュー", "메인 메뉴");
        A("quit_game", "QUIT GAME", "ออกจากเกม", "退出游戏", "ゲームを終了", "게임 종료");
        A("cancel", "CANCEL", "ยกเลิก", "取消", "キャンセル", "취소");
        A("leave_q", "Leave this battle?", "ออกจากการต่อสู้นี้หรือไม่?", "要离开这场战斗吗？", "この戦いから離れますか？", "이 전투에서 나가시겠습니까?");
        A("you_died", "YOU DIED", "คุณตายแล้ว", "你死了", "あなたは死んだ", "사망했습니다");
        A("retry", "RETRY", "ลองใหม่", "重试", "リトライ", "다시 도전");
        A("victory", "VICTORY!", "ชัยชนะ!", "胜利！", "勝利！", "승리!");
        A("all_cleared", "All {0} stages cleared", "ผ่านครบทั้ง {0} ด่านแล้ว", "已通关全部{0}关", "全{0}ステージクリア", "{0}개 스테이지 모두 클리어");
        A("armor_line", "Armor: {0} / {1}", "เกราะ: {0} / {1}", "护甲: {0} / {1}", "アーマー: {0} / {1}", "방어구: {0} / {1}");
        A("boss_deaths", "Boss was defeated {0} times", "บอสถูกกำจัด {0} ครั้ง", "Boss被击败了{0}次", "ボスを{0}回倒した", "보스를 {0}번 처치했다");
        A("play_again", "PLAY AGAIN", "เล่นอีกครั้ง", "再玩一次", "もう一度", "다시 하기");
        A("stage_line", "Stage {0} / {1}", "ด่าน {0} / {1}", "第{0}关 / 共{1}关", "ステージ {0} / {1}", "스테이지 {0} / {1}");
        A("stage_clear", "STAGE CLEAR!", "ผ่านด่าน!", "通关！", "ステージクリア！", "스테이지 클리어!");
        A("walk_hint", "Walk to the right to face the boss  >>>", "เดินไปทางขวาเพื่อเผชิญหน้าบอส  >>>", "向右走去面对Boss  >>>", "右へ進んでボスと対峙しよう  >>>", "오른쪽으로 걸어가 보스와 마주하세요  >>>");

        // ---------- ปุ่มบนมือถือ ----------
        A("btn_attack", "ATTACK", "โจมตี", "攻击", "攻撃", "공격");
        A("btn_guard", "GUARD", "ป้องกัน", "防御", "ガード", "방어");
        A("btn_dash", "DASH", "พุ่ง", "冲刺", "ダッシュ", "대시");

        // ---------- OPTION ----------
        A("opt_drag", "Drag the buttons to where you like.", "ลากปุ่มไปวางตำแหน่งที่ต้องการ", "拖动按钮到你喜欢的位置。", "ボタンをドラッグして好きな位置に移動できます。", "버튼을 원하는 위치로 드래그하세요.");
        A("opt_size", "Button size", "ขนาดปุ่ม", "按钮大小", "ボタンの大きさ", "버튼 크기");
        A("opt_reset", "RESET", "คืนค่าเริ่มต้น", "恢复默认", "初期化", "초기화");
        A("opt_press", "Press a key...", "กดปุ่มที่ต้องการ...", "请按键…", "キーを押して…", "키를 누르세요…");
        A("opt_note", "ESC = pause (cannot be changed). Your character aims at the mouse.", "ESC = หยุดเกม (เปลี่ยนไม่ได้) ตัวละครจะหันตามเมาส์", "ESC = 暂停（不可更改）。角色会朝向鼠标。", "ESC = 一時停止（変更不可）。キャラクターはマウスの方向を向きます。", "ESC = 일시정지 (변경 불가). 캐릭터는 마우스 방향을 봅니다.");
        A("opt_move", "MOVE", "เดิน", "移动", "移動", "이동");
        A("act_up", "Move Up", "เดินขึ้น", "向上移动", "上へ移動", "위로 이동");
        A("act_down", "Move Down", "เดินลง", "向下移动", "下へ移動", "아래로 이동");
        A("act_left", "Move Left", "เดินซ้าย", "向左移动", "左へ移動", "왼쪽으로 이동");
        A("act_right", "Move Right", "เดินขวา", "向右移动", "右へ移動", "오른쪽으로 이동");
        A("act_dash", "Dash", "พุ่งหลบ", "冲刺", "ダッシュ", "대시");
        A("act_attack", "Attack", "โจมตี", "攻击", "攻撃", "공격");
        A("act_guard", "Guard / Parry / Bow", "ป้องกัน / ปัดป้อง / ยิงธนู", "防御 / 弹反 / 射箭", "ガード / パリィ / 弓", "방어 / 패링 / 활");
        A("act_hud", "Boss info box", "กล่องข้อมูลบอส", "Boss信息框", "ボス情報ボックス", "보스 정보창");
        A("key_m0", "LEFT CLICK", "คลิกซ้าย", "鼠标左键", "左クリック", "좌클릭");
        A("key_m1", "RIGHT CLICK", "คลิกขวา", "鼠标右键", "右クリック", "우클릭");
        A("key_m2", "MIDDLE CLICK", "คลิกกลาง", "鼠标中键", "中クリック", "휠클릭");

        // ---------- How to play: แท็บ + ปุ่ม ----------
        A("h_tab1", "CONTROLS", "การควบคุม", "操作", "操作方法", "조작");
        A("h_tab2", "THE BOSS", "บอส", "Boss", "ボス", "보스");
        A("h_tab3", "TIPS", "เคล็ดลับ", "技巧", "コツ", "팁");
        A("k_drag", "DRAG\nLEFT SIDE", "ลาก\nฝั่งซ้าย", "拖动\n屏幕左侧", "ドラッグ\n画面左側", "드래그\n화면 왼쪽");
        A("k_hold", "HOLD", "กดค้าง", "按住", "長押し", "길게 누르기");

        // ---------- How to play: การควบคุม ----------
        A("h_move_t", "Move", "เคลื่อนที่", "移动", "移動", "이동");
        A("h_move_pc", "Walk with the keys and aim with the mouse. Your character always faces the mouse cursor.",
            "เดินด้วยปุ่มคีย์ แล้วเล็งด้วยเมาส์ ตัวละครจะหันหน้าไปทางเมาส์เสมอ",
            "用按键移动，用鼠标瞄准。角色始终朝向鼠标光标。",
            "キーで移動し、マウスで狙います。キャラクターは常にマウスカーソルの方を向きます。",
            "키로 이동하고 마우스로 조준합니다. 캐릭터는 항상 마우스 커서를 바라봅니다.");
        A("h_move_mb", "Touch and drag anywhere on the left half of the screen to walk. Your character faces the direction you walk, so walk toward the boss to attack it.",
            "แตะแล้วลากที่ครึ่งซ้ายของจอเพื่อเดิน ตัวละครจะหันหน้าไปทางที่เดิน ดังนั้นให้เดินไปทางบอสเพื่อโจมตี",
            "在屏幕左半边触摸并拖动即可移动。角色会朝移动方向看，所以要朝Boss走去才能攻击它。",
            "画面左半分をタッチしてドラッグすると移動します。キャラクターは移動方向を向くので、ボスに向かって歩いて攻撃しましょう。",
            "화면 왼쪽 절반을 터치하고 드래그해 이동합니다. 캐릭터는 이동하는 방향을 보므로 보스 쪽으로 걸어가 공격하세요.");
        A("h_atk_t", "Normal Attack", "โจมตีปกติ", "普通攻击", "通常攻撃", "일반 공격");
        A("h_atk", "A quick sword slash. Fast and safe, but each hit only chips away a little. Many quick hits in a row will also shake the boss's balance.",
            "ฟันดาบรวดเร็ว ปลอดภัย แต่ดาเมจน้อย การโจมตีติดกันหลายครั้งจะทำให้บอสเสียสมดุลได้",
            "快速挥剑。又快又安全，但每次伤害很小。连续快速命中也会动摇Boss的平衡。",
            "素早い剣撃。速くて安全ですが、1回のダメージは小さいです。連続で当てるとボスの体勢も崩れます。",
            "빠른 검 공격. 빠르고 안전하지만 한 번의 피해는 작습니다. 연속으로 맞히면 보스의 균형도 흔들립니다.");
        A("h_chg_t", "Charged Attack", "โจมตีชาร์จ", "蓄力攻击", "溜め攻撃", "차지 공격");
        A("h_chg", "Hold the button to charge, let go to swing a heavy blow. It hurts the boss's balance a lot. The boss notices when you charge and may rush in to interrupt you, so charge when it is busy recovering.",
            "กดค้างเพื่อชาร์จ ปล่อยเพื่อฟันหนัก ทำให้บอสเสียสมดุลมาก บอสรู้เมื่อคุณชาร์จและอาจพุ่งเข้ามาขัดจังหวะ ควรชาร์จตอนที่บอสกำลังพักตัว",
            "按住按钮蓄力，松开挥出重击。能大幅削减Boss的平衡。Boss会察觉你在蓄力并可能冲上来打断，所以请在它硬直时蓄力。",
            "ボタンを長押しで溜め、離すと強烈な一撃。ボスの体勢を大きく崩せます。溜めているとボスが気づいて割り込むことがあるので、隙を見せている時に溜めましょう。",
            "버튼을 길게 눌러 차지하고 떼면 강타. 보스의 균형을 크게 무너뜨립니다. 보스는 차지를 눈치채고 끼어들 수 있으니 보스가 회복 중일 때 차지하세요.");
        A("h_grd_t", "Parry + Ranged Attack", "ปัดป้อง + โจมตีระยะไกล", "弹反 + 远程攻击", "パリィ + 遠距離攻撃", "패링 + 원거리 공격");
        A("h_grd_pc", "Press it just before an enemy strike lands to <b>Parry</b> it. The attack bounces off and the boss loses balance. Release it to shoot your <b>arrow</b> from far away.",
            "กดก่อนการโจมตีของศัตรูจะโดนเล็กน้อยเพื่อ <b>ปัดป้อง</b> การโจมตีจะสะท้อนกลับและบอสเสียสมดุล ปล่อยปุ่มเพื่อยิง <b>ธนู</b> จากระยะไกล",
            "在敌人攻击命中前一刻按下即可<b>弹反</b>，攻击被弹开，Boss失去平衡。松开则从远处射出<b>箭</b>。",
            "敵の攻撃が当たる直前に押すと<b>パリィ</b>になり、攻撃を弾いてボスの体勢が崩れます。離すと遠くから<b>矢</b>を放ちます。",
            "적의 공격이 닿기 직전에 누르면 <b>패링</b>이 되어 공격이 튕겨 나가고 보스의 균형이 무너집니다. 떼면 멀리서 <b>화살</b>을 쏩니다.");
        A("h_grd_mb", "Tap it just before an enemy strike lands to <b>Parry</b> it. The attack bounces off and the boss loses balance. Keep holding to block, then let go to shoot your <b>arrow</b> from far away.",
            "แตะก่อนการโจมตีของศัตรูจะโดนเล็กน้อยเพื่อ <b>ปัดป้อง</b> การโจมตีจะสะท้อนกลับและบอสเสียสมดุล กดค้างเพื่อป้องกัน แล้วปล่อยเพื่อยิง <b>ธนู</b> จากระยะไกล",
            "在敌人攻击命中前一刻点按即可<b>弹反</b>，攻击被弹开，Boss失去平衡。持续按住是格挡，松开则从远处射出<b>箭</b>。",
            "敵の攻撃が当たる直前にタップすると<b>パリィ</b>になり、攻撃を弾いてボスの体勢が崩れます。押し続けるとガード、離すと遠くから<b>矢</b>を放ちます。",
            "적의 공격이 닿기 직전에 탭하면 <b>패링</b>이 되어 공격이 튕겨 나가고 보스의 균형이 무너집니다. 계속 누르면 방어, 떼면 멀리서 <b>화살</b>을 쏩니다.");
        A("h_dash_t", "Dash", "พุ่งหลบ", "冲刺", "ダッシュ", "대시");
        A("h_dash", "A quick burst of movement. You cannot be hurt while dashing, so use it to slip through attacks. A few attacks cannot be dodged this way. See the TIPS page.",
            "พุ่งตัวอย่างรวดเร็ว ขณะพุ่งจะไม่โดนทำร้าย ใช้หลบการโจมตีได้ แต่มีบางท่าที่หลบด้วยวิธีนี้ไม่ได้ ดูในหน้าเคล็ดลับ",
            "快速位移。冲刺期间不会受伤，可用来穿过攻击。少数攻击无法这样躲避，请看“技巧”页。",
            "素早い移動。ダッシュ中はダメージを受けないので、攻撃をすり抜けるのに使えます。避けられない技もあります。コツのページを参照。",
            "빠른 이동. 대시 중에는 피해를 입지 않아 공격을 피할 수 있습니다. 이 방법으로 피할 수 없는 공격도 있으니 팁 페이지를 보세요.");
        A("h_arrow_t", "Getting your arrows back", "เก็บธนูคืน", "取回箭矢", "矢を回収する", "화살 회수");
        A("h_arrow", "<b>Arrow stuck in the Boss:</b> hit the boss with your sword to pull it out.\n<b>Arrow stuck in a wall:</b> walk over to it and pick it up.\nDon't fire carelessly. With no arrows left you only have your sword.",
            "<b>ธนูปักอยู่ที่บอส:</b> ฟันบอสด้วยดาบเพื่อดึงธนูคืน\n<b>ธนูปักที่กำแพง:</b> เดินไปเก็บ\nอย่ายิงพร่ำเพรื่อ ถ้าไม่มีธนูเหลือจะมีเพียงดาบ",
            "<b>箭插在Boss身上：</b>用剑击中Boss即可拔回。\n<b>箭插在墙上：</b>走过去捡起。\n不要乱射，没有箭时只剩下剑。",
            "<b>ボスに刺さった矢：</b>剣で斬ると回収できます。\n<b>壁に刺さった矢：</b>近づいて拾います。\nむやみに撃たないで。矢がないと剣だけになります。",
            "<b>보스에 박힌 화살:</b> 검으로 보스를 때리면 회수됩니다.\n<b>벽에 박힌 화살:</b> 걸어가서 주우세요.\n함부로 쏘지 마세요. 화살이 없으면 검뿐입니다.");

        // ---------- How to play: บอส ----------
        A("b1_t", "This boss learns from YOU", "บอสตัวนี้เรียนรู้จากคุณ", "这个Boss会向你学习", "このボスはあなたから学ぶ", "이 보스는 당신에게서 배웁니다");
        A("b1_b", "It is not a boss that does the same thing every time. While you fight, it quietly watches your habits and changes how it fights to beat them. The more you rely on one trick, the faster it finds an answer.",
            "มันไม่ใช่บอสที่ทำท่าเดิมซ้ำๆ ระหว่างต่อสู้ มันจะแอบสังเกตนิสัยของคุณและเปลี่ยนวิธีสู้เพื่อเอาชนะ ยิ่งคุณพึ่งท่าเดียวมากเท่าไร มันก็ยิ่งหาทางแก้ได้เร็ว",
            "它不会每次都重复同样的动作。战斗中它会默默观察你的习惯，并改变打法来克制你。你越依赖同一招，它就越快找到对策。",
            "毎回同じ行動をするボスではありません。戦いの間にあなたの癖を観察し、それに勝つように戦い方を変えます。同じ技に頼るほど、早く対策されます。",
            "매번 같은 행동을 하는 보스가 아닙니다. 싸우는 동안 당신의 습관을 조용히 관찰하고 그에 맞서도록 전투 방식을 바꿉니다. 한 가지 기술에 의존할수록 더 빨리 해답을 찾아냅니다.");
        A("b2_t", "How it adapts to your habits", "มันปรับตัวตามนิสัยคุณอย่างไร", "它如何适应你的习惯", "あなたの癖への適応", "습관에 적응하는 방식");
        A("b2_b", "<b>You block or parry a lot</b>  →  it uses an unblockable Charge Slash.\n<b>You dash a lot</b>  →  it slams the ground in a wide circle.\n<b>You stay far away and shoot</b>  →  it runs faster, shoots back, and even slaps your arrows out of the air.\n<b>You mash the guard button</b>  →  it uses a Guard Break that crushes your guard.\n<b>You charge your sword</b>  →  it lunges in to knock you out of it.\n<b>You stop attacking for a while</b>  →  it sends out a shockwave to force you to fight.",
            "<b>คุณป้องกัน/ปัดป้องบ่อย</b>  →  มันใช้ฟันชาร์จที่ป้องกันไม่ได้\n<b>คุณพุ่งหลบบ่อย</b>  →  มันทุบพื้นเป็นวงกว้าง\n<b>คุณอยู่ไกลและยิงธนู</b>  →  มันวิ่งเร็วขึ้น ยิงตอบโต้ และปัดธนูทิ้งกลางอากาศ\n<b>คุณกดป้องกันรัว</b>  →  มันใช้ท่าทำลายการ์ดที่ทำให้การ์ดแตก\n<b>คุณชาร์จดาบ</b>  →  มันพุ่งเข้ามาขัดจังหวะ\n<b>คุณหยุดโจมตีไปนาน</b>  →  มันปล่อยคลื่นกระแทกบังคับให้คุณสู้",
            "<b>你经常格挡或弹反</b>  →  它会使用无法格挡的蓄力斩。\n<b>你经常冲刺</b>  →  它会大范围重击地面。\n<b>你远远站着射箭</b>  →  它会加速奔跑、远程反击，甚至把你的箭拍飞。\n<b>你狂按防御键</b>  →  它会使用破防技击碎你的防御。\n<b>你蓄力挥剑</b>  →  它会扑上来打断你。\n<b>你很久不攻击</b>  →  它会放出冲击波逼你战斗。",
            "<b>ガードやパリィが多い</b>  →  防げない溜め斬りを使う。\n<b>ダッシュが多い</b>  →  広範囲に地面を叩きつける。\n<b>遠くから矢を撃つ</b>  →  速く走り、撃ち返し、矢を空中で叩き落とす。\n<b>ガードを連打する</b>  →  ガードを砕くガードブレイクを使う。\n<b>剣を溜める</b>  →  飛び込んで溜めを中断させる。\n<b>しばらく攻撃しない</b>  →  衝撃波を放って戦いを強いる。",
            "<b>방어나 패링을 자주 한다</b>  →  막을 수 없는 차지 베기를 쓴다.\n<b>대시를 자주 한다</b>  →  넓은 원으로 땅을 내려친다.\n<b>멀리서 화살을 쏜다</b>  →  더 빨리 달리고, 반격하며, 화살을 공중에서 쳐낸다.\n<b>방어 버튼을 연타한다</b>  →  방어를 부수는 가드 브레이크를 쓴다.\n<b>검을 차지한다</b>  →  달려들어 차지를 끊는다.\n<b>한동안 공격하지 않는다</b>  →  충격파를 보내 싸우도록 강요한다.");
        A("b3_t", "It hits back when you spam it", "ถ้ารัวมากไป มันจะสวนกลับ", "连续猛攻会遭到反击", "連打すると反撃してくる", "연타하면 반격합니다");
        A("b3_b", "Hit it too many times in a row and it will stop and counter: it may block and strike back, power through your next hit, or vanish and appear behind you. Watch for a flashing \"!\" and a colour change. That is your warning.",
            "โจมตีติดกันมากเกินไป มันจะหยุดแล้วสวนกลับ: อาจตั้งรับแล้วฟันสวน ฝืนรับการโจมตีถัดไป หรือหายตัวไปโผล่ข้างหลังคุณ ระวังเครื่องหมาย \"!\" ที่กะพริบและสีที่เปลี่ยน นั่นคือสัญญาณเตือน",
            "连续命中太多次，它会停下来反击：可能格挡后反斩、硬吃你的下一击，或消失后出现在你身后。注意闪烁的“!”和颜色变化，那就是警告。",
            "連続で当てすぎると、ボスは止まって反撃します。受け止めて斬り返す、次の攻撃を耐える、消えて背後に現れる、など。点滅する「!」と色の変化が警告です。",
            "연속으로 너무 많이 때리면 멈춰서 반격합니다. 막고 되받아치거나, 다음 공격을 버티거나, 사라져서 뒤에 나타납니다. 깜박이는 \"!\"와 색 변화가 경고입니다.");
        A("b4_t", "It gets angrier", "มันโกรธมากขึ้น", "它会越来越愤怒", "怒りが増していく", "점점 더 화를 냅니다");
        A("b4_b", "At about 60% and 30% health it enters a new mood. It moves faster, and the warning before each attack gets shorter.",
            "เมื่อเลือดเหลือประมาณ 60% และ 30% มันจะเข้าสู่อารมณ์ใหม่ เคลื่อนที่เร็วขึ้น และเวลาเตือนก่อนโจมตีสั้นลง",
            "生命值降到约60%和30%时会进入新的状态。移动更快，每次攻击前的预警也更短。",
            "体力が約60%と30%になると新しい状態に入ります。動きが速くなり、攻撃前の予兆も短くなります。",
            "체력이 약 60%와 30%가 되면 새로운 상태가 됩니다. 더 빨리 움직이고 공격 전 경고도 짧아집니다.");
        A("b5_t", "It comes back stronger", "มันกลับมาแข็งแกร่งกว่าเดิม", "它会变得更强再回来", "より強くなって復活する", "더 강해져서 돌아옵니다");
        A("b5_b", "When you beat it, it does not always stay down. It can <b>get back up up to 2 times</b>, each time in a stronger form:\n<b>Form 1:</b> sword attacks only.\n<b>Form 2:</b> learns to shoot and slam the ground, with a bit more health.\n<b>Form 3:</b> splits into clones and fights faster than ever.\nYou can follow its current form in the boss info box.",
            "เมื่อคุณชนะ มันไม่ได้ล้มเสมอไป มัน<b>ลุกขึ้นมาได้สูงสุด 2 ครั้ง</b> แต่ละครั้งแข็งแกร่งขึ้น:\n<b>ร่าง 1:</b> โจมตีด้วยดาบอย่างเดียว\n<b>ร่าง 2:</b> ยิงกระสุนและทุบพื้นได้ เลือดมากขึ้นเล็กน้อย\n<b>ร่าง 3:</b> แยกร่างเป็นโคลนและสู้เร็วกว่าเดิม\nดูร่างปัจจุบันได้ในกล่องข้อมูลบอส",
            "击败它后它不一定会倒下。它<b>最多可以再站起2次</b>，每次都是更强的形态：\n<b>形态1：</b>只会用剑攻击。\n<b>形态2：</b>学会射击和重击地面，生命值略增。\n<b>形态3：</b>分裂出分身，战斗速度前所未有。\n可在Boss信息框中查看当前形态。",
            "倒しても必ず倒れるとは限りません。<b>最大2回まで復活</b>し、そのたびに強い姿になります：\n<b>第1形態：</b>剣攻撃のみ。\n<b>第2形態：</b>射撃と地面叩きを覚え、体力が少し増える。\n<b>第3形態：</b>分身を作り、かつてない速さで戦う。\n現在の形態はボス情報ボックスで確認できます。",
            "쓰러뜨려도 항상 쓰러져 있지는 않습니다. <b>최대 2번까지 다시 일어나며</b> 매번 더 강한 모습이 됩니다:\n<b>1형태:</b> 검 공격만 한다.\n<b>2형태:</b> 사격과 지면 강타를 익히고 체력이 조금 늘어난다.\n<b>3형태:</b> 분신으로 갈라지고 더욱 빠르게 싸운다.\n현재 형태는 보스 정보창에서 볼 수 있습니다.");
        A("b6_t", "It remembers you", "มันจดจำคุณ", "它会记住你", "あなたを覚えている", "당신을 기억합니다");
        A("b6_b", "What the boss learns is saved, even if you die and restart. Every time it dies it gets slightly faster, and later stages have a tougher boss. If you want a fresh start, use <b>{0}</b> on the main menu.",
            "สิ่งที่บอสเรียนรู้จะถูกบันทึกไว้ แม้คุณจะตายแล้วเริ่มใหม่ ทุกครั้งที่มันตายจะเร็วขึ้นเล็กน้อย และด่านหลังๆ บอสจะแกร่งขึ้น ถ้าอยากเริ่มใหม่หมด ให้ใช้ <b>{0}</b> ที่เมนูหลัก",
            "Boss学到的东西会被保存，即使你死亡重来也一样。它每死一次就会稍微变快，后面的关卡Boss更强。想要重新开始，请在主菜单使用<b>{0}</b>。",
            "ボスが学んだ内容は、死んでやり直しても保存されます。倒されるたびに少し速くなり、後のステージほど強くなります。最初からにしたい時はメインメニューの<b>{0}</b>を使ってください。",
            "보스가 배운 내용은 죽고 다시 시작해도 저장됩니다. 죽을 때마다 조금씩 빨라지고 후반 스테이지의 보스는 더 강합니다. 처음부터 다시 하려면 메인 메뉴의 <b>{0}</b>을 사용하세요.");
        A("b7_t", "Break its balance", "ทำลายสมดุลของมัน", "打破它的平衡", "体勢を崩す", "균형을 무너뜨리기");
        A("b7_b", "The yellow bar under the boss's health is its balance. Hits, parries and arrows fill it. When it is full, the boss is <b>staggered</b> for a few seconds and takes <b>double damage</b>. That is your best chance to hit hard.",
            "แถบสีเหลืองใต้เลือดบอสคือสมดุลของมัน การโจมตี ปัดป้อง และธนูจะทำให้แถบเต็มขึ้น เมื่อเต็มบอสจะ<b>มึนงง</b>ไม่กี่วินาทีและรับ<b>ดาเมจสองเท่า</b> นี่คือโอกาสที่ดีที่สุดในการโจมตีหนัก",
            "Boss血条下方的黄色条是它的平衡值。攻击、弹反和箭矢都会填充它。满了之后Boss会<b>失衡</b>数秒并承受<b>双倍伤害</b>，这是你猛攻的最佳时机。",
            "ボスの体力バーの下にある黄色いバーが体勢ゲージです。攻撃・パリィ・矢で溜まり、満タンになるとボスは数秒間<b>ひるみ</b>、<b>ダメージが2倍</b>になります。最大のチャンスです。",
            "보스 체력 아래의 노란 바가 균형입니다. 타격, 패링, 화살로 차오르며, 가득 차면 보스가 몇 초간 <b>그로기</b> 상태가 되어 <b>2배의 피해</b>를 입습니다. 강하게 공격할 최고의 기회입니다.");

        // ---------- How to play: เคล็ดลับ ----------
        A("t1_t", "Read the warning colours", "อ่านสีเตือน", "看懂预警颜色", "警告色を読む", "경고 색을 읽으세요");
        A("t1_b", "Before a big attack the boss glows or draws a circle on the floor. The colour tells you how to survive.",
            "ก่อนโจมตีหนัก บอสจะเรืองแสงหรือวาดวงกลมบนพื้น สีบอกวิธีเอาตัวรอด",
            "大招之前Boss会发光或在地面画出圆圈。颜色会告诉你如何存活。",
            "大技の前にボスは光ったり、床に円を描きます。色が生き残る方法を教えてくれます。",
            "큰 공격 전에 보스는 빛나거나 바닥에 원을 그립니다. 색이 생존 방법을 알려줍니다.");
        A("t2_t", "Red glow: Charge Slash", "เรืองแสงแดง: ฟันชาร์จ", "红光：蓄力斩", "赤い光：溜め斬り", "붉은 빛: 차지 베기");
        A("t2_b", "Dash and Parry both fail against this. Do not stand and fight. <b>Back away</b> out of its reach.",
            "พุ่งและปัดป้องใช้ไม่ได้ผล อย่ายืนสู้ <b>ถอยหนี</b>ให้พ้นระยะ",
            "冲刺和弹反都无效。不要硬拼，<b>后退</b>到它够不到的地方。",
            "ダッシュもパリィも通用しません。立ち向かわず、射程外へ<b>下がって</b>ください。",
            "대시와 패링이 모두 통하지 않습니다. 맞서지 말고 사거리 밖으로 <b>물러나세요</b>.");
        A("t3_t", "Red circle on the floor: Ground Slam", "วงกลมแดงบนพื้น: ทุบพื้น", "地面红圈：重击地面", "床の赤い円：地面叩きつけ", "바닥의 붉은 원: 지면 강타");
        A("t3_b", "Guarding will not save you. <b>Dash out of the circle</b> as it closes.",
            "การป้องกันช่วยไม่ได้ <b>พุ่งออกจากวงกลม</b>ตอนที่วงปิดเข้ามา",
            "防御救不了你。圆圈合拢时<b>冲刺离开圆圈</b>。",
            "ガードでは防げません。円が閉じる時に<b>円の外へダッシュ</b>してください。",
            "방어로는 막을 수 없습니다. 원이 닫힐 때 <b>대시로 원 밖으로</b> 나가세요.");
        A("t4_t", "Orange glow and circle: Guard Break", "แสงและวงกลมสีส้ม: ทำลายการ์ด", "橙色光与圆圈：破防", "オレンジの光と円：ガードブレイク", "주황빛과 원: 가드 브레이크");
        A("t4_b", "It ignores your guard and drains your stamina. <b>Dash away</b> and stop hiding behind your shield.",
            "มันไม่สนการ์ดและดูดสตามินาของคุณ <b>พุ่งหนี</b> อย่าหลบอยู่หลังโล่",
            "它无视你的防御并榨干你的体力。<b>冲刺躲开</b>，别再躲在盾后面。",
            "ガードを無視してスタミナを奪います。盾に隠れず<b>ダッシュで離れて</b>ください。",
            "방어를 무시하고 스태미나를 빼앗습니다. 방패 뒤에 숨지 말고 <b>대시로 피하세요</b>.");
        A("t5_t", "Gold glow and circle: Power Trade", "แสงและวงกลมสีทอง: ท่าพลัง", "金色光与圆圈：强力对撼", "金色の光と円：パワートレード", "금빛과 원: 파워 트레이드");
        A("t5_b", "The boss cannot be interrupted while it charges. Do not attack. <b>Move out of the circle</b>.",
            "ขณะบอสชาร์จจะขัดจังหวะไม่ได้ อย่าโจมตี <b>เดินออกจากวงกลม</b>",
            "Boss蓄力时无法被打断。不要攻击，<b>走出圆圈</b>。",
            "溜めている間のボスは中断できません。攻撃せず、<b>円の外へ移動</b>してください。",
            "보스가 차지하는 동안은 끊을 수 없습니다. 공격하지 말고 <b>원 밖으로 이동하세요</b>.");
        A("t6_t", "Purple glow: Backstab", "แสงสีม่วง: แทงข้างหลัง", "紫光：背刺", "紫の光：バックスタブ", "보랏빛: 백스탭");
        A("t6_b", "It disappears and shows up right behind you. <b>Dash</b> when it strikes.",
    "มันหายตัวไปโผล่ข้างหลังคุณทันที <b>พุ่งหลบ</b>ตอนมันโจมตี",
    "它会消失并出现在你正后方。它出手时<b>冲刺</b>。",
    "消えて背後に現れます。攻撃が来る時に<b>ダッシュ</b>してください。",
    "사라졌다가 바로 뒤에 나타납니다. 공격할 때 <b>대시</b>하세요.");
        A("t7_t", "Cyan glow, boss standing still: Counter Stance", "แสงฟ้า บอสยืนนิ่ง: ท่าตั้งรับสวนกลับ", "青光、Boss站立不动：反击架势", "水色の光で静止：カウンター体勢", "청록빛, 보스가 가만히 서 있음: 반격 자세");
        A("t7_b", "The boss is waiting for you to attack. <b>Do not hit it.</b> Wait until the glow fades, then continue.",
            "บอสกำลังรอให้คุณโจมตี <b>อย่าโจมตี</b> รอจนแสงจางลงแล้วค่อยสู้ต่อ",
            "Boss正在等你攻击。<b>不要打它。</b>等光芒消退后再继续。",
            "ボスはあなたの攻撃を待っています。<b>攻撃しないで。</b>光が消えるまで待ってから続けましょう。",
            "보스가 당신의 공격을 기다리고 있습니다. <b>때리지 마세요.</b> 빛이 사라질 때까지 기다렸다가 계속하세요.");
        A("t8_t", "General advice", "คำแนะนำทั่วไป", "通用建议", "全般的なアドバイス", "일반 조언");
        A("t8_b", "Mix up your moves. If you do the same thing over and over, the boss will notice and punish it. Keep an eye on your stamina, and use staggers to finish the fight.",
            "ใช้ท่าให้หลากหลาย ถ้าทำซ้ำๆ บอสจะสังเกตและโต้กลับ คอยดูสตามินา และใช้จังหวะบอสมึนงงเพื่อจบการต่อสู้",
            "多变换招式。如果一直重复同样的动作，Boss会察觉并惩罚你。留意体力，并利用失衡来结束战斗。",
            "技を使い分けましょう。同じ行動を繰り返すとボスに気づかれ、反撃されます。スタミナに注意し、ひるみを利用して決着をつけましょう。",
            "기술을 섞어 쓰세요. 같은 행동만 반복하면 보스가 눈치채고 응징합니다. 스태미나를 주시하고 그로기를 활용해 전투를 끝내세요.");

        // ---------- กล่องอธิบายบอส (HUD มุมซ้ายบน) ----------
        A("hud_form1", "FORM 1 - The Fighter", "ร่าง 1 - นักสู้", "形态1 - 战士", "第1形態 - 闘士", "1형태 - 투사");
        A("hud_form2", "FORM 2 - The Hunter", "ร่าง 2 - นักล่า", "形态2 - 猎手", "第2形態 - 狩人", "2형태 - 사냥꾼");
        A("hud_form3", "FORM 3 - The Nightmare", "ร่าง 3 - ฝันร้าย", "形态3 - 梦魇", "第3形態 - 悪夢", "3형태 - 악몽");
        A("hud_info1", "Uses sword attacks and learns how you play.", "ใช้ดาบโจมตี และเรียนรู้วิธีเล่นของคุณ", "使用剑攻击，并学习你的玩法。", "剣で攻撃し、あなたの戦い方を学習する。", "검으로 공격하며 당신의 플레이 방식을 학습한다.");
        A("hud_info2", "Evolved! It now shoots, slams the ground and moves faster.", "วิวัฒนาการแล้ว! ตอนนี้ยิงกระสุน ทุบพื้น และเคลื่อนที่เร็วขึ้น", "进化了！现在会射击、重击地面，移动更快。", "進化した！射撃と地面叩きを使い、動きも速い。", "진화했다! 이제 사격과 지면 강타를 하고 더 빠르다.");
        A("hud_info3", "Final form! It splits into clones and fights even faster.", "ร่างสุดท้าย! แยกร่างเป็นโคลนและสู้เร็วขึ้นอีก", "最终形态！会分裂出分身，战斗更快。", "最終形態！分身を作り、さらに速く戦う。", "최종 형태! 분신을 만들고 더욱 빠르게 싸운다.");
        A("hud_mood1", "Calm", "สงบ", "冷静", "冷静", "침착");
        A("hud_mood2", "Angry", "โกรธ", "愤怒", "怒り", "분노");
        A("hud_mood3", "Enraged", "คลั่ง", "狂暴", "激昂", "광폭");
        A("hud_mood_l", "Mood:", "อารมณ์:", "状态:", "状態:", "상태:");
        A("hud_next", "Next:", "ต่อไป:", "下一步:", "次:", "다음:");
        A("hud_next_b", "when beaten it rises again, stronger. ({0} more)", "เมื่อพ่ายแพ้จะลุกขึ้นมาอีกครั้งและแข็งแกร่งขึ้น (อีก {0} ครั้ง)", "被击败后会再次站起，变得更强。(还有{0}次)", "倒されても、より強くなって復活する。(あと{0}回)", "쓰러져도 더 강해져 다시 일어난다. ({0}번 더)");
        A("hud_last", "Last life! It will not get back up.", "ชีวิตสุดท้าย! มันจะไม่ลุกขึ้นอีก", "最后一命！它不会再站起来。", "ラストライフ！もう復活しない。", "마지막 목숨! 더 이상 일어나지 않는다.");
        A("hud_learned", "It learned:", "สิ่งที่มันเรียนรู้:", "它学到了:", "学習内容:", "학습한 것:");
        A("hud_watching", "Still watching you...", "กำลังจับตาดูคุณอยู่...", "仍在观察你……", "まだあなたを観察中……", "아직 당신을 지켜보는 중…");
        A("hud_l_guard", "You block a lot, so it uses unblockable slashes.", "คุณป้องกันบ่อย มันจึงใช้ฟันที่ป้องกันไม่ได้", "你经常格挡，所以它使用无法格挡的斩击。", "ガードが多いので、防げない斬撃を使う。", "방어를 자주 해서 막을 수 없는 베기를 쓴다.");
        A("hud_l_dash", "You dash a lot, so it slams the ground.", "คุณพุ่งบ่อย มันจึงทุบพื้น", "你经常冲刺，所以它重击地面。", "ダッシュが多いので、地面を叩きつける。", "대시를 자주 해서 땅을 내려친다.");
        A("hud_l_range", "You fight from afar, so it shoots back and runs faster.", "คุณสู้จากระยะไกล มันจึงยิงตอบโต้และวิ่งเร็วขึ้น", "你远程作战，所以它会反击并跑得更快。", "遠距離で戦うので、撃ち返して速く走る。", "멀리서 싸워서 반격하고 더 빨리 달린다.");
        A("hud_l_aggro", "You fight up close, so it counters you.", "คุณสู้ระยะประชิด มันจึงสวนกลับ", "你近身作战，所以它会反击你。", "近距離で戦うので、カウンターしてくる。", "근접전을 해서 반격한다.");
        A("hud_now", "Now:", "ตอนนี้:", "当前:", "現在:", "지금:");
        A("hud_stagger", "Off balance! Hit it now, double damage.", "เสียสมดุล! โจมตีเลย ดาเมจสองเท่า", "失去平衡！现在攻击，双倍伤害。", "体勢が崩れた！今すぐ攻撃、ダメージ2倍。", "균형 상실! 지금 공격하면 피해 2배.");
        A("hud_cwarn", "It is about to fight back. Get ready.", "มันกำลังจะสวนกลับ เตรียมตัว", "它要反击了，做好准备。", "反撃しようとしている。身構えて。", "반격하려 한다. 준비하세요.");
        A("hud_cstance", "Counter stance. Do not hit it!", "ท่าตั้งรับสวนกลับ อย่าโจมตี!", "反击架势。不要攻击它！", "カウンター体勢。攻撃しないで！", "반격 자세. 때리지 마세요!");
        A("hud_cdodge", "It is vanishing to get behind you.", "มันกำลังหายตัวไปอยู่ข้างหลังคุณ", "它正在消失并绕到你身后。", "姿を消して背後に回り込もうとしている。", "사라져서 당신의 뒤로 가려 한다.");
        A("hud_deflect", "It is swatting your arrows away.", "มันกำลังปัดธนูของคุณทิ้ง", "它正在拍飞你的箭。", "矢を叩き落としている。", "당신의 화살을 쳐내고 있다.");
        A("hud_rising", "It is rising again, stronger...", "มันกำลังลุกขึ้นอีกครั้ง แข็งแกร่งขึ้น...", "它正在重新站起，变得更强……", "より強くなって復活しようとしている……", "더 강해져서 다시 일어나고 있다…");
        A("hud_charge", "Red glow! Dash and parry fail. Back away.", "เรืองแสงแดง! พุ่งและปัดป้องไม่ได้ ถอยออกไป", "红光！冲刺和弹反无效。快后退。", "赤い光！ダッシュもパリィも通用しない。下がれ。", "붉은 빛! 대시와 패링이 통하지 않는다. 물러나세요.");
        A("hud_slam", "Ground slam. Dash out of the circle.", "ทุบพื้น พุ่งออกจากวงกลม", "重击地面。冲刺离开圆圈。", "地面叩きつけ。円の外へダッシュ。", "지면 강타. 대시로 원 밖으로 나가세요.");
        A("hud_gbreak", "Guard break! Dash, do not block.", "ทำลายการ์ด! พุ่งหลบ อย่าป้องกัน", "破防！冲刺，不要格挡。", "ガードブレイク！ダッシュで避けて、ガードしない。", "가드 브레이크! 막지 말고 대시하세요.");
        A("hud_trade", "Power move. Leave the circle.", "ท่าพลัง ออกจากวงกลม", "强力招式。离开圆圈。", "強力な技。円から離れろ。", "강력한 기술. 원 밖으로 벗어나세요.");
        A("hud_bstab", "Backstab. Dash!", "แทงข้างหลัง พุ่งหลบ!", "背刺。冲刺！", "バックスタブ。ダッシュ！", "백스탭. 대시!");
        A("hud_interrupt", "It is rushing in to stop your charge.", "มันพุ่งเข้ามาขัดการชาร์จของคุณ", "它正冲上来打断你的蓄力。", "溜めを止めようと突進してくる。", "당신의 차지를 막으려 돌진한다.");
        A("hud_zone", "Shockwave coming. Keep moving.", "คลื่นกระแทกกำลังมา เคลื่อนที่ต่อไป", "冲击波来了。继续移动。", "衝撃波が来る。動き続けて。", "충격파가 온다. 계속 움직이세요.");
        A("hud_shoot", "Shooting! Dodge or parry the bullets.", "กำลังยิง! หลบหรือปัดกระสุน", "射击中！躲开或弹反子弹。", "射撃中！弾を避けるかパリィしよう。", "사격 중! 탄환을 피하거나 패링하세요.");
        A("hud_clone", "It is splitting into clones.", "มันกำลังแยกร่างเป็นโคลน", "它正在分裂出分身。", "分身を作っている。", "분신을 만들고 있다.");
        A("hud_roar", "It is getting angrier.", "มันกำลังโกรธมากขึ้น", "它越来越愤怒了。", "怒りが増している。", "점점 더 화가 나고 있다.");
    }
}