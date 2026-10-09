using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum BossState { Idle, Chase, Windup, Attack, Recover, Stunned, Staggered, Deflect, Dead, Reviving, CounterWarn, CounterStance, CounterDodge }
public enum BossMove { Slash, ChargeSlash, GroundSlam, Shoot, Clone, Dash, Riposte, HyperTrade, BackStab, GuardBreak, Interrupt, ZoneBlast }
public enum CounterKind { ParryCounter, HyperTrade, PerfectDodge }
public enum HitType { Slash, Charge, Arrow, Reflected, Other }

public class BossAI : MonoBehaviour
{
    public static readonly List<BossAI> All = new List<BossAI>();

    public static BossAI Main
    {
        get
        {
            foreach (var b in All)
                if (b != null && !b.IsClone) return b;
            return null;
        }
    }

    [Header("References")]
    public Transform player;
    public PlayerHealth playerHealth;
    public Transform dashIndicator;

    [Header("Stats")]
    public int maxHealth = 20;
    public float moveSpeed = 3f;
    public float stopDistance = 1.2f;

    [Header("Slash (ฟันปกติ)")]
    public float attackRange = 2.0f;
    public float hitRadius = 2.4f;
    public float slashWindup = 0.6f;
    public float slashActiveTime = 0.25f;
    public float slashRecover = 1.0f;
    public float windupJitterMax = 0.3f;

    [Header("Charge Slash (ทะลุแดชและ Parry)")]
    public float chargeRange = 2.8f;
    public float chargeHitRadius = 3.2f;
    public float chargeWindup = 1.1f;
    public float chargeMinWindup = 0.8f;
    public float chargeActiveTime = 0.3f;
    public float chargeRecover = 1.6f;
    public float chargeCooldown = 5f;
    public int chargeHitDamage = 1;

    [Header("Ground Slam (ทะลุการ์ด/Parry แต่แดชหลบได้)")]
    public float slamTriggerRange = 4.5f;
    public float slamRadius = 4.2f;
    public float slamWindup = 1.0f;
    public float slamActiveTime = 0.8f;
    public float slamSecondHitDelay = 0.45f;
    public float slamRecover = 1.4f;
    public float slamCooldown = 7f;
    public int slamDamage = 1;

    [Header("Shoot (ยิงระยะไกล)")]
    public float shootMinRange = 3.5f;
    public float shootWindup = 0.7f;
    public float shootActiveTime = 0.2f;
    public float shootRecover = 0.9f;
    public float shootCooldown = 4f;
    public int bulletCount = 3;
    public float bulletSpread = 18f;
    public float bulletSpeed = 7.5f;
    public float leadFactor = 0.6f;

    [Header("Clone (แยกร่าง)")]
    [Header("Clone Size/Speed (ร่างแยก)")]
    public float cloneScale = 0.75f;        // เล็กลง 25%
    public float cloneSpeedMult = 1.25f;    // เร็วกว่าบอสหลัก 25%
    public float cloneWindup = 1.0f;
    public float cloneActiveTime = 0.3f;
    public float cloneRecover = 0.8f;
    public float cloneCooldown = 25f;
    public int cloneCount = 2;
    public float cloneHealthDivisorMin = 2f;
    public float cloneHealthDivisorMax = 3f;
    public float cloneLifetime = 30f;
    public int cloneMatureDeaths = 3;

    [Header("Dash (ปิดไว้ ไม่มีภาพท่านี้)")]
    public bool enableDash = false;
    public float dashWindup = 0.8f;
    public float dashSpeed = 12f;
    public float dashDuration = 0.4f;
    public float dashHitRadius = 1.4f;
    public float dashRecover = 1.3f;

    [Header("Counter (สวนกลับเมื่อถูกตีรัว)")]
    public int counterHitsNeeded = 3;          // ฟันปกติโดนครบนี้
    public float counterWindow = 2f;           // ภายในกี่วินาที
    public int counterStunsNeeded = 2;         // หรือบอสถูกสตั้นติดกันครบนี้ (ภายใน 8 วินาที)
    public float counterWarnTime = 0.3f;       // สัญญาณเตือนก่อนสวนกลับ (ประกายตา/เรืองสี/เสียง)
    public float counterCooldownMin = 3f;
    public float counterCooldownMax = 5f;
    public float counterFailCooldown = 2f;
    [Header("Counter 1: Parry & Counter")]
    public float parryStanceTime = 0.55f;
    public float riposteWindup = 0.12f;
    public float riposteActive = 0.2f;
    public float riposteRecover = 0.9f;
    public float riposteHitRadius = 2.8f;
    public float riposteStun = 0.9f;
    [Header("Counter 2: Hyper-Armor Trade")]
    public float tradeWindup = 0.6f;
    public float tradeActive = 0.35f;
    public float tradeRecover = 1.3f;
    public float tradeRadius = 3.6f;
    public float tradePush = 18f;
    public float tradePushTime = 0.35f;
    [Header("Counter 3: Perfect Dodge & Counter")]
    public float dodgeBehindDist = 1.7f;
    public float dodgeTime = 0.18f;
    public float backstabWindup = 0.35f;
    public float backstabActive = 0.2f;
    public float backstabRecover = 1.0f;
    public float backstabRadius = 2.2f;

    [Header("Anti-Habit (ตอบโต้นิสัยผู้เล่น)")]
    public int guardSpamPresses = 3;           // กดการ์ดครบนี้
    public float guardSpamWindow = 5f;         // ภายในกี่วินาที → Guard Break
    public float guardBreakRange = 3f;
    public float guardBreakWindup = 0.9f;
    public float guardBreakActive = 0.3f;
    public float guardBreakRecover = 1.5f;
    public float guardBreakRadius = 2.6f;
    public float guardBreakCooldown = 7f;
    public float interruptRange = 5.5f;        // ผู้เล่นชาร์จฟัน → พุ่งขัดจังหวะ
    public float interruptChargeMin = 0.25f;
    public float interruptWindup = 0.3f;
    public float interruptLungeTime = 0.25f;
    public float interruptSpeed = 18f;
    public float interruptRecover = 1.0f;
    public float interruptRadius = 1.8f;
    public float interruptStun = 0.9f;
    public float interruptCooldown = 5f;
    public float kiteTime = 3f;                // ผู้เล่นไม่โจมตีเกินนี้ → Zone Blast
    public float zoneWindup = 1.0f;
    public float zoneActive = 0.3f;
    public float zoneRecover = 1.2f;
    public float zoneCooldown = 8f;
    public float waveSpeed = 9f;
    public float waveMaxRadius = 12f;

    [Header("Posture / Stagger")]
    public float maxPosture = 100f;
    public float postureSlash = 6f;
    public float postureCharge = 34f;
    public float postureArrow = 20f;
    public float postureParry = 25f;
    public float postureReflect = 20f;
    public float postureDecayDelay = 3.5f;
    public float postureDecayRate = 10f;
    public float staggerTime = 3f;
    public float staggerDamageMult = 2f;

    [Header("Arrow Deflect (ฟันธนูทิ้ง)")]
    public int deflectStreakNeeded = 3;
    public float arrowStreakReset = 20f;
    public float deflectDetectRange = 7f;
    public float deflectKillRadius = 2.6f;
    public float deflectDuration = 0.55f;
    public float deflectCooldown = 2f;

    [Header("Revive / Evolution (ฟื้นคืนชีพ)")]
    public int maxRevives = 2;
    public float reviveHealthGrowth = 0.1f;
    public float deathAnimTime = 1.2f;
    public float deathHoldTime = 0.5f;
    public float reviveAnimTime = 1.2f;
    public float evoSpeedBonus = 0.2f;
    public float evoWindupMult = 0.88f;
    public float evoRecoverMult = 0.9f;
    public int shootUnlockEvo = 1;
    public int slamUnlockEvo = 1;
    public int cloneUnlockEvo = 2;

    [Header("AI")]
    public float decisionThreshold = 0.3f;
    public float noiseAmount = 0.25f;
    public float repeatPenalty = 0.35f;
    public float repeatPenalty2 = 0.15f;
    [Range(0f, 1f)] public float adaptStrength = 1f;
    public Vector2 thinkDelayRange = new Vector2(0.1f, 0.45f);
    public float comboChanceBase = 0.15f;
    public float comboChancePerEvo = 0.2f;
    public float strafeMinDist = 2.5f;
    public float strafeMaxDist = 8f;
    public float strafeWeight = 0.6f;

