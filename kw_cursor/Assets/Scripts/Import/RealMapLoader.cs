using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KwCursor
{
    /// <summary>
    /// 尝试从用户自备的原版数据加载真实战场（地形 + 画面）。
    /// 成功时切换为方格模式并改写演示关卡；失败时保持手工演示地图，不影响运行。
    /// </summary>
    public static class RealMapLoader
    {
        public static bool Loaded { get; private set; }

        public static IEnumerator TryApply(ScenarioData s)
        {
            Loaded = false;
            byte[] bytes = null;
            yield return OriginalAssets.LoadBytes("kw/hexzmap.e5", b => bytes = b);
            if (bytes == null) yield break;

            E5Archive arc = E5Archive.Parse(bytes);
            if (arc == null || arc.Count == 0)
            {
                Debug.LogWarning("hexzmap.e5 不是有效的 Ls11/Ls12 封包");
                yield break;
            }

            int mapIndex = Mathf.Clamp(s.mapIndex, 0, arc.Count - 1);
            byte[] blob;
            try
            {
                blob = arc.Extract(mapIndex);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("hexzmap 条目解压失败: " + ex.Message);
                yield break;
            }

            int w, h;
            byte[] cells;
            if (!HexzMap.TryParse(blob, out w, out h, out cells))
            {
                Debug.LogWarning("hexzmap 条目结构不符合预期");
                yield break;
            }

            // 原版真实地图：轴对齐 96×24 方格（实测），四方向移动
            GridGeometry.Mode = GridMode.SquareFlat;

            s.realW = w;
            s.realH = h;
            s.realCells = cells;
            RepositionSpawns(s, w, h, cells);

            // 原版战场画面（可缺省：无 Hm 或 Spalet 时保持地形色块渲染）
            Sprite art = null;
            yield return BattleArtLoader.Load(mapIndex, w, h, sp => art = sp);
            s.artSprite = art;

            // 原版小地图（可缺省）
            Sprite mini = null;
            yield return MinimapLoader.Load(mapIndex, w, h, sp => mini = sp);
            s.minimapSprite = mini;

            s.introLines.Insert(0, new[]
            {
                "系统",
                "已加载原版战场 " + (mapIndex + 1) + " 号（" + w + "×" + h + "）" +
                (art != null ? "，含原版画面与调色板。" : "，画面文件缺失，使用地形配色。")
            });
            Loaded = true;
        }

        static bool Passable(byte v)
        {
            TerrainType t = TerrainIdMap.ToTerrain(v);
            return TerrainTable.MoveCost(t, UnitClassType.Infantry) < 99;
        }

        /// <summary>把部队安置到真实地图上：我方靠下方，敌方靠中心。</summary>
        static void RepositionSpawns(ScenarioData s, int w, int h, byte[] cells)
        {
            var taken = new HashSet<int>();

            List<int> Collect(IEnumerable<int> order, int need)
            {
                var res = new List<int>();
                foreach (int i in order)
                {
                    if (res.Count >= need) break;
                    if (taken.Contains(i)) continue;
                    if (!Passable(cells[i])) continue;
                    res.Add(i);
                    taken.Add(i);
                }
                return res;
            }

            IEnumerable<int> FromBottom()
            {
                for (int row = h - 2; row >= 1; row--)
                    for (int col = 2; col < w - 2; col++)
                        yield return row * w + col;
            }

            IEnumerable<int> FromCenter()
            {
                int cy = h / 2, cx = w / 2;
                for (int radius = 0; radius < w + h; radius++)
                    for (int row = Mathf.Max(1, cy - radius); row <= Mathf.Min(h - 2, cy + radius); row++)
                        for (int col = Mathf.Max(1, cx - radius); col <= Mathf.Min(w - 2, cx + radius); col++)
                        {
                            if (Mathf.Max(Mathf.Abs(row - cy), Mathf.Abs(col - cx)) != radius) continue;
                            yield return row * w + col;
                        }
            }

            var playerSpawns = new List<UnitSpawn>();
            var enemySpawns = new List<UnitSpawn>();
            foreach (UnitSpawn u in s.units)
            {
                if (u.data.isEnemy) enemySpawns.Add(u);
                else playerSpawns.Add(u);
            }

            List<int> enemyCells = Collect(FromCenter(), enemySpawns.Count);
            List<int> playerCells = Collect(FromBottom(), playerSpawns.Count);

            for (int i = 0; i < enemySpawns.Count && i < enemyCells.Count; i++)
            {
                enemySpawns[i].col = enemyCells[i] % w;
                enemySpawns[i].row = enemyCells[i] / w;
            }
            for (int i = 0; i < playerSpawns.Count && i < playerCells.Count; i++)
            {
                playerSpawns[i].col = playerCells[i] % w;
                playerSpawns[i].row = playerCells[i] / w;
            }
        }
    }
}
