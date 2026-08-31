using System.Collections.Generic;

namespace KwCursor
{
    public class UnitSpawn
    {
        public UnitData data;
        public int col;
        public int row;

        public UnitSpawn(UnitData data, int col, int row)
        {
            this.data = data;
            this.col = col;
            this.row = row;
        }
    }

    public class ScenarioData
    {
        public string[] mapRows;
        public List<UnitSpawn> units;
        public List<string[]> introLines;
        public List<string[]> outroLines;

        public int chapter;          // 章节序号
        public int mapIndex;         // 原版地图序号（hexzmap/Hm/Spalet 同序号）

        // 由 RealMapLoader 填充：加载成功时优先使用原版地形网格与画面
        public int realW;
        public int realH;
        public byte[] realCells;
        public UnityEngine.Sprite artSprite;
        public UnityEngine.Sprite minimapSprite;
    }

    /// <summary>
    /// 演示关卡：颍川遭遇战（背景取材于公有领域的东汉黄巾之乱史实；
    /// 台词为本项目原创撰写，不复制任何原作剧本文本）。
    /// 地图字符：'.'平地 'F'森林 'H'山地 '~'河流 'v'村庄
    /// </summary>
    public static class DemoScenario
    {
        public static ScenarioData Create()
        {
            var s = new ScenarioData();

            s.mapRows = new[]
            {
                "....F....HH.",
                "..F...~....H",
                "......~.....",
                "..v.........",
                "......~~..H.",
                ".F....~.....",
                "......~....H",
                "..F.........",
                ".........HH."
            };

            s.units = new List<UnitSpawn>
            {
                // 我方
                new UnitSpawn(Hero("曹操", "曹", UnitClassType.Cavalry, 5, 70, 24, 24, 15, 17, true,
                    new List<Skill> { Skill.FireAttack(), Skill.Medicine() }), 2, 4),
                new UnitSpawn(Hero("夏侯惇", "惇", UnitClassType.Infantry, 4, 64, 6, 23, 13, 8, false, null), 3, 3),
                new UnitSpawn(Hero("夏侯渊", "渊", UnitClassType.Archer, 4, 55, 6, 21, 10, 10, false, null), 2, 5),
                new UnitSpawn(Hero("曹洪", "洪", UnitClassType.Cavalry, 3, 58, 6, 20, 12, 7, false, null), 3, 5),

                // 敌方（黄巾军）
                new UnitSpawn(Foe("黄巾兵", "贼", UnitClassType.Infantry, 2, 40, 16, 7, 4, false), 8, 3),
                new UnitSpawn(Foe("黄巾兵", "贼", UnitClassType.Infantry, 2, 40, 16, 7, 4, false), 8, 5),
                new UnitSpawn(Foe("黄巾兵", "贼", UnitClassType.Infantry, 2, 38, 15, 7, 4, false), 9, 2),
                new UnitSpawn(Foe("黄巾兵", "贼", UnitClassType.Infantry, 2, 38, 15, 7, 4, false), 9, 6),
                new UnitSpawn(Foe("黄巾弓手", "弓", UnitClassType.Archer, 2, 36, 15, 6, 4, false), 9, 4),
                new UnitSpawn(Foe("波才", "波", UnitClassType.Infantry, 5, 66, 21, 12, 9, true), 10, 4),
            };

            s.introLines = new List<string[]>
            {
                new[] { "旁白", "中平元年，黄巾之乱骤起。朝廷诏令各路兵马讨伐，曹操引军驰援颍川，与贼军先锋在旷野遭遇。" },
                new[] { "曹操", "贼众虽多，阵形散乱，不足为惧。诸将听令，随我破敌！" },
                new[] { "夏侯惇", "得令！末将愿为先锋，直取贼将！" },
                new[] { "提示", "点击我方武将下达指令：移动、攻击、策略、待机。单指拖动平移地图，双指捏合缩放。击败敌将波才即可获胜；曹操阵亡则战败。" }
            };

            return s;
        }

        static UnitData Hero(string name, string label, UnitClassType cls, int lv,
            int hp, int mp, int atk, int def, int mind, bool leader, List<Skill> skills)
        {
            var d = new UnitData
            {
                name = name,
                shortLabel = label,
                unitClass = cls,
                level = lv,
                maxHp = hp,
                hp = hp,
                maxMp = mp,
                mp = mp,
                atk = atk,
                def = def,
                mind = mind,
                isEnemy = false,
                isLeader = leader
            };
            if (skills != null) d.skills = skills;
            return d;
        }

        static UnitData Foe(string name, string label, UnitClassType cls, int lv,
            int hp, int atk, int def, int mind, bool leader)
        {
            return new UnitData
            {
                name = name,
                shortLabel = label,
                unitClass = cls,
                level = lv,
                maxHp = hp,
                hp = hp,
                maxMp = 0,
                mp = 0,
                atk = atk,
                def = def,
                mind = mind,
                isEnemy = true,
                isLeader = leader
            };
        }
    }
}
