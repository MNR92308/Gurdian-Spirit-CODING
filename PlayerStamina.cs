using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    public float maxStamina = 40f;              // ค่าตั้งต้น (ร้านค้าบวกเพิ่มได้สูงสุด +40)
    public float regenPerSecond = 30f;
    public float regenDelay = 0.7f;             // รอนานเท่านี้หลังใช้ ก่อนเริ่มฟื้น
    public float exhaustedRegenDelay = 1.5f;    // รอนานกว่าถ้าใช้จนหมด
    public float blockHitCost = 15f;            // เสียเท่านี้ตอนการ์ดธรรมดารับการโจมตี

    public float Current { get; private set; }
    public float Max => maxStamina + UpgradeData.StaminaBonus;
    public bool PauseRegen { get; set; }        // PlayerParry สั่งหยุดฟื้นตอนตั้งการ์ด

    float regenResumeTime;

    void Awake()
    {
        Current = Max;
    }

    void Update()
    {
        if (PauseRegen || Time.time < regenResumeTime) return;
        Current = Mathf.Min(Max, Current + regenPerSecond * Time.deltaTime);
    }

    // ใช้ถ้ามีพอ (ใช้กับแดช) คืนค่า false ถ้าไม่พอ
    public bool TrySpend(float amount)
    {
        if (Current < amount) return false;
        Spend(amount);
        return true;
    }

    public void Spend(float amount)
    {
        Current = Mathf.Max(0f, Current - amount);
        regenResumeTime = Time.time + regenDelay;
    }

    // ใช้จนหมด (การ์ดหลุด)
    public void Exhaust()
    {
        Current = 0f;
        regenResumeTime = Time.time + exhaustedRegenDelay;
    }
}