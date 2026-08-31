using System.Collections.Generic;
using UnityEngine;

namespace KwCursor
{
    /// <summary>战场逻辑网格：地形、占位、寻路与范围计算。</summary>
    public class HexGrid
    {
        public int Cols { get; private set; }
        public int Rows { get; private set; }

        readonly Dictionary<Hex, TerrainType> terrain = new Dictionary<Hex, TerrainType>();
        readonly Dictionary<Hex, UnitView> occupants = new Dictionary<Hex, UnitView>();

        public IEnumerable<Hex> AllCells { get { return terrain.Keys; } }

        public static HexGrid FromRows(string[] rows)
        {
            var g = new HexGrid();
            g.Rows = rows.Length;
            g.Cols = rows[0].Length;
            for (int row = 0; row < rows.Length; row++)
            {
                string line = rows[row];
                for (int col = 0; col < line.Length; col++)
                {
                    Hex h = GridGeometry.FromColRow(col, row);
                    g.terrain[h] = TerrainTable.FromChar(line[col]);
                }
            }
            return g;
        }

        /// <summary>从原版 hexzmap 地形 ID 网格构建（row 主序）。</summary>
        public static HexGrid FromCells(int w, int h, byte[] cells)
        {
            var g = new HexGrid();
            g.Rows = h;
            g.Cols = w;
            for (int row = 0; row < h; row++)
            {
                for (int col = 0; col < w; col++)
                {
                    Hex hex = GridGeometry.FromColRow(col, row);
                    g.terrain[hex] = TerrainIdMap.ToTerrain(cells[row * w + col]);
                }
            }
            return g;
        }

        public bool InBounds(Hex h) { return terrain.ContainsKey(h); }

        public TerrainType GetTerrain(Hex h)
        {
            TerrainType t;
            return terrain.TryGetValue(h, out t) ? t : TerrainType.Plain;
        }

        public UnitView GetUnit(Hex h)
        {
            UnitView u;
            return occupants.TryGetValue(h, out u) ? u : null;
        }

        public void PlaceUnit(UnitView u, Hex h)
        {
            occupants[h] = u;
            u.Pos = h;
            u.transform.position = GridGeometry.ToWorld(h);
        }

        public void MoveUnitInstant(UnitView u, Hex to)
        {
            occupants.Remove(u.Pos);
            occupants[to] = u;
            u.Pos = to;
            u.transform.position = GridGeometry.ToWorld(to);
        }

        public void RemoveUnit(UnitView u)
        {
            UnitView cur;
            if (occupants.TryGetValue(u.Pos, out cur) && cur == u)
                occupants.Remove(u.Pos);
        }

        /// <summary>
        /// Dijkstra 求可达格与最短耗费。不可穿过敌军，可穿过友军。
        /// 返回 hex→耗费；parents 用于回溯路径。
        /// </summary>
        public Dictionary<Hex, int> MoveField(UnitView u, out Dictionary<Hex, Hex> parents)
        {
            var dist = new Dictionary<Hex, int>();
            parents = new Dictionary<Hex, Hex>();
            dist[u.Pos] = 0;
            var open = new List<Hex> { u.Pos };

            while (open.Count > 0)
            {
                int best = 0;
                for (int i = 1; i < open.Count; i++)
                    if (dist[open[i]] < dist[open[best]]) best = i;
                Hex cur = open[best];
                open.RemoveAt(best);

                for (int d = 0; d < GridGeometry.DirCount; d++)
                {
                    Hex nb = GridGeometry.Step(cur, d);
                    if (!InBounds(nb)) continue;

                    UnitView occ = GetUnit(nb);
                    if (occ != null && occ.Data.isEnemy != u.Data.isEnemy) continue; // 敌军阻挡

                    int step = TerrainTable.MoveCost(GetTerrain(nb), u.Data.unitClass);
                    if (step >= 99) continue;

                    int nd = dist[cur] + step;
                    if (nd > u.Data.Move) continue;

                    int old;
                    if (dist.TryGetValue(nb, out old) && old <= nd) continue;

                    dist[nb] = nd;
                    parents[nb] = cur;
                    if (!open.Contains(nb)) open.Add(nb);
                }
            }
            return dist;
        }

        /// <summary>可作为移动终点的格子（未被其他单位占用）。</summary>
        public List<Hex> Destinations(UnitView u, Dictionary<Hex, int> field)
        {
            var list = new List<Hex>();
            foreach (var kv in field)
            {
                UnitView occ = GetUnit(kv.Key);
                if (occ == null || occ == u) list.Add(kv.Key);
            }
            return list;
        }

        public List<Hex> BuildPath(Hex start, Hex goal, Dictionary<Hex, Hex> parents)
        {
            var path = new List<Hex>();
            Hex cur = goal;
            path.Add(cur);
            int guard = 0;
            while (cur != start && guard++ < 1024)
            {
                Hex p;
                if (!parents.TryGetValue(cur, out p)) break;
                cur = p;
                path.Add(cur);
            }
            path.Reverse();
            return path;
        }

        /// <summary>从 origin 出发、射程 [min,max] 内的所有敌对目标。</summary>
        public List<UnitView> TargetsFrom(Hex origin, UnitView attacker, int minRange, int maxRange)
        {
            var res = new List<UnitView>();
            foreach (var kv in occupants)
            {
                UnitView t = kv.Value;
                if (t == null || t == attacker) continue;
                if (t.Data.isEnemy == attacker.Data.isEnemy) continue;
                int d = GridGeometry.Distance(origin, t.Pos);
                if (d >= minRange && d <= maxRange) res.Add(t);
            }
            return res;
        }

        /// <summary>射程 [min,max] 内的友方目标（含自己，用于治疗类策略）。</summary>
        public List<UnitView> AlliesFrom(Hex origin, UnitView caster, int maxRange)
        {
            var res = new List<UnitView>();
            foreach (var kv in occupants)
            {
                UnitView t = kv.Value;
                if (t == null) continue;
                if (t.Data.isEnemy != caster.Data.isEnemy) continue;
                if (GridGeometry.Distance(origin, t.Pos) <= maxRange) res.Add(t);
            }
            return res;
        }
    }
}
