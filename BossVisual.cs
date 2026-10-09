using UnityEngine;

public class BossVisual : MonoBehaviour
{
    [Header("Parts")]
    public SpriteRenderer body;          // = Sprite Renderer บนตัว Boss เอง (ปล่อยว่างได้ ระบบหาให้)

    [Header("Frames")]
    public Sprite[] idleFrames;
    public Sprite[] walkFrames;
    public Sprite[] slashFrames;
    public Sprite[] chargeSlashFrames;
    public Sprite[] hurtFrames;
    public Sprite[] deathFrames;         // ท่าตาย (ตอนฟื้นระบบเล่นย้อนกลับให้เอง)

    [Header("Frames ท่าใหม่ (ไม่มีภาพก็ได้ ระบบใช้ภาพอื่นแทน)")]
    public Sprite[] slamFrames;
    public Sprite[] shootFrames;
    public Sprite[] cloneFrames;
    public Sprite[] staggerFrames;

    [Header("Settings")]
    public bool spritesFaceRight = true;
    public float idleFps = 5f;
    public float walkFps = 8f;
    public float hurtDuration = 0.25f;

    [Header("Glow (แสงรอบตัว)")]
    public SpriteRenderer glow;          // ปล่อยว่างไว้ ระบบสร้างให้เอง
    public Color glowColor = new Color(1f, 0.1f, 0.05f, 0.9f);   // สีแดง = ท่าทะลุทั้งแดชและ Parry
    public float glowSize = 1.9f;
    public float glowPulseSpeed = 9f;

    static readonly Color CyanC = new Color(0.3f, 0.9f, 1f, 0.9f);
    static readonly Color GoldC = new Color(1f, 0.8f, 0.2f, 0.9f);
    static readonly Color PurpleC = new Color(0.75f, 0.35f, 1f, 0.9f);
    static readonly Color OrangeC = new Color(1f, 0.55f, 0.1f, 0.9f);

    BossAI boss;
    BossState lastState = BossState.Idle;
    int lastHealth = -1;

    float moveStart, moveDuration;
    float stunStart, deflectStart, deathStart, reviveStart;
    float hurtStart = -10f;
    float flashUntil;
    float nextFx;
    bool facingLeft;

    SpriteRenderer cloneOverlay;
    Transform telegraphRoot;
    SpriteRenderer telegraphDisc, telegraphFill, telegraphRing;

    void Awake()
    {
        boss = GetComponent<BossAI>();
        if (body == null) body = GetComponent<SpriteRenderer>();
        if (glow == null) CreateGlow();
    }

    static bool Has(Sprite[] f) { return f != null && f.Length > 0; }

    static Color CounterColor(CounterKind k)
    {
        switch (k)
        {
            case CounterKind.ParryCounter: return CyanC;
            case CounterKind.HyperTrade: return GoldC;
            default: return PurpleC;
        }
    }

