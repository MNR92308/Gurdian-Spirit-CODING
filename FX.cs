using System.Collections.Generic;
using UnityEngine;

// เอฟเฟกต์ที่สร้างด้วยโค้ด: ประกาย วงคลื่น จอสั่น หยุดเฟรม ตัวหนังสือลอย แฟลชจอ
public class FX : MonoBehaviour
{
    static FX inst;

    static FX Inst
    {
        get
        {
            if (inst == null)
            {
                var go = new GameObject("FX");
                inst = go.AddComponent<FX>();
            }
            return inst;
        }
    }

    class Particle { public Transform t; public SpriteRenderer sr; public Vector2 vel; public float age, life, size; public Color color; }
    class RingFx { public Transform t; public SpriteRenderer sr; public float age, dur, startD, endD; public Color color; }
    class PopupFx { public Vector3 pos; public string text; public Color color; public float age, life; }

    readonly List<Particle> particles = new List<Particle>();
    readonly List<RingFx> rings = new List<RingFx>();
    readonly List<PopupFx> popups = new List<PopupFx>();

    float shakeTimer, shakeDuration, shakeMag;
    bool shaking;
    Vector3 camBase;

    float hitStopUntil;
    bool stopActive;

    Color flashColor;
    float flashTimer, flashDuration;

    GUIStyle popupStyle;

    // ===== เรียกใช้จากสคริปต์อื่น =====

    public static void Burst(Vector2 pos, Color color, int count, float speed, float life, float size)
    {
        Inst.DoBurst(pos, color, count, speed, life, size);
    }

    public static void Ring(Vector2 pos, Color color, float startRadius, float endRadius, float duration)
    {
        Inst.DoRing(pos, color, startRadius, endRadius, duration);
    }

    public static void Shake(float duration, float magnitude)
    {
        Inst.DoShake(duration, magnitude);
    }

    public static void HitStop(float duration)
    {
        Inst.hitStopUntil = Mathf.Max(Inst.hitStopUntil, Time.unscaledTime + duration);
    }

    public static void Popup(Vector3 worldPos, string text, Color color)
    {
        Inst.popups.Add(new PopupFx { pos = worldPos, text = text, color = color, life = 1.2f });
    }

    public static void Flash(Color color, float duration)
    {
        Inst.flashColor = color;
        Inst.flashTimer = duration;
        Inst.flashDuration = duration;
    }

    // ===== ภายใน =====

    void DoBurst(Vector2 pos, Color color, int count, float speed, float life, float size)
    {
        int cap = GameSettings.LowFx ? 120 : 250;
        for (int i = 0; i < count; i++)
        {
            if (particles.Count > cap) return;

            var go = new GameObject("fx");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteTools.SoftCircle();
            sr.sortingOrder = 30;
            sr.color = color;

            Vector2 dir = Random.insideUnitCircle.normalized;
            float v = speed * Random.Range(0.4f, 1f);
            float l = life * Random.Range(0.7f, 1.2f);

            particles.Add(new Particle
            {
                t = go.transform,
                sr = sr,
                vel = dir * v,
                life = l,
                size = size * Random.Range(0.6f, 1.2f),
                color = color
            });
        }
    }

    void DoRing(Vector2 pos, Color color, float startRadius, float endRadius, float duration)
    {
        var go = new GameObject("fxring");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteTools.Ring();
        sr.sortingOrder = 28;
        sr.color = color;

        rings.Add(new RingFx
        {
            t = go.transform,
            sr = sr,
            dur = Mathf.Max(0.05f, duration),
            startD = startRadius * 2f,
            endD = endRadius * 2f,
            color = color
        });
    }

    void DoShake(float duration, float magnitude)
    {
        if (magnitude >= shakeMag || shakeTimer <= 0f)
        {
            shakeMag = magnitude;
        }
        shakeDuration = Mathf.Max(duration, 0.01f);
        shakeTimer = Mathf.Max(shakeTimer, duration);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // หยุดเฟรมสั้นๆ ให้การปะทะรู้สึกหนัก
        if (PauseMenu.Paused)
        {
            stopActive = false;            // ห้ามแตะ timeScale ตอนหยุดเกม
        }
        else if (Time.unscaledTime < hitStopUntil)
        {
            Time.timeScale = 0.05f;
            stopActive = true;
        }
        else if (stopActive)
        {
            Time.timeScale = 1f;
            stopActive = false;
        }

        for (int i = particles.Count - 1; i >= 0; i--)
        {
            Particle p = particles[i];
            p.age += dt;
            if (p.t == null || p.age >= p.life)
            {
                if (p.t != null) Destroy(p.t.gameObject);
                particles.RemoveAt(i);
                continue;
            }

            float k = p.age / p.life;
            p.t.position += (Vector3)(p.vel * dt);
            p.vel *= Mathf.Max(0f, 1f - 3f * dt);
            p.t.localScale = Vector3.one * p.size * (1f - k * 0.7f);

            Color c = p.color;
            c.a = p.color.a * (1f - k);
            p.sr.color = c;
        }

        for (int i = rings.Count - 1; i >= 0; i--)
        {
            RingFx r = rings[i];
            r.age += dt;
            if (r.t == null || r.age >= r.dur)
            {
                if (r.t != null) Destroy(r.t.gameObject);
                rings.RemoveAt(i);
                continue;
            }

            float k = r.age / r.dur;
            float ease = 1f - (1f - k) * (1f - k);
            r.t.localScale = Vector3.one * Mathf.Lerp(r.startD, r.endD, ease);

            Color c = r.color;
            c.a = r.color.a * (1f - k);
            r.sr.color = c;
        }

        for (int i = popups.Count - 1; i >= 0; i--)
        {
            popups[i].age += dt;
            if (popups[i].age >= popups[i].life) popups.RemoveAt(i);
        }

        if (flashTimer > 0f) flashTimer -= dt;
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.unscaledDeltaTime;

            if (!shaking)
            {
                camBase = cam.transform.position;
                shaking = true;
            }

            float k = Mathf.Clamp01(shakeTimer / shakeDuration);
            Vector2 off = Random.insideUnitCircle * shakeMag * k;
            cam.transform.position = new Vector3(camBase.x + off.x, camBase.y + off.y, camBase.z);
        }
        else if (shaking)
        {
            cam.transform.position = camBase;
            shaking = false;
            shakeMag = 0f;
        }
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;

        // แฟลชทั้งจอ
        if (flashTimer > 0f)
        {
            Color c = flashColor;
            c.a = flashColor.a * Mathf.Clamp01(flashTimer / Mathf.Max(0.01f, flashDuration));
            GUI.color = c;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // ตัวหนังสือลอย
        Camera cam = Camera.main;
        if (cam == null || popups.Count == 0) return;

        if (popupStyle == null)
        {
            popupStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }

        foreach (var p in popups)
        {
            float k = p.age / p.life;
            Vector3 sp = cam.WorldToScreenPoint(p.pos + Vector3.up * (k * 1.2f));
            Rect r = new Rect(sp.x - 150, Screen.height - sp.y - 20, 300, 40);

            Color shadow = new Color(0f, 0f, 0f, 1f - k);
            popupStyle.normal.textColor = shadow;
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), p.text, popupStyle);

            Color c = p.color;
            c.a = 1f - k * k;
            popupStyle.normal.textColor = c;
            GUI.Label(r, p.text, popupStyle);
        }
    }

    void OnDestroy()
    {
        if (stopActive) Time.timeScale = 1f;
    }
}