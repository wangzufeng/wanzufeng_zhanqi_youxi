using System.Collections.Generic;
using UnityEngine;

namespace KwGeminid
{
    public enum WeatherType
    {
        Sunny,      // 晴天
        Cloudy,     // 阴天
        Rainy,      // 雨天
        HeavyRain,  // 豪雨
        Snowy       // 雪天
    }

    public enum TreasurePassive
    {
        None,
        CleaveAttack,    // 方天画戟：引导奋战
        NoCounter,       // 青龙偃月刀 / 李广弓：封杀反击
        PierceAttack,    // 丈八蛇矛：穿透两格
        HeavenShield,    // 倚天剑：+15% 格挡
        CritBonus,       // 古锭刀：+20% 暴击
        SpellHalve,      // 白银铠：策略减伤 50%
        ImmuneCrit,      // 黄金铠：免疫暴击
        MpShield,        // 龙鳞铠：MP 抵御伤害
        RushMove,        // 赤兔马：移动 +2
        MovePlus1,       // 绝影 / 爪黄飞电：移动 +1
        RegenMp,         // 太平要术：每回合恢复 15 MP
        CleanseDebuffs,  // 太平清领道：每回合解异常
        ExpBonus50,      // 孟德新书：获得经验 +50%
        AllSpells        // 遁甲天书：施展全系基础策略
    }

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

    public enum SkillKind { Damage, Heal, Buff }
    public enum SkillElement { None, Fire, Water, Wind, Earth, Heal }

    public class Skill
    {
        public string name;
        public SkillKind kind;
        public SkillElement element;
        public int mpCost;
        public int range;
        public int power;

        public Skill(string name, SkillKind kind, SkillElement element, int mpCost, int range, int power)
        {
            this.name = name;
            this.kind = kind;
            this.element = element;
            this.mpCost = mpCost;
            this.range = range;
            this.power = power;
        }

        public static Skill FireAttack() { return new Skill("火计", SkillKind.Damage, SkillElement.Fire, 8, 2, 16); }
        public static Skill Medicine() { return new Skill("医术", SkillKind.Heal, SkillElement.Heal, 6, 2, 15); }
    }

    public class UnitData
    {
        public string name;
        public string shortLabel;
        public UnitClassType unitClass;
        public int level;
        public int maxHp;
        public int hp;
        public int maxMp;
        public int mp;
        public int atk;
        public int def;
        public int mind;
        public int agility = 70;
        public int luck = 70;
        public bool isEnemy;
        public bool isLeader;
        public int exp;
        public int faceId = -1;
        public int spriteId = -1;

        // 装备与等级 (Lv.1 to Lv.3)
        public int weaponId = -1;
        public int weaponLevel = 1;
        public int weaponExp = 0;

        public int armorId = -1;
        public int armorLevel = 1;
        public int armorExp = 0;

        public int accessoryId = -1;
        public TreasurePassive passive = TreasurePassive.None;

        public List<Skill> skills = new List<Skill>();
        public const int ExpPerLevel = 100;

        public int GetStatGrowth(int stat)
        {
            if (stat >= 90) return 4; // S
            if (stat >= 70) return 3; // A
            if (stat >= 50) return 2; // B
            return 1;                // C
        }

        public void LevelUp()
        {
            level++;
            int hpInc = 5 + GetStatGrowth(def);
            int mpInc = 2 + GetStatGrowth(mind);
            maxHp += hpInc;
            hp = Mathf.Min(maxHp, hp + hpInc);
            atk += GetStatGrowth(atk);
            def += GetStatGrowth(def);
            mind += GetStatGrowth(mind);
            maxMp += mpInc;
            mp = Mathf.Min(maxMp, mp + mpInc);
        }

        public int Move
        {
            get
            {
                int m = ClassTable.Get(unitClass).move;
                if (passive == TreasurePassive.RushMove) m += 2;
                if (passive == TreasurePassive.MovePlus1) m += 1;
                return m;
            }
        }
        public int MinRange { get { return ClassTable.Get(unitClass).minRange; } }
        public int MaxRange { get { return ClassTable.Get(unitClass).maxRange; } }
        public bool Alive { get { return hp > 0; } }
        public string ClassName { get { return ClassTable.Get(unitClass).name; } }

        public void OnRoundStart()
        {
            if (passive == TreasurePassive.RegenMp)
            {
                mp = Mathf.Min(maxMp, mp + 15);
            }
        }

        public int TakeDamage(int dmg)
        {
            if (passive == TreasurePassive.MpShield && mp > 0)
            {
                int absorb = Mathf.Min(mp, Mathf.RoundToInt(dmg * 0.6f));
                mp -= absorb;
                dmg -= absorb;
            }
            int actual = Mathf.Max(1, Mathf.Min(hp, dmg));
            hp -= actual;
            return actual;
        }
    }

    public static class DamageCalc
    {
        public static int Physical(UnitData a, UnitData d, float terrainDef, out bool isCrit)
        {
            isCrit = false;
            if (d.passive != TreasurePassive.ImmuneCrit)
            {
                float critChance = 0.05f + (a.luck - d.luck) * 0.003f;
                if (a.passive == TreasurePassive.CritBonus) critChance += 0.20f;
                if (Random.value <= Mathf.Clamp(critChance, 0.02f, 0.50f)) isCrit = true;
            }

            float affinity = ClassTable.Affinity(a.unitClass, d.unitClass);
            float raw = a.atk * affinity - d.def * (1f + terrainDef) * 0.65f;
            raw *= Random.Range(0.95f, 1.05f);
            if (isCrit) raw *= 1.5f;
            return Mathf.Max(1, Mathf.RoundToInt(raw));
        }

        public static int SkillDamage(UnitData a, UnitData d, Skill s, float terrainDef, WeatherType weather)
        {
            float weatherMod = 1f;
            if (s.element == SkillElement.Fire) weatherMod = (weather == WeatherType.Sunny) ? 1.2f : ((weather == WeatherType.HeavyRain) ? 0.0f : 0.8f);
            if (s.element == SkillElement.Water) weatherMod = (weather == WeatherType.HeavyRain || weather == WeatherType.Rainy) ? 1.4f : 0.9f;

            float raw = (s.power * 1.5f + a.mind * 0.9f - d.def * 0.45f * (1f + terrainDef)) * weatherMod;
            if (d.passive == TreasurePassive.SpellHalve) raw *= 0.5f;
            raw *= Random.Range(0.95f, 1.05f);
            return Mathf.Max(1, Mathf.RoundToInt(raw));
        }

        public static int SkillHeal(UnitData a, Skill s)
        {
            return Mathf.Max(1, Mathf.RoundToInt(s.power + a.mind * 0.6f));
        }
    }
}
