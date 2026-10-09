using UnityEngine;

[System.Serializable]
public class StageSkin
{
    [Header("Frames (เว้นว่าง = ใช้ของเดิม)")]
    public Sprite[] idle, walk, slash, chargeSlash, hurt, death, slam, shoot, clone, stagger;
    [Header("Look")]
    public Color tint = Color.white;
    public float scale = 1f;
    [Header("Sound")]
    public AudioClip attackSfx, hurtSfx, deathSfx, reviveSfx, roarSfx;
    [Range(0f, 1f)] public float volume = 0.8f;
}

// เงาตามตัว (ใช้กับบอส) ลบตัวเองเมื่อหมดเวลา
public class FxGhost : MonoBehaviour
{
    public SpriteRenderer sr;
    public float life = 0.3f;
    public Color start;
    float t;

    void Update()
    {
        t += Time.deltaTime;
        if (t >= life || sr == null) { Destroy(gameObject); return; }
        Color c = start;
        c.a = Mathf.Lerp(start.a, 0f, t / life);
        sr.color = c;
    }
}

public class BossStageSkin : MonoBehaviour
{
    public StageSkin[] stages = new StageSkin[4];

    BossAI boss;
    BossVisual vis;
    AudioSource sfx;
    StageSkin skin;
    BossState lastState = BossState.Idle;
    int lastHp = -1;
    float nextAura, nextLaser, nextGhost;
    bool wasRoar;

    static readonly Color[] FormCol =
    {
        new Color(0.45f, 0.7f, 1f), new Color(1f, 0.6f, 0.2f), new Color(1f, 0.2f, 0.25f)
    };

    void Awake()
    {
        boss = GetComponent<BossAI>();
        vis = GetComponent<BossVisual>();

        int stage = Mathf.Clamp(PlayerPrefs.GetInt("Stage", 1), 1, 4) - 1;
        if (stages != null && stage < stages.Length) skin = stages[stage];

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.spatialBlend = 0f;

        if (skin == null || vis == null) return;

        if (Has(skin.idle)) vis.idleFrames = skin.idle;
        if (Has(skin.walk)) vis.walkFrames = skin.walk;
        if (Has(skin.slash)) vis.slashFrames = skin.slash;
        if (Has(skin.chargeSlash)) vis.chargeSlashFrames = skin.chargeSlash;
        if (Has(skin.hurt)) vis.hurtFrames = skin.hurt;
        if (Has(skin.death)) vis.deathFrames = skin.death;
        if (Has(skin.slam)) vis.slamFrames = skin.slam;
        if (Has(skin.shoot)) vis.shootFrames = skin.shoot;
        if (Has(skin.clone)) vis.cloneFrames = skin.clone;
        if (Has(skin.stagger)) vis.staggerFrames = skin.stagger;
    }

    void Start()
    {
        if (skin != null && boss != null && !boss.IsClone && !Mathf.Approximately(skin.scale, 1f))
            transform.localScale *= skin.scale;

        if (skin != null && vis != null && skin.tint != Color.white)
            vis.glowColor = skin.tint;
    }

    static bool Has(Sprite[] f) { return f != null && f.Length > 0; }

    void Play(AudioClip c)
    {
        if (c != null && sfx != null) sfx.PlayOneShot(c, skin != null ? skin.volume : 0.8f);
    }

    int Count(int n)
    {
        float k = GameSettings.LowFx ? 0.6f : 1f;
        if (boss != null && boss.IsClone) k *= 0.5f;
        return Mathf.Max(1, Mathf.RoundToInt(n * k));
    }

    static Color MoveColor(BossMove m)
    {
        switch (m)
        {
            case BossMove.Slash: return new Color(0.55f, 0.85f, 1f);
            case BossMove.ChargeSlash: return new Color(1f, 0.12f, 0.08f);
            case BossMove.GroundSlam: return new Color(1f, 0.55f, 0.1f);
            case BossMove.Shoot: return new Color(1f, 0.85f, 0.2f);
            case BossMove.Clone: return new Color(0.75f, 1f, 1f);
            case BossMove.Riposte: return new Color(0.3f, 0.9f, 1f);
            case BossMove.HyperTrade: return new Color(1f, 0.8f, 0.15f);
            case BossMove.BackStab: return new Color(0.75f, 0.35f, 1f);
            case BossMove.GuardBreak: return new Color(1f, 0.5f, 0.05f);
            case BossMove.Interrupt: return new Color(1f, 1f, 1f);
            case BossMove.ZoneBlast: return new Color(0.9f, 0.3f, 1f);
            default: return Color.white;
        }
    }

