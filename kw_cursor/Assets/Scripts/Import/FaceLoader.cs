using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KwCursor
{
    /// <summary>
    /// 武将头像：Face.e5（Ls12，505 条，每条 64×80 8bpp，部分带 2 字节尾）。
    /// 调色板暂用 Pmpalet.e5 条目 0 近似（渲染自然，专用头像调色板位置仍待精确定位）。
    /// </summary>
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
            Debug.Log("FaceLoader: 头像 " + arc.Count + " 张就绪");
        }

        public static Sprite Get(int faceId)
        {
            if (archive == null || faceId < 0 || faceId >= archive.Count) return null;

            Sprite s;
            if (cache.TryGetValue(faceId, out s)) return s;

            byte[] blob;
            try { blob = archive.Extract(faceId); }
            catch (System.Exception) { cache[faceId] = null; return null; }
            if (blob.Length < FaceW * FaceH) { cache[faceId] = null; return null; }

            var buf = new Color32[FaceW * FaceH];
            for (int y = 0; y < FaceH; y++)
            {
                int dstRow = FaceH - 1 - y; // Texture2D 第 0 行在底部
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

    /// <summary>
    /// Imsg.e5 明文信息库：200 字节定长记录。
    /// 实测记录 249 为区段标记“Hex Battle Name Start”，250 起依序为各战场的战役名。
    /// </summary>
    public static class ImsgLoader
    {
        public const int RecordSize = 200;
        public const int BattleNameBase = 250;

        static byte[] data;

        public static bool Loaded { get { return data != null; } }

        public static IEnumerator TryLoad()
        {
            if (data != null) yield break;
            byte[] b = null;
            yield return OriginalAssets.LoadBytes("kw/Imsg.e5", x => b = x);
            if (b != null && b.Length > RecordSize) data = b;
        }

        /// <summary>指定战场序号的战役名；缺数据时返回 null。</summary>
        public static string BattleName(int mapIndex)
        {
            return Record(BattleNameBase + mapIndex);
        }

        public static string ItemDescription(int itemIndex)
        {
            return Record(itemIndex);
        }

        public static string UnitDescription(int classIndex)
        {
            return Record(350 + classIndex);
        }

        public static string OfficerHistory(int officerIndex)
        {
            return Record(450 + officerIndex);
        }

        public static string RetireLine(int officerIndex)
        {
            return Record(650 + officerIndex);
        }

        public static string CriticalLine(int officerIndex)
        {
            return Record(700 + officerIndex);
        }

        static string Record(int recordIndex)
        {
            if (data == null || recordIndex < 0) return null;
            int off = recordIndex * RecordSize;
            if (off + RecordSize > data.Length) return null;
            string s = KwText.DecodeGbkZ(data, off, RecordSize);
            return string.IsNullOrEmpty(s) ? null : s;
        }
    }

    /// <summary>Data.e5 条目 5 策略表：74 条 × 97 字节（名字 10B；MP 在偏移 15，威力候选在 18/19）。</summary>
    public class StrategyDef
    {
        public int index;
        public string name;
        public int mp;
        public int power;
    }

    public static class StrategyTable
    {
        public static List<StrategyDef> Strategies { get; private set; }
        public static bool Loaded { get { return Strategies != null && Strategies.Count > 0; } }

        public static IEnumerator TryLoad()
        {
            if (Loaded) yield break;
            byte[] bytes = null;
            yield return OriginalAssets.LoadBytes("kw/Data.e5", b => bytes = b);
            if (bytes == null) yield break;

            E5Archive arc = E5Archive.Parse(bytes);
            if (arc == null || arc.Count < 6) yield break;

            byte[] table;
            try { table = arc.Extract(5); }
            catch (System.Exception) { yield break; }

            var list = new List<StrategyDef>();
            const int stride = 97;
            for (int i = 0; i * stride + stride <= table.Length; i++)
            {
                int o = i * stride;
                string name = KwText.DecodeGbkZ(table, o, 10);
                if (string.IsNullOrEmpty(name)) continue;
                int mp = table[o + 15];
                int power = Mathf.Max(table[o + 18], table[o + 19]);
                if (mp <= 0) continue;
                list.Add(new StrategyDef { index = i, name = name, mp = mp, power = power });
            }
            Strategies = list;
            Debug.Log("StrategyTable: 策略 " + list.Count + " 条");
        }

        /// <summary>把第 idx 条策略转成本工程的伤害类技能；数值缺失时用保底值。</summary>
        public static Skill MakeDamageSkill(int idx)
        {
            if (Strategies == null || idx < 0 || idx >= Strategies.Count) return null;
            StrategyDef d = Strategies[idx];
            return new Skill(d.name, SkillKind.Damage, d.mp, 2, Mathf.Max(10, d.power));
        }
    }
}