    [Header("Phases")]
    public float phase2Threshold = 0.6f;
    public float phase3Threshold = 0.3f;
    public float roarTime = 1.0f;
    public float minWindup = 0.35f;
    public float[] phaseSpeedMult = { 1f, 1.2f, 1.4f };
    public float[] phaseWindupMult = { 1f, 0.85f, 0.7f };
    public float[] phaseRecoverMult = { 1f, 0.85f, 0.7f };

    [Header("Stage Scaling")]
    public int healthPerStage = 6;
    public float speedPerStage = 0.1f;
    public float recoverReducePerStage = 0.08f;

    [Header("Timing")]
    public float idleTime = 0.8f;
    public float stunTime = 0.5f;
    public Vector2 rangedDelayRange = new Vector2(1f, 2f);

    [Header("Debug")]
    public bool useForcedMove;
    public BossMove forcedMove;
    public int debugStartEvolution = -1;

    // ===== ค่าที่สคริปต์อื่นอ่าน =====
    public BossState State { get; private set; }
    public BossMove CurrentMove { get; private set; }
    public CounterKind ActiveCounter { get; private set; }
    public int CurrentHealth { get; private set; }
    public int Phase { get; private set; }
    public int Evolution { get; private set; }
    public bool IsClone { get; private set; }
    public float Posture { get; private set; }
    public int ImpactCount { get; private set; }
    public int DeflectCount { get; private set; }

    public Collider2D Col => col;               // ให้ PlayerMovement ใช้ทำแดชทะลุบอส

    public float CurrentWindup => currentWindup;
    public float CurrentActive => currentActive;
    public float SlamRadius => slamRadius;
    public float MaxPosture => maxPosture * (1f + 0.15f * Evolution);
    public bool IsTargetable => State != BossState.Dead && State != BossState.Reviving;
    public bool IsRoaring => State == BossState.Idle && roaring;
    public bool IsChargeSlashWarning =>
        CurrentMove == BossMove.ChargeSlash && (State == BossState.Windup || State == BossState.Attack);
    public bool IsSlamWarning => CurrentMove == BossMove.GroundSlam && State == BossState.Windup;
    public bool IsFinalDeath => State == BossState.Dead && !willRevive;
    public bool HyperArmor =>
        CurrentMove == BossMove.HyperTrade && (State == BossState.Windup || State == BossState.Attack);

    // รัศมีวงเตือนบนพื้นตอนง้างท่า (0 = ไม่มี)
    public float TelegraphRadius
    {
        get
        {
            if (State != BossState.Windup) return 0f;
            switch (CurrentMove)
            {
                case BossMove.GroundSlam: return slamRadius;
                case BossMove.HyperTrade: return tradeRadius;
                case BossMove.GuardBreak: return guardBreakRadius;
                default: return 0f;
            }
        }
    }

    Rigidbody2D rb;
    Collider2D col;
    Rigidbody2D playerRb;
    PlayerMovement playerMovement;
    PlayerSword playerSword;
    PlayerParry playerParry;
    PlayerStamina playerStamina;
    PlayerArrow playerArrow;

    float stateTimer;
    float rangedTimer;
    float currentWindup;
    float currentActive;
    float attackStartTime;
    float thinkTimer;
    float strafeTimer;
    float strafeSign = 1f;
    float stageRecoverMult = 1f;
    float habitTimer;
    float cloneExpireTime;
    float lastPostureGain;
    float lastArrowHitTime = -999f;
    float nextChargeTime, nextSlamTime, nextShootTime, nextCloneTime, nextDeflectTime;
    float nextInterruptTime, nextGuardBreakTime, nextZoneTime, nextCounterTime;
    int arrowStreak;
    int revivesUsed;

    Vector2 dashDir;
    bool dashHitDone;
    bool slamSecondDone;
    bool comboNext, comboWindup;
    bool wasPlayerDashing, wasPlayerShooting;
    bool willRevive;

    // ความจำของโปรไฟล์ผู้เล่น (จำข้ามการรีสตาร์ทฉาก กด F8 ในเอดิเตอร์เพื่อล้าง)
    static readonly float[] moveEffect = new float[16];
    static float dashEarly;
    static float parryEarly;
    float arrowPressure;
    float windupStartTime;
    float lastPlayerDashEnd = -99f;
    bool wasPlayerParrying;
    bool punishDodge;
    Vector2 playerVelSmooth;

    // ===== Counter / Anti-Habit =====
    bool counterPending;
    float counterPendingUntil;
    int lastCounter = -1;
    readonly List<float> hitTimes = new List<float>();
    readonly List<float> stunTimes = new List<float>();
    readonly List<float> guardPresses = new List<float>();
    float lastAggroTime;
    bool guardSpam, kiting;
    GuardState prevPlayerGuard = GuardState.None;
    bool wasPlayerAttacking;
    Vector2 dodgeFrom, dodgeTo;
    Vector2 lungeDir;
    bool interruptHit;

    int lastMoveIdx = -1;
    int prevMoveIdx = -1;

    int targetPhase;
    bool pendingPhase;
    bool roaring;

    GUIStyle hpStyle;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void OnDestroy()
    {
        if (!IsClone) BossMemory.Save();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (GetComponent<BossStageSkin>() == null) gameObject.AddComponent<BossStageSkin>();
    }

    void Start()
    {
        FindPlayer();
        IgnoreOtherBosses();
        BossMemory.Load();
        lastAggroTime = Time.time;

        if (!IsClone)
        {
            int stage = Mathf.Clamp(PlayerPrefs.GetInt("Stage", 1), 1, 4);
            maxHealth += (stage - 1) * healthPerStage;
            moveSpeed *= 1f + (stage - 1) * speedPerStage;
            stageRecoverMult = 1f - (stage - 1) * recoverReducePerStage;

            if (debugStartEvolution >= 0) Evolution = Mathf.Clamp(debugStartEvolution, 0, maxRevives);
        }

        CurrentHealth = maxHealth;
        ChangeState(BossState.Idle);
    }

    void FindPlayer()
    {
        if (player == null || playerHealth == null)
        {
            var ph = FindAnyObjectByType<PlayerHealth>();
            if (ph != null) { player = ph.transform; playerHealth = ph; }
        }

        if (player != null && playerMovement == null)
        {
            playerRb = player.GetComponent<Rigidbody2D>();
            playerMovement = player.GetComponent<PlayerMovement>();
            playerSword = player.GetComponent<PlayerSword>();
            playerParry = player.GetComponent<PlayerParry>();
            playerStamina = player.GetComponent<PlayerStamina>();
            playerArrow = player.GetComponent<PlayerArrow>();
        }
    }

    // บอสทุกตัว (ตัวจริง + ร่างแยก) ไม่ชนกันเอง
    void IgnoreOtherBosses()
    {
        if (col == null) return;
        foreach (var b in All)
        {
            if (b == null || b == this || b.col == null) continue;
            Physics2D.IgnoreCollision(col, b.col, true);
        }
    }

    public void InitAsClone(BossAI parent, int hp)
    {
        IsClone = true;
        IgnoreOtherBosses();
        maxHealth = hp;
        Evolution = parent.Evolution;
        dashIndicator = null;
        useForcedMove = false;
        cloneExpireTime = Time.time + parent.cloneLifetime;
        transform.localScale *= cloneScale;
    }

    float M(float[] arr) { return arr[Mathf.Clamp(Phase, 0, arr.Length - 1)]; }
    float Legacy => Mathf.Min(0.3f, BossMemory.TotalDeaths * 0.03f);
    float SpeedMult => M(phaseSpeedMult) * (1f + evoSpeedBonus * Evolution) * (1f + Legacy);
    float WindupMult => M(phaseWindupMult) * Mathf.Pow(evoWindupMult, Evolution) * (1f - 0.5f * Legacy);
    float RecoverMult => M(phaseRecoverMult) * Mathf.Pow(evoRecoverMult, Evolution) * stageRecoverMult;
    bool Unlocked(int evo) { return Evolution >= evo; }

    float DistanceToPlayer()
    {
        return Vector2.Distance(transform.position, player.position);
    }

