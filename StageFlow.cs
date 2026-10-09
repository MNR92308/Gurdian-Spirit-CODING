using System.Collections;
using UnityEngine;

// วางในฉาก Arena บนวัตถุที่ชื่อ "StageFlow" (ชื่อต้องตรง BossAI ค้นหาด้วยชื่อนี้)
public class StageFlow : MonoBehaviour
{
    public static StageFlow Instance { get; private set; }

    public float clearDelay = 3f;

    [Header("Mobile")]
    public float mobileScale = 1.3f;     // ขยาย Stage / Coin บนมือถือ (เพิ่มค่านี้ถ้ายังเล็กไป)

    bool cleared;
    GUIStyle stageSt, coinSt, bigSt;

    void Awake()
    {
        Instance = this;
    }

    // BossAI เรียกเมื่อบอสตัวจริงตายสมบูรณ์
    public void OnBossDefeated()
    {
        if (cleared) return;
        cleared = true;
        StartCoroutine(ClearRoutine());
    }

    IEnumerator ClearRoutine()
    {
        yield return new WaitForSeconds(clearDelay);

        // บันทึกเกราะที่ได้ในด่านนี้ (ตายระหว่างด่านจะไม่ได้เก็บ)
        var ph = FindAnyObjectByType<PlayerHealth>();
        if (ph != null) GameProgress.SaveArmor(ph.ArmorLevel);

        UpgradeData.CommitRun();             // ชนะด่าน: ได้เหรียญที่สะสมไว้ทั้งหมด
        GameProgress.CompleteStage();

        if (GameProgress.Won) GameScenes.Load(GameScenes.Victory);
        else GameScenes.Load(GameScenes.Arena);        // โหลดฉากเดิมซ้ำ ด่านถัดไป
    }

#if UNITY_EDITOR
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))     // รีเซ็ตความคืบหน้าทั้งหมด
        {
            GameProgress.ResetAll();
            GameScenes.Load(GameScenes.Arena);
        }
        if (Input.GetKeyDown(KeyCode.F10))    // ฆ่าบอสหนึ่งชีวิตทันที (ทดสอบ)
        {
            var boss = BossAI.Main;
            if (boss != null) boss.ReceiveHit(9999, HitType.Other);
        }
    }
#endif

    void MakeStyles()
    {
        if (stageSt != null) return;

        stageSt = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight };
        stageSt.normal.textColor = Color.white;

        coinSt = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight };
        coinSt.normal.textColor = new Color(1f, 0.85f, 0.25f);

        bigSt = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 56 };
        bigSt.normal.textColor = Color.yellow;
    }

    void OnGUI()
    {
        MakeStyles();

        bool mobile = MobileInput.Active;
        float u = Screen.height / 720f;
        float k = mobile ? u * mobileScale : 1f;     // PC ขนาดเดิม / มือถือสเกลตามความสูงจอ

        stageSt.fontSize = Mathf.RoundToInt(24f * k);
        coinSt.fontSize = Mathf.RoundToInt(20f * k);

        float right, top;
        if (mobile)
        {
            // ชิดซ้ายของปุ่มหยุดเกม และเริ่มที่ระดับเดียวกับปุ่ม จะได้ไม่ทับกัน
            Rect pr = MobileInput.PauseRect(u);
            right = pr.xMin - 12f * u;
            top = pr.y;
        }
        else
        {
            right = Screen.width - 15f;
            top = 10f;
        }

        float w = 260f * k;
        GUI.Label(new Rect(right - w, top, w, 40f * k),
            "Stage " + GameProgress.Stage + " / " + GameProgress.TotalStages, stageSt);
        GUI.Label(new Rect(right - w, top + 36f * k, w, 30f * k),
            "COIN +" + UpgradeData.RunCoins, coinSt);

        if (cleared)
            GUI.Label(new Rect(0, Screen.height / 2f - 60, Screen.width, 120), "STAGE CLEAR!", bigSt);
    }
}