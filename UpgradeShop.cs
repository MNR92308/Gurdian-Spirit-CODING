using UnityEngine;

public enum Up { StartArmor = 0, ArmorCap = 1, Arrows = 2, Stamina = 3 }

// เหรียญ + ระดับอัพเกรด (เก็บใน PlayerPrefs ไม่ถูกลบตอน New Game)
public static class UpgradeData
{
    public const int BaseStartArmor = 1;     // เกราะตอนเข้าเกมก่อนอัพเกรด (อัพเกรดได้ถึง 3)
    public const int BaseArmorCap = 3;       // ขีดจำกัดเกราะก่อนอัพเกรด (อัพเกรดได้ถึง 5)
    public const int BaseArrows = 1;         // ลูกธนูก่อนอัพเกรด (อัพเกรดได้ถึง 3)
    public const float StaminaPerLevel = 10f;

    // ราคาของแต่ละระดับ (จำนวนช่อง = จำนวนระดับที่ซื้อได้)
    static readonly int[][] Prices =
    {
        new[] { 4, 8 },
        new[] { 10, 20 },
        new[] { 8, 16 },
        new[] { 5, 8, 12, 16 },
    };

    public static int Coins { get { return PlayerPrefs.GetInt("Coins", 0); } }

    public static int MaxLevel(Up u) { return Prices[(int)u].Length; }
    public static int Level(Up u) { return Mathf.Clamp(PlayerPrefs.GetInt("Up_" + u, 0), 0, MaxLevel(u)); }
    public static bool IsMax(Up u) { return Level(u) >= MaxLevel(u); }
    public static int Price(Up u) { return IsMax(u) ? 0 : Prices[(int)u][Level(u)]; }

    public static bool Buy(Up u)
    {
        if (IsMax(u)) return false;
        int p = Price(u);
        if (Coins < p) return false;
        PlayerPrefs.SetInt("Coins", Coins - p);
        PlayerPrefs.SetInt("Up_" + u, Level(u) + 1);
        PlayerPrefs.Save();
        return true;
    }

    // ===== ค่าที่เกมอ่านไปใช้ =====
    public static int ArmorCap { get { return BaseArmorCap + Level(Up.ArmorCap); } }
    public static int StartArmor { get { return Mathf.Min(BaseStartArmor + Level(Up.StartArmor), ArmorCap); } }
    public static int Arrows { get { return BaseArrows + Level(Up.Arrows); } }
    public static float StaminaBonus { get { return Level(Up.Stamina) * StaminaPerLevel; } }

    // ===== เหรียญของรอบที่กำลังเล่น =====
    public static int RunCoins { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { RunCoins = 0; }

    // บอสตายหนึ่งชีวิต: ได้เหรียญเท่าเลขด่าน
    public static void OnBossKilled(Vector3 pos)
    {
        int n = Mathf.Max(1, GameProgress.Stage);
        RunCoins += n;
        FX.Popup(pos + Vector3.up * 2.6f, "+" + n + " COIN", new Color(1f, 0.85f, 0.25f));
    }

    // ชนะด่าน: ได้ทั้งหมด
    public static void CommitRun()
    {
        Add(RunCoins);
        RunCoins = 0;
    }

    // ผู้เล่นตาย: ได้ครึ่งหนึ่ง (ปัดขึ้น)
    public static void OnPlayerDied()
    {
        Add((RunCoins + 1) / 2);
        RunCoins = 0;
    }

    // ออกไปหน้าเมนูกลางด่าน: ทิ้งเหรียญของรอบนั้น
    public static void DiscardRun() { RunCoins = 0; }

    static void Add(int n)
    {
        if (n <= 0) return;
        PlayerPrefs.SetInt("Coins", Coins + n);
        PlayerPrefs.Save();
    }
}

// พ่อค้า + หน้าต่างร้าน (วาดด้วย IMGUI ในพิกัดออกแบบ "จอสูง 720" ผู้เรียกต้องตั้ง GUI.matrix ไว้ก่อน)
public static class ShopUI
{
    // ลำดับภาษา: 0=EN 1=TH 2=ZH 3=JA 4=KO (ตรงกับ Loc.Current)
    const int tShop = 0, tTap = 1, tCoins = 2, tBuy = 3, tMax = 4, tFoot = 5, tName = 6, tDesc = 10;