    static Color A(Color c, float a) { return new Color(c.r, c.g, c.b, a); }

    Vector2 BodyCenter()
    {
        if (vis != null && vis.body != null) return vis.body.bounds.center;
        return transform.position;
    }

    Vector2 DirToPlayer(Vector2 from)
    {
        if (boss.player == null) return Vector2.right;
        Vector2 d = (Vector2)boss.player.position - from;
        return d.sqrMagnitude < 0.0001f ? Vector2.right : d.normalized;
    }

    void Shake(float d, float m) { if (!boss.IsClone) FX.Shake(d, m); }
    void Flash(Color c, float d) { if (!boss.IsClone) FX.Flash(c, d); }

    void Update()
    {
        if (boss == null) return;

        if (lastHp < 0) lastHp = boss.CurrentHealth;
        if (boss.CurrentHealth < lastHp && boss.IsTargetable) Play(skin != null ? skin.hurtSfx : null);
        lastHp = boss.CurrentHealth;

        BossState st = boss.State;
        Vector2 pos = BodyCenter();
        Vector2 dir = DirToPlayer(pos);
        Color mc = MoveColor(boss.CurrentMove);
        int evo = Mathf.Clamp(boss.Evolution, 0, 2);

        // ---- เปลี่ยนสถานะ: ง้างท่า / ปล่อยท่า / ตาย / ฟื้น ----
        if (st != lastState)
        {
            switch (st)
            {
                case BossState.Windup: OnWindup(pos, dir, mc); break;
                case BossState.Attack: Play(skin != null ? skin.attackSfx : null); OnAttack(pos, dir, mc); break;

                case BossState.Staggered:
                    FXPlus.Shockwave(pos, A(new Color(1f, 0.95f, 0.3f), 0.9f), 3.2f);
                    FXPlus.Flare(pos, new Color(1f, 0.95f, 0.4f, 0.8f), 3.5f, 0.35f);
                    break;

                case BossState.Dead:
                    Play(skin != null ? skin.deathSfx : null);
                    FXPlus.Pillar(pos, new Color(1f, 0.9f, 0.8f, 0.9f), 9f, 2.2f, 0.9f);
                    FXPlus.Flare(pos, new Color(1f, 0.6f, 0.3f, 0.9f), 6f, 0.6f);
                    FXPlus.Cracks(pos, new Color(1f, 0.5f, 0.2f, 0.9f), 5f, Count(10), 0.8f);
                    break;

                case BossState.Reviving:
                    Play(skin != null ? skin.reviveSfx : null);
                    FXPlus.Converge(pos, A(FormCol[evo], 0.95f), 6f, boss.reviveAnimTime, Count(30));
                    FXPlus.Pillar(pos, A(FormCol[evo], 0.8f), 10f, 2.6f, boss.reviveAnimTime);
                    break;
            }
            lastState = st;
        }

        // ---- คำราม (เข้าเฟสใหม่/ฟื้น) ----
        bool roar = boss.IsRoaring;
        if (roar && !wasRoar)
        {
            Play(skin != null ? skin.roarSfx : null);
            Color fc = FormCol[evo];
            FXPlus.Shockwave(pos, A(fc, 0.95f), 6f + boss.Phase * 1.5f);
            FXPlus.Pillar(pos, A(fc, 0.85f), 7f, 1.6f, 0.7f);
            FXPlus.Flare(pos, A(fc, 0.8f), 5f, 0.5f);
            Shake(0.35f, 0.25f);
            Flash(A(fc, 0.22f), 0.25f);
        }
        wasRoar = roar;

        if (!boss.IsTargetable) return;

        // ---- เลเซอร์เล็งตอนบอสง้างท่ายิง ----
        if (st == BossState.Windup && boss.CurrentMove == BossMove.Shoot && Time.time >= nextLaser)
        {
            nextLaser = Time.time + 0.04f;
            float pulse = 0.35f + 0.25f * Mathf.Sin(Time.time * 30f);
            FXPlus.Beam(pos, pos + dir * 9f, A(mc, pulse), 0.07f, 0.06f);
        }

        // ---- ประกายสีลอยรอบตัวตอนง้างท่า ----
        if (st == BossState.Windup && Time.time >= nextAura)
        {
            nextAura = Time.time + (GameSettings.LowFx ? 0.12f : 0.06f);
            FX.Burst(pos + Random.insideUnitCircle * 1.2f, A(mc, 0.9f), 1, 1.5f, 0.5f, 0.16f);
        }

        // ---- เงาตามตัว ตอนพุ่งขัดจังหวะ / วาร์ปไปด้านหลัง ----
        bool trail = (st == BossState.Attack && boss.CurrentMove == BossMove.Interrupt) || st == BossState.CounterDodge;
        if (trail && Time.time >= nextGhost && vis != null && vis.body != null && vis.body.sprite != null)
        {
            nextGhost = Time.time + 0.03f;
            SpawnGhost(st == BossState.CounterDodge ? A(MoveColor(BossMove.BackStab), 0.6f) : new Color(1f, 1f, 1f, 0.55f));
        }
    }

