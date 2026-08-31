using System.Collections.Generic;
using UnityEngine;

namespace KwGeminid
{
    public class GridView
    {
        readonly HexGrid grid;
        readonly Transform parent;
        readonly Sprite backgroundArt;
        readonly Dictionary<Hex, SpriteRenderer> renderers = new Dictionary<Hex, SpriteRenderer>();
        GameObject highlightRoot;

        public GridView(HexGrid grid, Transform parent, Sprite backgroundArt = null)
        {
            this.grid = grid;
            this.parent = parent;
            this.backgroundArt = backgroundArt;
            Build();
        }

        void Build()
        {
            if (backgroundArt != null)
            {
                var bg = new GameObject("BattleMapBackground");
                bg.transform.SetParent(parent, false);
                var sr = bg.AddComponent<SpriteRenderer>();
                sr.sprite = backgroundArt;
                sr.sortingOrder = -10;
                bg.transform.position = new Vector3(
                    -0.5f * GridGeometry.SquareCellW,
                    0.5f * GridGeometry.SquareCellH,
                    0f);
            }
            else
            {
                for (int r = 0; r < grid.Height; r++)
                {
                    for (int c = 0; c < grid.Width; c++)
                    {
                        Hex h = GridGeometry.FromColRow(c, r);
                        TerrainType t = grid.GetTerrain(h);

                        var go = new GameObject(string.Format("Cell_{0}_{1}", c, r));
                        go.transform.SetParent(parent, false);
                        go.transform.position = GridGeometry.HexToWorld(h);

                        var sr = go.AddComponent<SpriteRenderer>();
                        sr.sprite = SpriteFactory.MakeHexSprite(t);
                        sr.sortingOrder = 0;
                        renderers[h] = sr;
                    }
                }
            }

            highlightRoot = new GameObject("Highlights");
            highlightRoot.transform.SetParent(parent, false);
        }

        public Bounds WorldBounds()
        {
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            for (int r = 0; r < grid.Height; r++)
            {
                for (int c = 0; c < grid.Width; c++)
                {
                    Vector3 p = GridGeometry.HexToWorld(GridGeometry.FromColRow(c, r));
                    if (p.x < minX) minX = p.x;
                    if (p.x > maxX) maxX = p.x;
                    if (p.y < minY) minY = p.y;
                    if (p.y > maxY) maxY = p.y;
                }
            }
            Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
            Vector3 size = new Vector3(maxX - minX + 2f, maxY - minY + 2f, 1f);
            return new Bounds(center, size);
        }

        public void ClearHighlights()
        {
            if (highlightRoot == null) return;
            for (int i = highlightRoot.transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(highlightRoot.transform.GetChild(i).gameObject);
            }
        }

        public void HighlightCells(IEnumerable<Hex> hexes, Color color)
        {
            foreach (Hex h in hexes)
            {
                if (!grid.InBounds(h)) continue;
                var go = new GameObject("HL_" + h);
                go.transform.SetParent(highlightRoot.transform, false);
                go.transform.position = GridGeometry.HexToWorld(h);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteFactory.MakeHighlightSprite(color);
                sr.color = color;
                sr.sortingOrder = 5;
            }
        }
    }

    public class PopupText : MonoBehaviour
    {
        public static void Show(Vector3 worldPos, string text, Color color)
        {
            var go = new GameObject("PopupText");
            go.transform.position = worldPos + Vector3.up * 0.4f;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 40;
            tm.characterSize = 0.08f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;

            var mr = go.GetComponent<MeshRenderer>();
            mr.sortingOrder = 50;

            var pt = go.AddComponent<PopupText>();
            pt.StartCoroutine(pt.AnimateAndDie());
        }

        System.Collections.IEnumerator AnimateAndDie()
        {
            float t = 0f;
            Vector3 startPos = transform.position;
            TextMesh tm = GetComponent<TextMesh>();
            Color c0 = tm.color;

            while (t < 0.8f)
            {
                t += Time.deltaTime;
                transform.position = startPos + Vector3.up * (t * 0.6f);
                tm.color = new Color(c0.r, c0.g, c0.b, 1f - (t / 0.8f));
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