    void Update()
    {
        if (body == null || boss == null) return;

        if (lastHealth < 0) lastHealth = boss.CurrentHealth;
        if (boss.CurrentHealth < lastHealth && boss.IsTargetable)
        {
            flashUntil = Time.time + 0.08f;
            bool busy = boss.State == BossState.Windup || boss.State == BossState.Attack
                     || boss.State == BossState.Staggered || boss.State == BossState.Deflect
                     || boss.State == BossState.CounterWarn || boss.State == BossState.CounterStance;
            if (!busy) hurtStart = Time.time;
        }
        lastHealth = boss.CurrentHealth;

        if (boss.State != lastState)
        {
            switch (boss.State)
            {
                case BossState.Windup:
                    moveStart = Time.time;
                    moveDuration = Mathf.Max(0.05f, boss.CurrentWindup + boss.CurrentActive);
                    break;
                case BossState.Stunned: stunStart = Time.time; break;
                case BossState.Deflect: deflectStart = Time.time; break;
                case BossState.Dead: deathStart = Time.time; break;
                case BossState.Reviving: reviveStart = Time.time; break;
                case BossState.CounterWarn:
                    FX.Popup(transform.position + Vector3.up * 2.2f, "!", CounterColor(boss.ActiveCounter));
                    break;
            }
            lastState = boss.State;
        }

        if (boss.State != BossState.Attack && boss.IsTargetable) FaceTarget();

        BossState st = boss.State;

        // ===== เลือกอนิเมชัน =====
        if (st == BossState.Dead)
        {
            ShowByProgress(deathFrames, Time.time - deathStart, boss.deathAnimTime);
        }
        else if (st == BossState.Reviving)
        {
            ShowReversed(deathFrames, Time.time - reviveStart, boss.reviveAnimTime);
        }
        else if (st == BossState.Staggered)
        {
            ShowLoop(Has(staggerFrames) ? staggerFrames : hurtFrames, 6f);
        }
        else if (st == BossState.Stunned)
        {
            ShowByProgress(hurtFrames, Time.time - stunStart, Mathf.Max(0.05f, boss.stunTime));
        }
        else if (st == BossState.Deflect)
        {
            ShowByProgress(slashFrames, Time.time - deflectStart, Mathf.Max(0.05f, boss.deflectDuration));
        }
        else if (st == BossState.CounterStance)
        {
            if (Has(slashFrames)) body.sprite = slashFrames[0];      // ท่าตั้งรับ = เฟรมแรกของท่าฟัน
            else ShowLoop(idleFrames, idleFps);
        }
        else if (st == BossState.CounterDodge)
        {
            ShowLoop(walkFrames, walkFps * 3f);
        }
        else if (st == BossState.CounterWarn)
        {
            ShowLoop(idleFrames, idleFps);
        }
        else if (Time.time - hurtStart < hurtDuration)
        {
            ShowByProgress(hurtFrames, Time.time - hurtStart, hurtDuration);
        }
        else if (st == BossState.Windup || st == BossState.Attack)
        {
            Sprite[] f = FramesFor(boss.CurrentMove);
            if (Has(f)) ShowByProgress(f, Time.time - moveStart, moveDuration);
            else ShowLoop(walkFrames, walkFps * 1.5f);
        }
        else if (st == BossState.Chase)
        {
            ShowLoop(walkFrames, walkFps);
        }
        else
        {
            ShowLoop(idleFrames, idleFps);
        }

        UpdateColor(st);
        UpdateGlow();
        UpdateTelegraph();
        UpdateCloneOverlay();
        UpdateEffects(st);
    }

    Sprite[] FramesFor(BossMove m)
    {
        switch (m)
        {
            case BossMove.Slash:
            case BossMove.Riposte:
            case BossMove.BackStab:
            case BossMove.Interrupt:
                return slashFrames;
            case BossMove.ChargeSlash:
            case BossMove.GuardBreak:
                return chargeSlashFrames;
            case BossMove.GroundSlam:
            case BossMove.HyperTrade:
            case BossMove.ZoneBlast:
                return Has(slamFrames) ? slamFrames : chargeSlashFrames;
            case BossMove.Shoot:
                return Has(shootFrames) ? shootFrames : slashFrames;
            case BossMove.Clone:
                return Has(cloneFrames) ? cloneFrames : chargeSlashFrames;
            default:
                return null;
        }
    }

    void ShowLoop(Sprite[] frames, float fps)
    {
        if (!Has(frames)) return;
        int i = Mathf.FloorToInt(Time.time * fps) % frames.Length;
        body.sprite = frames[i];
    }

    void ShowByProgress(Sprite[] frames, float elapsed, float duration)
    {
        if (!Has(frames)) return;
        int i = Mathf.Clamp(Mathf.FloorToInt(elapsed / Mathf.Max(0.05f, duration) * frames.Length), 0, frames.Length - 1);
        body.sprite = frames[i];
    }

    void ShowReversed(Sprite[] frames, float elapsed, float duration)
    {
        if (!Has(frames)) return;
        int i = Mathf.Clamp(Mathf.FloorToInt(elapsed / Mathf.Max(0.05f, duration) * frames.Length), 0, frames.Length - 1);
        body.sprite = frames[frames.Length - 1 - i];
    }

