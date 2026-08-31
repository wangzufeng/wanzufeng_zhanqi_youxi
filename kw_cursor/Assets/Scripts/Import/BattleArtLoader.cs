using System.Collections;
using UnityEngine;

namespace KwCursor
{
    /// <summary>
    /// 加载原版战场画面：Hm??.e5（w×h 个 96×24 像素 8bpp 图块，行主序）
    /// + Spalet.e5（Ls12 封包，78 张 768 字节 RGB 调色板）。
    /// 调色板与地图的对应关系按同序号取（超界取最后一张），可按需校准。
    /// </summary>
    public static class BattleArtLoader
    {
        public const int TileW = 96;
        public const int TileH = 24;
        public const int TileBytes = TileW * TileH;

        public static IEnumerator Load(int mapIndex, int w, int h, System.Action<Sprite> done)
        {
            byte[] hm = null;
            string file = "kw/Hm" + mapIndex.ToString("D2") + ".e5";
            yield return OriginalAssets.LoadBytes(file, b => hm = b);
            if (hm == null || hm.Length < w * h * TileBytes)
            {
                if (hm != null) Debug.LogWarning(file + " 大小与地图尺寸不符");
                done(null);
                yield break;
            }

            byte[] palArc = null;
            yield return OriginalAssets.LoadBytes("kw/Spalet.e5", b => palArc = b);
            Color32[] pal = PaletteUtil.Build(palArc, mapIndex);

            int texW = w * TileW, texH = h * TileH;
            var buf = new Color32[texW * texH];
            for (int ti = 0; ti < w * h; ti++)
            {
                int baseOff = ti * TileBytes;
                int tx = (ti % w) * TileW;
                int ty = (ti / w) * TileH;
                for (int y = 0; y < TileH; y++)
                {
                    // Texture2D 第 0 行在底部，需要垂直翻转
                    int dstRow = texH - 1 - (ty + y);
                    int dst = dstRow * texW + tx;
                    int src = baseOff + y * TileW;
                    for (int x = 0; x < TileW; x++)
                        buf[dst + x] = pal[hm[src + x]];
                }
            }

            var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(buf);
            tex.Apply(false, true);

            float ppu = TileW / GridGeometry.SquareW; // 96px = 1.6 世界单位
            var sprite = Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0f, 1f), ppu);
            done(sprite);
        }
    }
}
