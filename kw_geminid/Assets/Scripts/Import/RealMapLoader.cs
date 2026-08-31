using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KwGeminid
{
    public static class HexzMap
    {
        public static bool TryParse(byte[] blob, out int w, out int h, out byte[] cells)
        {
            w = 0; h = 0; cells = null;
            if (blob == null || blob.Length < 2) return false;
            w = blob[0] / 3;
            h = blob[1] / 3;
            if (w <= 0 || h <= 0 || 2 + w * h > blob.Length) return false;
            cells = new byte[w * h];
            System.Array.Copy(blob, 2, cells, 0, w * h);
            return true;
        }
    }

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
            if (arc == null || arc.Count == 0) yield break;

            int mapIndex = Mathf.Clamp(s.mapIndex, 0, arc.Count - 1);
            byte[] blob = null;
            try { blob = arc.Extract(mapIndex); } catch { yield break; }

            int w, h;
            byte[] cells;
            if (!HexzMap.TryParse(blob, out w, out h, out cells)) yield break;

            GridGeometry.Mode = GridMode.SquareFlat;
            s.realW = w;
            s.realH = h;
            s.realCells = cells;

            RepositionSpawns(s, w, h, cells);

            Sprite art = null;
            yield return BattleArtLoader.Load(mapIndex, w, h, sp => art = sp);
            s.artSprite = art;

            Sprite mini = null;
            yield return MinimapLoader.Load(mapIndex, w, h, sp => mini = sp);
            s.minimapSprite = mini;

            Loaded = true;
        }

        static bool Passable(byte v)
        {
            TerrainType t = TerrainIdMap.ToTerrain(v);
            return TerrainTable.MoveCost(t, UnitClassType.Infantry) < 99;
        }

        static void RepositionSpawns(ScenarioData s, int w, int h, byte[] cells)
        {
            var taken = new HashSet<int>();
            List<int> Collect(IEnumerable<int> order, int need)
            {
                var res = new List<int>();
                foreach (int i in order)
                {
                    if (res.Count >= need) break;
                    if (taken.Contains(i) || !Passable(cells[i])) continue;
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
                            yield return row * w + col;
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

    public static class BattleArtLoader
    {
        public const int TileW = 96;
        public const int TileH = 24;
        public const int TileBytes = TileW * TileH;

        public static IEnumerator Load(int mapIndex, int mapW, int mapH, System.Action<Sprite> onDone)
        {
            string hmFile = string.Format("kw/Hm{0:D2}.e5", mapIndex);
            byte[] hmData = null;
            yield return OriginalAssets.LoadBytes(hmFile, b => hmData = b);
            if (hmData == null || hmData.Length < mapW * mapH * TileBytes)
            {
                onDone(null);
                yield break;
            }

            byte[] spaletData = null;
            yield return OriginalAssets.LoadBytes("kw/Spalet.e5", b => spaletData = b);
            Color32[] pal = PaletteUtil.Build(spaletData, mapIndex);

            int imw = mapW * TileW;
            int imh = mapH * TileH;
            var pixels = new Color32[imw * imh];

            for (int ti = 0; ti < mapW * mapH; ti++)
            {
                int tx = (ti % mapW) * TileW;
                int ty = (ti / mapW) * TileH;
                int tileOff = ti * TileBytes;

                for (int y = 0; y < TileH; y++)
                {
                    int dstRow = (imh - 1) - (ty + y);
                    int srcRowOff = tileOff + y * TileW;
                    for (int x = 0; x < TileW; x++)
                    {
                        byte v = hmData[srcRowOff + x];
                        pixels[dstRow * imw + (tx + x)] = pal[v];
                    }
                }
            }

            var tex = new Texture2D(imw, imh, TextureFormat.RGB24, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            float ppu = TileW / GridGeometry.SquareCellW;
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, imw, imh), new Vector2(0f, 1f), ppu);
            onDone(sp);
        }
    }
}