    static readonly string[][] T =
    {
        /* 0 shop  */ new[] { "UPGRADE SHOP", "ร้านอัพเกรด", "升级商店", "アップグレードショップ", "업그레이드 상점" },
        /* 1 tap   */ new[] { "SHOP", "ร้านค้า", "商店", "ショップ", "상점" },
        /* 2 coins */ new[] { "Coins", "เหรียญ", "金币", "コイン", "코인" },
        /* 3 buy   */ new[] { "BUY", "ซื้อ", "购买", "購入", "구매" },
        /* 4 max   */ new[] { "MAX", "สูงสุด", "已满", "最大", "최대" },
        /* 5 foot  */ new[] {
            "Defeat a boss life: coins = stage number. Die: keep half.",
            "โค่นบอสหนึ่งชีวิตได้เหรียญเท่าเลขด่าน ตายได้ครึ่งหนึ่ง",
            "击败Boss一条命获得与关卡数相同的金币,死亡时保留一半",
            "ボスの命を1つ倒すとステージ数分のコイン。死亡時は半分",
            "보스 생명 1개 처치 시 스테이지 수만큼 코인. 사망 시 절반" },
        /* 6 name StartArmor */ new[] { "Starting Armor", "เกราะเริ่มเกม", "初始护甲", "初期アーマー", "시작 방어구" },
        /* 7 name ArmorCap   */ new[] { "Armor Limit", "ขีดจำกัดเกราะ", "护甲上限", "アーマー上限", "방어구 한도" },
        /* 8 name Arrows     */ new[] { "Arrows", "จำนวนลูกธนู", "箭矢数量", "矢の数", "화살 수" },
        /* 9 name Stamina    */ new[] { "Stamina", "Stamina", "体力", "スタミナ", "스태미나" },
        /* 10 desc StartArmor */ new[] { "Armor you start with (max 3)", "เกราะที่ได้ตอนเข้าเกม (สูงสุด 3)", "每局开始时的护甲(最多3)", "開始時のアーマー(最大3)", "시작 시 방어구(최대 3)" },
        /* 11 desc ArmorCap   */ new[] { "Max armor you can hold (3 to 5)", "เกราะสูงสุดที่ถือได้ (3 ถึง 5)", "可持有的最大护甲(3到5)", "所持できるアーマー上限(3から5)", "보유 가능한 최대 방어구(3에서 5)" },
        /* 12 desc Arrows     */ new[] { "Arrows you can carry (max 3)", "จำนวนลูกธนูที่พกได้ (สูงสุด 3)", "可携带的箭矢(最多3)", "所持できる矢の数(最大3)", "소지 가능한 화살(최대 3)" },
        /* 13 desc Stamina    */ new[] { "+10 max stamina per level", "แต่ละเลเวล +10 Stamina สูงสุด", "每级 +10 体力上限", "1レベルごとに最大スタミナ +10", "레벨당 최대 스태미나 +10" },
    };

    static GUIStyle nameSt, descSt, btnSt, smallSt, titleSt;

    static string Tr(int key) { return T[key][Mathf.Clamp((int)Loc.Current, 0, 4)]; }

    static void Styles()
    {
        if (nameSt != null) return;
        titleSt = UIKit.MakeStyle(44, FontStyle.Bold, TextAnchor.MiddleCenter);
        nameSt = UIKit.MakeStyle(26, FontStyle.Bold, TextAnchor.MiddleLeft);
        descSt = UIKit.MakeStyle(18, FontStyle.Normal, TextAnchor.UpperLeft, true);
        descSt.normal.textColor = new Color(0.85f, 0.88f, 0.95f);
        btnSt = UIKit.MakeStyle(24, FontStyle.Bold, TextAnchor.MiddleCenter);
        smallSt = UIKit.MakeStyle(22, FontStyle.Bold, TextAnchor.MiddleCenter);
    }

