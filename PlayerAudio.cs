using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudio : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip[] footstepClips;      // ใส่ได้หลายเสียง จะสุ่มสลับกัน
    public AudioClip slashClip;
    public AudioClip chargeSlashClip;      // ไม่ใส่ก็ได้ จะใช้เสียงฟันธรรมดาแทน
    public AudioClip hurtClip;
    public AudioClip deathClip;
    public AudioClip dashClip;
    public AudioClip bowDrawClip;          // ใหม่: เสียงง้างธนู (ตอนเริ่มกดการ์ดและมีธนู)
    public AudioClip bowShootClip;         // ใหม่: เสียงยิงธนู (ตอนธนูออกจากสาย)

    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float footstepVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Footsteps")]
    public float footstepInterval = 0.32f;
    public float slowStepMultiplier = 2f;
    public float pitchVariation = 0.06f;

    AudioSource src;
    PlayerSword sword;
    PlayerMovement movement;
    PlayerHealth health;
    PlayerParry parry;

    bool wasAttacking, wasDashing, wasDead;
    int lastHits = -1;
    float footTimer;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;

        sword = GetComponent<PlayerSword>();
        movement = GetComponent<PlayerMovement>();
        health = GetComponent<PlayerHealth>();
        parry = GetComponent<PlayerParry>();
    }

    void Update()
    {
        // ===== ตาย / เจ็บ =====
        if (health != null)
        {
            if (lastHits < 0) lastHits = health.CurrentHits;

            if (health.IsDead)
            {
                if (!wasDead) Play(deathClip, sfxVolume);
                wasDead = true;
                return;
            }

            if (health.CurrentHits < lastHits)
                Play(hurtClip, sfxVolume);
            lastHits = health.CurrentHits;
        }

        // ===== ฟัน =====
        bool attacking = sword != null && sword.IsAttacking;
        if (attacking && !wasAttacking)
        {
            AudioClip c = sword.LastAttackWasCharge && chargeSlashClip != null ? chargeSlashClip : slashClip;
            Play(c, sfxVolume);
        }
        wasAttacking = attacking;

        // ===== แดช =====
        bool dashing = movement != null && movement.IsDashing;
        if (dashing && !wasDashing)
            Play(dashClip, sfxVolume);
        wasDashing = dashing;

        // ===== เดิน (คีย์บอร์ด หรือจอยสติ๊กบนมือถือ) =====
        bool moving = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f
                   || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f
                   || MobileInput.HasMove;

        if (moving && !dashing)
        {
            footTimer -= Time.deltaTime;
            if (footTimer <= 0f)
            {
                PlayFootstep();
                bool slow = (sword != null && sword.IsCharging) || (parry != null && parry.IsGuarding);
                footTimer = footstepInterval * (slow ? slowStepMultiplier : 1f);
            }
        }
        else
        {
            footTimer = 0f;
        }
    }

    // PlayerArrow เรียกตอนง้างธนู
    public void PlayBowDraw()
    {
        Play(bowDrawClip, sfxVolume);
    }

    // PlayerArrow เรียกตอนธนูออกจากสาย
    public void PlayBowShoot()
    {
        Play(bowShootClip, sfxVolume);
    }

    void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;
        AudioClip c = footstepClips[Random.Range(0, footstepClips.Length)];
        Play(c, footstepVolume);
    }

    void Play(AudioClip clip, float volume)
    {
        if (clip == null) return;
        src.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        src.PlayOneShot(clip, volume * masterVolume);
    }
}