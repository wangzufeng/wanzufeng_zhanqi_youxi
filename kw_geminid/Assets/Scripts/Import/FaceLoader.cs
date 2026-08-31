using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KwGeminid
{
    public static class FaceLoader
    {
        public const int FaceW = 64;
        public const int FaceH = 80;

        static E5Archive archive;
        static Color32[] pal;
        static readonly Dictionary<int, Sprite> cache = new Dictionary<int, Sprite>();

        public static bool Loaded { get { return archive != null; } }

        public static IEnumerator TryLoad()
        {
            if (archive != null) yield break;

            byte[] fb = null;
            yield return OriginalAssets.LoadBytes("kw/Face.e5", b => fb = b);
            if (fb == null) yield break;

            E5Archive arc = E5Archive.Parse(fb);
            if (arc == null || arc.Count == 0) yield break;

            byte[] pb = null;
            yield return OriginalAssets.LoadBytes("kw/Pmpalet.e5", b => pb = b);
            pal = PaletteUtil.Build(pb, 0);
            archive = arc;
        }

        public static Sprite Get(int faceId)
        {
            if (archive == null || faceId < 0 || faceId >= archive.Count) return null;

            Sprite s;
            if (cache.TryGetValue(faceId, out s)) return s;

            byte[] blob = null;
            try { blob = archive.Extract(faceId); } catch { return null; }
            if (blob == null || blob.Length < FaceW * FaceH) return null;

            var buf = new Color32[FaceW * FaceH];
            for (int y = 0; y < FaceH; y++)
            {
                int dstRow = FaceH - 1 - y;
                for (int x = 0; x < FaceW; x++)
                    buf[dstRow * FaceW + x] = pal[blob[y * FaceW + x]];
            }
            var tex = new Texture2D(FaceW, FaceH, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(buf);
            tex.Apply(false, true);

            s = Sprite.Create(tex, new Rect(0, 0, FaceW, FaceH), new Vector2(0.5f, 0.5f), 80f);
            cache[faceId] = s;
            return s;
        }
    }

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
        }

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

        static Sprite[] BuildFrames(int spriteId)
        {
            byte[] blob = null;
            try { blob = archive.Extract(spriteId); } catch { return null; }
            if (blob == null || blob.Length < FrameW * FrameH) return null;

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
