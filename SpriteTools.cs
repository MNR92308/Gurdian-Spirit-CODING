using System.Collections.Generic;
using UnityEngine;

// ตัวช่วยสร้างภาพด้วยโค้ด: วงกลมนุ่ม วงกลมทึบ วงแหวน และภาพเงาขาว (silhouette) ของสไปรต์
public static class SpriteTools
{
    static readonly Dictionary<Sprite, Sprite> whiteSprites = new Dictionary<Sprite, Sprite>();
    static readonly Dictionary<Texture2D, Texture2D> whiteTextures = new Dictionary<Texture2D, Texture2D>();
    static Sprite softCircle, disc, ring;

    public static Sprite SoftCircle()
    {
        if (softCircle == null)
            softCircle = MakeShape(128, d => { float a = Mathf.Clamp01(1f - d); return a * a; });
        return softCircle;
    }

    public static Sprite Disc()
    {
        if (disc == null)
            disc = MakeShape(128, d => Mathf.Clamp01((1f - d) * 64f));
        return disc;
    }

    public static Sprite Ring()
    {
        if (ring == null)
            ring = MakeShape(128, d => Mathf.Clamp01((1f - d) * 64f) * Mathf.Clamp01((d - 0.92f) * 64f));
        return ring;
    }

    static Sprite MakeShape(int size, System.Func<float, float> alphaFromDist)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        float c = (size - 1) / 2f;
        float r = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / r;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alphaFromDist(d)));
            }
        }
        tex.Apply();

        // ขนาด 1 หน่วยเต็ม (เส้นผ่านศูนย์กลาง = 1)
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // ภาพเงาสีขาวของสไปรต์ (รูปร่างเดิม สีขาวล้วน) ใช้กับร่างแยกของบอส
    public static Sprite GetWhite(Sprite src)
    {
        if (src == null) return null;

        Sprite cached;
        if (whiteSprites.TryGetValue(src, out cached) && cached != null) return cached;

        Texture2D tex = src.texture;
        Texture2D wt;
        if (!whiteTextures.TryGetValue(tex, out wt) || wt == null)
        {
            RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(tex, rt);

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;

            wt = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            wt.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            Color32[] px = wt.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                px[i].r = 255; px[i].g = 255; px[i].b = 255;     // เก็บเฉพาะค่าความโปร่งใส
            }
            wt.SetPixels32(px);
            wt.Apply();
            wt.filterMode = tex.filterMode;

            whiteTextures[tex] = wt;
        }

        Rect rect = src.rect;
        Vector2 pivot = new Vector2(src.pivot.x / rect.width, src.pivot.y / rect.height);
        Sprite ws = Sprite.Create(wt, rect, pivot, src.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        whiteSprites[src] = ws;
        return ws;
    }
}