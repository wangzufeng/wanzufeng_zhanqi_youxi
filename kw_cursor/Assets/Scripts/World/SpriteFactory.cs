using System.Collections.Generic;
using UnityEngine;

namespace KwCursor
{
    /// <summary>
    /// 程序化生成占位精灵（六角格、圆形棋子、血条），避免使用任何外部受版权保护素材。
    /// </summary>
    public static class SpriteFactory
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        const int TexSize = 128;

        public static Sprite Hexagon(Color fill, Color outline, float outlineWidth = 0.06f)
        {
            string key = "hex_" + ColorUtility.ToHtmlStringRGBA(fill) + "_" +
                         ColorUtility.ToHtmlStringRGBA(outline) + "_" + outlineWidth.ToString("F2");
            Sprite s;
            if (cache.TryGetValue(key, out s)) return s;

            int S = TexSize;
            float R = S * 0.48f;
            float inner = R * (1f - outlineWidth);
            float k = Mathf.Sqrt(3f) / 2f;
            var tex = NewTex(S);
            var px = new Color32[S * S];
            float cx = (S - 1) * 0.5f, cy = (S - 1) * 0.5f;

            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    bool inOuter = InsidePointyHex(dx, dy, R, k);
                    if (!inOuter) { px[y * S + x] = new Color32(0, 0, 0, 0); continue; }
                    bool inInner = InsidePointyHex(dx, dy, inner, k);
                    px[y * S + x] = inInner ? (Color32)fill : (Color32)outline;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();

            float ppu = R / HexLayout.Size; // 让六角高度等于 2*Size 世界单位
            s = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), ppu);
            cache[key] = s;
            return s;
        }

        static bool InsidePointyHex(float dx, float dy, float R, float k)
        {
            // 尖顶正六边形：|x| <= (sqrt3/2)R 且 |y| <= R - |x|/sqrt3
            float ax = Mathf.Abs(dx), ay = Mathf.Abs(dy);
            return ax <= k * R && ay <= R - ax / Mathf.Sqrt(3f);
        }

        public static Sprite Circle(Color fill, Color outline, float radiusWorld = 0.42f)
        {
            string key = "cir_" + ColorUtility.ToHtmlStringRGBA(fill) + "_" +
                         ColorUtility.ToHtmlStringRGBA(outline) + "_" + radiusWorld.ToString("F2");
            Sprite s;
            if (cache.TryGetValue(key, out s)) return s;

            int S = TexSize;
            float R = S * 0.47f;
            float inner = R * 0.88f;
            var tex = NewTex(S);
            var px = new Color32[S * S];
            float cx = (S - 1) * 0.5f, cy = (S - 1) * 0.5f;

            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > R) px[y * S + x] = new Color32(0, 0, 0, 0);
                    else if (d > inner) px[y * S + x] = (Color32)outline;
                    else px[y * S + x] = (Color32)fill;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();

            float ppu = R / radiusWorld;
            s = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), ppu);
            cache[key] = s;
            return s;
        }

        /// <summary>纯色圆角矩形（世界尺寸 w×h，中心枢轴），用于方格模式的地块与高亮。</summary>
        public static Sprite Rect(Color fill, Color outline, float worldW, float worldH)
        {
            string key = "rect_" + ColorUtility.ToHtmlStringRGBA(fill) + "_" +
                         ColorUtility.ToHtmlStringRGBA(outline) + "_" + worldW.ToString("F2") + "x" + worldH.ToString("F2");
            Sprite s;
            if (cache.TryGetValue(key, out s)) return s;

            int W = 96, H = Mathf.Max(8, Mathf.RoundToInt(96f * worldH / worldW));
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[W * H];
            int bw = Mathf.Max(1, H / 12);
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    bool edge = x < bw || y < bw || x >= W - bw || y >= H - bw;
                    px[y * W + x] = edge ? (Color32)outline : (Color32)fill;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();

            float ppu = W / worldW;
            s = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), ppu);
            cache[key] = s;
            return s;
        }

        /// <summary>白色矩形条（枢轴在左侧中点，便于按比例缩放表示血量）。</summary>
        public static Sprite Bar()
        {
            const string key = "bar";
            Sprite s;
            if (cache.TryGetValue(key, out s)) return s;

            int w = 64, h = 12;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();

            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), 64f);
            cache[key] = s;
            return s;
        }

        static Texture2D NewTex(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }
    }
}
