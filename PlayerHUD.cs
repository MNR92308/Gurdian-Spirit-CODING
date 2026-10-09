using UnityEngine;

// HUD มุมซ้ายล่าง: เลือด / เกราะ / Stamina
// วิธีใช้: แปะสคริปต์นี้บนออบเจกต์ Player (ตัวเดียวกับ PlayerHealth และ PlayerStamina)
public class PlayerHUD : MonoBehaviour
{
    [Header("Layout (ที่ความสูงจอ 1080)")]
    public float margin = 24f;
    public float labelWidth = 80f;
    public float barWidth = 260f;
    public float barHeight = 22f;
    public float spacing = 10f;
    public float segmentGap = 4f;

    [Header("Colors")]
    public Color bgColor = new Color(0f, 0f, 0f, 0.65f);
    public Color emptyColor = new Color(1f, 1f, 1f, 0.12f);
    public Color hpColor = new Color(0.9f, 0.2f, 0.2f);
    public Color armorColor = new Color(1f, 0.85f, 0.25f);

    [Header("Stamina")]
    public float staminaSmooth = 12f;

    PlayerHealth health;
    PlayerStamina stamina;
    float shownStamina;
    GUIStyle labelStyle;

    void Awake()
    {
        health = GetComponent<PlayerHealth>();
        stamina = GetComponent<PlayerStamina>();
        if (health == null) health = FindAnyObjectByType<PlayerHealth>();
        if (stamina == null) stamina = FindAnyObjectByType<PlayerStamina>();
    }

    void Start()
    {
        if (stamina != null) shownStamina = stamina.Current;
    }

    void Update()
    {
        if (stamina != null)
            shownStamina = Mathf.Lerp(shownStamina, stamina.Current, 1f - Mathf.Exp(-staminaSmooth * Time.unscaledDeltaTime));
    }

    void OnGUI()
    {
        float s = Screen.height / 1080f;
        float m = margin * s, lw = labelWidth * s, bw = barWidth * s, bh = barHeight * s, sp = spacing * s;

        if (labelStyle == null) labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft };
        labelStyle.fontSize = Mathf.RoundToInt(20f * s);
        labelStyle.normal.textColor = Color.white;

        // แถวล่างสุด = Stamina, ถัดขึ้นไป = เกราะ, บนสุด = เลือด
        float yStamina = Screen.height - m - bh;
        float yArmor = yStamina - sp - bh;
        float yHp = yArmor - sp - bh;
        float x = m;

        if (health != null)
        {
            DrawRow(x, yHp, lw, bw, bh, "HP");
            DrawSegments(new Rect(x + lw, yHp, bw, bh), health.CurrentHits, health.MaxHits, hpColor, s);

            DrawRow(x, yArmor, lw, bw, bh, "ARMOR");
            DrawSegments(new Rect(x + lw, yArmor, bw, bh), health.ArmorLevel, PlayerHealth.MaxArmor, armorColor, s);
        }

        if (stamina != null)
        {
            DrawRow(x, yStamina, lw, bw, bh, "STAMINA");

            float ratio = Mathf.Clamp01(shownStamina / Mathf.Max(0.01f, stamina.Max));
            Color c;
            if (ratio < 0.2f) c = Color.red;
            else if (ratio < 0.5f) c = Color.yellow;
            else c = new Color(0.3f, 0.9f, 0.3f);

            Rect r = new Rect(x + lw, yStamina, bw, bh);
            Fill(r, emptyColor);
            Fill(new Rect(r.x, r.y, r.width * ratio, r.height), c);
        }

        GUI.color = Color.white;
    }

    // ป้ายชื่อ + กรอบพื้นหลังดำ
    void DrawRow(float x, float y, float lw, float bw, float bh, string label)
    {
        float pad = 3f;
        Fill(new Rect(x + lw - pad, y - pad, bw + pad * 2f, bh + pad * 2f), bgColor);
        GUI.Label(new Rect(x, y, lw - 6f, bh), label, labelStyle);
    }

    // แถบแบ่งช่อง (ช่องละ 1 hit / 1 เกราะ)
    void DrawSegments(Rect r, int filled, int total, Color on, float s)
    {
        total = Mathf.Max(1, total);
        float gap = segmentGap * s;
        float segW = (r.width - gap * (total - 1)) / total;

        for (int i = 0; i < total; i++)
        {
            Rect seg = new Rect(r.x + i * (segW + gap), r.y, segW, r.height);
            Fill(seg, i < filled ? on : emptyColor);
        }
    }

    static void Fill(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
    }
}