using System;
using UnityEngine;

namespace KwCursor
{
    /// <summary>轴坐标六角格（尖顶朝上，odd-r 排布用于地图数据行）。</summary>
    [Serializable]
    public struct Hex : IEquatable<Hex>
    {
        public int q;
        public int r;

        public Hex(int q, int r)
        {
            this.q = q;
            this.r = r;
        }

        public static readonly Hex[] Directions =
        {
            new Hex(1, 0), new Hex(1, -1), new Hex(0, -1),
            new Hex(-1, 0), new Hex(-1, 1), new Hex(0, 1)
        };

        public Hex Neighbor(int i)
        {
            Hex d = Directions[i];
            return new Hex(q + d.q, r + d.r);
        }

        public static int Distance(Hex a, Hex b)
        {
            int dq = a.q - b.q;
            int dr = a.r - b.r;
            return (Mathf.Abs(dq) + Mathf.Abs(dr) + Mathf.Abs(dq + dr)) / 2;
        }

        public bool Equals(Hex other)
        {
            return q == other.q && r == other.r;
        }

        public override bool Equals(object obj)
        {
            return obj is Hex && Equals((Hex)obj);
        }

        public override int GetHashCode()
        {
            return (q * 397) ^ r;
        }

        public override string ToString()
        {
            return "(" + q + "," + r + ")";
        }

        public static bool operator ==(Hex a, Hex b) { return a.Equals(b); }
        public static bool operator !=(Hex a, Hex b) { return !a.Equals(b); }
    }

    /// <summary>六角格与世界坐标的换算（尖顶六角）。</summary>
    public static class HexLayout
    {
        /// <summary>六角格外接圆半径（世界单位）。</summary>
        public const float Size = 0.6f;

        public static readonly float Width = Mathf.Sqrt(3f) * Size; // 相邻列水平间距
        public const float VertSpacing = Size * 1.5f;               // 相邻行垂直间距

        public static Vector3 ToWorld(Hex h)
        {
            float x = Width * (h.q + h.r * 0.5f);
            float y = -VertSpacing * h.r; // 行号向下增长
            return new Vector3(x, y, 0f);
        }

        public static Hex FromWorld(Vector3 p)
        {
            float r = -p.y / VertSpacing;
            float q = p.x / Width - r * 0.5f;
            return Round(q, r);
        }

        static Hex Round(float q, float r)
        {
            float s = -q - r;
            int rq = Mathf.RoundToInt(q);
            int rr = Mathf.RoundToInt(r);
            int rs = Mathf.RoundToInt(s);
            float dq = Mathf.Abs(rq - q);
            float dr = Mathf.Abs(rr - r);
            float ds = Mathf.Abs(rs - s);
            if (dq > dr && dq > ds) rq = -rr - rs;
            else if (dr > ds) rr = -rq - rs;
            return new Hex(rq, rr);
        }

        /// <summary>odd-r 偏移坐标（地图行列）转轴坐标。</summary>
        public static Hex OffsetToAxial(int col, int row)
        {
            return new Hex(col - (row - (row & 1)) / 2, row);
        }
    }
}
