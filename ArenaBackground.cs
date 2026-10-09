using UnityEngine;

// ภาพพื้นหลังสนาม: ปรับขนาดให้เต็มจอเอง มีภาพซ้อน ขอบมืด และโทนสีที่ร้อนขึ้นตามความดุของบอส
public class ArenaBackground : MonoBehaviour
{
    [Header("Boss Light (แสงสีรอบบอส)")]
    public bool bossLight = true;
    public float bossLightSize = 9f;
    SpriteRenderer lightSr;

    [Header("Images")]
    public Sprite backgroundSprite;
    public Sprite overlaySprite;             // ภาพซ้อน เช่น หมอก แสง (ไม่ใส่ก็ได้)

    [Header("Fit")]
    public bool fitToCamera = true;          // ปรับขนาดให้เต็มภาพที่กล้องเห็น
    public Vector2 arenaSize = new Vector2(21f, 13f);   // ใช้เมื่อปิด Fit To Camera
    public int backgroundOrder = -20;
    public int overlayOrder = -15;

    [Header("Mood")]
    public Color tint = Color.white;
    public bool reactToBoss = true;          // บอสวิวัฒนาการแล้วพื้นหลังออกแดงขึ้น
    public bool vignette = true;
    [Range(0f, 1f)] public float vignetteStrength = 0.6f;

    SpriteRenderer bg, overlay;
    Color currentTint;

    void Start()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();

        Vector2 area = arenaSize;
        Vector3 center = Vector3.zero;

        if (cam != null)
        {
            center = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
            if (fitToCamera)
            {
                float h = cam.orthographicSize * 2f;
                area = new Vector2(h * cam.aspect, h);
            }
        }

        if (backgroundSprite != null)
            bg = Make("Background", backgroundSprite, backgroundOrder, center, area, true);

        if (overlaySprite != null)
            overlay = Make("BackgroundOverlay", overlaySprite, overlayOrder, center, area, true);

        if (vignette)
        {
            var v = Make("Vignette", MakeVignetteSprite(), 90, center, area * 1.05f, false);
            v.color = new Color(0f, 0f, 0f, vignetteStrength);
        }

        currentTint = tint; if (lightSr != null)
        {
            BossAI b = BossAI.Main;
            lightSr.enabled = b != null && b.IsTargetable;
            if (lightSr.enabled)
            {
                Color[] form = { new Color(0.4f, 0.65f, 1f), new Color(1f, 0.6f, 0.2f), new Color(1f, 0.2f, 0.25f) };
                Color c = form[Mathf.Clamp(b.Evolution, 0, 2)];
                float pulse = 0.16f + 0.06f * Mathf.Sin(Time.time * (2f + b.Phase));
                lightSr.color = new Color(c.r, c.g, c.b, pulse + 0.05f * b.Phase);
                lightSr.transform.position = new Vector3(b.transform.position.x, b.transform.position.y, 0f);
                lightSr.transform.localScale = Vector3.one * bossLightSize * (1f + 0.15f * b.Phase);
            }
        }
    }

    SpriteRenderer Make(string name, Sprite sprite, int order, Vector3 center, Vector2 area, bool cover)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = center;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        sr.color = tint;

        Vector2 size = sprite.bounds.size;
        float sx = area.x / Mathf.Max(0.0001f, size.x);
        float sy = area.y / Mathf.Max(0.0001f, size.y);
        float s = cover ? Mathf.Max(sx, sy) : 1f;

        if (cover) go.transform.localScale = new Vector3(s, s, 1f);
        else go.transform.localScale = new Vector3(sx, sy, 1f);

        return sr;
    }

    void Update()
    {
        if (bg == null) return;

        Color target = tint;
        BossAI boss = BossAI.Main;
        if (reactToBoss && boss != null)
        {
            float heat = Mathf.Clamp01(boss.Evolution * 0.25f + boss.Phase * 0.1f);
            target = Color.Lerp(tint, new Color(tint.r, tint.g * 0.6f, tint.b * 0.6f, tint.a), heat);
        }

        currentTint = Color.Lerp(currentTint, target, Time.unscaledDeltaTime * 2f);
        bg.color = currentTint;
        if (overlay != null) overlay.color = currentTint;
        if (lightSr != null)
        {
            BossAI b = BossAI.Main;
            lightSr.enabled = b != null && b.IsTargetable;
            if (lightSr.enabled)
            {
                Color[] form = { new Color(0.4f, 0.65f, 1f), new Color(1f, 0.6f, 0.2f), new Color(1f, 0.2f, 0.25f) };
                Color c = form[Mathf.Clamp(b.Evolution, 0, 2)];
                float pulse = 0.16f + 0.06f * Mathf.Sin(Time.time * (2f + b.Phase));
                lightSr.color = new Color(c.r, c.g, c.b, pulse + 0.05f * b.Phase);
                lightSr.transform.position = new Vector3(b.transform.position.x, b.transform.position.y, 0f);
                lightSr.transform.localScale = Vector3.one * bossLightSize * (1f + 0.15f * b.Phase);
            }
        }
    }

    // ขอบมืด ตรงกลางโปร่ง
    static Sprite MakeVignetteSprite()
    {
        int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        float c = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / (size / 2f);
                float a = Mathf.Pow(Mathf.Clamp01((d - 0.45f) / 0.75f), 1.6f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
