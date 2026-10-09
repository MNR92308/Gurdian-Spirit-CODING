using UnityEngine;
using UnityEngine.SceneManagement;

// เพลงตามฉาก: เมนู / ต่อสู้ปกติ / ต่อสู้ดุ (เฟส 2 หรือบอสฟื้น) / ชัยชนะ (บอสตายสมบูรณ์) / ตาย
public class BackgroundMusic : MonoBehaviour
{
    public static BackgroundMusic Instance;

    [Header("Clips (ช่องไหนไม่มีเพลง เว้นว่างได้)")]
    public AudioClip menuMusic;
    public AudioClip calmMusic;
    public AudioClip intenseMusic;
    public AudioClip victoryMusic;      // เล่นเมื่อบอสตายสมบูรณ์ และในฉาก Victory
    public AudioClip gameOverMusic;

    [Header("Settings")]
    [Range(0f, 1f)] public float volume = 0.5f;
    public float fadeSpeed = 0.7f;

    const int Menu = 0, Calm = 1, Intense = 2, Victory = 3, Over = 4;

    AudioSource[] src = new AudioSource[5];
    bool bossDefeated;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        src[Menu] = Make(menuMusic);
        src[Calm] = Make(calmMusic);
        src[Intense] = Make(intenseMusic);
        src[Victory] = Make(victoryMusic);
        src[Over] = Make(gameOverMusic);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bossDefeated = false;      // เข้าฉากใหม่ เริ่มเพลงใหม่ตามฉาก
    }

    AudioSource Make(AudioClip clip)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.clip = clip;
        s.loop = true;
        s.playOnAwake = false;
        s.spatialBlend = 0f;
        s.volume = 0f;
        if (clip != null) s.Play();
        return s;
    }

    bool Has(int i) { return src[i] != null && src[i].clip != null; }

    void Update()
    {
        string scene = SceneManager.GetActiveScene().name;
        int want;

        if (scene == GameScenes.Menu) want = Menu;
        else if (scene == GameScenes.GameOver) want = Over;
        else if (scene == GameScenes.Victory) want = Victory;
        else
        {
            BossAI boss = BossAI.Main;
            if (boss != null && boss.IsFinalDeath) bossDefeated = true;

            if (bossDefeated) want = Victory;
            else if (boss != null && (boss.Phase >= 1 || boss.Evolution >= 1) && Has(Intense)) want = Intense;
            else want = Calm;
        }

        // ไม่มีเพลงที่ต้องการ ใช้เพลงต่อสู้ปกติแทน
        if (!Has(want)) want = Has(Calm) ? Calm : -1;

        for (int i = 0; i < src.Length; i++)
        {
            float target = (i == want) ? volume : 0f;
            src[i].volume = Mathf.MoveTowards(src[i].volume, target, fadeSpeed * volume * Time.unscaledDeltaTime * 2f);
        }
    }
}