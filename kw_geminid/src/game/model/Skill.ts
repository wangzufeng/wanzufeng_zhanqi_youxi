/**
 * 策略技能体系
 * 覆盖五行法术、治疗与状态增益
 */

export enum SkillKind {
  Damage = 'Damage',
  Heal = 'Heal',
  Buff = 'Buff',
  Debuff = 'Debuff'
}

export enum SkillElement {
  Fire = 'Fire',     // 火系 (灼热/烈火/火龙)
  Water = 'Water',   // 水系 (水斗/水阵/水龙)
  Earth = 'Earth',   // 地系 (落石/滚石/山崩)
  Wind = 'Wind',     // 风系 (旋风/暴风)
  Heal = 'Heal',     // 医术 (小补给/大补给/援军)
  Support = 'Support'// 状态 (气合/霸气/定身/中毒)
}

export enum SkillAoeShape {
  Single = 'Single', // 单体 1格
  Cross = 'Cross',   // 十字 5格
  Box3x3 = 'Box3x3', // 3x3 范围 9格
  Diamond = 'Diamond'// 菱形
}

export interface SkillData {
  id: number;
  name: string;
  kind: SkillKind;
  element: SkillElement;
  mpCost: number;
  range: number;
  power: number;
  aoe: SkillAoeShape;
  description: string;
}

export const DEFAULT_SKILLS: SkillData[] = [
  {
    id: 0,
    name: '灼热',
    kind: SkillKind.Damage,
    element: SkillElement.Fire,
    mpCost: 6,
    range: 2,
    power: 16,
    aoe: SkillAoeShape.Single,
    description: '凝聚烈火射击单体目标，造成火系策略伤害。'
  },
  {
    id: 1,
    name: '烈火',
    kind: SkillKind.Damage,
    element: SkillElement.Fire,
    mpCost: 12,
    range: 2,
    power: 26,
    aoe: SkillAoeShape.Single,
    description: '释放熊熊烈焰，对单体目标造成中度火系伤害。'
  },
  {
    id: 2,
    name: '火阵',
    kind: SkillKind.Damage,
    element: SkillElement.Fire,
    mpCost: 18,
    range: 3,
    power: 22,
    aoe: SkillAoeShape.Cross,
    description: '在目标区域唤起十字火海，伤害范围内所有敌军。'
  },
  {
    id: 3,
    name: '火龙',
    kind: SkillKind.Damage,
    element: SkillElement.Fire,
    mpCost: 32,
    range: 3,
    power: 38,
    aoe: SkillAoeShape.Box3x3,
    description: '召唤灭世火龙席卷九宫格战场，造成极高范围火系伤害。'
  },
  {
    id: 4,
    name: '小补给',
    kind: SkillKind.Heal,
    element: SkillElement.Heal,
    mpCost: 6,
    range: 2,
    power: 30,
    aoe: SkillAoeShape.Single,
    description: '为一名友军回复部分体力。'
  },
  {
    id: 5,
    name: '大补给',
    kind: SkillKind.Heal,
    element: SkillElement.Heal,
    mpCost: 14,
    range: 2,
    power: 60,
    aoe: SkillAoeShape.Single,
    description: '为一名友军大量回复体力。'
  },
  {
    id: 6,
    name: '援军',
    kind: SkillKind.Heal,
    element: SkillElement.Heal,
    mpCost: 28,
    range: 3,
    power: 45,
    aoe: SkillAoeShape.Cross,
    description: '大范围治愈十字范围内多名友军伤势。'
  },
  {
    id: 7,
    name: '落石',
    kind: SkillKind.Damage,
    element: SkillElement.Earth,
    mpCost: 8,
    range: 2,
    power: 20,
    aoe: SkillAoeShape.Single,
    description: '引动山石砸击单体敌人。'
  },
  {
    id: 8,
    name: '水阵',
    kind: SkillKind.Damage,
    element: SkillElement.Water,
    mpCost: 16,
    range: 2,
    power: 24,
    aoe: SkillAoeShape.Cross,
    description: '引水浪冲击敌方阵线，十字范围伤害。'
  },
  {
    id: 9,
    name: '霸气',
    kind: SkillKind.Buff,
    element: SkillElement.Support,
    mpCost: 20,
    range: 0,
    power: 0,
    aoe: SkillAoeShape.Single,
    description: '主帅专属技能，全方位大幅提升自身五维能力！'
  },
  {
    id: 10,
    name: '定身',
    kind: SkillKind.Debuff,
    element: SkillElement.Support,
    mpCost: 10,
    range: 2,
    power: 0,
    aoe: SkillAoeShape.Single,
    description: '定身封锁敌方行动力，使其下回合无法移动。'
  }
];

export class SkillHelper {
  public static getOffsets(aoe: SkillAoeShape): { dx: number; dy: number }[] {
    switch (aoe) {
      case SkillAoeShape.Single:
        return [{ dx: 0, dy: 0 }];
      case SkillAoeShape.Cross:
        return [
          { dx: 0, dy: 0 },
          { dx: -1, dy: 0 },
          { dx: 1, dy: 0 },
          { dx: 0, dy: -1 },
          { dx: 0, dy: 1 }
        ];
      case SkillAoeShape.Box3x3: {
        const offsets = [];
        for (let dy = -1; dy <= 1; dy++) {
          for (let dx = -1; dx <= 1; dx++) {
            offsets.push({ dx, dy });
          }
        }
        return offsets;
      }
      case SkillAoeShape.Diamond:
        return [
          { dx: 0, dy: 0 },
          { dx: -1, dy: 0 },
          { dx: 1, dy: 0 },
          { dx: 0, dy: -1 },
          { dx: 0, dy: 1 },
          { dx: -2, dy: 0 },
          { dx: 2, dy: 0 },
          { dx: 0, dy: -2 },
          { dx: 0, dy: 2 }
        ];
    }
  }
}
