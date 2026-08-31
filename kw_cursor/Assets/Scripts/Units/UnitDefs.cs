using System.Collections.Generic;
using UnityEngine;

namespace KwCursor
{
    public enum UnitClassType
    {
        Infantry,   // 步兵
        Cavalry,    // 骑兵
        Archer,     // 弓兵
        Strategist  // 军师
    }

    public class ClassData
    {
        public string name;
        public int move;
        public int minRange;
        public int maxRange;

        public ClassData(string name, int move, int minRange, int maxRange)
        {
            this.name = name;
            this.move = move;
            this.minRange = minRange;
            this.maxRange = maxRange;
        }
    }

    /// <summary>
    /// 兵种数据表与相克关系。
    /// 数值为近似实现（骑克弓、弓克步、步克骑各 +25% 伤害），可按需校准。
    /// </summary>
    public static class ClassTable
    {
        static readonly Dictionary<UnitClassType, ClassData> table = new Dictionary<UnitClassType, ClassData>
        {
            { UnitClassType.Infantry,   new ClassData("步兵", 4, 1, 1) },
            { UnitClassType.Cavalry,    new ClassData("骑兵", 6, 1, 1) },
            { UnitClassType.Archer,     new ClassData("弓兵", 4, 1, 2) },
            { UnitClassType.Strategist, new ClassData("军师", 5, 1, 1) },
        };

        public static ClassData Get(UnitClassType t) { return table[t]; }

        public static float Affinity(UnitClassType attacker, UnitClassType defender)
        {
            if (attacker == UnitClassType.Cavalry && defender == UnitClassType.Archer) return 1.25f;
            if (attacker == UnitClassType.Archer && defender == UnitClassType.Infantry) return 1.25f;
            if (attacker == UnitClassType.Infantry && defender == UnitClassType.Cavalry) return 1.25f;
            return 1f;
        }
    }

    public enum SkillKind { Damage, Heal }

    /// <summary>策略（消耗 MP 的技能）。</summary>
    public class Skill
    {
        public string name;
        public SkillKind kind;
        public int mpCost;
        public int range;
        public int power;

        public Skill(string name, SkillKind kind, int mpCost, int range, int power)
        {
            this.name = name;
            this.kind = kind;
            this.mpCost = mpCost;
            this.range = range;
            this.power = power;
        }

        public static Skill FireAttack() { return new Skill("火计", SkillKind.Damage, 8, 2, 14); }
        public static Skill Medicine() { return new Skill("医术", SkillKind.Heal, 6, 2, 12); }
    }

    /// <summary>武将/单位的纯数据。</summary>
    public class UnitData
    {
        public string name;
        public string shortLabel; // 棋子上显示的单字
        public UnitClassType unitClass;
        public int level;
        public int maxHp;
        public int hp;
        public int maxMp;
        public int mp;
        public int atk;
        public int def;
        public int mind; // 精神，影响策略威力
        public bool isEnemy;
        public bool isLeader;
        public int exp;
        public int faceId = -1;   // 原版头像编号（Face.e5 条目；-1 表示无）
        public int spriteId = -1; // 原版形象编号（Pmapobj.e5 条目；-1 表示无）
        public int itemId = -1;   // 原版装备编号（Data.e5 道具表 / Item.e5 图标）
        public List<Skill> skills = new List<Skill>();

        public const int ExpPerLevel = 100;

        /// <summary>升级成长（近似数值，可按需校准）。</summary>
        public void LevelUp()
        {
            level++;
            maxHp += 5;
            hp = UnityEngine.Mathf.Min(maxHp, hp + 5);
            atk += 2;
            def += 1;
            mind += 1;
            maxMp += 2;
            mp = UnityEngine.Mathf.Min(maxMp, mp + 2);
        }

        public int Move { get { return ClassTable.Get(unitClass).move; } }
        public int MinRange { get { return ClassTable.Get(unitClass).minRange; } }
        public int MaxRange { get { return ClassTable.Get(unitClass).maxRange; } }
        public bool Alive { get { return hp > 0; } }
        public string ClassName { get { return ClassTable.Get(unitClass).name; } }
    }

    /// <summary>伤害/回复公式。近似实现，可按需校准。</summary>
    public static class DamageCalc
    {
        public static int Physical(UnitData a, UnitData d, float terrainDef)
        {
            float affinity = ClassTable.Affinity(a.unitClass, d.unitClass);
            float raw = a.atk * affinity - d.def * (1f + terrainDef);
            raw *= Random.Range(0.9f, 1.1f);
            return Mathf.Max(1, Mathf.RoundToInt(raw));
        }

        public static int SkillDamage(UnitData a, UnitData d, Skill s, float terrainDef)
        {
            float raw = s.power + a.mind * 0.8f - d.def * 0.3f * (1f + terrainDef);
            raw *= Random.Range(0.95f, 1.05f);
            return Mathf.Max(1, Mathf.RoundToInt(raw));
        }

        public static int SkillHeal(UnitData a, Skill s)
        {
            return Mathf.Max(1, Mathf.RoundToInt(s.power + a.mind * 0.5f));
        }
    }
}
