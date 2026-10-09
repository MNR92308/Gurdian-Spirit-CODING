using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerArrow : MonoBehaviour
{
    [Header("Arrow")]
    public Sprite arrowSprite;               // ลากภาพธนูใส่ช่องนี้
    public float spriteAngleOffset = 0f;     // ภาพธนูชี้ขวาใช้ 0 / ชี้ขึ้นใช้ -90 / ชี้ซ้ายใช้ 180
    public bool useAutoSize = true;
    public float arrowLength = 1.2f;
    public float arrowScale = 1f;
    public int arrowDamage = 1;              // 0 = ไม่ทำดาเมจ
    public float arrowSpeed = 14f;
    public float maxDistance = 9f;
    public float spawnOffset = 0.6f;
    public int sortingOrder = 6;

    [Header("Shoot Animation")]
    public float shootDuration = 0.3f;
    [Range(0f, 1f)] public float releaseTime = 0.5f;

    [Header("Pickup")]
    public float pickupRadius = 0.7f;
    public float pickupDelay = 0.4f;
    public float stuckPickupRadius = 0.5f;
    public float arrowRestockDelay = 3f;     // ธนูหาย (ถูกบอสฟันทิ้ง/บอสตายตอนธนูปักอยู่) คืนให้ทีละดอกทุกกี่วินาที

    [Header("HUD (ไอคอนธนูข้างหลอด Stamina)")]
    public bool showHud = true;
    public float hudBarWidth = 380f;
    public float hudBarHeight = 22f;
    public float hudBottomMargin = 46f;

    public int MaxArrows { get { return UpgradeData.Arrows; } }     // 1-3 ตามร้านค้า
    public int ArrowCount { get; private set; }
    public bool HasArrow { get { return ArrowCount > 0; } }
    public bool IsShooting => shooting;

    PlayerParry parry;
    PlayerVisual visual;
    PlayerHealth health;
    PlayerAudio sfx;
    Camera cam;
    GuardState prevGuard = GuardState.None;
    bool shooting;
    bool warnedNoSprite;

    readonly List<ArrowProjectile> field = new List<ArrowProjectile>();   // ธนูที่อยู่ในสนาม
    int lost;                                                              // ธนูที่หายไป รอคืนให้
    float restockTime;

    void Awake()
    {
        var all = GetComponents<PlayerArrow>();
        if (all.Length > 1 && all[0] != this)
        {
            Debug.LogWarning("พบ Player Arrow ซ้ำบน Player ปิดตัวซ้ำให้แล้ว แนะนำให้ลบตัวที่ซ้ำออกจาก Inspector");
            enabled = false;
            return;
        }

        parry = GetComponent<PlayerParry>();
        visual = GetComponent<PlayerVisual>();
        health = GetComponent<PlayerHealth>();
        sfx = GetComponent<PlayerAudio>();

        cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();

        ArrowCount = MaxArrows;
    }

    void Update()
    {
        // ธนูที่ถูกทำลายเอง (เช่น บอสตายตอนธนูปักอยู่) นับเป็นหาย แล้วรอคืนให้
        for (int i = field.Count - 1; i >= 0; i--)
        {
            if (field[i] == null)
            {
                field.RemoveAt(i);
                BeginLose();
            }
        }

        // คืนธนูที่หายไปทีละดอก
        if (lost > 0 && !shooting && Time.time >= restockTime)
        {
            lost--;
            ArrowCount = Mathf.Min(MaxArrows, ArrowCount + 1);
            restockTime = Time.time + arrowRestockDelay;
        }

        if (parry == null) return;

        GuardState gs = parry.CurrentState;

        // ปล่อยปุ่มการ์ด (ปุ่มที่ตั้งใน OPTION หรือปุ่ม GUARD บนมือถือ) ตอนที่ยังตั้งการ์ดอยู่ → ยิง
        if (Controls.GuardUp && prevGuard != GuardState.None)
            TryFire();

        // เริ่มตั้งการ์ดตอนมีธนู = ง้างธนู (เล่นเสียงง้าง)
        if (gs != GuardState.None && prevGuard == GuardState.None && HasArrow && !shooting && arrowSprite != null)
        {
            if (sfx != null) sfx.PlayBowDraw();
        }

        prevGuard = gs;
    }

    void BeginLose()
    {
        lost++;
        if (lost == 1) restockTime = Time.time + arrowRestockDelay;
    }

    void TryFire()
    {
        if (!HasArrow || shooting) return;
        if (health != null && health.IsDead) return;

        if (arrowSprite == null)
        {
            if (!warnedNoSprite)
            {
                Debug.LogWarning("PlayerArrow: ยังไม่ได้ลากภาพธนูใส่ช่อง Arrow Sprite");
                warnedNoSprite = true;
            }
            return;
        }

        ArrowCount--;
        StartCoroutine(ShootRoutine());
    }

    IEnumerator ShootRoutine()
    {
        shooting = true;

        if (visual != null) visual.PlayShoot(shootDuration);

        yield return new WaitForSeconds(shootDuration * releaseTime);

        if (health != null && health.IsDead)
        {
            ArrowCount = Mathf.Min(MaxArrows, ArrowCount + 1);
        }
        else
        {
            Vector2 dir = PlayerAim.Direction(transform.position, cam);
            SpawnArrow(dir);
            if (sfx != null) sfx.PlayBowShoot();
        }

        shooting = false;
    }

    float ComputeScale()
    {
        if (!useAutoSize) return arrowScale;

        Vector2 size = arrowSprite.bounds.size;
        float longest = Mathf.Max(size.x, size.y);
        if (longest < 0.0001f) return arrowScale;

        return arrowLength / longest * arrowScale;
    }

    void SpawnArrow(Vector2 dir)
    {
        Vector2 origin = transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        var go = new GameObject("PlayerArrow");
        go.transform.position = origin + dir * spawnOffset;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle + spriteAngleOffset);
        go.transform.localScale = Vector3.one * ComputeScale();

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = arrowSprite;
        sr.sortingOrder = sortingOrder;

        var proj = go.AddComponent<ArrowProjectile>();
        proj.Init(this, dir);
        field.Add(proj);

        Color ac = new Color(0.8f, 1f, 1f, 0.9f);
        FXPlus.Flare(origin + dir * spawnOffset, ac, 1.4f, 0.15f);
        FXPlus.Slash(origin + dir * (spawnOffset + 0.3f), dir, ac, 1.6f, 0.16f);
    }

    // ธนูดอกนี้ถูกเก็บคืน
    public void OnArrowCollected(ArrowProjectile p)
    {
        field.Remove(p);
        ArrowCount = Mathf.Min(MaxArrows, ArrowCount + 1);
    }

    // บอสฟันธนูดอกนี้ทิ้ง: หาย คืนให้ทีหลัง
    public void OnArrowDestroyed(ArrowProjectile p)
    {
        field.Remove(p);
        BeginLose();

        FX.Popup(transform.position + Vector3.up * 1.4f, "ARROW LOST", new Color(1f, 0.6f, 0.4f));
    }

    public void NotifySlash(Vector2 center, float radius, Collider2D[] hits)
    {
        var copy = field.ToArray();      // การเก็บคืนแก้ลิสต์ระหว่างวน จึงต้องวนสำเนา
        foreach (var a in copy)
            if (a != null) a.TryCollectBySlash(center, radius, hits);
    }

    void OnGUI()
    {
        if (!showHud || arrowSprite == null) return;
        if (Event.current.type != EventType.Repaint) return;

        float s = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float W = Screen.width / s;
        float H = Screen.height / s;

        float box = 44f;
        float y = H - hudBottomMargin - hudBarHeight / 2f - box / 2f;

        Texture2D tex = arrowSprite.texture;
        Rect r = arrowSprite.textureRect;
        Rect uv = new Rect(r.x / tex.width, r.y / tex.height, r.width / tex.width, r.height / tex.height);
        float aspect = r.width / Mathf.Max(1f, r.height);
        float w = aspect >= 1f ? box : box * aspect;
        float h = aspect >= 1f ? box / aspect : box;

        for (int i = 0; i < MaxArrows; i++)
        {
            float x = (W + hudBarWidth) / 2f + 22f + i * (box + 10f);

            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(new Rect(x - 4, y - 4, box + 8, box + 8), Texture2D.whiteTexture);

            Rect dest = new Rect(x + (box - w) / 2f, y + (box - h) / 2f, w, h);
            GUI.color = i < ArrowCount ? Color.white : new Color(1f, 1f, 1f, 0.25f);
            GUI.DrawTextureWithTexCoords(dest, tex, uv);
        }
    
        GUI.color = Color.white;
        GUI.matrix = Matrix4x4.identity;
    }
}