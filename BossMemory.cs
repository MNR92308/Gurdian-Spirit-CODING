using UnityEngine;

// ความจำของบอส: เรียนรู้นิสัยผู้เล่นและจำนวนครั้งที่ตาย เก็บข้ามด่านและข้ามการเล่น
public static class BossMemory
{
    static bool loaded;

    public static int TotalDeaths;
    public static float Guard = 0.25f;    // ผู้เล่นชอบ Parry/การ์ดตอนถูกโจมตี (0-1)
    public static float Dash = 0.25f;     // ผู้เล่นชอบแดชหลบ
    public static float Range = 0.25f;    // ผู้เล่นชอบอยู่ไกล/ยิงธนู
    public static float Aggro = 0.25f;    // ผู้เล่นชอบอยู่ประชิดบอส

    public static void Load()
    {
        if (loaded) return;
        loaded = true;

        TotalDeaths = PlayerPrefs.GetInt("BossDeaths", 0);
        Guard = PlayerPrefs.GetFloat("BossGuard", 0.25f);
        Dash = PlayerPrefs.GetFloat("BossDash", 0.25f);
        Range = PlayerPrefs.GetFloat("BossRange", 0.25f);
        Aggro = PlayerPrefs.GetFloat("BossAggro", 0.25f);
    }

    public static void Save()
    {
        PlayerPrefs.SetInt("BossDeaths", TotalDeaths);
        PlayerPrefs.SetFloat("BossGuard", Guard);
        PlayerPrefs.SetFloat("BossDash", Dash);
        PlayerPrefs.SetFloat("BossRange", Range);
        PlayerPrefs.SetFloat("BossAggro", Aggro);
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        TotalDeaths = 0;
        Guard = Dash = Range = Aggro = 0.25f;
        loaded = true;

        PlayerPrefs.DeleteKey("BossDeaths");
        PlayerPrefs.DeleteKey("BossGuard");
        PlayerPrefs.DeleteKey("BossDash");
        PlayerPrefs.DeleteKey("BossRange");
        PlayerPrefs.DeleteKey("BossAggro");
        PlayerPrefs.Save();
    }
}