    // ================= Update =================
    void Update()
    {
#if UNITY_EDITOR
        if (!IsClone && Input.GetKeyDown(KeyCode.F8))
        {
            BossMemory.ResetAll();
            ResetProfile();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }
#endif
        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        float dt = Time.deltaTime;

        if (State == BossState.Dead) { UpdateDead(dt); return; }
        if (State == BossState.Reviving) { UpdateReviving(dt); return; }

        if (IsClone && Time.time >= cloneExpireTime) { Die(false); return; }

        TrackPlayerHabits(dt);
        UpdatePosture(dt);
        TryDeflect();
        if (counterPending) TryStartCounter();

        stateTimer -= dt;

        switch (State)
        {
            case BossState.Idle:
                if (stateTimer <= 0f)
                {
                    roaring = false;
                    ChangeState(BossState.Chase);
                }
                break;

            case BossState.Chase:
                strafeTimer -= dt;
                if (strafeTimer <= 0f)
                {
                    strafeSign = Random.value < 0.5f ? -1f : 1f;
                    strafeTimer = Random.Range(1.2f, 2.8f);
                }
                rangedTimer -= dt;
                DecideMove(dt);
                break;

            case BossState.Windup:
                if (stateTimer <= 0f) ChangeState(BossState.Attack);
                break;

            case BossState.Attack:
                UpdateAttack();
                break;

            case BossState.Recover:
                if (stateTimer <= 0f) EndRecover();
                break;

            case BossState.Stunned:
                if (stateTimer <= 0f) ChangeState(BossState.Chase);
                break;

            case BossState.Staggered:
                Posture = MaxPosture * Mathf.Clamp01(stateTimer / Mathf.Max(0.01f, staggerTime));
                if (stateTimer <= 0f)
                {
                    Posture = 0f;
                    ChangeState(BossState.Chase);
                }
                break;

            case BossState.Deflect:
                UpdateDeflect();
                break;

            case BossState.CounterWarn:
                if (stateTimer <= 0f) BeginCounter();
                break;

            case BossState.CounterStance:
                if (stateTimer <= 0f)
                {
                    nextCounterTime = Time.time + counterFailCooldown;   // ไม่มีใครตีเข้ามา
                    ChangeState(BossState.Chase);
                }
                break;

            case BossState.CounterDodge:
                if (stateTimer <= 0f)
                {
                    CurrentMove = BossMove.BackStab;
                    ChangeState(BossState.Windup);
                }
                break;
        }

        UpdateIndicator();
    }

    void UpdateAttack()
    {
        if (CurrentMove == BossMove.Dash && !dashHitDone && DistanceToPlayer() <= dashHitRadius)
        {
            dashHitDone = true;
            ApplyHit(false, 1, false);
        }

        // Interrupt: พุ่งเข้าใส่ ถ้าโดนผู้เล่นชะงักและชาร์จถูกยกเลิก
        if (State == BossState.Attack && CurrentMove == BossMove.Interrupt && !interruptHit
            && DistanceToPlayer() <= interruptRadius)
        {
            interruptHit = true;
            HitResult r = ApplyHit(false, 1, false);
            if (r == HitResult.Hurt) StunPlayer(interruptStun);
            if (State == BossState.Attack) stateTimer = Mathf.Min(stateTimer, 0.08f);
        }

        if (State == BossState.Attack && CurrentMove == BossMove.GroundSlam && !slamSecondDone
            && Time.time - attackStartTime >= slamSecondHitDelay)
        {
            slamSecondDone = true;
            SlamWave();
        }

        if (State == BossState.Attack && stateTimer <= 0f)
            ChangeState(BossState.Recover);
    }

    void EndRecover()
    {
        if (comboNext)
        {
            comboNext = false;
            if (DistanceToPlayer() <= attackRange * 1.4f)
            {
                comboWindup = true;
                StartMove(BossMove.Slash);
                return;
            }
        }
        ChangeState(BossState.Chase);
    }

