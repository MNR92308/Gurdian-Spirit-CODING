using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 5f;
    [Range(0f, 1f)] public float chargeSlowAmount = 0.8f;   // ช้าลง 80% ตอนชาร์จ
    [Range(0f, 1f)] public float guardSlowAmount = 0.5f;    // ช้าลง 50% ตอนตั้งการ์ด

    [Header("Dash (ปุ่มแดชปรับได้ใน OPTION -> Controls)")]
    public float dashSpeed = 16f;
    public float dashDuration = 0.18f;
    public float dashCooldown = 0.6f;
    public float dashStaminaCost = 25f;       // 0 = แดชฟรี
    public bool dashInvincible = true;

    public bool IsDashing { get; private set; }
    public bool IsInvincibleDash => IsDashing && dashInvincible;

    Rigidbody2D rb;
    PlayerSword sword;
    PlayerParry parry;
    PlayerStamina stamina;
    Camera cam;
    Collider2D myCol;

    Vector2 input;
    Vector2 dashDir;
    float dashEnd;
    float nextDashTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sword = GetComponent<PlayerSword>();
        parry = GetComponent<PlayerParry>();
        stamina = GetComponent<PlayerStamina>();
        myCol = GetComponentInChildren<Collider2D>();

        cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();
    }

    void Update()
    {
        if (IsDashing && Time.time >= dashEnd)
        {
            IsDashing = false;
            SetDashPassthrough(false);
        }

        // Controls.Move() = จอยสติ๊กมือถือ หรือปุ่มคีย์ที่ผู้เล่นตั้งไว้ (ลูกศรใช้ได้เสมอ)
        input = PauseMenu.Paused ? Vector2.zero : Controls.Move();
        if (PlayerStatus.IsStunned) input = Vector2.zero;

        if (Controls.DashDown) TryDash();
    }

    void TryDash()
    {
        if (PlayerStatus.IsStunned) return;
        if (IsDashing || Time.time < nextDashTime) return;
        if (sword != null && sword.IsAttacking) return;
        if (stamina != null && !stamina.TrySpend(dashStaminaCost)) return;

        Vector2 dir = input;
        if (dir.sqrMagnitude < 0.01f)
            dir = PlayerAim.Direction(transform.position, cam);   // ไม่ได้กดเดิน: แดชถอยหลัง

        dashDir = dir.normalized;
        IsDashing = true;
        SetDashPassthrough(true);
        dashEnd = Time.time + dashDuration;
        nextDashTime = Time.time + dashCooldown;

        if (sword != null) sword.CancelCharge();
    }

    void SetDashPassthrough(bool on)
    {
        if (myCol == null) return;
        foreach (var b in BossAI.All)
        {
            if (b == null || b.Col == null) continue;
            Physics2D.IgnoreCollision(myCol, b.Col, on);
        }
    }

    void FixedUpdate()
    {
        if (PlayerStatus.IsKnocked)
        {
            rb.linearVelocity = PlayerStatus.KnockVelocity();
            return;
        }
        if (IsDashing)
        {
            rb.linearVelocity = dashDir * dashSpeed;
            return;
        }

        float mult = 1f;
        if (sword != null && sword.IsCharging) mult = 1f - chargeSlowAmount;
        else if (parry != null && parry.IsGuarding) mult = 1f - guardSlowAmount;

        rb.linearVelocity = input * moveSpeed * mult;
    }
}