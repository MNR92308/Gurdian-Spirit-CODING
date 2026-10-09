using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// ไม่ต้องตั้งค่าอะไรในฉาก: พอเปิดฉากที่มี BossAI ระบบสร้างตัวนี้ให้เอง
// (ถ้าอยากปรับค่า ให้วางสคริปต์นี้บน GameObject ว่างในฉาก Arena เอง)
//
// สิ่งที่ทำ:
//  1) สร้างห้อง Lobby สี่เหลี่ยม + ทางเดิน อยู่ทางซ้ายของห้องบอส
//  2) ย้ายผู้เล่นมาเกิดที่ Lobby กล้องตามผู้เล่น
//  3) บอสหลับอยู่ (BossAI ถูกปิดไว้) จนผู้เล่นเดินเข้าห้องบอสเอง
//  4) พอเข้าห้องบอส: ประตูปิด กล้องล็อคที่ห้องบอส บอสตื่น
//  5) คบไฟกะพริบในโลก + เงามืดขอบจอ (Vignette) ติดหน้าจอตลอด ไม่ว่ากล้องจะเลื่อนไปไหน
//     ตอนประตูปิด เงาขอบจอและไฟห้องบอสเปลี่ยนเป็นโทนแดง
public class ArenaIntro : MonoBehaviour
{
    [Header("Boss room (ศูนย์กลาง = ตำแหน่งบอสตอนเริ่ม)")]
    public Vector2 bossRoomSize = new Vector2(20f, 12f);

    [Header("Lobby")]
    public float lobbySize = 10f;
    public float corridorLength = 6f;
    public float corridorWidth = 3.5f;
    public float wallThickness = 1f;

    [Header("Trigger / Door")]
    public float lockTriggerDepth = 2f;      // เดินเข้าห้องบอสลึกกี่หน่วยถึงล็อค
    public float doorCloseTime = 0.5f;
    public float bossWakeDelay = 0.8f;

    [Header("Camera")]
    public float followSmooth = 6f;
    public float lockSmooth = 3f;
    public float lockMargin = 0.5f;

    [Header("Colors (ปรับให้สว่างขึ้นได้ที่นี่)")]
    public Color floorColor = new Color(0.36f, 0.34f, 0.4f, 1f);
    public Color floorAltColor = new Color(0.31f, 0.29f, 0.35f, 1f);
    public Color corridorFloorColor = new Color(0.3f, 0.28f, 0.33f, 1f);
    public Color wallColor = new Color(0.55f, 0.5f, 0.6f, 1f);
    public Color doorColor = new Color(0.6f, 0.15f, 0.12f, 1f);

    [Header("Vignette (เงามืดติดขอบจอ)")]
    public bool useVignette = true;
    [Range(0f, 1f)] public float vignetteStrength = 0.8f;    // ความมืดสุดที่ขอบจอ
    [Range(0f, 1f)] public float vignetteInner = 0.3f;       // เริ่มมืดที่ระยะเท่าไรจากกลางจอ (น้อย = มืดเข้ามาลึก)
    public float vignettePulse = 0.04f;                      // ความกะพริบเบาๆ ให้รู้สึกมีชีวิต
    public Color vignetteTint = new Color(0.02f, 0.01f, 0.05f, 1f);
    public Color vignetteSealedTint = new Color(0.22f, 0.0f, 0.0f, 1f);

    [Header("Torches (คบไฟในโลก)")]
    public bool useTorches = true;
    public Color torchColor = new Color(1f, 0.62f, 0.25f, 1f);
    public Color sealedTorchColor = new Color(1f, 0.18f, 0.1f, 1f);
    public float torchRadius = 5.5f;
    [Range(0f, 1f)] public float torchIntensity = 0.34f;
    [Range(0f, 1f)] public float ambientPool = 0.14f;      // แสงนวลกลางห้อง

    class Torch
    {
        public SpriteRenderer glow, flame;
        public float seed, radius;
        public bool bossRoom;
    }

    Transform player;
    Rigidbody2D playerRb;
    BossAI boss;
    Collider2D bossCol;
    Camera cam;
    Sprite white;
    Sprite soft;
    Transform root;
    Texture2D vignetteTex;

