using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KwCursor
{
    /// <summary>Data.e5 条目 0 的武将记录（每条 32 字节，共 1024 条）。</summary>
    public class Officer
    {
        public int index;
        public string name;
        public int spriteId;   // 偏移 13-14 u16（战场形象，推测）
        public int faceId;     // 偏移 15（头像号，推测）
        public int classId;    // 偏移 16（兵种号，推测）
        public int[] stats5;   // 偏移 18-22 五项能力（实测 50-100 区间）
        public int hpBase;     // 偏移 23-24 u16（体力基数，实测 ~180-200）
        public int extra25;    // 偏移 25
        public int level;      // 偏移 27（多为 1，推测为初始等级）
    }

    /// <summary>
    /// 从用户自备的 Data.e5 读取武将表。
    /// 记录布局为对本仓库文件的实测结果；字段语义标注“推测”的仍在校准中。
    /// </summary>
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

            byte[] table;
            try { table = arc.Extract(0); }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Data.e5 武将表解压失败: " + ex.Message);
                yield break;
            }

            var list = new List<Officer>();
            int count = table.Length / 32;
            for (int i = 0; i < count; i++)
            {
                int o = i * 32;
                if (table[o] == 0) continue; // 空记录

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
                    extra25 = table[o + 25],
                    level = table[o + 27]
                };
                list.Add(off);
            }

            Officers = list;
            Debug.Log("OfficerTable: 读取武将 " + list.Count + " 名");
        }
    }
}