    void FaceTarget()
    {
        if (boss.player == null) return;
        float dx = boss.player.position.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.05f) facingLeft = dx < 0f;
        body.flipX = spritesFaceRight ? facingLeft : !facingLeft;
    }

    void UpdateColor(BossState st)
    {
        float heat = Mathf.Clamp01(boss.Phase * 0.35f + boss.Evolution * 0.3f);
        Color c = Color.Lerp(Color.white, new Color(1f, 0.7f, 0.7f), heat);

        if (Time.time < flashUntil)
            c = new Color(1f, 0.45f, 0.45f);
        else if (boss.IsRoaring)
            c = (Mathf.FloorToInt(Time.time * 10f) % 2 == 0) ? Color.white : c;
        else if (st == BossState.Staggered)
            c = Color.Lerp(c, new Color(1f, 0.95f, 0.3f), 0.4f + 0.3f * Mathf.Sin(Time.time * 12f));
        else if (st == BossState.CounterWarn)
            c = Color.Lerp(c, CounterColor(boss.ActiveCounter), 0.5f + 0.3f * Mathf.Sin(Time.time * 30f));   // ตัวเปลี่ยนสี = สัญญาณเตือน
        else if (st == BossState.CounterStance)
            c = Color.Lerp(c, CyanC, 0.55f);
        else if (boss.HyperArmor)
            c = Color.Lerp(c, GoldC, 0.4f + 0.2f * Mathf.Sin(Time.time * 14f));
        else if (st == BossState.CounterDodge)
            c.a = 0.35f;

        if (boss.IsFinalDeath && Time.time - deathStart > boss.deathAnimTime)
            c.a = 1f - Mathf.Clamp01((Time.time - deathStart - boss.deathAnimTime) / 0.5f);

        body.color = c;
    }

    // แสงรอบตัว: สีบอกชนิดของท่า
    bool WantGlow(out Color c)
    {
        c = glowColor;
        BossState st = boss.State;

        if (st == BossState.CounterWarn) { c = CounterColor(boss.ActiveCounter); return true; }
        if (st == BossState.CounterStance) { c = CyanC; return true; }

        if (st == BossState.Windup || st == BossState.Attack)
        {
            switch (boss.CurrentMove)
            {
                case BossMove.ChargeSlash: c = glowColor; return true;        // แดง = ทะลุทั้งแดชและ Parry
                case BossMove.HyperTrade: c = GoldC; return true;
                case BossMove.GuardBreak: c = OrangeC; return true;
                case BossMove.BackStab: c = PurpleC; return st == BossState.Windup;
            }
        }
        return false;
    }

    void UpdateGlow()
    {
        if (glow == null) return;

        Color gc;
        bool show = boss.IsTargetable && WantGlow(out gc);
        glow.enabled = show;
        if (!show) return;

        WantGlow(out gc);

        float progress = Mathf.Clamp01((Time.time - moveStart) / moveDuration);
        float pulse = 0.65f + 0.35f * Mathf.Sin(Time.time * glowPulseSpeed * (1f + progress));

        Color c = gc;
        c.a = gc.a * pulse;
        glow.color = c;

        Bounds b = body.bounds;
        Vector3 center = b.center;
        center.z = body.transform.position.z;
        glow.transform.position = center;

        float target = Mathf.Max(b.size.x, b.size.y) * glowSize * Mathf.Lerp(0.9f, 1.15f, progress);
        float spriteSize = glow.sprite != null ? glow.sprite.bounds.size.x : 1f;
        float parentScale = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        float s = target / (Mathf.Max(0.0001f, spriteSize) * parentScale);
        glow.transform.localScale = new Vector3(s, s, 1f);

        glow.sortingLayerID = body.sortingLayerID;
        glow.sortingOrder = body.sortingOrder - 1;
    }

    void CreateGlow()
    {
        var go = new GameObject("BossGlow");
        go.transform.SetParent(transform, false);
        glow = go.AddComponent<SpriteRenderer>();
        glow.sprite = SpriteTools.SoftCircle();
        glow.color = glowColor;
        glow.enabled = false;
    }

    // วงเตือนบนพื้นตอนง้างท่าพื้นที่ (ทุบพื้น / Hyper-Armor / Guard Break)
    void UpdateTelegraph()
    {
        float radius = boss.TelegraphRadius;

        if (telegraphRoot == null)
        {
            if (radius <= 0f) return;
            CreateTelegraph();
        }

        bool show = radius > 0f && boss.IsTargetable;
        telegraphDisc.enabled = telegraphFill.enabled = telegraphRing.enabled = show;
        if (!show) return;

        float progress = Mathf.Clamp01((Time.time - moveStart) / Mathf.Max(0.05f, boss.CurrentWindup));

        Color tc = boss.CurrentMove == BossMove.HyperTrade ? new Color(1f, 0.8f, 0.2f)
                 : boss.CurrentMove == BossMove.GuardBreak ? new Color(1f, 0.55f, 0.1f)
                 : new Color(1f, 0.2f, 0.1f);

        float parentScale = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        telegraphRoot.localScale = Vector3.one * (radius * 2f / parentScale);
        telegraphFill.transform.localScale = Vector3.one * progress;

        telegraphDisc.color = new Color(tc.r, tc.g, tc.b, 0.12f);
        telegraphFill.color = new Color(tc.r, tc.g, tc.b, 0.18f + 0.22f * progress);
        telegraphRing.color = new Color(tc.r, tc.g, tc.b, 0.9f);

        int order = body.sortingOrder;
        telegraphDisc.sortingOrder = order - 3;
        telegraphFill.sortingOrder = order - 2;
        telegraphRing.sortingOrder = order - 1;
    }

    void CreateTelegraph()
    {
        var root = new GameObject("Telegraph");
        root.transform.SetParent(transform, false);
        telegraphRoot = root.transform;
        telegraphDisc = MakeSr(telegraphRoot, "Disc", SpriteTools.Disc());
        telegraphFill = MakeSr(telegraphRoot, "Fill", SpriteTools.Disc());
        telegraphRing = MakeSr(telegraphRoot, "Ring", SpriteTools.Ring());
    }

    static SpriteRenderer MakeSr(Transform parent, string name, Sprite sprite)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.enabled = false;
        return sr;
    }

    void UpdateCloneOverlay()
    {
        if (!boss.IsClone)
        {
            if (cloneOverlay != null) cloneOverlay.enabled = false;
            return;
        }

        if (cloneOverlay == null)
        {
            var go = new GameObject("CloneWhite");
            go.transform.SetParent(body.transform, false);
            cloneOverlay = go.AddComponent<SpriteRenderer>();
        }

        float maturity = Mathf.Clamp01(BossMemory.TotalDeaths / (float)Mathf.Max(1, boss.cloneMatureDeaths));
        float white = 1f - maturity;

        cloneOverlay.enabled = white > 0.02f && body.sprite != null;
        if (!cloneOverlay.enabled) return;

        cloneOverlay.sprite = SpriteTools.GetWhite(body.sprite);
        cloneOverlay.flipX = body.flipX;
        cloneOverlay.sortingLayerID = body.sortingLayerID;
        cloneOverlay.sortingOrder = body.sortingOrder + 1;

        Color c = Color.white;
        c.a = white * body.color.a;
        cloneOverlay.color = c;
    }

    void UpdateEffects(BossState st)
    {
        if (Time.time < nextFx || !boss.IsTargetable) return;

        Bounds b = body.bounds;

        // ประกายตา: ช่วงสัญญาณเตือนก่อนสวนกลับ
        if (st == BossState.CounterWarn)
        {
            Color cc = CounterColor(boss.ActiveCounter);
            FX.Burst(new Vector2(b.center.x, b.max.y - b.size.y * 0.2f), cc, 2, 3f, 0.25f, 0.18f);
            nextFx = Time.time + 0.05f;
            return;
        }

        Vector2 p = new Vector2(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y));

        if (st == BossState.Staggered)
        {
            FX.Burst(new Vector2(b.center.x + Random.Range(-0.6f, 0.6f), b.max.y), new Color(1f, 0.95f, 0.3f), 1, 1.5f, 0.6f, 0.2f);
            nextFx = Time.time + 0.15f;
        }
        else if (boss.Evolution >= 1 && !boss.IsClone)
        {
            FX.Burst(p, new Color(1f, 0.35f, 0.15f, 0.9f), 1, 1.2f, 0.7f, 0.12f);
            nextFx = Time.time + (boss.Evolution >= 2 ? 0.08f : 0.16f);
        }
    }
}