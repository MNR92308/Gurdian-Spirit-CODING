using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum HitResult { Dead, Hurt, Blocked, Parried, Ignored }

[RequireComponent(typeof(PlayerParry))]
public class PlayerHealth : MonoBehaviour
{
    public static int MaxArmor { get { return UpgradeData.ArmorCap; } }   // เกราะสูงสุด (3 → 5 ตามร้านค้า)
    public const int MaxHealth = 1;                                       // เลือดมีหลอดเดียว (โดนตอนไม่มีเกราะ = ตาย)

    [Header("Settings")]
    public int startingArmor = 3;      // ไม่ใช้แล้ว: เกราะตอนเข้าเกมมาจากร้านค้า (UpgradeData.StartArmor)
    public float invincibleTime = 1f;
    public float restartDelay = 1f;

    public int Armor { get; private set; }
    public int Health { get; private set; }
    public bool IsDead { get; private set; }

    public int ArmorLevel => Armor;
    public int CurrentHits => Health;
    public int MaxHits => MaxHealth;
    public int TotalHits => Armor + Health;

    PlayerParry parry;
    PlayerVisual visual;
    PlayerStamina stamina;
    PlayerMovement movement;
    float invincibleUntil;

    void Awake()
    {
        PlayerStatus.Reset();
        parry = GetComponent<PlayerParry>();
        visual = GetComponent<PlayerVisual>();
        stamina = GetComponent<PlayerStamina>();
        movement = GetComponent<PlayerMovement>();
    }

    void Start()
    {
        Armor = Mathf.Clamp(UpgradeData.StartArmor, 0, MaxArmor);
        Health = MaxHealth;
    }

    public HitResult TakeHit(bool unblockable = false, int damage = 1, bool ignoreGuard = false)
    {
        if (IsDead) return HitResult.Ignored;

        Vector2 pos = transform.position;

        if (!unblockable)
        {
            if (movement != null && movement.IsInvincibleDash) return HitResult.Ignored;

            if (!ignoreGuard)
            {
                switch (parry.CurrentState)
                {
                    case GuardState.Parry:
                        {
                            Vector2 pd = PlayerAim.Direction(pos, Camera.main);
                            Color g = new Color(1f, 0.92f, 0.35f, 0.95f);

                            FX.Ring(pos, new Color(1f, 0.9f, 0.2f, 0.95f), 0.3f, 1.6f, 0.25f);
                            FX.Burst(pos + pd * 0.7f, new Color(1f, 0.9f, 0.3f), 12, 6f, 0.3f, 0.14f);
                            FXPlus.Slash(pos + pd * 0.8f, pd, g, 3f, 0.25f);
                            FXPlus.Flare(pos + pd * 0.8f, g, 2.6f, 0.22f);
                            FXPlus.Shockwave(pos, g, 2.4f);
                            FX.Shake(0.12f, 0.12f);
                            FX.HitStop(0.06f);
                            return HitResult.Parried;
                        }

                    case GuardState.Block:
                        {
                            Vector2 bd = AimTarget.Direction(pos, Camera.main);
                            if (stamina != null) stamina.Spend(stamina.blockHitCost);
                            FX.Burst(pos + bd * 0.7f, new Color(0.4f, 0.95f, 1f), 7, 4f, 0.25f, 0.12f);
                            FXPlus.Flare(pos + bd * 0.7f, new Color(0.4f, 0.95f, 1f, 0.8f), 1.6f, 0.15f);
                            return HitResult.Blocked;
                        }
                }
            }
        }

        if (Time.time < invincibleUntil) return HitResult.Ignored;

        int dmg = Mathf.Max(1, damage);
        int absorbed = Mathf.Min(Armor, dmg);
        Armor -= absorbed;
        dmg -= absorbed;
        Health -= dmg;

        if (Health <= 0)
        {
            Health = 0;
            Die();
            return HitResult.Dead;
        }

        invincibleUntil = Time.time + invincibleTime;
        if (visual) visual.PlayHurt();
        StartCoroutine(Blink());

        if (absorbed > 0)
        {
            Color steel = new Color(0.75f, 0.85f, 1f);
            FX.Burst(pos, steel, 12, 5f, 0.4f, 0.14f);
            FX.Ring(pos, steel, 0.3f, 1.8f, 0.3f);
            FX.Popup(transform.position + Vector3.up * 1.4f, Armor > 0 ? "ARMOR -" + absorbed : "ARMOR BREAK", steel);
            FX.Shake(0.15f, 0.12f);
            FX.HitStop(0.05f);

            FXPlus.Flare(pos, steel, 2.5f, 0.25f);
            if (Armor <= 0)
            {
                FXPlus.Shockwave(pos, steel, 3f);
                FXPlus.Cracks(pos, steel, 2f, 8, 0.5f);
            }
        }
        else
        {
            FX.Burst(pos, new Color(1f, 0.25f, 0.2f), 12, 5f, 0.4f, 0.14f);
            FX.Shake(0.18f, 0.15f);
            FX.HitStop(0.05f);
            FX.Flash(new Color(1f, 0.1f, 0.1f, 0.25f), 0.15f);
        }

        return HitResult.Hurt;
    }

    // ฆ่าบอสหรือร่างแยกได้ → เกราะ +1 (ไม่เกิน MaxArmor)
    public void GainArmor()
    {
        if (IsDead) return;

        Vector2 pos = transform.position;
        Vector3 popPos = transform.position + Vector3.up * 1.4f;
        Color gold = new Color(1f, 0.85f, 0.25f);

        if (Armor < MaxArmor)
        {
            Armor++;
            FX.Popup(popPos, "ARMOR +1", gold);
        }
        else if (Health < MaxHealth)
        {
            Health++;
            FX.Popup(popPos, "HP +1", new Color(0.4f, 1f, 0.5f));
        }
        else
        {
            FX.Popup(popPos, "MAX ARMOR", gold);
        }

        FX.Ring(pos, gold, 0.4f, 2.2f, 0.5f);
        FX.Burst(pos, gold, 16, 6f * 0.8333f, 0.6f, 0.16f);
        FXPlus.Pillar(pos, new Color(1f, 0.85f, 0.3f, 0.8f), 4f, 1.2f, 0.5f);
        FXPlus.Flare(pos, new Color(1f, 0.9f, 0.4f, 0.8f), 3f, 0.4f);
    }

    IEnumerator Blink()
    {
        bool vis = true;
        while (Time.time < invincibleUntil)
        {
            vis = !vis;
            if (visual) visual.SetVisible(vis);
            yield return new WaitForSeconds(0.08f);
        }
        if (visual) visual.SetVisible(true);
    }

    void Die()
    {
        IsDead = true;
        StopAllCoroutines();

        UpgradeData.OnPlayerDied();          // ตายได้เหรียญครึ่งหนึ่งของที่สะสมในรอบนี้

        var move = GetComponent<PlayerMovement>();
        var sword = GetComponent<PlayerSword>();
        if (move) move.enabled = false;
        if (sword) sword.enabled = false;
        parry.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = Vector2.zero;   // Unity เก่ากว่า 6 ใช้ rb.velocity

        if (visual) visual.PlayDeath();

        FX.Burst(transform.position, new Color(1f, 0.3f, 0.25f), 28, 6f, 0.8f, 0.2f);
        FX.Ring(transform.position, new Color(1f, 0.3f, 0.25f, 0.9f), 0.4f, 3f, 0.5f);
        FX.Shake(0.4f, 0.25f);

        Invoke(nameof(Restart), restartDelay);
    }

    void Restart()
    {
        GameScenes.Load(GameScenes.GameOver);
    }
}