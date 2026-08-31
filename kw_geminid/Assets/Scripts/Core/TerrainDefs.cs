using UnityEngine;

namespace KwGeminid
{
    public enum TerrainType
    {
        Plain,   // 平地
        Forest,  // 森林
        Hill,    // 山地
        Water,   // 河流（不可通行）
        Village  // 村庄
    }

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

    public static class TerrainIdMap
    {
        public static TerrainType ToTerrain(byte id)
        {
            switch (id)
            {
                case 0: return TerrainType.Plain;
                case 4: return TerrainType.Hill;
                case 2:
                case 13: return TerrainType.Forest;
                case 1:
                case 7:
                case 15: return TerrainType.Water;
                case 14:
                case 20:
                case 22:
                case 23:
                case 24: return TerrainType.Village;
                default: return TerrainType.Plain;
            }
        }
    }
}
