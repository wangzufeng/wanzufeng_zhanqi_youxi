using System.Collections.Generic;
using UnityEngine;

namespace KwGeminid
{
    public class UnitSpawn
    {
        public int col;
        public int row;
        public UnitData data;
    }

    public class ScenarioData
    {
        public int chapter;
        public int mapIndex;
        public string[] mapRows;
        public List<UnitSpawn> units = new List<UnitSpawn>();
        public List<string[]> introLines = new List<string[]>();
        public List<string[]> outroLines = new List<string[]>();

        public int realW;
        public int realH;
        public byte[] realCells;
        public Sprite artSprite;
        public Sprite minimapSprite;
    }

    public static class Campaign
    {
        const string ProgressKey = "kw_geminid_campaign_chapter";
        public static readonly int[] ChapterMaps = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

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
            ScenarioData s = DemoScenario.Create();
            s.chapter = chapter;
            s.mapIndex = ChapterMaps[Mathf.Clamp(chapter, 0, ChapterMaps.Length - 1)];

            if (OfficerTable.Loaded)
            {
                ApplyRealOfficers(s, chapter);
            }

            string title = ImsgLoader.BattleName(s.mapIndex);
            if (string.IsNullOrEmpty(title)) title = "第 " + (chapter + 1) + " 战";

            string leader = "曹操";
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

        static void ApplyRealOfficers(ScenarioData s, int chapter)
        {
            var players = new List<UnitSpawn>();
            var enemies = new List<UnitSpawn>();
            foreach (UnitSpawn u in s.units)
            {
                if (u.data.isEnemy) enemies.Add(u);
                else players.Add(u);
            }

            for (int i = 0; i < players.Count && i < 4 && i < OfficerTable.Officers.Count; i++)
            {
                Officer o = OfficerTable.Officers[i];
                UnitData d = players[i].data;
                d.name = o.name;
                d.shortLabel = o.name.Length > 0 ? o.name.Substring(0, 1) : "将";
                d.unitClass = (i == 0) ? UnitClassType.Cavalry : (UnitClassType)(i % 4);
                d.level = Mathf.Max(1, o.level) + chapter;
                d.maxHp = 40 + o.hpBase / 4 + chapter * 6;
                d.hp = d.maxHp;
                d.atk = 10 + o.stats5[1] * 22 / 100 + chapter * 2;
                d.def = 8 + o.stats5[3] * 16 / 100 + chapter;
                d.mind = 6 + o.stats5[2] * 18 / 100;
                d.maxMp = 12 + o.stats5[2] / 8;
                d.mp = d.maxMp;
                d.isLeader = (i == 0);
                d.faceId = o.faceId;
                d.spriteId = o.spriteId;
                d.itemId = i;
            }

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
                d.spriteId = 102;
            }
        }
    }

    public static class DemoScenario
    {
        public static ScenarioData Create()
        {
            var s = new ScenarioData();
            s.mapRows = new[]
            {
                "........HH..~~~~",
                "...FFFF..H..~~~~",
                "..FFFFFF....~~~~",
                "..FFFF......~~~~",
                "....vv........~~",
                "....vv..........",
                "................",
                "................"
            };

            // 我方
            s.units.Add(new UnitSpawn
            {
                col = 2, row = 6,
                data = new UnitData
                {
                    name = "曹操", shortLabel = "操", unitClass = UnitClassType.Cavalry,
                    level = 1, maxHp = 50, hp = 50, maxMp = 20, mp = 20,
                    atk = 18, def = 14, mind = 12, isEnemy = false, isLeader = true,
                    skills = { Skill.FireAttack(), Skill.Medicine() }
                }
            });
            s.units.Add(new UnitSpawn
            {
                col = 1, row = 7,
                data = new UnitData
                {
                    name = "夏侯惇", shortLabel = "惇", unitClass = UnitClassType.Cavalry,
                    level = 1, maxHp = 55, hp = 55, maxMp = 10, mp = 10,
                    atk = 20, def = 15, mind = 8, isEnemy = false, isLeader = false
                }
            });
            s.units.Add(new UnitSpawn
            {
                col = 3, row = 7,
                data = new UnitData
                {
                    name = "夏侯渊", shortLabel = "渊", unitClass = UnitClassType.Archer,
                    level = 1, maxHp = 42, hp = 42, maxMp = 10, mp = 10,
                    atk = 16, def = 10, mind = 8, isEnemy = false, isLeader = false
                }
            });
            s.units.Add(new UnitSpawn
            {
                col = 2, row = 7,
                data = new UnitData
                {
                    name = "曹洪", shortLabel = "洪", unitClass = UnitClassType.Infantry,
                    level = 1, maxHp = 48, hp = 48, maxMp = 10, mp = 10,
                    atk = 15, def = 16, mind = 8, isEnemy = false, isLeader = false
                }
            });

            // 敌方
            s.units.Add(new UnitSpawn
            {
                col = 8, row = 2,
                data = new UnitData
                {
                    name = "波才", shortLabel = "才", unitClass = UnitClassType.Cavalry,
                    level = 2, maxHp = 60, hp = 60, maxMp = 10, mp = 10,
                    atk = 17, def = 13, mind = 8, isEnemy = true, isLeader = true
                }
            });
            s.units.Add(new UnitSpawn
            {
                col = 7, row = 3,
                data = new UnitData
                {
                    name = "黄巾骑兵", shortLabel = "骑", unitClass = UnitClassType.Cavalry,
                    level = 1, maxHp = 40, hp = 40, maxMp = 0, mp = 0,
                    atk = 14, def = 10, mind = 5, isEnemy = true, isLeader = false
                }
            });
            s.units.Add(new UnitSpawn
            {
                col = 9, row = 3,
                data = new UnitData
                {
                    name = "黄巾步兵", shortLabel = "步", unitClass = UnitClassType.Infantry,
                    level = 1, maxHp = 38, hp = 38, maxMp = 0, mp = 0,
                    atk = 12, def = 12, mind = 5, isEnemy = true, isLeader = false
                }
            });

            return s;
        }
    }

    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnAfterSceneLoad()
        {
            if (Object.FindObjectOfType<BattleManager>() != null) return;
            var go = new GameObject("BattleRoot");
            go.AddComponent<AudioManager>();
            go.AddComponent<BattleManager>();
        }
    }
}
