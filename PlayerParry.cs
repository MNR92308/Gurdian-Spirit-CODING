using UnityEngine;

public enum GuardState { None, Parry, Block }

[RequireComponent(typeof(PlayerSword))]
[RequireComponent(typeof(PlayerStamina))]
public class PlayerParry : MonoBehaviour
{
    public float parryWindow = 0.2f;           // ช่วง Parry สมบูรณ์ (ไม่เสีย Stamina)
    public float guardCooldown = 0.5f;
    public float guardDrainPerSecond = 12f;    // Stamina ที่ลดต่อวินาทีตอนการ์ดค้าง (หลังช่วง Parry)
    public float minStaminaToGuard = 5f;       // ต้องมี Stamina อย่างน้อยเท่านี้ถึงจะตั้งการ์ดได้

    PlayerSword sword;
    PlayerStamina stamina;
    PlayerMovement movement;

    float guardStartTime;
    float nextGuardTime;
    bool guarding;

    public bool IsGuarding => guarding;

    void Awake()
    {
        sword = GetComponent<PlayerSword>();
        stamina = GetComponent<PlayerStamina>();
        movement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (PlayerStatus.IsStunned)
        {
            if (guarding) EndGuard();
            stamina.PauseRegen = false;
            return;
        }

        // ปุ่มการ์ดตามที่ผู้เล่นตั้งใน OPTION (คอม) หรือปุ่ม GUARD บนจอ (มือถือ)
        bool guardDown = Controls.GuardDown;
        bool guardUp = Controls.GuardUp;

        // เริ่มกัน
        if (guardDown && CanStartGuard())
        {
            guarding = true;
            guardStartTime = Time.time;
        }

        if (guarding)
        {
            if (sword.IsAttacking || (movement != null && movement.IsDashing))
            {
                EndGuard();                          // ฟันหรือแดช = เลิกกัน
            }
            else if (guardUp)
            {
                EndGuard();
            }
            else if (CurrentState == GuardState.Block)
            {
                // การ์ดค้างหลังช่วง Parry: Stamina ลดลงเรื่อยๆ
                stamina.Spend(guardDrainPerSecond * Time.deltaTime);

                if (stamina.Current <= 0f)
                {
                    stamina.Exhaust();               // หมดแรง การ์ดหลุด
                    EndGuard();
                }
            }
        }

        stamina.PauseRegen = guarding;               // ตั้งการ์ดอยู่ไม่ฟื้น Stamina
    }

    bool CanStartGuard()
    {
        if (Time.time < nextGuardTime) return false;
        if (sword.IsAttacking) return false;
        if (movement != null && movement.IsDashing) return false;
        if (stamina.Current < minStaminaToGuard) return false;
        return true;
    }

    void EndGuard()
    {
        guarding = false;
        nextGuardTime = Time.time + guardCooldown;
    }

    public GuardState CurrentState
    {
        get
        {
            if (!guarding) return GuardState.None;
            return (Time.time - guardStartTime <= parryWindow) ? GuardState.Parry : GuardState.Block;
        }
    }
}