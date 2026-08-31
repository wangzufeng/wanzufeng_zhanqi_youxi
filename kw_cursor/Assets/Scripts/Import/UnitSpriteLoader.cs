using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KwCursor
{
    /// <summary>
    /// 战场/内场景单位小人图：Pmapobj.e5（Ls12，520 条）。
    /// 每条 = 20 帧 48×64 像素 8bpp 行走图（61440 字节，部分带 2 字节尾），
    /// 透明键 = 索引 0，调色板用 Pmpalet.e5 条目 0。
    /// 武将表偏移 13-14 的形象编号即本封包条目序号（实测 0=主角形象）。
    /// </summary>
    public static class UnitSpriteLoader
    {
        public const int FrameW = 48;
        public const int FrameH = 64;
        public const int FrameCount = 20;
        public const byte TransparentIndex = 0;

        static E5Archive archive;
        static Color32[] pal;
        static readonly Dictionary<int, Sprite[]> cache = new Dictionary<int, Sprite[]>();

        public static bool Loaded { get { return archive != null; } }

        public static IEnumerator TryLoad()
        {
            if (archive != null) yield break;

            byte[] pb = null;
            yield return OriginalAssets.LoadBytes("kw/Pmapobj.e5", b => pb = b);
            if (pb == null) yield break;

            E5Archive arc = E5Archive.Parse(pb);
            if (arc == null || arc.Count == 0) yield break;

            byte[] palArc = null;
            yield return OriginalAssets.LoadBytes("kw/Pmpalet.e5", b => palArc = b);
            pal = PaletteUtil.Build(palArc, 0);
            archive = arc;
            Debug.Log("UnitSpriteLoader: 形象 " + arc.Count + " 条就绪");
        }

        /// <summary>取指定动作帧。缺数据/空帧/越界时回退第 0 帧。</summary>
        public static Sprite GetFrame(int spriteId, int frame)
        {
            if (archive == null || spriteId < 0 || spriteId >= archive.Count) return null;

            Sprite[] frames;
            if (!cache.TryGetValue(spriteId, out frames))
            {
                frames = BuildFrames(spriteId);
                cache[spriteId] = frames;
            }
            if (frames == null) return null;
            if (frame < 0 || frame >= frames.Length || frames[frame] == null) frame = 0;
            return frames[frame];
        }

        public static Sprite Get(int spriteId)
        {
            return GetFrame(spriteId, 0);
        }

        static Sprite[] BuildFrames(int spriteId)
        {
            byte[] blob;
            try { blob = archive.Extract(spriteId); }
            catch (System.Exception) { return null; }
            if (blob.Length < FrameW * FrameH) return null;

            int count = Mathf.Min(FrameCount, blob.Length / (FrameW * FrameH));
            var frames = new Sprite[FrameCount];
            for (int frame = 0; frame < count; frame++)
            {
                bool hasPixels = false;
                var buf = new Color32[FrameW * FrameH];
                int frameOffset = frame * FrameW * FrameH;
                for (int y = 0; y < FrameH; y++)
                {
                    int dstRow = FrameH - 1 - y;
                    for (int x = 0; x < FrameW; x++)
                    {
                        byte v = blob[frameOffset + y * FrameW + x];
                        if (v != TransparentIndex) hasPixels = true;
                        buf[dstRow * FrameW + x] = v == TransparentIndex
                            ? new Color32(0, 0, 0, 0)
                            : pal[v];
                    }
                }
                if (!hasPixels) continue;

                var tex = new Texture2D(FrameW, FrameH, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.SetPixels32(buf);
                tex.Apply(false, true);
                frames[frame] = Sprite.Create(tex, new Rect(0, 0, FrameW, FrameH),
                    new Vector2(0.5f, 0.5f), 64f);
            }
            return frames;
        }
    }
}