    void SpawnGhost(Color col)
    {
        var body = vis.body;
        var go = new GameObject("BossGhost");
        go.transform.position = body.transform.position;
        go.transform.rotation = body.transform.rotation;
        go.transform.localScale = body.transform.lossyScale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = body.sprite;
        sr.flipX = body.flipX;
        sr.sortingLayerID = body.sortingLayerID;
        sr.sortingOrder = body.sortingOrder - 1;
        sr.color = col;

        var g = go.AddComponent<FxGhost>();
        g.sr = sr;
        g.start = col;
        g.life = 0.3f;
    }

    // ================= ตอนง้างท่า: ให้ผู้เล่นอ่านท่าออก =================
    void OnWindup(Vector2 pos, Vector2 dir, Color c)
    {
        float w = Mathf.Max(0.2f, boss.CurrentWindup);
        Color ca = A(c, 0.9f);

        switch (boss.CurrentMove)
        {
            case BossMove.Slash:
                FXPlus.Converge(pos + dir * 1.2f, ca, 1.4f, w, Count(6));
                break;

            case BossMove.ChargeSlash:
                FXPlus.Converge(pos, ca, 3.4f, w, Count(18));
                FXPlus.Pillar(pos, A(c, 0.5f), 5f, 1.0f, w);
                FX.Ring(pos, A(c, 0.8f), 3.6f, 0.5f, w);
                break;

            case BossMove.GroundSlam:
                FXPlus.Converge(pos + Vector2.down * 0.6f, ca, 3.2f, w, Count(16));
                FXPlus.Cracks(pos, A(c, 0.45f), boss.SlamRadius * 0.7f, Count(8), w);
                FX.Ring(pos, A(c, 0.7f), boss.SlamRadius, 0.5f, w);
                break;

            case BossMove.Shoot:
                FXPlus.Converge(pos + dir * 1f, ca, 1.8f, w, Count(10));
                break;

            case BossMove.Clone:
                FXPlus.Converge(pos, ca, 3.2f, w, Count(16));
                FX.Ring(pos, A(c, 0.8f), 3.2f, 0.4f, w);
                break;

            case BossMove.HyperTrade:
                FXPlus.Converge(pos, ca, 3.8f, w, Count(18));
                FXPlus.Pillar(pos, A(c, 0.6f), 6f, 1.2f, w);
                FX.Ring(pos, A(c, 0.8f), boss.tradeRadius, 0.5f, w);
                break;

            case BossMove.GuardBreak:
                FXPlus.Converge(pos, ca, 3f, w, Count(14));
                FXPlus.Cracks(pos, A(c, 0.45f), boss.guardBreakRadius * 0.8f, Count(6), w);
                FX.Ring(pos, A(c, 0.7f), boss.guardBreakRadius, 0.5f, w);
                break;

            case BossMove.ZoneBlast:
                FXPlus.Converge(pos, ca, 4.2f, w, Count(18));
                FX.Ring(pos, A(c, 0.8f), 4.2f, 0.5f, w);
                break;

            case BossMove.BackStab:
                FXPlus.Converge(pos, ca, 2f, w, Count(8));
                FXPlus.Flare(pos, A(c, 0.7f), 2.5f, w);
                break;

            default:    // Riposte / Interrupt / Dash
                FXPlus.Converge(pos, ca, 2f, w, Count(8));
                break;
        }
    }

