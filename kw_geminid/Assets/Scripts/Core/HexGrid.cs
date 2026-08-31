using System;
using System.Collections.Generic;
using UnityEngine;

namespace KwGeminid
{
    public class HexGrid
    {
        public readonly int Width;
        public readonly int Height;
        public readonly TerrainType[] Cells;
        readonly Dictionary<Hex, UnitView> units = new Dictionary<Hex, UnitView>();

        public HexGrid(int w, int h, TerrainType[] cells)
        {
            Width = w;
            Height = h;
            Cells = cells ?? new TerrainType[w * h];
        }

        public static HexGrid FromRows(string[] rows)
        {
            int h = rows.Length;
            int w = rows[0].Length;
            var cells = new TerrainType[w * h];
            for (int r = 0; r < h; r++)
            {
                string row = rows[r];
                for (int c = 0; c < w; c++)
                {
                    char ch = c < row.Length ? row[c] : '.';
                    cells[r * w + c] = TerrainTable.FromChar(ch);
                }
            }
            return new HexGrid(w, h, cells);
        }

        public static HexGrid FromCells(int w, int h, byte[] rawCells)
        {
            var cells = new TerrainType[w * h];
            for (int i = 0; i < cells.Length; i++)
            {
                byte v = (rawCells != null && i < rawCells.Length) ? rawCells[i] : (byte)0;
                cells[i] = TerrainIdMap.ToTerrain(v);
            }
            return new HexGrid(w, h, cells);
        }

        public bool InBounds(Hex h)
        {
            return h.col >= 0 && h.col < Width && h.row >= 0 && h.row < Height;
        }

        public TerrainType GetTerrain(Hex h)
        {
            if (!InBounds(h)) return TerrainType.Water;
            return Cells[h.row * Width + h.col];
        }

        public UnitView GetUnit(Hex h)
        {
            UnitView u;
            units.TryGetValue(h, out u);
            return u;
        }

        public void PlaceUnit(UnitView u, Hex h)
        {
            if (u == null) return;
            if (units.ContainsKey(u.Position)) units.Remove(u.Position);
            units[h] = u;
            u.Position = h;
            u.transform.position = GridGeometry.HexToWorld(h);
        }

        public void RemoveUnit(UnitView u)
        {
            if (u == null) return;
            if (units.ContainsKey(u.Position)) units.Remove(u.Position);
        }

        public void MoveUnit(UnitView u, Hex dest)
        {
            if (u == null) return;
            RemoveUnit(u);
            PlaceUnit(u, dest);
        }

        public Dictionary<Hex, int> Reachable(UnitView u, out Dictionary<Hex, Hex> parents)
        {
            var costSoFar = new Dictionary<Hex, int>();
            parents = new Dictionary<Hex, Hex>();
            if (u == null) return costSoFar;

            Hex start = u.Position;
            costSoFar[start] = 0;

            var open = new List<Hex> { start };
            int maxMove = u.Data.Move;
            UnitClassType uClass = u.Data.unitClass;

            while (open.Count > 0)
            {
                open.Sort((a, b) => costSoFar[a].CompareTo(costSoFar[b]));
                Hex cur = open[0];
                open.RemoveAt(0);

                int curCost = costSoFar[cur];
                if (curCost >= maxMove) continue;

                for (int dir = 0; dir < cur.NeighborCount; dir++)
                {
                    Hex nxt = cur.Neighbor(dir);
                    if (!InBounds(nxt)) continue;

                    UnitView occ = GetUnit(nxt);
                    if (occ != null && occ.Data.isEnemy != u.Data.isEnemy) continue;

                    int stepCost = TerrainTable.MoveCost(GetTerrain(nxt), uClass);
                    if (stepCost >= 99) continue;

                    int newCost = curCost + stepCost;
                    if (newCost > maxMove) continue;

                    if (!costSoFar.ContainsKey(nxt) || newCost < costSoFar[nxt])
                    {
                        costSoFar[nxt] = newCost;
                        parents[nxt] = cur;
                        if (!open.Contains(nxt)) open.Add(nxt);
                    }
                }
            }
            return costSoFar;
        }

        public List<Hex> AttackRange(Hex from, int minRange, int maxRange)
        {
            var res = new List<Hex>();
            for (int r = 0; r < Height; r++)
            {
                for (int c = 0; c < Width; c++)
                {
                    Hex target = GridGeometry.FromColRow(c, r);
                    int d = from.Distance(target);
                    if (d >= minRange && d <= maxRange) res.Add(target);
                }
            }
            return res;
        }
    }
}
