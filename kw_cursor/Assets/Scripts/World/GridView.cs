using System.Collections.Generic;
using UnityEngine;

namespace KwCursor
{
    /// <summary>地图渲染与高亮显示。支持程序化地块或原版战场画面作为背景。</summary>
    public class GridView
    {
        readonly HexGrid grid;
        readonly Transform root;
        readonly Dictionary<Hex, SpriteRenderer> overlays = new Dictionary<Hex, SpriteRenderer>();
        readonly List<Hex> activeOverlays = new List<Hex>();
        readonly Transform overlayRoot;

        public static readonly Color MoveColor = new Color(0.25f, 0.55f, 0.95f, 0.45f);
        public static readonly Color AttackColor = new Color(0.90f, 0.25f, 0.20f, 0.50f);
        public static readonly Color SkillColor = new Color(0.62f, 0.30f, 0.78f, 0.50f);
        public static readonly Color HealColor = new Color(0.25f, 0.85f, 0.40f, 0.50f);

        public GridView(HexGrid grid, Transform parent, Sprite mapArt = null)
        {
            this.grid = grid;

            root = new GameObject("Tiles").transform;
            root.SetParent(parent, false);
            overlayRoot = new GameObject("Overlays").transform;
            overlayRoot.SetParent(parent, false);

            if (mapArt != null)
            {
                // 原版战场画面作为整图背景（枢轴左上，对齐 (0,0) 格的左上角）
                var go = new GameObject("MapArt");
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(-GridGeometry.SquareW * 0.5f, GridGeometry.SquareH * 0.5f, 0.5f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = mapArt;
                sr.sortingOrder = 0;
                return;
            }

            Color outline = new Color(0.10f, 0.12f, 0.10f, 1f);
            foreach (Hex h in grid.AllCells)
            {
                TerrainType t = grid.GetTerrain(h);
                Color c = TerrainTable.BaseColor(t);
                float v = ((h.q * 7 + h.r * 13) % 5) * 0.015f;
                c = new Color(c.r + v, c.g + v, c.b + v, 1f);

                var go = new GameObject("Tile" + h);
                go.transform.SetParent(root, false);
                go.transform.position = GridGeometry.ToWorld(h);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = CellSprite(c, outline);
                sr.sortingOrder = 0;

                if (t == TerrainType.Village)
                {
                    var mark = new GameObject("VillageMark");
                    mark.transform.SetParent(go.transform, false);
                    mark.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                    var msr = mark.AddComponent<SpriteRenderer>();
                    msr.sprite = SpriteFactory.Circle(new Color(0.55f, 0.35f, 0.20f), new Color(0.3f, 0.2f, 0.1f), 0.16f);
                    msr.sortingOrder = 1;
                }
            }
        }

        static Sprite CellSprite(Color fill, Color outline)
        {
            if (GridGeometry.Mode == GridMode.SquareFlat)
                return SpriteFactory.Rect(fill, outline, GridGeometry.SquareW, GridGeometry.SquareH);
            return SpriteFactory.Hexagon(fill, outline);
        }

        static Sprite OverlaySprite()
        {
            if (GridGeometry.Mode == GridMode.SquareFlat)
                return SpriteFactory.Rect(Color.white, new Color(1f, 1f, 1f, 0.35f), GridGeometry.SquareW, GridGeometry.SquareH);
            return SpriteFactory.Hexagon(Color.white, new Color(1f, 1f, 1f, 0f));
        }

        public void ShowHighlights(IEnumerable<Hex> cells, Color color)
        {
            ClearHighlights();
            foreach (Hex h in cells)
            {
                if (!grid.InBounds(h)) continue;
                SpriteRenderer sr;
                if (!overlays.TryGetValue(h, out sr))
                {
                    var go = new GameObject("Ovl" + h);
                    go.transform.SetParent(overlayRoot, false);
                    go.transform.position = GridGeometry.ToWorld(h) + new Vector3(0f, 0f, -0.1f);
                    sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = OverlaySprite();
                    sr.sortingOrder = 2;
                    overlays[h] = sr;
                }
                sr.color = color;
                sr.gameObject.SetActive(true);
                activeOverlays.Add(h);
            }
        }

        public void ClearHighlights()
        {
            foreach (Hex h in activeOverlays)
            {
                SpriteRenderer sr;
                if (overlays.TryGetValue(h, out sr)) sr.gameObject.SetActive(false);
            }
            activeOverlays.Clear();
        }

        public Bounds WorldBounds()
        {
            var b = new Bounds();
            bool first = true;
            foreach (Hex h in grid.AllCells)
            {
                Vector3 p = GridGeometry.ToWorld(h);
                if (first) { b = new Bounds(p, Vector3.zero); first = false; }
                else b.Encapsulate(p);
            }
            b.Expand(new Vector3(2f, 2f, 0f));
            return b;
        }
    }
}