    // ================= ตอนปล่อยท่า =================
    void OnAttack(Vector2 pos, Vector2 dir, Color c)
    {
        Color white = new Color(1f, 1f, 1f, 0.95f);
        Color ca = A(c, 0.95f);
        Vector2 front = pos + dir * 1.5f;

        switch (boss.CurrentMove)
        {
            case BossMove.Slash:
                FXPlus.Slash(front, dir, ca, 2.8f, 0.24f);
                FXPlus.Flare(front, A(c, 0.7f), 2f, 0.2f);
                FX.Burst(front, c, Count(8), 6f, 0.3f, 0.14f);
                Shake(0.1f, 0.1f);
                break;

            case BossMove.ChargeSlash:
                FXPlus.Slash(front, dir, ca, 5f, 0.35f);
                FXPlus.Slash(front, dir, white, 3.4f, 0.28f);
                FXPlus.Shockwave(pos, ca, boss.chargeHitRadius);
                FXPlus.Cracks(pos, ca, boss.chargeHitRadius, Count(8), 0.5f);
                FXPlus.Pillar(pos, A(c, 0.8f), 6f, 1.6f, 0.4f);
                FXPlus.Flare(pos, A(c, 0.8f), 5f, 0.35f);
                FX.Burst(front, c, Count(18), 9f, 0.5f, 0.22f);
                Shake(0.28f, 0.28f);
                Flash(A(c, 0.22f), 0.18f);
                break;

            case BossMove.GroundSlam:
                FXPlus.Shockwave(pos, ca, boss.SlamRadius);
                FXPlus.Cracks(pos, ca, boss.SlamRadius, Count(12), 0.7f);
                FXPlus.Pillar(pos, A(c, 0.7f), 4f, 1.8f, 0.4f);
                FXPlus.Flare(pos, A(c, 0.8f), boss.SlamRadius * 0.9f, 0.3f);
                FX.Burst(pos, new Color(0.9f, 0.75f, 0.5f), Count(22), 7f, 0.6f, 0.26f);   // ฝุ่น
                Shake(0.35f, 0.32f);
                Flash(A(c, 0.2f), 0.18f);
                break;

            case BossMove.Shoot:
                FXPlus.Flare(pos + dir * 1f, ca, 2.2f, 0.18f);
                FXPlus.Slash(pos + dir * 1.1f, dir, ca, 2f, 0.18f);
                FX.Burst(pos + dir * 1.2f, c, Count(10), 6f, 0.3f, 0.14f);
                break;

            case BossMove.Clone:
                FXPlus.Shockwave(pos, white, 4f);
                FXPlus.Pillar(pos, A(c, 0.8f), 6f, 1.5f, 0.5f);
                FXPlus.Flare(pos, white, 5f, 0.4f);
                Shake(0.2f, 0.15f);
                break;

            case BossMove.Riposte:
                FXPlus.Slash(front, dir, ca, 4f, 0.28f);
                FXPlus.Slash(front, Quaternion.Euler(0f, 0f, 35f) * dir, white, 3f, 0.22f);
                FXPlus.Flare(front, A(c, 0.8f), 2.5f, 0.2f);
                Shake(0.15f, 0.15f);
                break;

            case BossMove.HyperTrade:
                FXPlus.Shockwave(pos, ca, boss.tradeRadius);
                FXPlus.Cracks(pos, ca, boss.tradeRadius, Count(10), 0.6f);
                FXPlus.Pillar(pos, A(c, 0.9f), 8f, 2.2f, 0.5f);
                FXPlus.Flare(pos, A(c, 0.9f), 6f, 0.4f);
                Shake(0.4f, 0.35f);
                Flash(A(c, 0.28f), 0.22f);
                break;

            case BossMove.BackStab:
                FXPlus.Slash(front, dir, ca, 3.6f, 0.26f);
                FXPlus.Beam(pos - dir * 1.5f, pos + dir * 2.5f, A(c, 0.9f), 0.2f, 0.18f);
                FXPlus.Flare(front, A(c, 0.8f), 2.4f, 0.2f);
                Shake(0.15f, 0.15f);
                break;

            case BossMove.GuardBreak:
                FXPlus.Slash(front, dir, ca, 4.4f, 0.3f);
                FXPlus.Shockwave(pos, ca, boss.guardBreakRadius);
                FXPlus.Cracks(pos, ca, boss.guardBreakRadius, Count(8), 0.5f);
                FXPlus.Flare(pos, A(c, 0.8f), 3.5f, 0.3f);
                Shake(0.3f, 0.28f);
                Flash(A(c, 0.2f), 0.15f);
                break;

            case BossMove.Interrupt:
                FXPlus.Beam(pos - dir * 0.5f, pos + dir * 5f, white, 0.3f, 0.2f);
                FXPlus.Flare(pos, white, 2.5f, 0.2f);
                break;

            case BossMove.ZoneBlast:
                FXPlus.Shockwave(pos, ca, 6f);
                FXPlus.Pillar(pos, A(c, 0.8f), 7f, 1.6f, 0.45f);
                FXPlus.Flare(pos, A(c, 0.9f), 5f, 0.4f);
                Shake(0.3f, 0.22f);
                Flash(A(c, 0.2f), 0.18f);
                break;
        }
    }
}