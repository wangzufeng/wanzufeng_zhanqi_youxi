using UnityEngine;

namespace KwCursor
{
    public enum TerrainType
    {
        Plain,   // 平地
        Forest,  // 森林
        Hill,    // 山地
        Water,   // 河流（不可通行）
        Village  // 村庄
    }

    /// <summary>
    /// 地形数据表。
    /// 注意：数值为近似实现，用于复现原作“地形影响移动与防御”的规则骨架，
    /// 并非逐项照搬原作数据，可按需校准。
    /// </summary>
    public static class TerrainTable
    {
        public static string DisplayName(TerrainType t)
        {
            switch (t)
            {
                case TerrainType.Plain: return "平地";
                case TerrainType.Forest: return "森林";
                case TerrainType.Hill: return "山地";
                case TerrainType.Water: return "河流";
                case TerrainType.Village: return "村庄";
                default: return "未知";
            }
        }

        /// <summary>进入该格的移动力消耗；返回 >=99 视为不可通行。</summary>
        public static int MoveCost(TerrainType t, UnitClassType c)
        {
            switch (t)
            {
                case TerrainType.Plain: return 1;
                case TerrainType.Village: return 1;
                case TerrainType.Forest: return c == UnitClassType.Cavalry ? 3 : 2;
                case TerrainType.Hill: return c == UnitClassType.Cavalry ? 3 : 2;
                case TerrainType.Water: return 99;
                default: return 1;
            }
        }

        /// <summary>站在该格时获得的防御加成（乘到防御力上）。</summary>
        public static float DefenseBonus(TerrainType t)
        {
            switch (t)
            {
                case TerrainType.Forest: return 0.15f;
                case TerrainType.Hill: return 0.30f;
                case TerrainType.Village: return 0.10f;
                default: return 0f;
            }
        }

        public static Color BaseColor(TerrainType t)
        {
            switch (t)
            {
                case TerrainType.Plain: return new Color(0.55f, 0.66f, 0.38f);
                case TerrainType.Forest: return new Color(0.22f, 0.45f, 0.24f);
                case TerrainType.Hill: return new Color(0.55f, 0.45f, 0.30f);
                case TerrainType.Water: return new Color(0.25f, 0.42f, 0.65f);
                case TerrainType.Village: return new Color(0.72f, 0.62f, 0.42f);
                default: return Color.magenta;
            }
        }

        public static TerrainType FromChar(char ch)
        {
            switch (ch)
            {
                case 'F': return TerrainType.Forest;
                case 'H': return TerrainType.Hill;
                case '~': return TerrainType.Water;
                case 'v': return TerrainType.Village;
                default: return TerrainType.Plain;
            }
        }
    }
}
