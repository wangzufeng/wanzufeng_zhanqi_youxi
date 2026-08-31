using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KwCursor
{
    public class ItemDef
    {
        public int index;
        public string name;
        public string description;
        public int attackBonus;
        public int defenseBonus;
    }

    /// <summary>
    /// 道具系统：
    /// - Data.e5 条目 1：104 条 × 25 字节，名字在记录开头；
    /// - Item.e5：126 张 32×32 8bpp 图标，透明键 0；
    /// - Imsg.e5 记录 0 起：同序号道具说明；
    /// - 调色板：Pmpalet.e5 条目 0（实测图标颜色正确）。
    /// </summary>
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

            byte[] table;
            try { table = dataArchive.Extract(1); }
            catch (System.Exception) { yield break; }

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

                // 后半段字段语义仍在校准；取合理范围内的两个候选字节作近似加成。
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
            Debug.Log("ItemLoader: 道具 " + list.Count + " 条，图标 " +
                      (iconArchive != null ? iconArchive.Count.ToString() : "0") + " 张");
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

            byte[] blob;
            try { blob = iconArchive.Extract(itemId); }
            catch (System.Exception) { iconCache[itemId] = null; return null; }
            if (blob.Length < IconW * IconH) { iconCache[itemId] = null; return null; }

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
}
