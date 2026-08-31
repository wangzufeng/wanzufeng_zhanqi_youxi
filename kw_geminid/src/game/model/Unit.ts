import { UnitClassType, ClassHelper } from './ClassType';
import { SkillData, DEFAULT_SKILLS } from './Skill';
import { ItemDef, ItemHelper, TreasurePassive } from './Item';

export type UnitDirection = 'left' | 'right' | 'up' | 'down';
export type UnitAnimState = 'idle' | 'walking' | 'attacking' | 'hurt' | 'casting' | 'dead';

export interface UnitStats {
  force: number;     // 武力 (>=90为S档 +4攻, 70-89为A档 +3攻, 50-69为B档 +2攻)
  intel: number;     // 智力 (精神/MP)
  command: number;   // 统率 (防御/HP)
  agility: number;   // 敏捷 (格挡/连击)
  luck: number;      // 运气 (暴击/策略命中)
}

export interface UnitConfig {
  id: string;
  officerId: number;
  name: string;
  unitClass: UnitClassType;
  isEnemy: boolean;
  isAlly?: boolean;
  isLeader?: boolean;
  level: number;
  faceId: number;
  spriteId: number;
  stats?: UnitStats;
  hpBase?: number;
  x: number;
  y: number;
  weaponId?: number;
  armorId?: number;
  accessoryId?: number;
  skills?: SkillData[];
  history?: string;
  criticalQuote?: string;
  retreatQuote?: string;
}

export class Unit {
  public id: string;
  public officerId: number;
  public name: string;
  public unitClass: UnitClassType;
  public isEnemy: boolean;
  public isAlly: boolean;
  public isLeader: boolean;
  public level: number;
  public exp: number = 0;

  public maxHp: number;
  public hp: number;
  public maxMp: number;
  public mp: number;

  public stats: UnitStats;
  public faceId: number;
  public spriteId: number;

  // Equipment & EXP Levels (1 to 3)
  public weaponId: number;
  public weaponExp: number = 0;
  public weaponLevel: number = 1;

  public armorId: number;
  public armorExp: number = 0;
  public armorLevel: number = 1;

  public accessoryId: number;

  public skills: SkillData[] = [];
  public x: number;
  public y: number;

  public hasMoved: boolean = false;
  public hasActed: boolean = false;
  public direction: UnitDirection = 'down';
  public animState: UnitAnimState = 'idle';
  public animFrame: number = 0;
  public animTimer: number = 0;

  public history: string;
  public criticalQuote: string;
  public retreatQuote: string;

  // Status Effects
  public buffAtk: boolean = false;
  public buffDef: boolean = false;
  public isConfused: boolean = false;
  public isPoisoned: boolean = false;
  public isParalyzed: boolean = false;

  constructor(cfg: UnitConfig) {
    this.id = cfg.id;
    this.officerId = cfg.officerId;
    this.name = cfg.name;
    this.unitClass = cfg.unitClass;
    this.isEnemy = cfg.isEnemy;
    this.isAlly = cfg.isAlly || false;
    this.isLeader = cfg.isLeader || false;
    this.level = Math.max(1, cfg.level);
    this.faceId = cfg.faceId >= 0 ? cfg.faceId : 0;
    this.spriteId = cfg.spriteId >= 0 ? cfg.spriteId : 0;

    this.stats = cfg.stats || {
      force: 75,
      intel: 70,
      command: 75,
      agility: 70,
      luck: 70
    };

    const hpBase = cfg.hpBase || 180;
    const hpGrowth = this.getStatGrowth(this.stats.command) * 2;
    this.maxHp = Math.round(50 + (hpBase / 4) + (this.level * (6 + hpGrowth)));
    this.hp = this.maxHp;

    const mpGrowth = this.getStatGrowth(this.stats.intel);
    this.maxMp = Math.round(20 + (this.stats.intel * 0.4) + (this.level * (2 + mpGrowth)));
    this.mp = this.maxMp;

    this.weaponId = cfg.weaponId ?? 0;
    this.armorId = cfg.armorId ?? 9;
    this.accessoryId = cfg.accessoryId ?? -1;

    this.x = cfg.x;
    this.y = cfg.y;

    this.skills = cfg.skills && cfg.skills.length > 0 ? [...cfg.skills] : this.initDefaultSkills();
    this.history = cfg.history || '';
    this.criticalQuote = cfg.criticalQuote || `${this.name}：看我这招！`;
    this.retreatQuote = cfg.retreatQuote || `${this.name}：可恶……先退一步！`;
  }

  /** 五维成长档位换算：S(4点), A(3点), B(2点), C(1点) */
  public getStatGrowth(val: number): number {
    if (val >= 90) return 4; // S
    if (val >= 70) return 3; // A
    if (val >= 50) return 2; // B
    return 1;                // C
  }

  public hasPassive(p: TreasurePassive): boolean {
    const w = ItemHelper.get(this.weaponId);
    const a = ItemHelper.get(this.armorId);
    const acc = ItemHelper.get(this.accessoryId);
    return w.passive === p || a.passive === p || acc.passive === p;
  }

