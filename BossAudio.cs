using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BossAudio : MonoBehaviour
{
    [Header("เดิน")]
    public AudioClip[] footstepClips;

    [Header("ฟันปกติ")]
    public AudioClip slashWindupClip;
    public AudioClip slashClip;

    [Header("ฟันชาร์จ")]
    public AudioClip chargeWindupClip;
    public AudioClip chargeSlashClip;

    [Header("ทุบพื้น")]
    public AudioClip slamWindupClip;
    public AudioClip slamClip;

    [Header("ยิงระยะไกล")]
    public AudioClip shootWindupClip;
    public AudioClip shootClip;

    [Header("แยกร่าง")]
    public AudioClip cloneWindupClip;
    public AudioClip cloneClip;

    [Header("สวนกลับ (เสียงเตือนล่วงหน้า 0.3 วินาที)")]
    public AudioClip counterWarnClip;
    public AudioClip counterStanceClip;     // ตั้งท่ารับ
    public AudioClip riposteClip;           // ฟันสวนกลับ
    public AudioClip tradeClip;             // Hyper-Armor Trade ปล่อยคลื่น
    public AudioClip dodgeClip;             // พุ่งหลบไปด้านหลัง
    public AudioClip backstabClip;          // แทงจากด้านหลัง

    [Header("ท่าตอบโต้นิสัย")]
    public AudioClip guardBreakClip;
    public AudioClip interruptClip;
    public AudioClip zoneClip;

    [Header("สถานะอื่น")]
    public AudioClip hurtClip;
    public AudioClip parriedClip;
    public AudioClip staggerClip;
    public AudioClip deflectClip;
    public AudioClip roarClip;
    public AudioClip deathClip;
    public AudioClip reviveClip;

    [Header("Volume")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0f, 1f)] public float cloneVolume = 0.5f;
    public float footstepInterval = 0.5f;
    public float pitchVariation = 0.05f;

    AudioSource src;
    BossAI boss;
    BossState lastState;
    int lastImpact, lastDeflect;
    int lastHealth = -1;
    bool wasRoar;
    float footTimer;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        boss = GetComponent<BossAI>();
    }

    void Start()
    {
        if (boss == null) return;
        lastState = boss.State;
        lastImpact = boss.ImpactCount;
        lastDeflect = boss.DeflectCount;
    }

    void Update()
    {
        if (boss == null) return;

        if (boss.State != lastState)
        {
            OnStateChanged(boss.State);
            lastState = boss.State;
        }

        if (boss.ImpactCount != lastImpact)
        {
            OnImpact();
            lastImpact = boss.ImpactCount;
        }

        if (boss.DeflectCount != lastDeflect)
        {
            Play(deflectClip);
            lastDeflect = boss.DeflectCount;
        }

        if (lastHealth < 0) lastHealth = boss.CurrentHealth;
        if (boss.CurrentHealth < lastHealth && boss.IsTargetable)
            Play(hurtClip);
        lastHealth = boss.CurrentHealth;

        bool roar = boss.IsRoaring;
        if (roar && !wasRoar) Play(roarClip);
        wasRoar = roar;

        if (boss.State == BossState.Chase)
        {
            footTimer -= Time.deltaTime;
            if (footTimer <= 0f)
            {
                if (footstepClips != null && footstepClips.Length > 0)
                    Play(footstepClips[Random.Range(0, footstepClips.Length)], 0.6f);
                footTimer = footstepInterval / Mathf.Max(0.5f, 1f + 0.2f * boss.Evolution);
            }
        }
        else
        {
            footTimer = 0f;
        }
    }

    void OnStateChanged(BossState s)
    {
        switch (s)
        {
            case BossState.Windup:
                switch (boss.CurrentMove)
                {
                    case BossMove.Slash:
                    case BossMove.Interrupt:
                        Play(slashWindupClip); break;
                    case BossMove.ChargeSlash:
                    case BossMove.HyperTrade:
                    case BossMove.GuardBreak:
                        Play(chargeWindupClip); break;
                    case BossMove.GroundSlam:
                    case BossMove.ZoneBlast:
                        Play(slamWindupClip); break;
                    case BossMove.Shoot: Play(shootWindupClip); break;
                    case BossMove.Clone: Play(cloneWindupClip); break;
                }
                break;

            case BossState.CounterWarn: Play(counterWarnClip); break;
            case BossState.CounterStance: Play(counterStanceClip); break;
            case BossState.CounterDodge: Play(dodgeClip); break;
            case BossState.Stunned: Play(parriedClip); break;
            case BossState.Staggered: Play(staggerClip); break;
            case BossState.Deflect: Play(slashClip); break;
            case BossState.Dead: Play(deathClip); break;
            case BossState.Reviving: Play(reviveClip); break;
        }
    }

    // จังหวะตีจริงของแต่ละท่า
    void OnImpact()
    {
        switch (boss.CurrentMove)
        {
            case BossMove.Slash: Play(slashClip); break;
            case BossMove.ChargeSlash: Play(chargeSlashClip); break;
            case BossMove.GroundSlam: Play(slamClip); break;
            case BossMove.Shoot: Play(shootClip); break;
            case BossMove.Clone: Play(cloneClip); break;
            case BossMove.Riposte: Play(riposteClip); break;
            case BossMove.HyperTrade: Play(tradeClip); break;
            case BossMove.BackStab: Play(backstabClip); break;
            case BossMove.GuardBreak: Play(guardBreakClip); break;
            case BossMove.Interrupt: Play(interruptClip); break;
            case BossMove.ZoneBlast: Play(zoneClip); break;
        }
    }

    void Play(AudioClip clip, float mult = 1f)
    {
        if (clip == null) return;
        float v = volume * mult * (boss != null && boss.IsClone ? cloneVolume : 1f);
        src.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        src.PlayOneShot(clip, v);
    }
}