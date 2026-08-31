using System;

namespace KwGeminid
{
    /// <summary>
    /// 六角格 / 轴对齐网格坐标体系（q + r + s = 0）。
    /// </summary>
    public struct Hex : IEquatable<Hex>
    {
        public readonly int q;
        public readonly int s;
        public readonly int r;

        public Hex(int q, int s, int r)
        {
            this.q = q;
            this.s = s;
            this.r = r;
        }

        public int col
        {
            get
            {
                if (GridGeometry.Mode == GridMode.SquareFlat) return q;
                return q + (r - (r & 1)) / 2;
            }
        }

        public int row
        {
            get { return r; }
        }

        public static Hex Zero { get { return new Hex(0, 0, 0); } }

        public static Hex operator +(Hex a, Hex b)
        {
            return new Hex(a.q + b.q, a.s + b.s, a.r + b.r);
        }

        public static Hex operator -(Hex a, Hex b)
        {
            return new Hex(a.q - b.q, a.s - b.s, a.r - b.r);
        }

        public static bool operator ==(Hex a, Hex b)
        {
            return a.q == b.q && a.r == b.r && a.s == b.s;
        }

        public static bool operator !=(Hex a, Hex b)
        {
            return !(a == b);
        }

        public bool Equals(Hex other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return obj is Hex && this == (Hex)obj;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + q;
                hash = hash * 31 + r;
                return hash;
            }
        }

        public int Distance(Hex other)
        {
            if (GridGeometry.Mode == GridMode.SquareFlat)
            {
                return Math.Abs(col - other.col) + Math.Abs(row - other.row);
            }
            return (Math.Abs(q - other.q) + Math.Abs(s - other.s) + Math.Abs(r - other.r)) / 2;
        }

        public static readonly Hex[] HexDirections =
        {
            new Hex(1, -1, 0),
            new Hex(1, 0, -1),
            new Hex(0, 1, -1),
            new Hex(-1, 1, 0),
            new Hex(-1, 0, 1),
            new Hex(0, -1, 1)
        };

        public static readonly Hex[] SquareFlatDirections =
        {
            new Hex(0, 1, -1),  // 上 (r-1)
            new Hex(0, -1, 1),  // 下 (r+1)
            new Hex(-1, 1, 0),  // 左 (q-1)
            new Hex(1, -1, 0)   // 右 (q+1)
        };

        public Hex Neighbor(int dir)
        {
            if (GridGeometry.Mode == GridMode.SquareFlat)
            {
                return this + SquareFlatDirections[dir % 4];
            }
            return this + HexDirections[dir % 6];
        }

        public int NeighborCount
        {
            get { return GridGeometry.Mode == GridMode.SquareFlat ? 4 : 6; }
        }

        public override string ToString()
        {
            return string.Format("({0},{1})", col, row);
        }
    }
}
