using UnityEngine;

namespace KwCursor
{
    public enum GridMode
    {
        Hex,        // 手工演示地图：尖顶六角格
        SquareFlat  // 原版真实地图：轴对齐 96×24 扁矩形格（实测无奇数行偏移），四方向移动
    }

    /// <summary>
    /// 网格几何抽象：坐标↔世界换算、邻接方向、距离。
    /// 原版战场画面每格 96×24 像素，这里以 1.6×0.4 世界单位等比呈现。
    /// </summary>
    public static class GridGeometry
    {
        public static GridMode Mode = GridMode.Hex;

        public const float SquareW = 1.6f;
        public const float SquareH = 0.4f;

        static readonly Hex[] SquareDirs =
        {
            new Hex(1, 0), new Hex(-1, 0), new Hex(0, 1), new Hex(0, -1)
        };

        public static int DirCount
        {
            get { return Mode == GridMode.Hex ? 6 : 4; }
        }

        public static Hex Step(Hex h, int d)
        {
            if (Mode == GridMode.Hex) return h.Neighbor(d);
            Hex s = SquareDirs[d];
            return new Hex(h.q + s.q, h.r + s.r);
        }

        public static int Distance(Hex a, Hex b)
        {
            if (Mode == GridMode.Hex) return Hex.Distance(a, b);
            return Mathf.Abs(a.q - b.q) + Mathf.Abs(a.r - b.r);
        }

        /// <summary>地图行列（偏移坐标）转内部坐标。</summary>
        public static Hex FromColRow(int col, int row)
        {
            if (Mode == GridMode.Hex) return HexLayout.OffsetToAxial(col, row);
            return new Hex(col, row);
        }

        public static Vector3 ToWorld(Hex h)
        {
            if (Mode == GridMode.Hex) return HexLayout.ToWorld(h);
            return new Vector3(h.q * SquareW, -h.r * SquareH, 0f);
        }

        public static Hex FromWorld(Vector3 p)
        {
            if (Mode == GridMode.Hex) return HexLayout.FromWorld(p);
            return new Hex(Mathf.RoundToInt(p.x / SquareW), Mathf.RoundToInt(-p.y / SquareH));
        }
    }
}
