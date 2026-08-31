using System.Collections.Generic;
using UnityEngine;

namespace KwCursor
{
    /// <summary>
    /// 章节制战役流程：依次使用原版真实地图作战，进度存于 PlayerPrefs。
    /// 我方武将取自原版 Data.e5 武将表（前 4 条记录），数值由原始字段缩放得到；
    /// 台词为本项目原创撰写（仓库内没有原版剧本文件 R/S_*.eex，无法还原原剧情）。
    /// </summary>
    public static class Campaign
    {
        const string ProgressKey = "kw_campaign_chapter";

        /// <summary>各章节使用的原版地图序号（对应 hexzmap/Hm/Spalet 同序号）。</summary>
        public static readonly int[] ChapterMaps = { 0, 1, 2, 3, 4 };

        public static int LoadProgress()
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(ProgressKey, 0), 0, ChapterMaps.Length - 1);
        }

        public static void SaveProgress(int chapter)
        {
            PlayerPrefs.SetInt(ProgressKey, Mathf.Clamp(chapter, 0, ChapterMaps.Length - 1));
            PlayerPrefs.Save();
        }

        public static ScenarioData Build(int chapter)
        {
            ScenarioData s = DemoScenario.Create(); // 兜底地图与阵容
            s.chapter = chapter;
            s.mapIndex = ChapterMaps[Mathf.Clamp(chapter, 0, ChapterMaps.Length - 1)];

            if (OfficerTable.Loaded)
            {
                ApplyRealOfficers(s, chapter);
            }

            // 章节标题：优先用原版 Imsg 的真实战役名
            string title = ImsgLoader.BattleName(s.mapIndex);
            if (string.IsNullOrEmpty(title)) title = "第 " + (chapter + 1) + " 战";

            string leader = LeaderName(s);
            s.introLines = new List<string[]>
            {
                new[] { "旁白", "第 " + (chapter + 1) + " 章 · " + title + "。敌军已在前方列阵。" },
                new[] { leader, "全军听令：稳住阵脚，先探敌情，再图破敌！" },
                new[] { "提示", "点击我方武将下达指令。击败敌方主将即可获胜；我方主将阵亡则战败。" }
            };
            s.outroLines = new List<string[]>
            {
                new[] { leader, title + "告捷！收兵休整，继续进军。" }
            };
            return s;
        }

        static string LeaderName(ScenarioData s)
        {
            foreach (UnitSpawn u in s.units)
                if (!u.data.isEnemy && u.data.isLeader) return u.data.name;
            return "主帅";
        }

        static void ApplyRealOfficers(ScenarioData s, int chapter)
        {
            var players = new List<UnitSpawn>();
            var enemies = new List<UnitSpawn>();
            foreach (UnitSpawn u in s.units)
            {
                if (u.data.isEnemy) enemies.Add(u);
                else players.Add(u);
            }

            // 我方：武将表前 4 条真实记录
            for (int i = 0; i < players.Count && i < 4 && i < OfficerTable.Officers.Count; i++)
            {
                Officer o = OfficerTable.Officers[i];
                UnitData d = players[i].data;
                d.name = o.name;
                d.shortLabel = o.name.Length > 0 ? o.name.Substring(0, 1) : "将";
                d.unitClass = MapClass(o.classId, i);
                d.level = Mathf.Max(1, o.level) + chapter;
                // 数值由原始字段缩放（映射为近似换算，字段语义仍在校准中）
                d.maxHp = 40 + o.hpBase / 4 + chapter * 6;
                d.hp = d.maxHp;
                d.atk = 10 + o.stats5[1] * 22 / 100 + chapter * 2;
                d.def = 8 + o.stats5[3] * 16 / 100 + chapter;
                d.mind = 6 + o.stats5[2] * 18 / 100;
                d.maxMp = 12 + o.stats5[2] / 8;
                d.mp = d.maxMp;
                d.isLeader = i == 0;
                d.faceId = o.faceId;
                d.spriteId = o.spriteId;
                d.itemId = i; // 演示：为前四名武将装备道具表前四件真实武器
                d.skills.Clear();
                // 策略优先取原版策略表（真实名称与 MP），缺表时回退内置技能
                Skill s0 = StrategyTable.MakeDamageSkill(0);
                Skill s1 = StrategyTable.MakeDamageSkill(1);
                if (i == 0)
                {
                    d.skills.Add(s0 != null ? s0 : Skill.FireAttack());
                    d.skills.Add(Skill.Medicine());
                }
                else if (d.mind >= 18)
                {
                    d.skills.Add(s1 != null ? s1 : Skill.FireAttack());
                }
            }

            // 敌方：随章节增强（沿用兜底阵容的占位，数值按章节缩放）
            for (int i = 0; i < enemies.Count; i++)
            {
                UnitData d = enemies[i].data;
                d.level += chapter;
                d.maxHp += chapter * 8;
                d.hp = d.maxHp;
                d.atk += chapter * 2;
                d.def += chapter;
                d.name = d.isLeader ? "敌将" : "敌兵";
                d.shortLabel = d.isLeader ? "将" : "兵";
                d.spriteId = 102; // 士兵形象（实测鬼卒类记录所用编号，占位可校准）
            }
        }

        static UnitClassType MapClass(int classId, int slot)
        {
            // 兵种号语义未校准前的占位映射：保证主帅骑兵、其余按记录散列
            if (slot == 0) return UnitClassType.Cavalry;
            switch (classId % 4)
            {
                case 0: return UnitClassType.Infantry;
                case 1: return UnitClassType.Cavalry;
                case 2: return UnitClassType.Archer;
                default: return UnitClassType.Strategist;
            }
        }
    }
}