    void FixedUpdate()
    {
        if (State == BossState.Chase && player != null)
        {
            Vector2 to = (Vector2)player.position - rb.position;
            float d = to.magnitude;
            Vector2 dir = to / Mathf.Max(0.0001f, d);
            float speed = moveSpeed * SpeedMult;
            if (IsClone)
            {
                BossAI m = Main;    // เร็วกว่าบอสหลัก 25% เสมอ ตามความเร็วปัจจุบันของบอสหลัก
                if (m != null) speed = m.moveSpeed * m.SpeedMult * cloneSpeedMult;
            }

            if (!IsClone && d > 5.5f)
                speed *= 1f + 0.35f * Mathf.Clamp01(arrowPressure / 4f) * adaptStrength;

            if (!IsClone && d > strafeMinDist && d < strafeMaxDist)
            {
                Vector2 tangent = new Vector2(-dir.y, dir.x) * strafeSign;
                dir = (dir + tangent * strafeWeight).normalized;
            }

            if (d < stopDistance) speed = 0f;
            rb.linearVelocity = dir * speed;     // Unity เก่ากว่า 6 ใช้ rb.velocity
        }
        else if (State == BossState.Attack && CurrentMove == BossMove.Dash)
        {
            rb.linearVelocity = dashDir * dashSpeed;
        }
        else if (State == BossState.Attack && CurrentMove == BossMove.Interrupt && !interruptHit)
        {
            rb.linearVelocity = lungeDir * interruptSpeed;
        }
        else if (State == BossState.CounterDodge)
        {
            rb.linearVelocity = Vector2.zero;
            float k = 1f - Mathf.Clamp01(stateTimer / Mathf.Max(0.01f, dodgeTime));
            rb.MovePosition(Vector2.Lerp(dodgeFrom, dodgeTo, k));
            FX.Burst(rb.position, new Color(0.7f, 0.4f, 1f, 0.8f), 1, 1f, 0.35f, 0.25f);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    // ================= เรียนรู้นิสัยผู้เล่น =================
    void TrackPlayerHabits(float dt)
    {
        if (IsClone) return;

        habitTimer += dt;
        if (habitTimer >= 0.5f)
        {
            habitTimer = 0f;
            float d = DistanceToPlayer();
            BossMemory.Range = Mathf.Lerp(BossMemory.Range, d > 5.5f ? 1f : 0f, 0.03f);
            BossMemory.Aggro = Mathf.Lerp(BossMemory.Aggro, d < 2.8f ? 1f : 0f, 0.03f);
        }

        if (playerRb != null)
            playerVelSmooth = Vector2.Lerp(playerVelSmooth, playerRb.linearVelocity, 1f - Mathf.Exp(-8f * dt));

        float windupFrac = State == BossState.Windup
            ? Mathf.Clamp01((Time.time - windupStartTime) / Mathf.Max(0.05f, currentWindup))
            : -1f;

        bool dashing = playerMovement != null && playerMovement.IsDashing;
        if (dashing && !wasPlayerDashing && windupFrac >= 0f)
        {
            BossMemory.Dash = Mathf.Lerp(BossMemory.Dash, 1f, 0.3f);
            dashEarly = Mathf.Lerp(dashEarly, windupFrac < 0.6f ? 1f : 0f, 0.35f);
        }
        if (!dashing && wasPlayerDashing) lastPlayerDashEnd = Time.time;
        wasPlayerDashing = dashing;

        GuardState gs = playerParry != null ? playerParry.CurrentState : GuardState.None;

        bool parrying = gs == GuardState.Parry;
        if (parrying && !wasPlayerParrying)
        {
            if (windupFrac >= 0f)
                parryEarly = Mathf.Lerp(parryEarly, windupFrac < 0.55f ? 1f : 0f, 0.3f);
            else if (State != BossState.Attack)
                parryEarly = Mathf.Lerp(parryEarly, 1f, 0.15f);
        }
        wasPlayerParrying = parrying;

        arrowPressure *= Mathf.Exp(-dt / 6f);
        bool shooting = playerArrow != null && playerArrow.IsShooting;
        if (shooting && !wasPlayerShooting)
        {
            BossMemory.Range = Mathf.Lerp(BossMemory.Range, 1f, 0.2f);
            arrowPressure += 1f;
        }
        wasPlayerShooting = shooting;

        // ผู้เล่นกดการ์ดถี่ → Guard Break
        if (gs != GuardState.None && prevPlayerGuard == GuardState.None) guardPresses.Add(Time.time);
        prevPlayerGuard = gs;
        guardPresses.RemoveAll(t => Time.time - t > guardSpamWindow);
        guardSpam = guardPresses.Count >= guardSpamPresses;

        // ผู้เล่นไม่โจมตีนาน → Zone Blast
        bool attacking = playerSword != null && playerSword.IsAttacking;
        if (attacking && !wasPlayerAttacking) lastAggroTime = Time.time;
        wasPlayerAttacking = attacking;
        kiting = Time.time - lastAggroTime > kiteTime;
    }

    HitResult RecordResultAndReturn(HitResult r, bool unblockable)
    {
        RecordResult(r, unblockable);
        return r;
    }

    void RecordResult(HitResult r, bool unblockable)
    {
        if (IsClone) return;

        if (r == HitResult.Ignored)
        {
            if (playerMovement != null && playerMovement.IsInvincibleDash)
            {
                BossMemory.Dash = Mathf.Lerp(BossMemory.Dash, 1f, 0.35f);
                RecordMoveEffect(CurrentMove, -0.5f);
            }
            return;
        }

        if (!unblockable)
        {
            float g = r == HitResult.Parried ? 1f : (r == HitResult.Blocked ? 0.7f : 0f);
            BossMemory.Guard = Mathf.Lerp(BossMemory.Guard, g, 0.3f);
        }

        if (r == HitResult.Hurt || r == HitResult.Dead)
            BossMemory.Dash = Mathf.Lerp(BossMemory.Dash, 0f, 0.1f);

        float eff = (r == HitResult.Hurt || r == HitResult.Dead) ? 1f
                  : (r == HitResult.Blocked ? 0.2f : -1f);
        RecordMoveEffect(CurrentMove, eff);
    }

    static void RecordMoveEffect(BossMove m, float value)
    {
        int i = (int)m;
        moveEffect[i] = Mathf.Lerp(moveEffect[i], value, 0.25f);
    }

    public static void ResetProfile()
    {
        for (int i = 0; i < moveEffect.Length; i++) moveEffect[i] = 0f;
        dashEarly = 0f;
        parryEarly = 0f;
    }

    // ================= Utility AI =================
    float ScoreMove(BossMove m, float d)
    {
        if (IsClone) return m == BossMove.Slash && d <= attackRange ? 1f + Random.Range(0f, noiseAmount) : 0f;

        float guard = BossMemory.Guard, dash = BossMemory.Dash, range = BossMemory.Range, aggro = BossMemory.Aggro;
        float a = adaptStrength;

        bool playerCharging = playerSword != null && playerSword.IsCharging;
        bool playerBlocking = playerParry != null && playerParry.CurrentState == GuardState.Block;
        float stamina01 = playerStamina != null ? playerStamina.Current / Mathf.Max(1f, playerStamina.Max) : 1f;
        bool exhausted = stamina01 < 0.25f;
        int hits = playerHealth != null ? playerHealth.CurrentHits : 4;
        bool justDashed = Time.time - lastPlayerDashEnd < 0.6f;
        float ranged = Mathf.Clamp01(arrowPressure / 3f);

        float s = 0f;

        switch (m)
        {
            case BossMove.Slash:
                if (d > attackRange) return 0f;
                s = 1.0f - 0.5f * guard * a;
                if (playerCharging) s += 0.5f;
                if (justDashed) s += 0.3f * a;
                break;

            case BossMove.ChargeSlash:
                if (Time.time < nextChargeTime || d > chargeRange) return 0f;
                s = 0.7f + 0.1f * Phase + 0.7f * guard * a;
                if (playerBlocking) s += 0.45f;
                if (exhausted) s += 0.25f;
                s += 0.3f * parryEarly * a;
                break;

            case BossMove.GroundSlam:
                if (!Unlocked(slamUnlockEvo) || Time.time < nextSlamTime || d > slamTriggerRange) return 0f;
                s = 0.65f + 0.7f * dash * a + 0.4f * aggro * a;
                s += 0.3f * dashEarly * a;
                break;

            case BossMove.Shoot:
                if (!Unlocked(shootUnlockEvo) || Time.time < nextShootTime || d < shootMinRange) return 0f;
                s = (d >= 6f ? 0.95f : 0.55f) + 0.7f * range * a;
                s += 0.3f * ranged * a;
                if (playerCharging) s += 0.2f;
                break;

            case BossMove.Clone:
                if (!Unlocked(cloneUnlockEvo) || Time.time < nextCloneTime || AliveClones() >= cloneCount || d < 3f) return 0f;
                s = 0.5f + (CurrentHealth < maxHealth * 0.6f ? 0.3f : 0f);
                break;

            case BossMove.Dash:
                if (!enableDash) return 0f;
                s = d < 2.5f ? 0.45f : (d <= 9f ? 0.8f : 0.5f);
                s += 0.1f * Phase;
                break;

            default:
                return 0f;      // ท่าสวนกลับและท่าตอบโต้นิสัยใช้ตามเงื่อนไขกระตุ้น ไม่ผ่านการให้คะแนน
        }

        if (s <= 0f) return 0f;

        s += moveEffect[(int)m] * 0.35f * a;

        if ((int)m == lastMoveIdx) s -= repeatPenalty;
        else if ((int)m == prevMoveIdx) s -= repeatPenalty2;

        if (hits <= 1 && (m == BossMove.ChargeSlash || m == BossMove.GroundSlam)) s += 0.15f;

        s += Random.Range(0f, noiseAmount);
        return Mathf.Max(0f, s);
    }

    bool ForcedReady(float d)
    {
        switch (forcedMove)
        {
            case BossMove.Slash: return d <= attackRange;
            case BossMove.ChargeSlash: return d <= chargeRange;
            case BossMove.GroundSlam: return d <= slamTriggerRange;
            case BossMove.Shoot: return d >= shootMinRange;
            case BossMove.Clone: return AliveClones() < cloneCount;
            case BossMove.GuardBreak: return d <= guardBreakRange;
            case BossMove.Interrupt: return true;
            case BossMove.ZoneBlast: return true;
            default: return rangedTimer <= 0f;
        }
    }

    // ท่าตอบโต้นิสัยผู้เล่น (ไม่ผ่านการหน่วงคิด) คืน true ถ้าเริ่มท่าแล้ว
    bool TrySpecialResponse(float d)
    {
        // Anti-Charge: ผู้เล่นกำลังชาร์จ → พุ่งขัดจังหวะ
        if (playerSword != null && playerSword.IsCharging && playerSword.ChargeProgress >= interruptChargeMin
            && Time.time >= nextInterruptTime && d <= interruptRange)
        {
            StartMove(BossMove.Interrupt);
            return true;
        }

        // Anti-Guard: กดการ์ดถี่ → ทุ่มทะลุการ์ด
        if (guardSpam && Time.time >= nextGuardBreakTime && d <= guardBreakRange)
        {
            guardPresses.Clear();
            StartMove(BossMove.GuardBreak);
            return true;
        }

        // Anti-Kiting: ไม่โจมตีบอสนาน → คลื่นพลัง หรือยิงกระสุน
        if (kiting && Time.time >= nextZoneTime && d >= 3f)
        {
            nextZoneTime = Time.time + zoneCooldown * Mathf.Pow(0.85f, Evolution);
            bool useShoot = Evolution >= shootUnlockEvo && Time.time >= nextShootTime && Random.value < 0.5f;
            StartMove(useShoot ? BossMove.Shoot : BossMove.ZoneBlast);
            return true;
        }

        return false;
    }

    void DecideMove(float dt)
    {
        float d = DistanceToPlayer();

        if (!IsClone && !useForcedMove && TrySpecialResponse(d)) return;

        if (thinkTimer > 0f) { thinkTimer -= dt; return; }

        if (useForcedMove)
        {
            if (ForcedReady(d)) StartMove(forcedMove);
            return;
        }

        bool canDecide = d <= chargeRange || (enableDash && rangedTimer <= 0f) || d <= slamTriggerRange || d >= shootMinRange;
        if (!canDecide) return;

        BossMove best = BossMove.Slash;
        float bestScore = -1f;

        foreach (BossMove m in System.Enum.GetValues(typeof(BossMove)))
        {
            float s = ScoreMove(m, d);
            if (s > bestScore) { bestScore = s; best = m; }
        }

        if (bestScore < decisionThreshold) return;

        StartMove(best);
    }

    void StartMove(BossMove move)
    {
        CurrentMove = move;

        prevMoveIdx = lastMoveIdx;
        lastMoveIdx = (int)move;

        float cd = Mathf.Pow(0.85f, Evolution);
        switch (move)
        {
            case BossMove.ChargeSlash: nextChargeTime = Time.time + chargeCooldown * cd; break;
            case BossMove.GroundSlam: nextSlamTime = Time.time + slamCooldown * cd; break;
            case BossMove.Shoot: nextShootTime = Time.time + shootCooldown * cd; break;
            case BossMove.Clone: nextCloneTime = Time.time + cloneCooldown; break;
            case BossMove.GuardBreak: nextGuardBreakTime = Time.time + guardBreakCooldown * cd; break;
            case BossMove.Interrupt: nextInterruptTime = Time.time + interruptCooldown * cd; break;
            case BossMove.ZoneBlast: nextZoneTime = Time.time + zoneCooldown * cd; break;
        }

        if (move == BossMove.Dash)
            dashDir = ((Vector2)player.position - (Vector2)transform.position).normalized;

        ChangeState(BossState.Windup);
    }

    float BaseWindup(BossMove m)
    {
        switch (m)
        {
            case BossMove.ChargeSlash: return chargeWindup;
            case BossMove.GroundSlam: return slamWindup;
            case BossMove.Shoot: return shootWindup;
            case BossMove.Clone: return cloneWindup;
            case BossMove.Dash: return dashWindup;
            case BossMove.Riposte: return riposteWindup;
            case BossMove.HyperTrade: return tradeWindup;
            case BossMove.BackStab: return backstabWindup;
            case BossMove.GuardBreak: return guardBreakWindup;
            case BossMove.Interrupt: return interruptWindup;
            case BossMove.ZoneBlast: return zoneWindup;
            default: return slashWindup;
        }
    }

    float MinWindupFor(BossMove m)
    {
        switch (m)
        {
            case BossMove.ChargeSlash: return chargeMinWindup;
            case BossMove.GroundSlam: return 0.6f;
            case BossMove.Clone: return 0.7f;
            case BossMove.Riposte: return 0.1f;
            case BossMove.HyperTrade: return 0.5f;
            case BossMove.BackStab: return 0.3f;
            case BossMove.GuardBreak: return 0.6f;
            case BossMove.Interrupt: return 0.25f;
            case BossMove.ZoneBlast: return 0.7f;
            default: return minWindup;
        }
    }

    float GetWindupTime()
    {
        float w = BaseWindup(CurrentMove);

        // ท่าสวนกลับและขัดจังหวะมีเวลาเตือนตายตัว ไม่ถูกย่อตามวิวัฒนาการ ผู้เล่นต้องอ่านทันเสมอ
        bool fixedTime = CurrentMove == BossMove.Riposte || CurrentMove == BossMove.BackStab || CurrentMove == BossMove.Interrupt;
        if (!fixedTime) w *= WindupMult;

        if (CurrentMove == BossMove.Slash)
        {
            if (!IsClone)
                w += Random.Range(0f, windupJitterMax) * (0.3f + (BossMemory.Guard + 0.6f * parryEarly) * adaptStrength);

            if (comboWindup) w *= 0.65f;
        }
        comboWindup = false;

        return Mathf.Max(MinWindupFor(CurrentMove), w);
    }

    float GetActiveTime()
    {
        switch (CurrentMove)
        {
            case BossMove.ChargeSlash: return chargeActiveTime;
            case BossMove.GroundSlam: return slamActiveTime;
            case BossMove.Shoot: return shootActiveTime;
            case BossMove.Clone: return cloneActiveTime;
            case BossMove.Dash: return dashDuration;
            case BossMove.Riposte: return riposteActive;
            case BossMove.HyperTrade: return tradeActive;
            case BossMove.BackStab: return backstabActive;
            case BossMove.GuardBreak: return guardBreakActive;
            case BossMove.Interrupt: return interruptLungeTime;
            case BossMove.ZoneBlast: return zoneActive;
            default: return slashActiveTime;
        }
    }

    float GetRecoverTime()
    {
        float baseT;
        switch (CurrentMove)
        {
            case BossMove.ChargeSlash: baseT = chargeRecover; break;
            case BossMove.GroundSlam: baseT = slamRecover; break;
            case BossMove.Shoot: baseT = shootRecover; break;
            case BossMove.Clone: baseT = cloneRecover; break;
            case BossMove.Dash: baseT = dashRecover; break;
            case BossMove.Riposte: baseT = riposteRecover; break;
            case BossMove.HyperTrade: baseT = tradeRecover; break;
            case BossMove.BackStab: baseT = backstabRecover; break;
            case BossMove.GuardBreak: baseT = guardBreakRecover; break;
            case BossMove.Interrupt: baseT = interruptRecover; break;
            case BossMove.ZoneBlast: baseT = zoneRecover; break;
            default: baseT = slashRecover; break;
        }
        return baseT * RecoverMult;
    }

    bool ShouldCombo()
    {
        if (IsClone || player == null) return false;
        float chance = comboChanceBase + comboChancePerEvo * Evolution;
        return Random.value < chance && DistanceToPlayer() <= attackRange * 1.25f;
    }

    // ================= Counter (สวนกลับ) =================
    void RecordPlayerHit()
    {
        hitTimes.Add(Time.time);
        hitTimes.RemoveAll(t => Time.time - t > counterWindow);
        if (hitTimes.Count >= counterHitsNeeded) RequestCounter();
    }

    void RegisterStun()
    {
        if (IsClone) return;
        stunTimes.Add(Time.time);
        stunTimes.RemoveAll(t => Time.time - t > 8f);
        if (stunTimes.Count >= counterStunsNeeded) RequestCounter();
    }

    void RequestCounter()
    {
        if (IsClone || counterPending || Time.time < nextCounterTime) return;

        counterPending = true;
        counterPendingUntil = Time.time + 1.5f;
        hitTimes.Clear();
        stunTimes.Clear();
    }

    void TryStartCounter()
    {
        if (Time.time > counterPendingUntil) { counterPending = false; return; }

        bool slashWindupEarly = State == BossState.Windup && CurrentMove == BossMove.Slash
                                && (Time.time - windupStartTime) < currentWindup * 0.6f;
        if (State != BossState.Chase && State != BossState.Recover && !slashWindupEarly) return;

        counterPending = false;
        ActiveCounter = PickCounter(DistanceToPlayer());
        lastCounter = (int)ActiveCounter;
        comboNext = false;
        ChangeState(BossState.CounterWarn);
    }

    CounterKind PickCounter(float d)
    {
        float hp = (float)CurrentHealth / Mathf.Max(1, maxHealth);
        float[] s = new float[3];
        s[0] = 1.0f + 0.4f * BossMemory.Aggro * adaptStrength;                                   // Parry & Counter
        s[1] = 0.7f + (hp < 0.4f ? 0.6f : 0f) + 0.25f * Evolution;                                // Hyper-Armor Trade
        s[2] = 0.8f + (d < 3f ? 0.3f : 0f) + 0.4f * BossMemory.Guard * adaptStrength;             // Perfect Dodge
        if (lastCounter >= 0) s[lastCounter] -= 0.4f;

        int best = 0;
        float bestV = -999f;
        for (int i = 0; i < 3; i++)
        {
            float v = s[i] + Random.Range(0f, 0.3f);
            if (v > bestV) { bestV = v; best = i; }
        }
        return (CounterKind)best;
    }

    void BeginCounter()
    {
        switch (ActiveCounter)
        {
            case CounterKind.ParryCounter:
                ChangeState(BossState.CounterStance);
                break;

            case CounterKind.HyperTrade:
                CurrentMove = BossMove.HyperTrade;
                ChangeState(BossState.Windup);
                break;

            case CounterKind.PerfectDodge:
                ChangeState(BossState.CounterDodge);
                break;
        }
    }

    void CounterExecuted()
    {
        float cd = Random.Range(counterCooldownMin, counterCooldownMax) * Mathf.Pow(0.9f, Evolution);
        nextCounterTime = Time.time + cd;
    }

    // ผู้เล่นตีเข้ามาตอนบอสตั้งท่ารับ → ไม่เสียเลือด แล้วฟันสวนทันที
    void OnStanceHit()
    {
        Vector2 p = transform.position;
        Color cyan = new Color(0.3f, 0.9f, 1f, 0.95f);
        FX.Ring(p, cyan, 0.4f, 2.8f, 0.3f);
        FX.Burst(p, cyan, 14, 7f, 0.35f, 0.16f);
        FX.Shake(0.15f, 0.15f);
        FX.HitStop(0.08f);
        FX.Popup(transform.position + Vector3.up * 1.8f, "COUNTER!", cyan);

        CurrentMove = BossMove.Riposte;
        ChangeState(BossState.Windup);
    }

    void PrepareDodge()
    {
        if (col) col.enabled = false;

        dodgeFrom = rb.position;
        Vector2 pp = player.position;
        Vector2 dir = (pp - dodgeFrom).normalized;
        float dist = dodgeBehindDist;

        // ไม่ให้ทะลุกำแพงด้านหลังผู้เล่น
        RaycastHit2D[] hits = Physics2D.RaycastAll(pp, dir, dist + 0.6f);
        foreach (var h in hits)
        {
            Collider2D c = h.collider;
            if (c == null || c.isTrigger) continue;
            if (c.GetComponentInParent<PlayerHealth>() != null || c.GetComponentInParent<BossAI>() != null) continue;
            dist = Mathf.Max(0.5f, h.distance - 0.6f);
            break;
        }

        dodgeTo = pp + dir * dist;
        FX.Ring(dodgeFrom, new Color(0.7f, 0.4f, 1f, 0.9f), 0.3f, 1.6f, 0.25f);
    }

    void StunPlayer(float duration)
    {
        if (player == null) return;
        PlayerStatus.Stun(duration);
        if (playerSword != null) playerSword.CancelCharge();
        FX.Popup(player.position + Vector3.up * 1.4f, "STUN", new Color(1f, 0.9f, 0.3f));
    }

    void KnockPlayer(float force, float duration)
    {
        if (player == null) return;
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        PlayerStatus.Knockback(dir * force, duration);
    }

    // ================= State Machine =================
    void ChangeState(BossState next)
    {
        State = next;

        switch (next)
        {
            case BossState.Idle:
                stateTimer = roaring ? roarTime : idleTime;
                break;

            case BossState.Chase:
                if (pendingPhase)
                {
                    pendingPhase = false;
                    Phase = targetPhase;
                    roaring = true;
                    Debug.Log("Boss เข้าสู่เฟส " + (Phase + 1));
                    ChangeState(BossState.Idle);
                    return;
                }
                stateTimer = 0f;
                thinkTimer = Random.Range(thinkDelayRange.x, thinkDelayRange.y) * Mathf.Pow(0.9f, Evolution);
                rangedTimer = Random.Range(rangedDelayRange.x, rangedDelayRange.y);
                break;

            case BossState.Windup:
                currentWindup = GetWindupTime();
                currentActive = GetActiveTime();
                stateTimer = currentWindup;
                windupStartTime = Time.time;
                punishDodge = false;
                if (CurrentMove == BossMove.BackStab && col) { col.enabled = true; IgnoreOtherBosses(); }
                break;

            case BossState.Attack:
                currentActive = GetActiveTime();
                stateTimer = currentActive;
                attackStartTime = Time.time;
                slamSecondDone = false;
                if (CurrentMove == BossMove.Dash) dashHitDone = false;
                else DoMeleeImpact();
                break;

            case BossState.Recover:
                stateTimer = GetRecoverTime();
                if (CurrentMove == BossMove.Slash && (punishDodge || ShouldCombo()))
                {
                    comboNext = true;
                    stateTimer = punishDodge ? 0.1f : 0.15f;
                }
                punishDodge = false;
                break;

            case BossState.Stunned:
                stateTimer = stunTime;
                comboNext = false;
                RegisterStun();
                break;

            case BossState.Staggered:
                stateTimer = staggerTime;
                comboNext = false;
                roaring = false;
                break;

            case BossState.Deflect:
                stateTimer = deflectDuration;
                comboNext = false;
                break;

            case BossState.CounterWarn:
                stateTimer = counterWarnTime;
                break;

            case BossState.CounterStance:
                stateTimer = parryStanceTime;
                break;

            case BossState.CounterDodge:
                stateTimer = dodgeTime;
                PrepareDodge();
                break;

            case BossState.Dead:
                OnDeath();
                break;
        }
    }

    // ================= ท่าโจมตี =================
    void DoMeleeImpact()
    {
        ImpactCount++;
        Vector2 pos = transform.position;
        Vector2 toPlayer = player != null ? ((Vector2)player.position - pos).normalized : Vector2.right;

        switch (CurrentMove)
        {
            case BossMove.Slash:
                FX.Burst(pos + toPlayer * 1.5f, Color.white, 8, 5f, 0.25f, 0.12f);
                if (playerHealth != null && DistanceToPlayer() <= hitRadius)
                    ApplyHit(false, 1, false);
                else if (!IsClone && playerHealth != null)
                    RecordMoveEffect(BossMove.Slash, -0.3f);
                break;

            case BossMove.ChargeSlash:
                FX.Ring(pos, new Color(1f, 0.15f, 0.1f, 0.9f), 0.5f, chargeHitRadius, 0.3f);
                FX.Burst(pos + toPlayer * 1.5f, new Color(1f, 0.3f, 0.2f), 16, 7f, 0.4f, 0.2f);
                FX.Shake(0.2f, 0.2f);
                if (playerHealth != null && DistanceToPlayer() <= chargeHitRadius)
                    ApplyHit(true, chargeHitDamage, false);
                else if (!IsClone && playerHealth != null)
                    RecordMoveEffect(BossMove.ChargeSlash, -0.3f);
                break;

            case BossMove.GroundSlam:
                SlamWave();
                break;

            case BossMove.Shoot:
                FireBullets();
                break;

            case BossMove.Clone:
                SpawnClones();
                break;

            case BossMove.Riposte:
                FX.Burst(pos + toPlayer * 1.5f, new Color(0.3f, 0.9f, 1f), 14, 7f, 0.3f, 0.16f);
                FX.Shake(0.15f, 0.15f);
                if (playerHealth != null && DistanceToPlayer() <= riposteHitRadius)
                {
                    HitResult r = ApplyHit(false, 1, false);
                    if (r == HitResult.Hurt) StunPlayer(riposteStun);      // โดนสวนกลับ → ชะงัก
                }
                CounterExecuted();
                break;

            case BossMove.HyperTrade:
                {
                    Color gold = new Color(1f, 0.8f, 0.2f, 0.95f);
                    FX.Ring(pos, gold, 0.5f, tradeRadius, 0.35f);
                    FX.Burst(pos, gold, 22, 8f, 0.5f, 0.22f);
                    FX.Shake(0.3f, 0.3f);
                    if (playerHealth != null && DistanceToPlayer() <= tradeRadius)
                    {
                        HitResult r = ApplyHit(false, 1, false);
                        if (r != HitResult.Ignored && r != HitResult.Dead) KnockPlayer(tradePush, tradePushTime);   // ผลักออกไป
                    }
                    CounterExecuted();
                    break;
                }

            case BossMove.BackStab:
                FX.Burst(pos + toPlayer * 1.2f, new Color(0.8f, 0.4f, 1f), 14, 7f, 0.3f, 0.16f);
                FX.Shake(0.15f, 0.15f);
                if (playerHealth != null && DistanceToPlayer() <= backstabRadius)
                    ApplyHit(false, 1, true);                               // แทงจากด้านหลัง ทะลุการ์ด แดชหลบได้
                CounterExecuted();
                break;

            case BossMove.GuardBreak:
                FX.Ring(pos, new Color(1f, 0.55f, 0.1f, 0.95f), 0.5f, guardBreakRadius, 0.3f);
                FX.Burst(pos + toPlayer * 1.3f, new Color(1f, 0.6f, 0.2f), 18, 7f, 0.4f, 0.2f);
                FX.Shake(0.25f, 0.25f);
                if (playerHealth != null && DistanceToPlayer() <= guardBreakRadius)
                {
                    HitResult r = ApplyHit(false, 1, true);                 // ทะลุการ์ดและ Parry
                    if (r == HitResult.Hurt && playerStamina != null)
                    {
                        playerStamina.Exhaust();                            // การ์ดแตก Stamina หมด
                        FX.Popup(player.position + Vector3.up * 1.4f, "GUARD BREAK!", new Color(1f, 0.6f, 0.2f));
                    }
                }
                break;

            case BossMove.Interrupt:
                lungeDir = player != null ? ((Vector2)player.position - (Vector2)transform.position).normalized : Vector2.right;
                interruptHit = false;
                FX.Burst(pos, new Color(1f, 1f, 1f), 10, 6f, 0.25f, 0.14f);
                break;

            case BossMove.ZoneBlast:
                FX.Ring(pos, new Color(0.8f, 0.35f, 1f, 0.9f), 0.5f, 2f, 0.3f);
                FX.Shake(0.2f, 0.18f);
                BossWave.Spawn(pos, waveSpeed, waveMaxRadius);
                break;
        }
    }

    void SlamWave()
    {
        ImpactCount++;
        Vector2 pos = transform.position;

        FX.Ring(pos, new Color(1f, 0.6f, 0.2f, 0.95f), 0.5f, slamRadius, 0.35f);
        FX.Burst(pos, new Color(0.9f, 0.7f, 0.4f), 18, 6f, 0.5f, 0.22f);
        FX.Shake(0.25f, 0.25f);

        if (playerHealth != null && DistanceToPlayer() <= slamRadius)
            ApplyHit(false, slamDamage, true);
        else if (!IsClone && playerHealth != null)
            RecordMoveEffect(BossMove.GroundSlam, -0.3f);
    }

    void FireBullets()
    {
        int count = Mathf.Min(7, bulletCount + Evolution);
        float spread = bulletSpread * (1f + 0.15f * Evolution);

        Vector2 target = player.position;
        if (playerRb != null)
        {
            float dist = DistanceToPlayer();
            float lead = leadFactor * Mathf.Clamp01(0.4f + 0.3f * Evolution);
            target += playerVelSmooth * (dist / Mathf.Max(0.1f, bulletSpeed)) * lead;
        }

        Vector2 origin = transform.position;
        Vector2 toTarget = (target - origin).normalized;
        float baseAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;

        for (int i = 0; i < count; i++)
        {
            float offset = count == 1 ? 0f : Mathf.Lerp(-spread, spread, i / (float)(count - 1));
            float rad = (baseAngle + offset) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            BossBullet.Spawn(origin + dir * 1f, dir, bulletSpeed, this);
        }

        FX.Burst(origin + toTarget, new Color(1f, 0.4f, 0.15f), 8, 4f, 0.25f, 0.14f);
    }

    int AliveClones()
    {
        int n = 0;
        foreach (var b in All)
            if (b != null && b.IsClone && b.State != BossState.Dead) n++;
        return n;
    }

    void SpawnClones()
    {
        int alive = AliveClones();

        for (int i = alive; i < cloneCount; i++)
        {
            Vector2 off = (i % 2 == 0 ? Vector2.left : Vector2.right) * 2.5f + Random.insideUnitCircle * 0.8f;
            Vector2 pos = (Vector2)transform.position + off;

            GameObject go = Instantiate(gameObject, pos, Quaternion.identity);
            BossAI clone = go.GetComponent<BossAI>();
            int hp = Mathf.Max(1, Mathf.RoundToInt(maxHealth / Random.Range(cloneHealthDivisorMin, cloneHealthDivisorMax)));
            clone.InitAsClone(this, hp);

            FX.Ring(pos, new Color(1f, 1f, 1f, 0.9f), 0.3f, 1.8f, 0.4f);
            FX.Burst(pos, Color.white, 12, 4f, 0.5f, 0.18f);
        }
    }

    HitResult ApplyHit(bool unblockable, int dmg, bool ignoreGuard)
    {
        if (playerHealth == null) return HitResult.Ignored;

        HitResult result = playerHealth.TakeHit(unblockable, dmg, ignoreGuard);
        Debug.Log("Boss " + CurrentMove + (IsClone ? " (clone)" : "") + " → " + result);

        RecordResult(result, unblockable);

        if (!IsClone && result == HitResult.Ignored && CurrentMove == BossMove.Slash
            && playerMovement != null && playerMovement.IsInvincibleDash)
            punishDodge = Random.value < 0.4f + 0.4f * dashEarly * adaptStrength;

        if (result == HitResult.Parried)
        {
            AddPosture(postureParry);
            if (State != BossState.Staggered && State != BossState.Dead)
                ChangeState(BossState.Stunned);
        }

        return result;
    }

    // ================= รับดาเมจ / Posture =================
    public void TakeDamage(int damage)
    {
        ReceiveHit(damage, HitType.Other);
    }

    public void ReceiveHit(int damage, HitType type)
    {
        if (!IsTargetable) return;
        if (State == BossState.CounterDodge) return;                     // กำลังหลบ อมตะ
        if (State == BossState.CounterStance) { OnStanceHit(); return; } // ตั้งท่ารับ ไม่เสียเลือด แล้วสวนกลับ

        bool staggered = State == BossState.Staggered;
        int dmg = damage <= 0 ? 0 : Mathf.Max(1, Mathf.CeilToInt(damage * (staggered ? staggerDamageMult : 1f)));

        Vector2 hitPos = (Vector2)transform.position + Random.insideUnitCircle * 0.5f;
        switch (type)
        {
            case HitType.Charge:
                FX.Burst(hitPos, new Color(1f, 0.85f, 0.3f), 14, 7f, 0.35f, 0.18f);
                FX.Ring(hitPos, new Color(1f, 0.9f, 0.5f, 0.9f), 0.2f, 1.6f, 0.25f);
                FX.Shake(0.15f, 0.15f);
                FX.HitStop(0.06f);
                break;
            case HitType.Arrow:
                FX.Burst(hitPos, new Color(0.8f, 1f, 1f), 8, 5f, 0.3f, 0.14f);
                break;
            case HitType.Reflected:
                FX.Burst(hitPos, new Color(1f, 0.9f, 0.3f), 12, 6f, 0.35f, 0.16f);
                FX.Shake(0.12f, 0.12f);
                break;
            default:
                FX.Burst(hitPos, Color.white, 6, 4.5f, 0.25f, 0.12f);
                break;
        }
        if (staggered) FX.Popup(transform.position + Vector3.up * 1.5f, "x" + staggerDamageMult.ToString("0.#"), new Color(1f, 0.9f, 0.3f));

        if (!IsClone)
        {
            lastAggroTime = Time.time;
            if (type == HitType.Slash) RecordPlayerHit();

            if (type == HitType.Arrow)
            {
                arrowStreak++;
                lastArrowHitTime = Time.time;
            }

            float p = 0f;
            switch (type)
            {
                case HitType.Slash: p = postureSlash; break;
                case HitType.Charge: p = postureCharge; break;
                case HitType.Arrow: p = postureArrow; break;
                case HitType.Reflected: p = postureReflect; break;
            }
            AddPosture(p);
        }

        CurrentHealth -= dmg;
        if (CurrentHealth <= 0)
        {
            Die(true);
            return;
        }

        if (!IsClone)
        {
            float ratio = (float)CurrentHealth / maxHealth;
            int t = ratio <= phase3Threshold ? 2 : (ratio <= phase2Threshold ? 1 : 0);

            if (t > Phase && t > targetPhase)
            {
                targetPhase = t;
                pendingPhase = true;
                if (State == BossState.Chase) ChangeState(BossState.Chase);
            }
        }
    }

    void AddPosture(float amount)
    {
        if (IsClone || amount <= 0f) return;
        if (HyperArmor) return;                                          // เบ่งพลังอยู่ ไม่สะดุ้ง
        if (State == BossState.Staggered || State == BossState.Dead || State == BossState.Reviving) return;

        Posture = Mathf.Min(MaxPosture, Posture + amount);
        lastPostureGain = Time.time;

        if (Posture >= MaxPosture) EnterStagger();
    }

    void UpdatePosture(float dt)
    {
        if (IsClone || State == BossState.Staggered) return;

        if (Posture > 0f && Time.time - lastPostureGain > postureDecayDelay)
            Posture = Mathf.Max(0f, Posture - postureDecayRate * dt);
    }

    void EnterStagger()
    {
        Posture = MaxPosture;
        counterPending = false;
        ChangeState(BossState.Staggered);

        Vector2 pos = transform.position;
        FX.Popup(pos + Vector2.up * 2f, "STAGGER!", new Color(1f, 0.9f, 0.2f));
        FX.Ring(pos, new Color(1f, 0.9f, 0.3f, 0.95f), 0.5f, 3.5f, 0.4f);
        FX.Burst(pos, new Color(1f, 0.9f, 0.3f), 16, 6f, 0.5f, 0.2f);
        FX.Shake(0.25f, 0.2f);
        FX.HitStop(0.1f);
    }

    // ================= ฟันธนูทิ้ง =================
    void TryDeflect()
    {
        if (IsClone) return;
        if (State != BossState.Chase && State != BossState.Recover) return;
        if (arrowStreak <= 0) return;

        if (Time.time - lastArrowHitTime > arrowStreakReset) { arrowStreak = 0; return; }

        int need = arrowPressure > 2.5f ? Mathf.Max(1, deflectStreakNeeded - 1) : deflectStreakNeeded;
        if (arrowStreak < need || Time.time < nextDeflectTime) return;

        ArrowProjectile a = ArrowProjectile.FindIncoming(transform.position, deflectDetectRange);
        if (a == null) return;

        nextDeflectTime = Time.time + deflectCooldown;
        ChangeState(BossState.Deflect);
    }

    void UpdateDeflect()
    {
        ArrowProjectile a = ArrowProjectile.FindFlyingWithin(transform.position, deflectKillRadius);
        if (a != null)
        {
            Vector2 p = a.transform.position;
            a.DeflectByBoss();
            DeflectCount++;

            FX.Burst(p, new Color(0.9f, 1f, 1f), 12, 6f, 0.3f, 0.15f);
            FX.Ring(p, new Color(1f, 1f, 1f, 0.9f), 0.2f, 1.2f, 0.25f);
            FX.Shake(0.1f, 0.1f);
            nextDeflectTime = Time.time + deflectCooldown;
        }

        if (stateTimer <= 0f) ChangeState(BossState.Chase);
    }

    // ================= ตาย / ฟื้นคืนชีพ =================
    void Die(bool byPlayer)
    {
        if (State == BossState.Dead || State == BossState.Reviving) return;

        CurrentHealth = 0;
        counterPending = false;

        if (byPlayer && playerHealth != null)
            playerHealth.GainArmor();

        if (!IsClone)
        {
            BossMemory.TotalDeaths++;
            if (byPlayer) UpgradeData.OnBossKilled(transform.position);   // บอสตายหนึ่งชีวิต = ได้เหรียญ
            BossMemory.Save();

            if (revivesUsed < maxRevives) { revivesUsed++; willRevive = true; }
            else willRevive = false;

            KillAllClones();
        }
        else
        {
            willRevive = false;
        }

        ChangeState(BossState.Dead);
    }

    void KillAllClones()
    {
        foreach (var b in new List<BossAI>(All))
            if (b != null && b.IsClone && b.State != BossState.Dead) b.Die(false);
    }

    void OnDeath()
    {
        rb.linearVelocity = Vector2.zero;
        if (col) col.enabled = false;
        if (dashIndicator) dashIndicator.gameObject.SetActive(false);

        Vector2 c = transform.position;

        if (willRevive)
        {
            stateTimer = deathAnimTime + deathHoldTime;
            FX.Burst(c, new Color(1f, 0.3f, 0.2f), 24, 6f, 0.7f, 0.25f);
            FX.Ring(c, new Color(1f, 0.4f, 0.2f, 0.9f), 0.5f, 5f, 0.6f);
            FX.Shake(0.4f, 0.25f);
            FX.HitStop(0.12f);
        }
        else
        {
            FX.Burst(c, IsClone ? Color.white : new Color(1f, 0.5f, 0.2f), IsClone ? 14 : 36, 7f, 0.8f, 0.28f);
            FX.Ring(c, Color.white, 0.5f, IsClone ? 3f : 7f, 0.7f);
            FX.Shake(IsClone ? 0.15f : 0.6f, IsClone ? 0.12f : 0.35f);

            if (!IsClone)
            {
                FX.Flash(new Color(1f, 1f, 1f, 0.6f), 0.35f);
                Debug.Log("Boss defeated!");

                GameObject flow = GameObject.Find("StageFlow");
                if (flow != null) flow.SendMessage("OnBossDefeated", SendMessageOptions.DontRequireReceiver);
            }

            Destroy(gameObject, deathAnimTime + 0.6f);
        }
    }

    void UpdateDead(float dt)
    {
        if (!willRevive) return;

        stateTimer -= dt;
        if (stateTimer <= 0f) BeginRevive();
    }

    void BeginRevive()
    {
        State = BossState.Reviving;
        stateTimer = reviveAnimTime;

        Evolution++;
        maxHealth = Mathf.RoundToInt(maxHealth * (1f + reviveHealthGrowth));
        CurrentHealth = maxHealth;

        Phase = 0;
        targetPhase = 0;
        pendingPhase = false;
        Posture = 0f;
        arrowStreak = 0;
        comboNext = false;
        counterPending = false;
        nextCounterTime = 0f;

        Vector2 c = transform.position;
        FX.Ring(c, new Color(1f, 0.3f, 0.2f, 0.95f), 6f, 0.5f, reviveAnimTime);
        FX.Burst(c, new Color(1f, 0.4f, 0.2f), 20, 5f, 0.8f, 0.22f);
        FX.Shake(reviveAnimTime, 0.12f);
        Debug.Log("Boss ฟื้นคืนชีพ! วิวัฒนาการระดับ " + Evolution);
    }

    void UpdateReviving(float dt)
    {
        stateTimer -= dt;
        if (stateTimer > 0f) return;

        if (col) { col.enabled = true; IgnoreOtherBosses(); }
        roaring = true;

        FX.Ring(transform.position, new Color(1f, 0.9f, 0.5f, 0.95f), 0.5f, 8f, 0.6f);
        FX.Flash(new Color(1f, 0.3f, 0.2f, 0.35f), 0.3f);
        FX.Shake(0.4f, 0.3f);
        ChangeState(BossState.Idle);
    }

    // ================= UI =================
    void UpdateIndicator()
    {
        if (dashIndicator == null) return;

        bool show = enableDash && State == BossState.Windup && CurrentMove == BossMove.Dash;
        dashIndicator.gameObject.SetActive(show);
        if (!show) return;

        float length = dashSpeed * dashDuration;
        float angle = Mathf.Atan2(dashDir.y, dashDir.x) * Mathf.Rad2Deg;

        dashIndicator.position = (Vector2)transform.position + dashDir * (length / 2f);
        dashIndicator.rotation = Quaternion.Euler(0, 0, angle);
        dashIndicator.localScale = new Vector3(length, dashHitRadius * 2f, 1f);
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;

        if (IsClone) { DrawCloneBar(); return; }
        if (Main != this) return;

        float w = 420f;
        float x = (Screen.width - w) / 2f;

        // แถบเลือด
        GUI.color = Color.white;
        GUI.Box(new Rect(x, 15, w, 22), "");
        float ratio = (float)CurrentHealth / Mathf.Max(1, maxHealth);
        GUI.color = Color.red;
        GUI.DrawTexture(new Rect(x + 2, 17, (w - 4) * ratio, 18), Texture2D.whiteTexture);

        // ตัวเลขเลือดกลางแถบ
        if (hpStyle == null)
        {
            hpStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            hpStyle.normal.textColor = Color.white;
        }
        GUI.color = Color.white;
        GUI.Label(new Rect(x, 15, w, 22), CurrentHealth + " / " + maxHealth, hpStyle);

        // แถบ Posture
        float pr = Mathf.Clamp01(Posture / MaxPosture);
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(x, 41, w, 10), Texture2D.whiteTexture);
        bool hot = pr > 0.8f && Mathf.FloorToInt(Time.unscaledTime * 8f) % 2 == 0;
        GUI.color = State == BossState.Staggered ? new Color(1f, 0.55f, 0.1f) : (hot ? Color.white : new Color(1f, 0.85f, 0.2f));
        GUI.DrawTexture(new Rect(x + 1, 42, (w - 2) * pr, 8), Texture2D.whiteTexture);

        GUI.color = Color.white;
    }

    void DrawCloneBar()
    {
        if (State == BossState.Dead) return;
        Camera cam = Camera.main;
        var sr = GetComponent<SpriteRenderer>();
        if (cam == null || sr == null) return;

        Vector3 sp = cam.WorldToScreenPoint(new Vector3(sr.bounds.center.x, sr.bounds.max.y + 0.2f, 0f));
        float w = 70f;
        float x = sp.x - w / 2f;
        float y = Screen.height - sp.y;

        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(new Rect(x - 1, y - 1, w + 2, 8), Texture2D.whiteTexture);
        GUI.color = new Color(0.9f, 0.9f, 1f);
        GUI.DrawTexture(new Rect(x, y, w * Mathf.Clamp01((float)CurrentHealth / Mathf.Max(1, maxHealth)), 6), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, chargeRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, chargeHitRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, slamRadius);
    }
}