  private initDefaultSkills(): SkillData[] {
    const classData = ClassHelper.get(this.unitClass);
    const list: SkillData[] = [];

    if (this.hasPassive(TreasurePassive.AllSpells)) {
      list.push(DEFAULT_SKILLS[0], DEFAULT_SKILLS[4], DEFAULT_SKILLS[7], DEFAULT_SKILLS[8]);
      return list;
    }

    if (!classData.canCastSpells) return [];

    if (this.unitClass === UnitClassType.Strategist) {
      list.push(DEFAULT_SKILLS[0]); // 灼热
      list.push(DEFAULT_SKILLS[1]); // 烈火
      if (this.level >= 4) list.push(DEFAULT_SKILLS[2]); // 火阵
      if (this.level >= 8) list.push(DEFAULT_SKILLS[3]); // 火龙
      if (this.isLeader) list.push(DEFAULT_SKILLS[9]); // 霸气
    } else if (this.unitClass === UnitClassType.Geomancer) {
      list.push(DEFAULT_SKILLS[4]); // 小补给
      if (this.level >= 4) list.push(DEFAULT_SKILLS[5]); // 大补给
      if (this.level >= 7) list.push(DEFAULT_SKILLS[6]); // 援军
    } else if (this.unitClass === UnitClassType.Thief) {
      list.push(DEFAULT_SKILLS[7]); // 落石
    } else if (this.unitClass === UnitClassType.Naval) {
      list.push(DEFAULT_SKILLS[8]); // 水阵
    } else if (this.unitClass === UnitClassType.Taoist) {
      list.push(DEFAULT_SKILLS[10]); // 定身
    }
    return list;
  }

  public getAtk(): number {
    const weapon = ItemHelper.get(this.weaponId);
    const weaponBonus = weapon.atk + (this.weaponLevel - 1) * 6;
    const growth = this.getStatGrowth(this.stats.force);
    let base = Math.round(20 + (this.stats.force * 0.5) + (this.level * (1 + growth)) + weaponBonus);
    if (this.buffAtk) base = Math.round(base * 1.25);
    return base;
  }

  public getDef(): number {
    const armor = ItemHelper.get(this.armorId);
    const armorBonus = armor.def + (this.armorLevel - 1) * 5;
    const growth = this.getStatGrowth(this.stats.command);
    let base = Math.round(15 + (this.stats.command * 0.4) + (this.level * (1 + growth)) + armorBonus);
    if (this.buffDef) base = Math.round(base * 1.25);
    return base;
  }

  public getMind(): number {
    const growth = this.getStatGrowth(this.stats.intel);
    return Math.round(15 + (this.stats.intel * 0.5) + (this.level * (1 + growth)));
  }

  public getMove(): number {
    let move = ClassHelper.get(this.unitClass).move;
    if (this.hasPassive(TreasurePassive.RushMove)) move += 2;
    if (this.hasPassive(TreasurePassive.MovePlus1)) move += 1;
    if (this.isParalyzed) move = 0;
    return move;
  }

  public getMinRange(): number {
    return ClassHelper.get(this.unitClass).minRange;
  }

  public getMaxRange(): number {
    return ClassHelper.get(this.unitClass).maxRange;
  }

  public isAlive(): boolean {
    return this.hp > 0;
  }

  public takeDamage(dmg: number): number {
    // 龙鳞铠 MP 护盾被动：消耗 MP 抵消致命扣血
    if (this.hasPassive(TreasurePassive.MpShield) && this.mp > 0) {
      const absorb = Math.min(this.mp, Math.round(dmg * 0.6));
      this.mp -= absorb;
      dmg -= absorb;
    }

    const actual = Math.max(1, Math.min(this.hp, dmg));
    this.hp -= actual;
    if (this.hp <= 0) {
      this.animState = 'dead';
    }

    // 防具积累装备经验
    this.addArmorExp(4);
    return actual;
  }

  public heal(amount: number): number {
    const actual = Math.min(this.maxHp - this.hp, amount);
    this.hp += actual;
    return actual;
  }

  public addWeaponExp(amount: number) {
    if (this.weaponLevel >= 3) return;
    this.weaponExp += amount;
    if (this.weaponExp >= 50) {
      this.weaponExp -= 50;
      this.weaponLevel++;
    }
  }

  public addArmorExp(amount: number) {
    if (this.armorLevel >= 3) return;
    this.armorExp += amount;
    if (this.armorExp >= 50) {
      this.armorExp -= 50;
      this.armorLevel++;
    }
  }

  public gainExp(amount: number): boolean {
    if (this.isEnemy) return false;
    if (this.hasPassive(TreasurePassive.ExpBonus50)) {
      amount = Math.round(amount * 1.5);
    }
    this.exp += amount;
    this.addWeaponExp(4);

    let leveledUp = false;
    while (this.exp >= 100) {
      this.exp -= 100;
      this.levelUp();
      leveledUp = true;
    }
    return leveledUp;
  }

  public levelUp() {
    this.level++;
    const hpInc = 5 + this.getStatGrowth(this.stats.command);
    const mpInc = 2 + this.getStatGrowth(this.stats.intel);
    this.maxHp += hpInc;
    this.hp = Math.min(this.maxHp, this.hp + hpInc);
    this.maxMp += mpInc;
    this.mp = Math.min(this.maxMp, this.mp + mpInc);
  }

  public onRoundStart() {
    // 太平要术恢复 MP
    if (this.hasPassive(TreasurePassive.RegenMp)) {
      this.mp = Math.min(this.maxMp, this.mp + 15);
    }
    // 太平清领道解除不良状态
    if (this.hasPassive(TreasurePassive.CleanseDebuffs)) {
      this.isConfused = false;
      this.isPoisoned = false;
      this.isParalyzed = false;
    }
    // 自动吃豆
    if (this.hasPassive(TreasurePassive.AutoBeans) && this.hp < this.maxHp * 0.5) {
      this.heal(50);
    }
  }

  public resetTurn() {
    this.hasMoved = false;
    this.hasActed = false;
    this.animState = this.isAlive() ? 'idle' : 'dead';
    this.animFrame = 0;
    this.onRoundStart();
  }
}