    // ===== ป้ายเหรียญมุมขวาบน =====
    public static void DrawCoins(float W)
    {
        Styles();
        float s = Screen.height / 720f;
        Rect sa = Screen.safeArea;
        float right = sa.xMax / s - 16f;
        float top = (Screen.height - sa.yMax) / s + 16f;
        Rect r = new Rect(right - 210f, top, 210f, 48f);

        if (Event.current.type == EventType.Repaint)
        {
            UIKit.Fill(new Rect(r.x - 2f, r.y - 2f, r.width + 4f, r.height + 4f), new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, 0.8f));
            UIKit.Fill(r, new Color(0.05f, 0.06f, 0.1f, 0.9f));
            UIKit.DrawCircle(new Vector2(r.x + 26f, r.center.y), 16f, UIKit.Gold);
            UIKit.DrawCircle(new Vector2(r.x + 26f, r.center.y), 10f, new Color(1f, 0.95f, 0.6f));
        }
        GUIStyle left = smallSt;
        TextAnchor old = left.alignment;
        left.alignment = TextAnchor.MiddleLeft;
        UIKit.ShadowLabel(new Rect(r.x + 52f, r.y, r.width - 60f, r.height), Tr(tCoins) + "  " + UpgradeData.Coins, left, Color.white);
        left.alignment = old;
    }

    // ===== พ่อค้าซ้ายจอ คืน true เมื่อถูกกด/แตะ =====
    public static bool DrawNpc(float W)
    {
        Styles();
        float s = Screen.height / 720f;
        Rect sa = Screen.safeArea;
        float leftEdge = sa.xMin / s;
        float cx = W / 2f;
        float x = Mathf.Max(leftEdge + 8f, Mathf.Min(leftEdge + 50f, cx - 210f - 190f));
        float t = Time.unscaledTime;

        Rect hit = new Rect(x - 10f, 380f, 200f, 270f);
        bool hover = !GameSettings.Touch && hit.Contains(Event.current.mousePosition);

        if (Event.current.type == EventType.Repaint)
        {
            float bob = Mathf.Sin(t * 2f) * 3f;
            Color wood = new Color(0.45f, 0.28f, 0.14f);

            if (hover) UIKit.Fill(new Rect(x - 14f, 396f, 208f, 254f), new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, 0.12f));

            UIKit.Fill(new Rect(x, 636f, 180f, 8f), new Color(0f, 0f, 0f, 0.35f));       // เงาพื้น
            UIKit.Fill(new Rect(x + 6f, 456f, 8f, 184f), wood);                           // เสา
            UIKit.Fill(new Rect(x + 166f, 456f, 8f, 184f), wood);
            for (int i = 0; i < 9; i++)                                                   // กันสาดลายทาง
                UIKit.Fill(new Rect(x - 6f + i * 21f, 440f, 21f, 26f), i % 2 == 0 ? UIKit.Red : Color.white);

            // พ่อค้า
            Vector2 head = new Vector2(x + 90f, 506f + bob);
            UIKit.Fill(new Rect(x + 62f, 530f + bob, 56f, 64f), new Color(0.35f, 0.2f, 0.6f));
            UIKit.DrawCircle(head, 24f, new Color(0.95f, 0.78f, 0.6f));
            UIKit.DrawCircle(new Vector2(head.x, head.y - 14f), 26f, UIKit.Orange);
            UIKit.Fill(new Rect(head.x - 34f, head.y - 6f, 68f, 7f), new Color(0.8f, 0.4f, 0.08f));
            UIKit.DrawCircle(new Vector2(head.x - 8f, head.y + 4f), 3f, new Color(0.1f, 0.1f, 0.15f));
            UIKit.DrawCircle(new Vector2(head.x + 8f, head.y + 4f), 3f, new Color(0.1f, 0.1f, 0.15f));

            // เคาน์เตอร์
            UIKit.Fill(new Rect(x, 580f, 180f, 14f), new Color(0.65f, 0.42f, 0.2f));
            UIKit.Fill(new Rect(x + 4f, 594f, 172f, 46f), wood);
            UIKit.DrawCircle(new Vector2(x + 30f, 574f), 10f, UIKit.Gold);
            UIKit.DrawCircle(new Vector2(x + 52f, 576f), 8f, UIKit.Gold);

            // ฟองคำพูด
            float by = 424f + Mathf.Sin(t * 3f) * 4f;
            UIKit.Fill(new Rect(x + 55f, by - 36f, 70f, 30f), new Color(0f, 0f, 0f, 0.65f));
            UIKit.Fill(new Rect(x + 85f, by - 6f, 10f, 8f), new Color(0f, 0f, 0f, 0.65f));
        }

