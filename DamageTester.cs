using System.Collections;
using UnityEngine;

public class DamageTester : MonoBehaviour
{
    public PlayerHealth player;
    public float windupTime = 0.5f;   // เวลาเตือนก่อนตี (สีส้ม)

    SpriteRenderer sr;
    Color original;
    bool busy;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        original = sr.color;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H) && !busy)
            StartCoroutine(Attack());
    }

    IEnumerator Attack()
    {
        busy = true;

        sr.color = new Color(1f, 0.6f, 0f);   // ส้ม = กำลังง้างท่า
        yield return new WaitForSeconds(windupTime);

        sr.color = Color.white;                // ขาว = ตีแล้ว
        HitResult result = player.TakeHit();
        Debug.Log("Hit result: " + result);

        yield return new WaitForSeconds(0.3f);
        sr.color = original;
        busy = false;
    }
}