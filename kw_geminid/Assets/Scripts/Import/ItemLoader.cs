using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KwGeminid
{
    public class ItemDef
    {
        public int index;
        public string name;
        public string description;
        public int attackBonus;
        public int defenseBonus;
    }

    public static class ItemLoader
    {
        const int RecordSize = 25;
        const int IconW = 32;
        const int IconH = 32;

        static E5Archive iconArchive;
        static Color32[] palette;
        static readonly Dictionary<int, Sprite> iconCache = new Dictionary<int, Sprite>();

        public static List<ItemDef> Items { get; private set; }
        public static bool Loaded { get { return Items != null && Items.Count > 0; } }

        public static IEnumerator TryLoad()
        {
            if (Loaded) yield break;

            byte[] dataBytes = null;
            yield return OriginalAssets.LoadBytes("kw/Data.e5", b => dataBytes = b);
            if (dataBytes == null) yield break;
            E5Archive dataArchive = E5Archive.Parse(dataBytes);
            if (dataArchive == null || dataArchive.Count < 2) yield break;

            byte[] table = null;
            try { table = dataArchive.Extract(1); } catch { yield break; }
            if (table == null) yield break;

            byte[] iconBytes = null;
            yield return OriginalAssets.LoadBytes("kw/Item.e5", b => iconBytes = b);
            iconArchive = E5Archive.Parse(iconBytes);

            byte[] palBytes = null;
            yield return OriginalAssets.LoadBytes("kw/Pmpalet.e5", b => palBytes = b);
            palette = PaletteUtil.Build(palBytes, 0);

            var list = new List<ItemDef>();
            int count = table.Length / RecordSize;
            for (int i = 0; i < count; i++)
            {
                int o = i * RecordSize;
                string name = KwText.DecodeGbkZ(table, o, 12);
                if (string.IsNullOrEmpty(name)) continue;

                int atk = table[o + 15] <= 50 ? table[o + 15] : 0;
                int def = table[o + 16] <= 50 ? table[o + 16] : 0;
                list.Add(new ItemDef
                {
                    index = i,
                    name = name,
                    description = ImsgLoader.ItemDescription(i),
                    attackBonus = atk,
                    defenseBonus = def
                });
            }
            Items = list;
        }

        public static ItemDef GetItem(int itemId)
        {
            if (Items == null) return null;
            foreach (ItemDef item in Items)
                if (item.index == itemId) return item;
            return null;
        }

        public static Sprite GetIcon(int itemId)
        {
            if (iconArchive == null || palette == null || itemId < 0 || itemId >= iconArchive.Count)
                return null;

            Sprite cached;
            if (iconCache.TryGetValue(itemId, out cached)) return cached;

            byte[] blob = null;
            try { blob = iconArchive.Extract(itemId); } catch { return null; }
            if (blob == null || blob.Length < IconW * IconH) return null;

            var pixels = new Color32[IconW * IconH];
            for (int y = 0; y < IconH; y++)
            {
                int dstRow = IconH - 1 - y;
                for (int x = 0; x < IconW; x++)
                {
                    byte v = blob[y * IconW + x];
                    pixels[dstRow * IconW + x] = v == 0
                        ? new Color32(0, 0, 0, 0)
                        : palette[v];
                }
            }

            var tex = new Texture2D(IconW, IconH, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, IconW, IconH),
                new Vector2(0.5f, 0.5f), 32f);
            iconCache[itemId] = sprite;
            return sprite;
        }
    }

    public class Officer
    {
        public int index;
        public string name;
        public int spriteId;
        public int faceId;
        public int classId;
        public int[] stats5;
        public int hpBase;
        public int level;
    }

    public static class OfficerTable
    {
        public static List<Officer> Officers { get; private set; }
        public static bool Loaded { get { return Officers != null && Officers.Count > 0; } }

        public static IEnumerator TryLoad()
        {
            if (Loaded) yield break;
            byte[] bytes = null;
            yield return OriginalAssets.LoadBytes("kw/Data.e5", b => bytes = b);
            if (bytes == null) yield break;

            E5Archive arc = E5Archive.Parse(bytes);
            if (arc == null || arc.Count < 1) yield break;

            byte[] table = null;
            try { table = arc.Extract(0); } catch { yield break; }
            if (table == null) yield break;

            var list = new List<Officer>();
            int count = table.Length / 32;
            for (int i = 0; i < count; i++)
            {
                int o = i * 32;
                if (table[o] == 0) continue;

                var off = new Officer
                {
                    index = i,
                    name = KwText.DecodeGbkZ(table, o, 13, "武将" + i),
                    spriteId = table[o + 13] | (table[o + 14] << 8),
                    faceId = table[o + 15],
                    classId = table[o + 16],
                    stats5 = new[]
                    {
                        (int)table[o + 18], (int)table[o + 19], (int)table[o + 20],
                        (int)table[o + 21], (int)table[o + 22]
                    },
                    hpBase = table[o + 23] | (table[o + 24] << 8),
                    level = table[o + 27]
                };
                list.Add(off);
            }
            Officers = list;
        }
    }

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

            byte[] table = null;
            try { table = arc.Extract(5); } catch { yield break; }
            if (table == null) yield break;

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
        }

        public static Skill MakeDamageSkill(int idx)
        {
            if (Strategies == null || idx < 0 || idx >= Strategies.Count) return null;
            StrategyDef d = Strategies[idx];
            return new Skill(d.name, SkillKind.Damage, d.mp, 2, Mathf.Max(10, d.power));
        }
    }

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

        public static string BattleName(int mapIndex)
        {
            return Record(BattleNameBase + mapIndex);
        }

        public static string ItemDescription(int itemIndex)
        {
            return Record(itemIndex);
        }

        static string Record(int recordIndex)
        {
            if (data == null || recordIndex < 0) return null;
            int off = recordIndex * RecordSize;
            if (off + RecordSize > data.Length) return null;
            return KwText.DecodeGbkZ(data, off, RecordSize);
        }
    }

    public static class MinimapLoader
    {
        public static IEnumerator Load(int mapIndex, int mapW, int mapH, System.Action<Sprite> onDone)
        {
            byte[] smlData = null;
            yield return OriginalAssets.LoadBytes("kw/Smlmap.e5", b => smlData = b);
            if (smlData == null) { onDone(null); yield break; }

            E5Archive arc = E5Archive.Parse(smlData);
            if (arc == null || mapIndex >= arc.Count) { onDone(null); yield break; }

            byte[] img = null;
            try { img = arc.Extract(mapIndex); } catch { onDone(null); yield break; }

            int mw = mapW * 6;
            int mh = mapH * 6;
            if (img == null || img.Length < mw * mh) { onDone(null); yield break; }

            byte[] spaletData = null;
            yield return OriginalAssets.LoadBytes("kw/Spalet.e5", b => spaletData = b);
            Color32[] pal = PaletteUtil.Build(spaletData, mapIndex);

            var pixels = new Color32[mw * mh];
            for (int y = 0; y < mh; y++)
            {
                int dstRow = (mh - 1) - y;
                for (int x = 0; x < mw; x++)
                    pixels[dstRow * mw + x] = pal[img[y * mw + x]];
            }

            var tex = new Texture2D(mw, mh, TextureFormat.RGB24, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            onDone(Sprite.Create(tex, new Rect(0, 0, mw, mh), new Vector2(0.5f, 0.5f), 100f));
        }
    }
}
