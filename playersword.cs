using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSword : MonoBehaviour
{
    [Header("Aim / Hit Point")]
    public Transform attackPoint;
    public float attackDistance = 0.8f;
    public LayerMask enemyLayer;

    [Header("Normal Slash")]
    public float attackRadius = 0.7f;
    public float attackDuration = 0.3f;
    public float attackCooldown = 0.45f;
    [Range(0f, 1f)] public float hitTime = 0.4f;
    public int attackDamage = 1;

    [Header("Charge Slash")]
    public float chargeTime = 0.8f;
    public float chargeShowDelay = 0.2f;
    public float chargeAttackRadius = 1.3f;
    public float chargeAttackDuration = 0.45f;
    public float chargeAttackCooldown = 0.7f;
    [Range(0f, 1f)] public float chargeHitTime = 0.5f;
    public int chargeAttackDamage = 3;

    float nextAttackTime;
    float chargeStart;
    bool holding;
    Camera cam;
    PlayerArrow arrow;

    public bool IsAttacking { get; private set; }
    public bool LastAttackWasCharge { get; private set; }

    public bool IsCharging => holding && Time.time - chargeStart >= chargeShowDelay;

    public float ChargeProgress
    {
        get
        {
            if (!IsCharging) return 0f;
            float t = Time.time - chargeStart - chargeShowDelay;
            return Mathf.Clamp01(t / Mathf.Max(0.01f, chargeTime - chargeShowDelay));
        }
    }

    void Start()
    {
        cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        arrow = GetComponent<PlayerArrow>();
    }

    public void CancelCharge()
    {
        holding = false;
    }

    void Update()
    {
        AimAtTarget();
        if (PlayerStatus.IsStunned) { holding = false; return; }   // ชะงัก โจมตีและชาร์จไม่ได้

        // คอม: คลิกซ้าย = ฟัน / คลิกขวา = การ์ด    มือถือ: ปุ่ม ATTACK / GUARD บนจอ
        bool attackDown = Controls.AttackDown;
        bool attackUp = Controls.AttackUp;
        bool guardDown = Controls.GuardDown;

        if (guardDown)
            holding = false;

        if (attackDown && Time.time >= nextAttackTime && !IsAttacking)
        {
            holding = true;
            chargeStart = Time.time;
        }

        if (attackUp && holding)
        {
            bool full = Time.time - chargeStart >= chargeTime;
            holding = false;
            StartCoroutine(Attack(full));
        }
    }

    // เล็งไปที่บอสเสมอ (ไม่มีเป้าจะเล็งตามเมาส์)
    void AimAtTarget()
    {
        if (IsAttacking) return;
        if (attackPoint == null) return;

        Vector2 dir = PlayerAim.Direction(transform.position, cam);
        attackPoint.position = (Vector2)transform.position + dir * attackDistance;
    }

    IEnumerator Attack(bool charge)
    {
        IsAttacking = true;
        LastAttackWasCharge = charge;

        float duration = charge ? chargeAttackDuration : attackDuration;
        float hitFrac = charge ? chargeHitTime : hitTime;
        float radius = charge ? chargeAttackRadius : attackRadius;
        int damage = charge ? chargeAttackDamage : attackDamage;
        nextAttackTime = Time.time + (charge ? chargeAttackCooldown : attackCooldown);

        yield return new WaitForSeconds(duration * hitFrac);

        Vector2 fxPos = attackPoint.position;
        if (charge)
        {
            FX.Burst(fxPos, new Color(1f, 0.85f, 0.4f), 14, 6f, 0.3f, 0.16f);
            FX.Ring(fxPos, new Color(1f, 0.9f, 0.5f, 0.9f), 0.3f, radius * 1.3f, 0.25f);
        }
        else
        {
            FX.Burst(fxPos, Color.white, 5, 4f, 0.2f, 0.1f);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, radius, enemyLayer);
        var done = new HashSet<BossAI>();

        foreach (var h in hits)
        {
            BossAI boss = h.GetComponentInParent<BossAI>();
            if (boss != null)
            {
                if (done.Add(boss))
                    boss.ReceiveHit(damage, charge ? HitType.Charge : HitType.Slash);
            }
            else
            {
                h.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            }
        }

        // ฟันโดนบอสตัวที่ธนูปักอยู่หรือไม่ (เก็บธนูคืน)
        if (arrow != null) arrow.NotifySlash(attackPoint.position, radius, hits);

        yield return new WaitForSeconds(duration * (1f - hitFrac));

        IsAttacking = false;
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint)
        {
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, chargeAttackRadius);
        }
    }
}