    Vector2 bossCenter;
    float westX;
    float followSize;
    bool locked;
    float sealT;

    Transform door;
    BoxCollider2D doorCol;
    SpriteRenderer doorSr;
    float gapH;

    readonly List<Torch> torches = new List<Torch>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (FindAnyObjectByType<ArenaIntro>() != null) return;
        if (FindAnyObjectByType<BossAI>() == null) return;
        new GameObject("ArenaIntro").AddComponent<ArenaIntro>();
    }

    void Awake()
    {
        boss = FindAnyObjectByType<BossAI>();
        var ph = FindAnyObjectByType<PlayerHealth>();
        cam = Camera.main;
        if (boss == null || ph == null) { enabled = false; return; }

        player = ph.transform;
        playerRb = player.GetComponent<Rigidbody2D>();

        // บอสหลับ: ปิดสคริปต์และคอลลิเดอร์จนกว่าผู้เล่นเข้าห้อง
        bossCenter = boss.transform.position;
        bossCol = boss.GetComponent<Collider2D>();
        if (bossCol != null) bossCol.enabled = false;
        boss.enabled = false;

        gapH = corridorWidth;
        BuildLevel();
        vignetteTex = MakeVignetteTexture(256);

        // ผู้เล่นเกิดกลาง Lobby
        float lobbyEastX = westX - corridorLength;
        Vector2 lobbyCenter = new Vector2(lobbyEastX - lobbySize / 2f, bossCenter.y);
        player.position = lobbyCenter;
        if (playerRb != null) { playerRb.position = lobbyCenter; playerRb.linearVelocity = Vector2.zero; }

        if (cam != null)
        {
            followSize = cam.orthographicSize;
            Vector3 p = cam.transform.position;
            cam.transform.position = new Vector3(lobbyCenter.x, lobbyCenter.y, p.z);
        }

        gameObject.AddComponent<BossEvolutionHUD>();
    }

    void OnDestroy()
    {
        if (vignetteTex != null) Destroy(vignetteTex);
    }

    // ================= เงามืดขอบจอ (Vignette) =================
    // texture ไล่ระดับจากกลางโปร่งใส → ขอบทึบ วาดเต็มหน้าจอใน OnGUI จึงติดจอเสมอ
    // ไม่เกี่ยวกับตำแหน่งกล้องหรือโลก
    static Texture2D MakeVignetteTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        var px = new Color32[size * size];
        float c = (size - 1) / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // ระยะแบบวงรี: 0 = กลางจอ, 1 = กลางขอบ, ~1.41 = มุมจอ
                float dx = (x - c) / c;
                float dy = (y - c) / c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(d / 1.41f) * 255f);
                px[y * size + x] = new Color32(255, 255, 255, a);   // เก็บ "ระยะ" ไว้ใน alpha แล้วค่อยคำนวณความเข้มตอนวาด
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    // สร้าง texture ที่คำนวณความเข้มตามค่าที่ตั้ง (สร้างใหม่เมื่อค่าเปลี่ยน)
    Texture2D shadedTex;
    float shadedInner = -1f;

    Texture2D GetShadedTexture()
    {
        if (shadedTex != null && Mathf.Approximately(shadedInner, vignetteInner)) return shadedTex;

        int size = vignetteTex.width;
        if (shadedTex == null)
        {
            shadedTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            shadedTex.wrapMode = TextureWrapMode.Clamp;
            shadedTex.filterMode = FilterMode.Bilinear;
        }

        Color32[] src = vignetteTex.GetPixels32();
        var dst = new Color32[src.Length];
        float inner = Mathf.Clamp(vignetteInner, 0f, 0.95f);

        for (int i = 0; i < src.Length; i++)
        {
            float d = src[i].a / 255f;                         // 0..1 (1 = มุมจอ)
            float t = Mathf.Clamp01((d - inner) / Mathf.Max(0.01f, 1f - inner));
            t = t * t * (3f - 2f * t);                         // smoothstep ให้ขอบนุ่ม
            dst[i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(t * 255f));
        }

        shadedTex.SetPixels32(dst);
        shadedTex.Apply();
        shadedInner = vignetteInner;
        return shadedTex;
    }

    void DrawVignette()
    {
        if (!useVignette || vignetteTex == null) return;
        if (Event.current.type != EventType.Repaint) return;

        Color tint = Color.Lerp(vignetteTint, vignetteSealedTint, sealT);
        float pulse = 1f + vignettePulse * Mathf.Sin(Time.time * 2.2f);
        tint.a = Mathf.Clamp01(vignetteStrength * pulse * Mathf.Lerp(1f, 1.1f, sealT));

        Color old = GUI.color;
        GUI.color = tint;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), GetShadedTexture(), ScaleMode.StretchToFill, true);
        GUI.color = old;
    }

    // ================= สร้างห้อง =================
    void BuildLevel()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        white = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        soft = SpriteTools.SoftCircle();

        root = new GameObject("LobbyLevel").transform;

        float t = wallThickness;
        float hw = bossRoomSize.x / 2f;
        westX = bossCenter.x - hw;
        float eastLobbyX = westX - corridorLength;
        Vector2 lobbyCenter = new Vector2(eastLobbyX - lobbySize / 2f, bossCenter.y);
        float midX = (westX + eastLobbyX) / 2f;

        // พื้น: Lobby ปูกระเบื้องสลับสี ทางเดินพื้นเรียบ
        FloorTiles(lobbyCenter, new Vector2(lobbySize, lobbySize), 2f);
        Block("CorridorFloor", new Vector2(midX, bossCenter.y), new Vector2(corridorLength + 0.02f, corridorWidth), corridorFloorColor, -30, false);

        // กำแพง Lobby (เจาะช่องด้านตะวันออกต่อทางเดิน)
        BuildRoomWalls("Lobby", lobbyCenter, new Vector2(lobbySize, lobbySize), false, true);
        // กำแพงห้องบอส (เจาะช่องด้านตะวันตก)
        BuildRoomWalls("BossRoom", bossCenter, bossRoomSize, true, false);

        // กำแพงทางเดิน
        float cy = corridorWidth / 2f + t / 2f;
        Block("CorridorTop", new Vector2(midX, bossCenter.y + cy), new Vector2(corridorLength, t), wallColor, -5, true);
        Block("CorridorBottom", new Vector2(midX, bossCenter.y - cy), new Vector2(corridorLength, t), wallColor, -5, true);

        // ประตู (เริ่มเปิด)
        var d = Block("Door", new Vector2(westX - t / 2f, bossCenter.y), new Vector2(t, gapH), doorColor, 5, true);
        door = d.transform;
        doorSr = d.GetComponent<SpriteRenderer>();
        doorCol = d.GetComponent<BoxCollider2D>();
        doorCol.enabled = false;
        doorSr.enabled = false;

        if (useTorches) BuildTorches(lobbyCenter, midX, eastLobbyX);
    }

    void FloorTiles(Vector2 center, Vector2 size, float tile)
    {
        int nx = Mathf.Max(1, Mathf.RoundToInt(size.x / tile));
        int ny = Mathf.Max(1, Mathf.RoundToInt(size.y / tile));
        float tw = size.x / nx, th = size.y / ny;
        Vector2 origin = center - size / 2f;

        for (int ix = 0; ix < nx; ix++)
            for (int iy = 0; iy < ny; iy++)
            {
                Color c = (ix + iy) % 2 == 0 ? floorColor : floorAltColor;
                Vector2 p = origin + new Vector2((ix + 0.5f) * tw, (iy + 0.5f) * th);
                Block("Tile", p, new Vector2(tw, th), c, -30, false);
            }
    }

    // ================= คบไฟ =================
    void BuildTorches(Vector2 lobbyCenter, float midX, float eastLobbyX)
    {
        // แสงนวลกลางห้อง
        Glow("LobbyPool", lobbyCenter, lobbySize * 0.95f, new Color(torchColor.r, torchColor.g, torchColor.b, ambientPool), -4);
        Glow("CorridorPool", new Vector2(midX, bossCenter.y), corridorLength * 0.8f, new Color(torchColor.r, torchColor.g, torchColor.b, ambientPool * 0.8f), -4);
        Glow("BossPool", bossCenter, Mathf.Max(bossRoomSize.x, bossRoomSize.y) * 0.8f, new Color(torchColor.r, torchColor.g, torchColor.b, ambientPool * 0.8f), -4);

        // คบไฟ Lobby 4 มุม
        float lx = lobbySize / 2f - 1.0f;
        AddTorch(lobbyCenter + new Vector2(-lx, lx), torchRadius, false);
        AddTorch(lobbyCenter + new Vector2(lx, lx), torchRadius, false);
        AddTorch(lobbyCenter + new Vector2(-lx, -lx), torchRadius, false);
        AddTorch(lobbyCenter + new Vector2(lx, -lx), torchRadius, false);

        // คบไฟทางเดิน
        float cyT = corridorWidth / 2f - 0.35f;
        AddTorch(new Vector2(eastLobbyX + 1.5f, bossCenter.y + cyT), torchRadius * 0.7f, false);
        AddTorch(new Vector2(westX - 1.5f, bossCenter.y - cyT), torchRadius * 0.7f, false);

        // คบไฟห้องบอส (ไฟกลายเป็นสีแดงตอนประตูปิด)
        float bx = bossRoomSize.x / 2f - 1.3f;
        float by = bossRoomSize.y / 2f - 1.3f;
        float br = torchRadius * 1.15f;
        AddTorch(bossCenter + new Vector2(-bx, by), br, true);
        AddTorch(bossCenter + new Vector2(bx, by), br, true);
        AddTorch(bossCenter + new Vector2(-bx, -by), br, true);
        AddTorch(bossCenter + new Vector2(bx, -by), br, true);
        AddTorch(bossCenter + new Vector2(0f, by), br, true);
        AddTorch(bossCenter + new Vector2(0f, -by), br, true);
    }

    SpriteRenderer Glow(string name, Vector2 pos, float radius, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = soft;
        sr.color = color;
        sr.sortingOrder = order;
        SetGlowSize(sr, radius);
        return sr;
    }

    void SetGlowSize(SpriteRenderer sr, float radius)
    {
        float sz = soft != null ? Mathf.Max(0.0001f, soft.bounds.size.x) : 1f;
        float s = radius * 2f / sz;
        sr.transform.localScale = new Vector3(s, s, 1f);
    }

    void AddTorch(Vector2 pos, float radius, bool bossRoom)
    {
        Block("Brazier", pos, new Vector2(0.55f, 0.55f), new Color(0.12f, 0.1f, 0.12f, 1f), -6, false);

        var t = new Torch();
        t.seed = Random.value * 100f;
        t.radius = radius;
        t.bossRoom = bossRoom;
        t.glow = Glow("TorchGlow", pos, radius, new Color(torchColor.r, torchColor.g, torchColor.b, torchIntensity), -4);
        t.flame = Glow("TorchFlame", pos, 0.4f, new Color(1f, 0.85f, 0.45f, 0.95f), -3);
        torches.Add(t);
    }

    void UpdateLights()
    {
        sealT = Mathf.MoveTowards(sealT, locked ? 1f : 0f, Time.deltaTime * 1.5f);

        foreach (var t in torches)
        {
            float n = Mathf.PerlinNoise(t.seed, Time.time * 5f);
            float k = 0.75f + 0.5f * n;

            Color baseC = t.bossRoom ? Color.Lerp(torchColor, sealedTorchColor, sealT) : torchColor;
            Color g = baseC;
            g.a = torchIntensity * k;
            t.glow.color = g;
            SetGlowSize(t.glow, t.radius * (0.94f + 0.12f * n));

            Color f = t.bossRoom ? Color.Lerp(new Color(1f, 0.85f, 0.45f), new Color(1f, 0.4f, 0.25f), sealT) : new Color(1f, 0.85f, 0.45f);
            f.a = 0.95f;
            t.flame.color = f;
            SetGlowSize(t.flame, 0.22f * (0.8f + 0.4f * n));
        }
    }

    // ================= กำแพง =================
    void BuildRoomWalls(string name, Vector2 c, Vector2 size, bool gapWest, bool gapEast)
    {
        float t = wallThickness;
        Block(name + "Top", new Vector2(c.x, c.y + size.y / 2f + t / 2f), new Vector2(size.x + 2f * t, t), wallColor, -5, true);
        Block(name + "Bottom", new Vector2(c.x, c.y - size.y / 2f - t / 2f), new Vector2(size.x + 2f * t, t), wallColor, -5, true);
        SideWall(name + "West", c.x - size.x / 2f - t / 2f, c.y, size.y, gapWest);
        SideWall(name + "East", c.x + size.x / 2f + t / 2f, c.y, size.y, gapEast);
    }

    void SideWall(string name, float x, float cy, float height, bool gap)
    {
        float t = wallThickness;
        if (!gap)
        {
            Block(name, new Vector2(x, cy), new Vector2(t, height), wallColor, -5, true);
            return;
        }
        float part = Mathf.Max(0.1f, (height - gapH) / 2f);
        Block(name + "Up", new Vector2(x, cy + gapH / 2f + part / 2f), new Vector2(t, part), wallColor, -5, true);
        Block(name + "Down", new Vector2(x, cy - gapH / 2f - part / 2f), new Vector2(t, part), wallColor, -5, true);
    }

    GameObject Block(string name, Vector2 center, Vector2 size, Color color, int order, bool collider)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(center.x, center.y, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = white;
        sr.color = color;
        sr.sortingOrder = order;

        if (collider)
        {
            var bc = go.AddComponent<BoxCollider2D>();
            bc.size = Vector2.one;      // ขนาดจริง = localScale
        }
        return go;
    }

    // ================= Loop =================
    void Update()
    {
        UpdateLights();

        if (locked || player == null) return;

        bool insideX = player.position.x > westX + lockTriggerDepth;
        bool insideY = Mathf.Abs(player.position.y - bossCenter.y) < bossRoomSize.y / 2f;
        if (insideX && insideY) StartCoroutine(LockRoutine());
    }

    IEnumerator LockRoutine()
    {
        locked = true;

        // ประตูปิด
        doorSr.enabled = true;
        doorCol.enabled = true;
        Vector3 full = new Vector3(wallThickness, gapH, 1f);
        float t = 0f;
        while (t < doorCloseTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / doorCloseTime);
            door.localScale = new Vector3(full.x, full.y * k, 1f);
            yield return null;
        }
        door.localScale = full;

        FX.Shake(0.3f, 0.25f);
        FX.Ring(door.position, new Color(1f, 0.3f, 0.2f, 0.9f), 0.3f, 2.5f, 0.4f);
        FX.Popup(door.position + Vector3.up * 2f, "SEALED", new Color(1f, 0.4f, 0.3f));

        // บอสตื่น
        yield return new WaitForSeconds(bossWakeDelay);
        if (boss != null)
        {
            if (bossCol != null) bossCol.enabled = true;
            boss.enabled = true;
        }
    }

    void LateUpdate()
    {
        if (cam == null || player == null) return;

        Vector3 cp = cam.transform.position;
        Vector2 target;
        float size, k;

        if (locked)
        {
            target = bossCenter;
            size = Mathf.Max(bossRoomSize.y / 2f, bossRoomSize.x / 2f / Mathf.Max(0.1f, cam.aspect)) + lockMargin;
            k = lockSmooth;
        }
        else
        {
            target = player.position;
            size = followSize;
            k = followSmooth;
        }

        float f = 1f - Mathf.Exp(-k * Time.deltaTime);
        cp.x = Mathf.Lerp(cp.x, target.x, f);
        cp.y = Mathf.Lerp(cp.y, target.y, f);
        cam.transform.position = cp;
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, size, f);
    }

    void OnGUI()
    {
        // depth สูง = วาดก่อน (อยู่หลัง) GUI อื่น เงามืดจึงไม่บังแถบเลือดและกล่องอธิบายบอส
        GUI.depth = 50;
        DrawVignette();

        if (locked) return;
        if (Event.current.type != EventType.Repaint) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        style.normal.textColor = new Color(1f, 1f, 1f, 0.6f + 0.4f * Mathf.Sin(Time.time * 3f));
        GUI.Label(new Rect(0, Screen.height - 90, Screen.width, 50), "Walk to the right to face the boss  >>>", style);
    }
}