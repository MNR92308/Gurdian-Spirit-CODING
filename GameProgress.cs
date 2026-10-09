using UnityEngine;
using UnityEngine.SceneManagement;

// ชื่อฉากทั้งหมด (ต้องตรงกับชื่อฉากใน Unity เป๊ะๆ)
public static class GameScenes
{
    public const string Menu = "MainMenu";
    public const string Arena = "Arena";
    public const string GameOver = "GameOver";
    public const string Victory = "Victory";

    public static void Load(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("ไม่พบฉาก '" + sceneName + "' ใน Build Profiles โหลดฉากปัจจุบันซ้ำแทน");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}

// ความคืบหน้าของผู้เล่น (ด่าน เกราะ ชนะหรือยัง)
public static class GameProgress
{
    public const int TotalStages = 4;

    const string StageKey = "Stage";
    const string ArmorKey = "ArmorLevel";
    const string WonKey = "Won";

    public static int Stage => Mathf.Clamp(PlayerPrefs.GetInt(StageKey, 1), 1, TotalStages);
    public static int ArmorLevel => Mathf.Clamp(PlayerPrefs.GetInt(ArmorKey, 0), 0, PlayerHealth.MaxArmor);
    public static bool Won => PlayerPrefs.GetInt(WonKey, 0) == 1;
    public static bool HasProgress => !Won && (Stage > 1 || ArmorLevel > 0);

    public static void SaveArmor(int level)
    {
        PlayerPrefs.SetInt(ArmorKey, Mathf.Clamp(level, 0, PlayerHealth.MaxArmor));
        PlayerPrefs.Save();
    }

    // ชนะบอสของด่านนี้: ไปด่านถัดไป หรือถ้าเป็นด่านสุดท้ายถือว่าชนะเกม
    public static void CompleteStage()
    {
        if (Stage >= TotalStages) PlayerPrefs.SetInt(WonKey, 1);
        else PlayerPrefs.SetInt(StageKey, Stage + 1);
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(StageKey);
        PlayerPrefs.DeleteKey(ArmorKey);
        PlayerPrefs.DeleteKey(WonKey);
        PlayerPrefs.Save();
    }
}