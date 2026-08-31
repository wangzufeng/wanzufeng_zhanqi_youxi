/**
 * 兵种系统
 * 包含移动力、攻击射程、兵种相克倍率
 */

export enum UnitClassType {
  Infantry = 'Infantry',           // 步兵
  Cavalry = 'Cavalry',             // 骑兵
  Archer = 'Archer',               // 弓兵
  BowCavalry = 'BowCavalry',       // 弓骑兵
  Strategist = 'Strategist',       // 军师/策士
  Geomancer = 'Geomancer',         // 风水士
  Taoist = 'Taoist',               // 道士
  MartialArtist = 'MartialArtist', // 武术家
  Thief = 'Thief',                 // 贼兵
  Catapult = 'Catapult',           // 炮车
  Naval = 'Naval',                 // 水军
  Dancer = 'Dancer'                // 舞娘
}

export interface UnitClassData {
  type: UnitClassType;
  name: string;
  move: number;
  minRange: number;
  maxRange: number;
  isCavalry: boolean;
  isNaval: boolean;
  canCastSpells: boolean;
  description: string;
}

export const CLASS_TABLE: Record<UnitClassType, UnitClassData> = {
  [UnitClassType.Infantry]: {
    type: UnitClassType.Infantry,
    name: '步兵',
    move: 4,
    minRange: 1,
    maxRange: 1,
    isCavalry: false,
    isNaval: false,
    canCastSpells: false,
    description: '基础步兵，防御扎实，对弓兵有较强克制。'
  },
  [UnitClassType.Cavalry]: {
    type: UnitClassType.Cavalry,
    name: '骑兵',
    move: 6,
    minRange: 1,
    maxRange: 1,
    isCavalry: true,
    isNaval: false,
    canCastSpells: false,
    description: '高机动高爆发兵种，平原冲锋极具威力，克制弓兵与步兵。'
  },
  [UnitClassType.Archer]: {
    type: UnitClassType.Archer,
    name: '弓兵',
    move: 4,
    minRange: 1,
    maxRange: 2,
    isCavalry: false,
    isNaval: false,
    canCastSpells: false,
    description: '远程打击兵种，射程1-2格，对骑兵有穿透优势。'
  },
  [UnitClassType.BowCavalry]: {
    type: UnitClassType.BowCavalry,
    name: '弓骑兵',
    move: 6,
    minRange: 1,
    maxRange: 2,
    isCavalry: true,
    isNaval: false,
    canCastSpells: false,
    description: '机动与远程兼备的精锐骑手。'
  },
  [UnitClassType.Strategist]: {
    type: UnitClassType.Strategist,
    name: '军师',
    move: 5,
    minRange: 1,
    maxRange: 1,
    isCavalry: false,
    isNaval: false,
    canCastSpells: true,
    description: '精通火系、水系、地系五行策略，法术伤害极高。'
  },
  [UnitClassType.Geomancer]: {
    type: UnitClassType.Geomancer,
    name: '风水士',
    move: 4,
    minRange: 1,
    maxRange: 1,
    isCavalry: false,
    isNaval: false,
    canCastSpells: true,
    description: '辅助与治疗专家，能够解除异常状态并大范围回复。'
  },
  [UnitClassType.Taoist]: {
    type: UnitClassType.Taoist,
    name: '道士',
    move: 4,
    minRange: 1,
    maxRange: 1,
    isCavalry: false,
    isNaval: false,
    canCastSpells: true,
    description: '擅长妨碍与诅咒，可施加中毒、混乱、定身等异常状态。'
  },
  [UnitClassType.MartialArtist]: {
    type: UnitClassType.MartialArtist,
    name: '武术家',
    move: 5,
    minRange: 1,
    maxRange: 1,
    isCavalry: false,
    isNaval: false,
    canCastSpells: false,
    description: '敏捷与格斗大师，暴击与反击率极高。'
  },
  [UnitClassType.Thief]: {
    type: UnitClassType.Thief,
    name: '贼兵',
    move: 5,
    minRange: 1,
    maxRange: 1,
    isCavalry: false,
    isNaval: false,
    canCastSpells: true,
    description: '山地游击好手，可施展落石与特殊计策。'
  },
  [UnitClassType.Catapult]: {
    type: UnitClassType.Catapult,
    name: '炮车',
    move: 3,
    minRange: 2,
    maxRange: 4,
    isCavalry: false,
    isNaval: false,
    canCastSpells: false,
    description: '超远距离重型攻城器械，射程2-4格，火力强劲。'
  },
  [UnitClassType.Naval]: {
    type: UnitClassType.Naval,
    name: '水军',
    move: 5,
    minRange: 1,
    maxRange: 1,
    isCavalry: false,
    isNaval: true,
    canCastSpells: true,
    description: '水战之王，在江河湖面拥有全额能力与水系计策。'
  },
  [UnitClassType.Dancer]: {
    type: UnitClassType.Dancer,
    name: '舞娘',
    move: 5,
    minRange: 1,
    maxRange: 1,
    isCavalry: false,
    isNaval: false,
    canCastSpells: true,
    description: '舞蹈激励友军恢复行动力或提供全军光环。'
  }
};

export class ClassHelper {
  public static get(type: UnitClassType): UnitClassData {
    return CLASS_TABLE[type] || CLASS_TABLE[UnitClassType.Infantry];
  }

  public static fromId(classId: number): UnitClassType {
    const list: UnitClassType[] = [
      UnitClassType.Cavalry,
      UnitClassType.Infantry,
      UnitClassType.Archer,
      UnitClassType.BowCavalry,
      UnitClassType.Strategist,
      UnitClassType.Geomancer,
      UnitClassType.Taoist,
      UnitClassType.MartialArtist,
      UnitClassType.Thief,
      UnitClassType.Catapult,
      UnitClassType.Naval,
      UnitClassType.Dancer
    ];
    return list[classId % list.length];
  }

  /**
   * 兵种相克倍率：
   * 骑兵克弓兵(+25%)，弓兵克骑兵(+15%远程克制)，步兵克弓兵(+20%)，骑兵克步兵(+15%)
   */
  public static getAffinity(attacker: UnitClassType, defender: UnitClassType): number {
    if (attacker === UnitClassType.Cavalry && defender === UnitClassType.Archer) return 1.25;
    if (attacker === UnitClassType.Archer && defender === UnitClassType.Cavalry) return 1.20;
    if (attacker === UnitClassType.Infantry && defender === UnitClassType.Archer) return 1.25;
    if (attacker === UnitClassType.Cavalry && defender === UnitClassType.Infantry) return 1.15;
    if (attacker === UnitClassType.Archer && defender === UnitClassType.Infantry) return 0.85;
    return 1.0;
  }
}
