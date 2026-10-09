using System.Collections.Generic;
using UnityEngine;

// เอฟเฟกต์เสริมที่สร้างด้วยโค้ด: รอยฟันโค้ง / เส้นแสง / เสาแสง / รอยแตกพื้น / อนุภาคพุ่งเข้าหาตัว / แสงวาบ
// ใช้ Time.deltaTime จึงหยุดตามเมนู Pause และช้าลงตอน Hit-stop เอง
public class FXPlus : MonoBehaviour
{
    static FXPlus inst;
    static FXPlus Inst
    {
        get
        {
            if (inst == null)
            {
                var go = new GameObject("FXPlus");
                inst = go.AddComponent<FXPlus>();
            }
            return inst;
        }
    }

    enum Kind { Slash, Beam, Crack, Particle, Pillar, Flare }

    class E
    {
        public Kind kind;
        public Transform t;
        public SpriteRenderer sr;
        public float age, dur, f1, f2;
        public Vector2 a, b;
        public Color col;
    }

    readonly List<E> list = new List<E>();
    static Sprite crescent, line, dot;

    static int Max { get { return GameSettings.LowFx ? 60 : 110; } }

    // ===== เรียกใช้จากสคริปต์อื่น =====

    // รอยฟันโค้ง พุ่งไปทาง dir
    public static void Slash(Vector2 pos, Vector2 dir, Color c, float size, float dur = 0.22f)
    {
        E e = Inst.Make(Kind.Slash, Crescent(), c, dur, 32);
        if (e == null) return;
        dir = dir.sqrMagnitude < 0.0001f ? Vector2.right : dir.normalized;
        e.a = pos; e.b = dir; e.f1 = size;
        e.t.position = pos;
        e.t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 180f);
    }

    // เส้นแสงจาก a ไป b
    public static void Beam(Vector2 a, Vector2 b, Color c, float width, float dur)
    {
        E e = Inst.Make(Kind.Beam, Line(), c, dur, 31);
        if (e == null) return;
        Vector2 d = b - a;
        float len = Mathf.Max(0.01f, d.magnitude);
        e.t.position = (a + b) * 0.5f;
        e.t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        e.t.localScale = new Vector3(len, width, 1f);
        e.f1 = width; e.f2 = len;
    }

    // เสาแสงพุ่งขึ้นจากพื้น
    public static void Pillar(Vector2 pos, Color c, float height, float width, float dur)
    {
        E e = Inst.Make(Kind.Pillar, Line(), c, dur, 25);
        if (e == null) return;
        e.a = pos; e.f1 = width; e.f2 = height;
        e.t.rotation = Quaternion.Euler(0f, 0f, 90f);
        e.t.position = pos;
    }

    // รอยแตกกระจายจากจุดศูนย์กลาง (ทุบพื้น)
    public static void Cracks(Vector2 pos, Color c, float radius, int count, float dur)
    {
        float off = Random.value * 360f;
        for (int i = 0; i < count; i++)
        {
            float ang = off + i * 360f / count + Random.Range(-12f, 12f);
            Vector2 d = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
            E e = Inst.Make(Kind.Crack, Line(), c, dur, 24);
            if (e == null) return;
            e.a = pos; e.b = d;
            e.f1 = Random.Range(0.08f, 0.17f);
            e.f2 = radius * Random.Range(0.6f, 1f);
            e.t.rotation = Quaternion.Euler(0f, 0f, ang);
            e.t.position = pos;
        }
    }

    // อนุภาคพุ่งเข้าหาจุด pos จากรัศมี radius (ใช้ตอนง้างท่า/ชาร์จ)
    public static void Converge(Vector2 pos, Color c, float radius, float dur, int count)
    {
        for (int i = 0; i < count; i++)
        {
            E e = Inst.Make(Kind.Particle, Dot(), c, dur * Random.Range(0.6f, 1f), 30);
            if (e == null) return;
            Vector2 d = Random.insideUnitCircle.normalized;
            e.a = pos + d * radius * Random.Range(0.7f, 1.2f);
            e.b = pos;
            e.f1 = Random.Range(0.16f, 0.3f);
            e.t.position = e.a;
        }
    }

    // แสงวาบนุ่มๆ ขยายแล้วจาง
    public static void Flare(Vector2 pos, Color c, float size, float dur)
    {
        E e = Inst.Make(Kind.Flare, Dot(), c, dur, 33);
        if (e == null) return;
        e.f1 = size;
        e.t.position = pos;
    }

    // วงคลื่นซ้อนสามชั้น
    public static void Shockwave(Vector2 pos, Color c, float radius)
    {
        FX.Ring(pos, c, 0.3f, radius, 0.35f);
        Color c2 = new Color(c.r, c.g, c.b, c.a * 0.7f);
        FX.Ring(pos, c2, 0.2f, radius * 0.75f, 0.28f);
        if (!GameSettings.LowFx)
        {
            Color c3 = Color.Lerp(c, Color.white, 0.6f);
            c3.a = c.a * 0.6f;
            FX.Ring(pos, c3, 0.1f, radius * 1.15f, 0.5f);
        }
    }

    // ===== ภายใน =====

    E Make(Kind k, Sprite s, Color c, float dur, int order)
    {
        if (list.Count >= Max) return null;
        var go = new GameObject("fxp");
        go.transform.SetParent(transform, true);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.color = c;
        sr.sortingOrder = order;
        var e = new E { kind = k, t = go.transform, sr = sr, dur = Mathf.Max(0.05f, dur), col = c };
        list.Add(e);
        return e;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            E e = list[i];
            e.age += dt;

            if (e.t == null || e.age >= e.dur)
            {
                if (e.t != null) Destroy(e.t.gameObject);
                list.RemoveAt(i);
                continue;
            }

            float k = e.age / e.dur;
            float ease = 1f - (1f - k) * (1f - k);
            Color c = e.col;

            switch (e.kind)
            {
                case Kind.Slash:
                    e.t.localScale = Vector3.one * e.f1 * (0.7f + 0.6f * ease);
                    e.t.position = e.a + e.b * (ease * e.f1 * 0.45f);
                    c.a = e.col.a * Mathf.Pow(1f - k, 1.5f);
                    break;

                case Kind.Beam:
                    e.t.localScale = new Vector3(e.f2, e.f1 * (1f - k * 0.6f), 1f);
                    c.a = e.col.a * (1f - k);
                    break;

                case Kind.Pillar:
                    {
                        float len = e.f2 * Mathf.Min(1f, k * 6f);
                        e.t.position = e.a + Vector2.up * (len * 0.5f);
                        e.t.localScale = new Vector3(len, e.f1 * (1f - k), 1f);
                        c.a = e.col.a * (1f - k);
                        break;
                    }

                case Kind.Crack:
                    {
                        float len = e.f2 * Mathf.Min(1f, k * 5f);
                        e.t.position = e.a + e.b * (len * 0.5f);
                        e.t.localScale = new Vector3(len, e.f1, 1f);
                        c.a = e.col.a * (k < 0.5f ? 1f : 1f - (k - 0.5f) * 2f);
                        break;
                    }

                case Kind.Particle:
                    e.t.position = Vector2.Lerp(e.a, e.b, k * k);
                    e.t.localScale = Vector3.one * e.f1 * (1f - 0.5f * k);
                    c.a = e.col.a * (k < 0.2f ? k / 0.2f : 1f - (k - 0.2f) * 0.4f);
                    break;

                case Kind.Flare:
                    e.t.localScale = Vector3.one * e.f1 * (0.5f + k);
                    c.a = e.col.a * (1f - k) * (1f - k);
                    break;
            }

            e.sr.color = c;
        }
    }

    // ===== Sprite ที่สร้างด้วยโค้ด =====

    static Sprite Crescent()
    {
        if (crescent != null) return crescent;
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color32[n * n];
        float c = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float nx = (x - c) / c, ny = (y - c) / c;
                float d1 = Mathf.Sqrt(nx * nx + ny * ny);
                float d2 = Mathf.Sqrt((nx - 0.25f) * (nx - 0.25f) + ny * ny);
                float a = Mathf.Clamp01((1f - d1) * 20f) * Mathf.Clamp01((d2 - 0.96f) * 20f);
                px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        crescent = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return crescent;
    }

    // เส้น: ทึบตามแนวยาว จางตามความหนา
    static Sprite Line()
    {
        if (line != null) return line;
        const int n = 32;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color32[n * n];
        float c = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
        {
            float a = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs((y - c) / c)), 1.5f);
            for (int x = 0; x < n; x++)
                px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
        }
        tex.SetPixels32(px);
        tex.Apply();
        line = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return line;
    }

    static Sprite Dot()
    {
        if (dot != null) return dot;
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color32[n * n];
        float c = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2f);
                px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        dot = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return dot;
    }
}