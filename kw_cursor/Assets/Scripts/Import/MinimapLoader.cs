using System.Collections;
using UnityEngine;

namespace KwCursor
{
    /// <summary>
    /// 战场小地图：Smlmap.e5（Ls12，78 条，每条 = 地图 w×6 × h×6 像素 8bpp），
    /// 调色板与战场画面共用 Spalet.e5 同序号条目。
    /// </summary>
    public static class MinimapLoader
    {
        public const int CellPx = 6;

        public static IEnumerator Load(int mapIndex, int mapW, int mapH, System.Action<Sprite> done)
        {
            byte[] sml = null;
            yield return OriginalAssets.LoadBytes("kw/Smlmap.e5", b => sml = b);
            if (sml == null) { done(null); yield break; }

            E5Archive arc = E5Archive.Parse(sml);
            if (arc == null || arc.Count == 0) { done(null); yield break; }

            byte[] img;
            try { img = arc.Extract(Mathf.Clamp(mapIndex, 0, arc.Count - 1)); }
            catch (System.Exception) { done(null); yield break; }

            int w = mapW * CellPx, h = mapH * CellPx;
            if (img.Length < w * h) { done(null); yield break; }

            byte[] palArc = null;
            yield return OriginalAssets.LoadBytes("kw/Spalet.e5", b => palArc = b);
            Color32[] pal = PaletteUtil.Build(palArc, mapIndex);

            var buf = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                int dstRow = h - 1 - y;
                for (int x = 0; x < w; x++)
                    buf[dstRow * w + x] = pal[img[y * w + x]];
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(buf);
            tex.Apply(false, true);
            done(Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f));
        }
    }
}
