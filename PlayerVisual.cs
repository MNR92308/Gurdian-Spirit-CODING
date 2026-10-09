using System.Collections;
using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    [Header("Parts")]
    public SpriteRenderer body;
    public SpriteRenderer shadow;       // ไม่ใส่ก็ได้
    public SpriteRenderer guardAura;    // ไม่ใส่ก็ได้

    [Header("Frames")]
    public Sprite[] idleFrames;
    public Sprite[] walkFrames;
    public Sprite[] slashFrames;
    public Sprite[] chargeHoldFrames;
    public Sprite[] chargeSlashFrames;
    public Sprite[] guardFrames;
    public Sprite[] parryFrames;
    public Sprite[] shootFrames;
    public Sprite[] hurtFrames;
    public Sprite[] deathFrames;

    [Header("Settings")]
    public bool spritesFaceRight = true;
    public float idleFps = 4f;
    public float walkFps = 10f;
    public float guardRaiseDuration = 0.12f;
    public float hurtDuration = 0.3f;
    public float deathDuration = 0.8f;

    [Header("Charge Effect")]
    public float chargeShake = 0f;
    public bool glowWhileCharging = true;

    [Header("Guard Aura")]
    public bool autoFitAura = true;
    public float auraSizeMultiplier = 1.1f;

    [Header("Dash Effect")]
    public Color dashTint = new Color(0.45f, 0.75f, 1f, 1f);
    public SpriteRenderer dashRing;
    public float dashRingSize = 1.3f;
    public float ghostInterval = 0.03f;
    public float ghostLife = 0.3f;
    public Color ghostColor = new Color(0.45f, 0.75f, 1f, 0.6f);
    public Color ghostColor2 = new Color(0.75f, 0.45f, 1f, 0.6f);   // สลับสีเงา

    [Header("Extra FX")]
    public bool walkDust = true;
    public Color slashColor = new Color(0.85f, 0.95f, 1f, 0.95f);
    public Color chargeColor = new Color(1f, 0.85f, 0.3f, 0.95f);

    enum LoopState { None, Idle, Walk, ChargeHold }

    PlayerSword sword;
    PlayerParry parry;
    PlayerMovement movement;
    Rigidbody2D rb;
    Camera cam;

    Vector3 bodyBasePos;
    bool dead;
    float deathStart;
    float hurtStart = -10f;
    float shootStart = -10f;
    float shootDur = 0.3f;
    bool facingLeft;
    Vector2 facingDir = Vector2.right;

    bool wasAttacking;
    Sprite[] attackFrames;
    float attackStart, attackDuration;

    GuardState prevGuard = GuardState.None;
    float guardStart;

    LoopState loopState;
    float loopStart;

    float ghostTimer;
    bool wasDashing;
    int ghostFlip;
    float dustTimer, chargeFxTimer;
    bool chargeReady;

    static Sprite ringSprite;

    void Awake()
    {
        sword = GetComponent<PlayerSword>();
        parry = GetComponent<PlayerParry>();
        movement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody2D>();

        cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();

        if (body != null) bodyBasePos = body.transform.localPosition;

        if (dashRing == null) CreateDashRing();

        if (walkFrames == null || walkFrames.Length == 0)
            Debug.LogWarning("PlayerVisual: ยังไม่ได้ใส่ภาพใน Walk Frames");
    }

    static bool Has(Sprite[] f) { return f != null && f.Length > 0; }

    Vector2 FeetPos()
    {
        if (body != null) return new Vector2(body.bounds.center.x, body.bounds.min.y + 0.1f);
        return transform.position;
    }

    void Update()
    {
        if (body == null) return;

        if (dead)
        {
            ShowByProgress(deathFrames, Time.time - deathStart, deathDuration);
            return;
        }

        FaceTarget();

        bool moving = !PauseMenu.Paused && Controls.IsMoving;

        // จับจังหวะเริ่มฟัน
        bool attacking = sword != null && sword.IsAttacking;
        if (attacking && !wasAttacking)
        {
            bool charge = sword.LastAttackWasCharge;
            attackFrames = charge ? chargeSlashFrames : slashFrames;
            attackDuration = charge ? sword.chargeAttackDuration : sword.attackDuration;
            attackStart = Time.time;

            float hitFrac = charge ? sword.chargeHitTime : sword.hitTime;
            StartCoroutine(SlashFx(charge, attackDuration * hitFrac));
        }
        wasAttacking = attacking;

        // จับจังหวะเริ่มกัน
        GuardState gs = parry != null ? parry.CurrentState : GuardState.None;
        if (gs != GuardState.None && prevGuard == GuardState.None)
            guardStart = Time.time;
        if (gs == GuardState.Parry && prevGuard != GuardState.Parry)
        {
            Vector2 c = body.bounds.center;
            FX.Ring(c, new Color(1f, 0.9f, 0.3f, 0.8f), 0.3f, 1.4f, 0.22f);
            FXPlus.Flare(c, new Color(1f, 0.95f, 0.5f, 0.6f), 1.8f, 0.2f);
        }
        prevGuard = gs;

        body.transform.localPosition = bodyBasePos;
        body.color = Color.white;

        bool shooting = Has(shootFrames) && Time.time - shootStart < shootDur;
        bool dashing = movement != null && movement.IsDashing;

        if (Time.time - hurtStart < hurtDuration || PlayerStatus.IsStunned)
        {
            loopState = LoopState.None;
            ShowByProgress(hurtFrames, Time.time - hurtStart, hurtDuration);

            // แดงวาบแล้วจางลง / ชะงัก = เหลืองกะพริบ
            if (PlayerStatus.IsStunned)
                body.color = Color.Lerp(Color.white, new Color(1f, 0.9f, 0.3f), 0.5f + 0.5f * Mathf.Sin(Time.time * 20f));
            else
                body.color = Color.Lerp(new Color(1f, 0.3f, 0.3f), Color.white, (Time.time - hurtStart) / hurtDuration);
        }
        else if (attacking)
        {
            loopState = LoopState.None;
            ShowByProgress(attackFrames, Time.time - attackStart, attackDuration);
        }
        else if (shooting)
        {
            loopState = LoopState.None;
            ShowByProgress(shootFrames, Time.time - shootStart, shootDur);
        }
        else if (gs != GuardState.None && (Has(guardFrames) || Has(parryFrames)))
        {
            loopState = LoopState.None;
            float t = Time.time - guardStart;

            if (gs == GuardState.Parry && Has(parryFrames))
                ShowByProgress(parryFrames, t, parry.parryWindow);
            else if (Has(guardFrames))
                ShowByProgress(guardFrames, t, guardRaiseDuration);
            else
                ShowByProgress(parryFrames, t, guardRaiseDuration);
        }
        else if (sword != null && sword.IsCharging)
        {
            ShowChargeHold();
        }
        else if (moving)
        {
            ShowLoop(LoopState.Walk, walkFrames, walkFps);
        }
        else
        {
            ShowLoop(LoopState.Idle, idleFrames, idleFps);
        }

        if (sword == null || !sword.IsCharging) chargeReady = false;

        // ฝุ่นตอนเดิน
        if (walkDust && moving && !dashing && !attacking)
        {
            dustTimer -= Time.deltaTime;
            if (dustTimer <= 0f)
            {
                dustTimer = GameSettings.LowFx ? 0.3f : 0.18f;
                FX.Burst(FeetPos(), new Color(0.85f, 0.8f, 0.7f, 0.5f), 2, 1.2f, 0.35f, 0.14f);
            }
        }
        else dustTimer = 0f;

        // เอฟเฟกต์แดช
        if (dashing)
        {
            if (!wasDashing) OnDashStart();

            body.color = dashTint;

            ghostTimer -= Time.deltaTime;
            if (ghostTimer <= 0f)
            {
                SpawnGhost();
                ghostTimer = ghostInterval;

                // เส้นความเร็วลากตามหลัง
                if (rb != null && rb.linearVelocity.sqrMagnitude > 1f)
                {
                    Vector2 v = rb.linearVelocity.normalized;
                    Vector2 c = body.bounds.center;
                    FXPlus.Beam(c, c - v * 1.6f, new Color(0.6f, 0.9f, 1f, 0.5f), 0.12f, 0.15f);
                }
            }
        }
        else
        {
            if (wasDashing) FX.Ring(body.bounds.center, new Color(0.5f, 0.85f, 1f, 0.6f), 0.3f, 1f, 0.2f);
            ghostTimer = 0f;
        }
        wasDashing = dashing;

        UpdateDashRing(movement != null && movement.IsInvincibleDash);
        UpdateGuardAura();
    }

    // ===== ตัวช่วยแสดงเฟรม =====

    void ShowLoop(LoopState state, Sprite[] frames, float fps)
    {
        if (!Has(frames)) return;

        if (loopState != state)
        {
            loopState = state;
            loopStart = Time.time;
        }

        int i = Mathf.FloorToInt((Time.time - loopStart) * fps) % frames.Length;
        body.sprite = frames[i];
    }

    void ShowByProgress(Sprite[] frames, float elapsed, float duration)
    {
        if (!Has(frames)) return;
        int i = Mathf.Clamp(Mathf.FloorToInt(elapsed / Mathf.Max(0.01f, duration) * frames.Length), 0, frames.Length - 1);
        body.sprite = frames[i];
    }

    void ShowChargeHold()
    {
        loopState = LoopState.ChargeHold;
        float p = sword.ChargeProgress;

        if (Has(chargeHoldFrames))
        {
            int i = Mathf.Clamp(Mathf.FloorToInt(p * chargeHoldFrames.Length), 0, chargeHoldFrames.Length - 1);
            body.sprite = chargeHoldFrames[i];
        }
        else if (Has(chargeSlashFrames))
        {
            body.sprite = chargeSlashFrames[0];
        }

        if (chargeShake > 0f)
        {
            float amount = chargeShake * (0.3f + p);
            float dx = Mathf.Sin(Time.time * 60f) * amount;
            body.transform.localPosition = bodyBasePos + new Vector3(dx, 0f, 0f);
        }

        if (glowWhileCharging)
        {
            Color glow = new Color(1f, 0.85f, 0.4f);
            if (p >= 1f)
                body.color = Color.Lerp(Color.white, glow, Mathf.PingPong(Time.time * 3f, 1f));
            else
                body.color = Color.Lerp(Color.white, glow, p * 0.7f);
        }

        // อนุภาคพุ่งเข้าตัว เยอะขึ้นตามความเต็มของชาร์จ
        Vector2 c = body.bounds.center;
        chargeFxTimer -= Time.deltaTime;
        if (chargeFxTimer <= 0f)
        {
            chargeFxTimer = GameSettings.LowFx ? 0.12f : 0.06f;
            FXPlus.Converge(c, new Color(chargeColor.r, chargeColor.g, chargeColor.b, 0.5f + 0.4f * p), 1f + 0.6f * p, 0.35f, p > 0.6f ? 2 : 1);
        }

        // ชาร์จเต็ม: แสงวาบ + วงคลื่น + ข้อความ
        if (p >= 1f && !chargeReady)
        {
            chargeReady = true;
            FX.Ring(c, chargeColor, 0.3f, 2.2f, 0.35f);
            FX.Burst(c, chargeColor, 12, 6f, 0.4f, 0.16f);
            FXPlus.Flare(c, chargeColor, 3f, 0.3f);
            FXPlus.Pillar(c, new Color(1f, 0.9f, 0.5f, 0.6f), 3f, 0.8f, 0.3f);
        }
    }

    // ===== หันหน้า =====
    //   มือถือ: หันตามทิศที่เดิน    คอม: หันตามเมาส์
    void FaceTarget()
    {
        if (PauseMenu.Paused)
        {
            ApplyFlip();
            return;
        }

        if (GameSettings.Touch)
        {
            if (MobileInput.HasMove)
            {
                Vector2 m = MobileInput.Move;
                facingDir = m.normalized;
                if (Mathf.Abs(m.x) > 0.1f) facingLeft = m.x < 0f;
            }
        }
        else if (cam != null)
        {
            Vector3 mw = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 d = (Vector2)mw - (Vector2)transform.position;
            if (d.sqrMagnitude > 0.0001f) facingDir = d.normalized;
            if (Mathf.Abs(d.x) > 0.05f) facingLeft = d.x < 0f;
        }

        ApplyFlip();
    }

    void ApplyFlip()
    {
        body.flipX = spritesFaceRight ? facingLeft : !facingLeft;
    }

    void UpdateGuardAura()
    {
        if (guardAura == null || parry == null || body == null) return;

        GuardState st = parry.CurrentState;
        guardAura.enabled = st != GuardState.None && body.enabled;
        if (!guardAura.enabled) return;

        if (st == GuardState.Parry)
            guardAura.color = new Color(1f, 0.9f, 0.1f, 0.55f + 0.2f * Mathf.Sin(Time.time * 30f));
        else
            guardAura.color = new Color(0.2f, 0.9f, 1f, 0.40f + 0.08f * Mathf.Sin(Time.time * 8f));

        if (!autoFitAura || body.sprite == null || guardAura.sprite == null) return;

        Bounds b = body.bounds;
        Vector3 c = b.center;
        c.z = guardAura.transform.position.z;
        guardAura.transform.position = c;

        float target = Mathf.Max(b.size.x, b.size.y) * auraSizeMultiplier;
        float spriteSize = guardAura.sprite.bounds.size.x;
        Transform parent = guardAura.transform.parent;
        float parentScale = parent != null ? Mathf.Abs(parent.lossyScale.x) : 1f;
        float s = target / Mathf.Max(0.0001f, spriteSize * parentScale);
        s *= 1f + 0.05f * Mathf.Sin(Time.time * 14f);      // กระเพื่อม
        guardAura.transform.localScale = new Vector3(s, s, 1f);
    }

    // ===== เอฟเฟกต์ฟัน =====
    IEnumerator SlashFx(bool charge, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (dead || sword == null || body == null) yield break;

        Vector2 origin = body.bounds.center;
        Vector2 hit = sword.attackPoint != null ? (Vector2)sword.attackPoint.position : origin + facingDir * 0.9f;
        Vector2 d = hit - origin;
        d = d.sqrMagnitude > 0.001f ? d.normalized : facingDir;

        if (charge)
        {
            FXPlus.Slash(hit, d, chargeColor, 3.6f, 0.34f);
            FXPlus.Slash(hit, d, new Color(1f, 1f, 1f, 0.95f), 2.4f, 0.26f);
            FXPlus.Flare(hit, chargeColor, 3f, 0.25f);
            FXPlus.Shockwave(hit, chargeColor, 2.4f);
            FX.Shake(0.12f, 0.12f);
        }
        else
        {
            FXPlus.Slash(hit, d, slashColor, 2.0f, 0.2f);
            FXPlus.Flare(hit, new Color(slashColor.r, slashColor.g, slashColor.b, 0.5f), 1.3f, 0.15f);
        }
    }

    // ===== เอฟเฟกต์แดช =====
    void OnDashStart()
    {
        Vector2 c = body.bounds.center;
        FX.Ring(c, new Color(0.5f, 0.85f, 1f, 0.8f), 0.2f, 1.6f, 0.25f);
        FX.Burst(FeetPos(), new Color(0.85f, 0.8f, 0.7f, 0.7f), 6, 4f, 0.4f, 0.18f);
        FXPlus.Flare(c, new Color(0.5f, 0.85f, 1f, 0.6f), 2f, 0.2f);
    }

    void UpdateDashRing(bool show)
    {
        if (dashRing == null || body == null) return;

        dashRing.enabled = show && body.enabled;
        if (!dashRing.enabled) return;

        Bounds b = body.bounds;
        Vector3 c = b.center;
        c.z = body.transform.position.z;
        dashRing.transform.position = c;

        float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 30f);
        float target = Mathf.Max(b.size.x, b.size.y) * dashRingSize * pulse;
        float spriteSize = dashRing.sprite != null ? dashRing.sprite.bounds.size.x : 1f;
        float parentScale = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        float s = target / (Mathf.Max(0.0001f, spriteSize) * parentScale);
        dashRing.transform.localScale = new Vector3(s, s, 1f);

        dashRing.sortingLayerID = body.sortingLayerID;
        dashRing.sortingOrder = body.sortingOrder + 1;
    }

    void CreateDashRing()
    {
        var go = new GameObject("DashRing");
        go.transform.SetParent(transform, false);
        dashRing = go.AddComponent<SpriteRenderer>();
        dashRing.sprite = GetRingSprite();
        dashRing.color = Color.white;
        dashRing.enabled = false;
    }

    static Sprite GetRingSprite()
    {
        if (ringSprite != null) return ringSprite;

        int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        float c = (size - 1) / 2f;
        float outer = size / 2f - 1f;
        float inner = outer - 6f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                float a = Mathf.Clamp01(outer - d) * Mathf.Clamp01(d - inner);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();

        ringSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return ringSprite;
    }

    void SpawnGhost()
    {
        if (body.sprite == null) return;

        ghostFlip++;
        Color col = (ghostFlip % 2 == 0) ? ghostColor : ghostColor2;

        var go = new GameObject("DashGhost");
        go.transform.position = body.transform.position;
        go.transform.rotation = body.transform.rotation;
        go.transform.localScale = body.transform.lossyScale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = body.sprite;
        sr.flipX = body.flipX;
        sr.sortingLayerID = body.sortingLayerID;
        sr.sortingOrder = body.sortingOrder - 1;
        sr.color = col;

        StartCoroutine(FadeGhost(sr, col));
    }

    IEnumerator FadeGhost(SpriteRenderer sr, Color start)
    {
        float t = 0f;

        while (t < ghostLife && sr != null)
        {
            t += Time.deltaTime;
            Color c = start;
            c.a = Mathf.Lerp(start.a, 0f, t / ghostLife);
            sr.color = c;
            yield return null;
        }

        if (sr != null) Destroy(sr.gameObject);
    }

    // ===== เรียกจากสคริปต์อื่น =====

    public void SetVisible(bool v)
    {
        if (body) body.enabled = v;
        if (shadow) shadow.enabled = v;
    }

    public void PlayHurt()
    {
        hurtStart = Time.time;
        if (body != null) FXPlus.Flare(body.bounds.center, new Color(1f, 0.25f, 0.2f, 0.7f), 2.2f, 0.25f);
    }

    public void PlayShoot(float duration)
    {
        shootStart = Time.time;
        shootDur = Mathf.Max(0.05f, duration);
    }

    public void PlayDeath()
    {
        if (dead) return;
        dead = true;
        deathStart = Time.time;
        SetVisible(true);
        if (guardAura) guardAura.enabled = false;
        if (dashRing) dashRing.enabled = false;
        if (body)
        {
            body.color = Color.white;
            body.transform.localPosition = bodyBasePos;

            Vector2 c = body.bounds.center;
            FXPlus.Pillar(c, new Color(1f, 0.3f, 0.25f, 0.9f), 6f, 1.6f, 0.7f);
            FXPlus.Cracks(c, new Color(1f, 0.3f, 0.25f, 0.9f), 3.5f, 8, 0.6f);
            FXPlus.Flare(c, new Color(1f, 0.4f, 0.3f, 0.9f), 4f, 0.5f);
        }
    }
}