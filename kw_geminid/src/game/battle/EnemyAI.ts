import { Unit } from '../model/Unit';
import { BattleGrid } from '../model/BattleGrid';
import { Pathfinding } from './Pathfinding';
import { SkillData, SkillKind } from '../model/Skill';
import { ClassHelper } from '../model/ClassType';

export interface AIAction {
  unit: Unit;
  destX: number;
  destY: number;
  path: { x: number; y: number }[];
  actionType: 'attack' | 'skill' | 'heal' | 'move_only' | 'wait';
  targetUnit?: Unit;
  skill?: SkillData;
}

export class EnemyAI {
  public static planAction(unit: Unit, grid: BattleGrid, allUnits: Unit[]): AIAction {
    const reachable = Pathfinding.getReachableTiles(unit, grid, allUnits);
    const playerUnits = allUnits.filter(u => u.isAlive() && !u.isEnemy);
    const allyUnits = allUnits.filter(u => u.isAlive() && u.isEnemy && u.id !== unit.id);

    if (playerUnits.length === 0) {
      return {
        unit,
        destX: unit.x,
        destY: unit.y,
        path: [{ x: unit.x, y: unit.y }],
        actionType: 'wait'
      };
    }

    // 1. 治愈/辅助判断：自身或友军血量危机（< 40%）且自身拥有治疗策略
    const healSkills = unit.skills.filter(s => s.kind === SkillKind.Heal && unit.mp >= s.mpCost);
    if (healSkills.length > 0) {
      const bestHealSkill = healSkills[0];
      // 检查自己或周围友军
      let mostInjured: Unit | null = null;
      let minHpRatio = 1.0;

      const candidates = [unit, ...allyUnits];
      for (const cand of candidates) {
        const ratio = cand.hp / cand.maxHp;
        if (ratio < 0.5 && ratio < minHpRatio) {
          minHpRatio = ratio;
          mostInjured = cand;
        }
      }

      if (mostInjured) {
        // 寻找能施放治疗的最佳移动点
        for (const [_, moveInfo] of reachable) {
          const dist = grid.getDistance(moveInfo.x, moveInfo.y, mostInjured.x, mostInjured.y);
          if (dist <= bestHealSkill.range) {
            return {
              unit,
              destX: moveInfo.x,
              destY: moveInfo.y,
              path: moveInfo.path,
              actionType: 'heal',
              targetUnit: mostInjured,
              skill: bestHealSkill
            };
          }
        }
      }
    }

    // 2. 进攻评估：在所有可达格点上评估打击方案
    let bestScore = -999999;
    let bestAction: AIAction = {
      unit,
      destX: unit.x,
      destY: unit.y,
      path: [{ x: unit.x, y: unit.y }],
      actionType: 'wait'
    };

    const damageSkills = unit.skills.filter(s => s.kind === SkillKind.Damage && unit.mp >= s.mpCost);

    for (const [_, moveInfo] of reachable) {
      const terrain = grid.getTerrainDef(moveInfo.x, moveInfo.y);
      const terrainScore = terrain.defBonus * 50;

      // A. 评估普通物理攻击
      const targets = Pathfinding.getAttackTargets(unit, moveInfo.x, moveInfo.y, grid, allUnits);
      for (const target of targets) {
        let score = 100 + terrainScore;

        // 斩杀收益极高 (+1000)
        const estDmg = Math.max(1, unit.getAtk() * ClassHelper.getAffinity(unit.unitClass, target.unitClass) - target.getDef() * 0.6);
        if (estDmg >= target.hp) {
          score += 1000;
        }

        // 优先攻击残血
        score += (1 - target.hp / target.maxHp) * 200;

        // 优先打击主帅或法师
        if (target.isLeader) score += 150;
        if (target.unitClass === 'Strategist' || target.unitClass === 'Geomancer') score += 120;

        if (score > bestScore) {
          bestScore = score;
          bestAction = {
            unit,
            destX: moveInfo.x,
            destY: moveInfo.y,
            path: moveInfo.path,
            actionType: 'attack',
            targetUnit: target
          };
        }
      }

      // B. 评估策略攻击
      for (const skill of damageSkills) {
        for (const target of playerUnits) {
          const dist = grid.getDistance(moveInfo.x, moveInfo.y, target.x, target.y);
          if (dist <= skill.range) {
            let score = 120 + terrainScore + skill.power * 2;
            const estSpellDmg = skill.power + unit.getMind() * 0.8 - target.getMind() * 0.4;
            if (estSpellDmg >= target.hp) score += 1000;
            score += (1 - target.hp / target.maxHp) * 200;
            if (target.isLeader) score += 150;

            if (score > bestScore) {
              bestScore = score;
              bestAction = {
                unit,
                destX: moveInfo.x,
                destY: moveInfo.y,
                path: moveInfo.path,
                actionType: 'skill',
                targetUnit: target,
                skill
              };
            }
          }
        }
      }
    }

    // 3. 若攻击范围内无目标，则向最近的我军挺进
    if (bestScore === -999999) {
      let closestTarget: Unit = playerUnits[0];
      let minDist = Infinity;
      for (const p of playerUnits) {
        const d = grid.getDistance(unit.x, unit.y, p.x, p.y);
        if (d < minDist) {
          minDist = d;
          closestTarget = p;
        }
      }

      let bestMoveTile = { x: unit.x, y: unit.y, path: [{ x: unit.x, y: unit.y }] };
      let bestMoveDist = Infinity;

      for (const [_, moveInfo] of reachable) {
        const d = grid.getDistance(moveInfo.x, moveInfo.y, closestTarget.x, closestTarget.y);
        if (d < bestMoveDist) {
          bestMoveDist = d;
          bestMoveTile = moveInfo;
        }
      }

      return {
        unit,
        destX: bestMoveTile.x,
        destY: bestMoveTile.y,
        path: bestMoveTile.path,
        actionType: 'move_only'
      };
    }

    return bestAction;
  }
}
