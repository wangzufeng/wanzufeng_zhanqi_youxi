using UnityEngine;

namespace KwGeminid
{
    public enum GridMode
    {
        Hex,        // 尖顶六角格（odd-r 排布）
        SquareFlat  // 原版 96×24 轴对齐方格（原版 Hm 战场画面使用）
    }

    /// <summary>
    /// 网格几何计算：负责世界坐标与网格坐标间的双向转换。
    /// 支持六角格（odd-r）与原版 96×24 轴对齐矩形方格两种模式。
    /// </summary>
    public static class GridGeometry
    {
        public static GridMode Mode = GridMode.SquareFlat;

        public const float HexRadius = 0.6f;
        public static readonly float HexW = Mathf.Sqrt(3f) * HexRadius; // 约 1.0392
        public static readonly float HexH = 1.5f * HexRadius;           // 0.9

        // 原版 96×24 比例：宽 1.0f、高 0.25f
        public const float SquareCellW = 1.0f;
        public const float SquareCellH = 0.25f;

        public static Vector3 HexToWorld(Hex h)
        {
            if (Mode == GridMode.SquareFlat)
            {
                return new Vector3(h.col * SquareCellW, -h.row * SquareCellH, 0f);
            }
            float x = (h.col + 0.5f * (h.row & 1)) * HexW;
            float y = -h.row * HexH;
            return new Vector3(x, y, 0f);
        }

        public static Hex WorldToHex(Vector3 worldPos)
        {
            if (Mode == GridMode.SquareFlat)
            {
                int c = Mathf.RoundToInt(worldPos.x / SquareCellW);
                int r = Mathf.RoundToInt(-worldPos.y / SquareCellH);
                return FromColRow(c, r);
            }
            int row = Mathf.RoundToInt(-worldPos.y / HexH);
            float xOffset = 0.5f * (row & 1) * HexW;
            int col = Mathf.RoundToInt((worldPos.x - xOffset) / HexW);
            return FromColRow(col, row);
        }

        public static Hex FromColRow(int col, int row)
        {
            if (Mode == GridMode.SquareFlat)
            {
                return new Hex(col, -col - row, row);
            }
            int q = col - (row - (row & 1)) / 2;
            int r = row;
            return new Hex(q, -q - r, r);
        }

        public static Hex FromWorld(Vector3 worldPos)
        {
            return WorldToHex(worldPos);
        }
    }
}