        UIKit.ShadowLabel(new Rect(x + 55f, 388f + Mathf.Sin(t * 3f) * 4f, 70f, 30f), "!", nameSt, UIKit.Gold);
        UIKit.ShadowLabel(new Rect(x, 596f, 180f, 40f), Tr(tTap), smallSt, Color.white);

        return GUI.Button(hit, GUIContent.none, GUIStyle.none);
    }

    // ===== หน้าต่างร้าน คืน true เมื่อกด BACK =====
    public static bool DrawShop(float cx, float W)
    {
        Styles();
        float pw = Mathf.Min(820f, W - 40f);
        Rect p = new Rect(cx - pw / 2f, 20f, pw, 680f);
        UIKit.Panel(p, UIKit.Gold, Tr(tShop), titleSt);
        DrawCoins(W);

        for (int k = 0; k < 4; k++)
        {
            Up u = (Up)k;
            Rect row = new Rect(p.x + 24f, p.y + 96f + k * 108f, p.width - 48f, 100f);
            int lv = UpgradeData.Level(u), max = UpgradeData.MaxLevel(u);

            UIKit.Fill(row, new Color(1f, 1f, 1f, 0.05f));
            UIKit.Fill(new Rect(row.x, row.y, 5f, row.height), UIKit.Gold);

            UIKit.ShadowLabel(new Rect(row.x + 18f, row.y + 6f, row.width - 250f, 34f), Tr(tName + k), nameSt, Color.white);
            GUI.Label(new Rect(row.x + 18f, row.y + 42f, row.width - 250f, 34f), Tr(tDesc + k), descSt);

            for (int i = 0; i < max; i++)
            {
                Vector2 c = new Vector2(row.x + 28f + i * 26f, row.y + 86f);
                UIKit.DrawCircle(c, 9f, new Color(1f, 1f, 1f, 0.25f));
                if (i < lv) UIKit.DrawCircle(c, 7f, UIKit.Gold);
            }

            Rect b = new Rect(row.xMax - 212f, row.y + 22f, 196f, 56f);
            if (UpgradeData.IsMax(u))
            {
                UIKit.Button(b, Tr(tMax), UIKit.Gray, btnSt, false);
            }
            else
            {
                int price = UpgradeData.Price(u);
                bool can = UpgradeData.Coins >= price;
                if (UIKit.Button(b, price + "  " + Tr(tBuy), can ? UIKit.Green : UIKit.Gray, btnSt, can))
                    UpgradeData.Buy(u);
            }
        }

        GUI.Label(new Rect(p.x + 30f, p.yMax - 112f, p.width - 60f, 44f), Tr(tFoot), descSt);

        return UIKit.Button(new Rect(cx - 130f, p.yMax - 64f, 260f, 50f), Loc.T("back"), UIKit.Blue, btnSt);
    }
}