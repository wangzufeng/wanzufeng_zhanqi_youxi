import { Unit } from '../model/Unit';
import { BattleGrid } from '../model/BattleGrid';
import { ClassHelper } from '../model/ClassType';
import { SkillData, SkillKind, SkillElement } from '../model/Skill';
import { TreasurePassive } from '../model/Item';
import { WeatherInfo } from '../model/Weather';

export interface CombatResult {
  hit: boolean;
  damage: number;
  isCrit: boolean;
  isDouble: boolean;
  isPierce: boolean;
  cleaveTriggered: boolean;
  quote?: string;
  counterHit?: boolean;
  counterDamage?: number;
  counterCrit?: boolean;
  targetDied: boolean;
  attackerDied: boolean;
  expGained: number;
}

export interface SpellResult {
  hit: boolean;
  damage: number;
  healAmount: number;
  targetDied: boolean;
  expGained: number;
}

export class CombatCalc {
  /**
   * 普通物理攻击结算（含宝物被动：青龙偃月刀封杀反击、倚天剑格挡、古锭刀暴击、黄金铠防暴击、方天画戟引导奋战）
   */
  public static calcPhysical(
    attacker: Unit,
    defender: Unit,
    grid: BattleGrid,
    allUnits?: Unit[]
  ): CombatResult {
    const terrainInfo = grid.getTerrainDef(defender.x, defender.y);
    const affinity = ClassHelper.getAffinity(attacker.unitClass, defender.unitClass);

    // 命中率计算 (倚天剑提供格挡加成)
    const agiDiff = attacker.stats.agility - defender.stats.agility;
    let hitRate = 0.90 + (agiDiff * 0.005);
    if (defender.hasPassive(TreasurePassive.HeavenShield)) {
      hitRate -= 0.15; // 倚天剑格挡 +15%
    }
    hitRate = Math.max(0.35, Math.min(1.0, hitRate));
    const hit = Math.random() <= hitRate;

    if (!hit) {
      return {
        hit: false,
        damage: 0,
        isCrit: false,
        isDouble: false,
        isPierce: false,
        cleaveTriggered: false,
        targetDied: false,
        attackerDied: false,
        expGained: 6
      };
    }

    // 暴击判定 (古锭刀加成，黄金铠免疫暴击)
    let isCrit = false;
    if (!defender.hasPassive(TreasurePassive.ImmuneCrit)) {
      const luckDiff = attacker.stats.luck - defender.stats.luck;
      let critRate = 0.05 + (luckDiff * 0.003);
      if (attacker.hasPassive(TreasurePassive.CritBonus)) critRate += 0.20; // 古锭刀
      critRate = Math.max(0.02, Math.min(0.50, critRate));
      isCrit = Math.random() <= critRate;
    }

    // 伤害公式
    const effAtk = attacker.getAtk() * affinity;
    const effDef = defender.getDef() * (1 + terrainInfo.defBonus);
    let rawDamage = Math.max(2, effAtk - effDef * 0.65);
    const variance = 0.95 + Math.random() * 0.10;
    let finalDamage = Math.round(rawDamage * variance);
    if (isCrit) finalDamage = Math.round(finalDamage * 1.5);
    finalDamage = Math.max(1, finalDamage);

    // 受到伤害
    defender.takeDamage(finalDamage);
    const targetDied = !defender.isAlive();

    // 经验结算
    let exp = 12 + Math.max(0, defender.level - attacker.level) * 4;
    if (targetDied) {
      exp += 20 + (defender.isLeader ? 25 : 0);
    }

    // 丈八蛇矛：穿透攻击两格判定
    let isPierce = attacker.hasPassive(TreasurePassive.PierceAttack);
    if (isPierce && allUnits) {
      const dx = defender.x - attacker.x;
      const dy = defender.y - attacker.y;
      const pierceX = defender.x + dx;
      const pierceY = defender.y + dy;
      const behindUnit = allUnits.find(u => u.isAlive() && u.x === pierceX && u.y === pierceY && u.isEnemy === defender.isEnemy);
      if (behindUnit) {
        behindUnit.takeDamage(Math.round(finalDamage * 0.75));
      }
    }

    // 方天画戟：引导奋战（击退敌人后触发）
    let cleaveTriggered = targetDied && attacker.hasPassive(TreasurePassive.CleaveAttack);

    // 反击判定 (青龙偃月刀 / 李广弓 封杀反击)
    let counterHit = false;
    let counterDamage = 0;
    let counterCrit = false;
    let attackerDied = false;

    const noCounter = attacker.hasPassive(TreasurePassive.NoCounter);

    if (!targetDied && !noCounter && !defender.isConfused && !defender.isParalyzed) {
      const dist = grid.getDistance(attacker.x, attacker.y, defender.x, defender.y);
      if (dist >= defender.getMinRange() && dist <= defender.getMaxRange()) {
        const defTerrain = grid.getTerrainDef(attacker.x, attacker.y);
        const defAffinity = ClassHelper.getAffinity(defender.unitClass, attacker.unitClass);
        const defHitRate = Math.max(0.40, Math.min(1.0, 0.88 - (agiDiff * 0.005)));
        counterHit = Math.random() <= defHitRate;

        if (counterHit) {
          counterCrit = !attacker.hasPassive(TreasurePassive.ImmuneCrit) && Math.random() <= 0.05;
          const defAtk = defender.getAtk() * defAffinity * 0.75;
          const defDef = attacker.getDef() * (1 + defTerrain.defBonus);
          let rawCounter = Math.max(1, defAtk - defDef * 0.65);
          counterDamage = Math.round(rawCounter * (0.95 + Math.random() * 0.10));
          if (counterCrit) counterDamage = Math.round(counterDamage * 1.5);
          counterDamage = Math.max(1, counterDamage);
          attacker.takeDamage(counterDamage);
          attackerDied = !attacker.isAlive();
        }
      }
    }

    // 连击判定
    const isDouble = agiDiff >= 20 && Math.random() < 0.25 && !targetDied && !attackerDied;
    if (isDouble) {
      const doubleDmg = Math.max(1, Math.round(finalDamage * 0.8));
      defender.takeDamage(doubleDmg);
    }

    return {
      hit: true,
      damage: finalDamage,
      isCrit,
      isDouble,
      isPierce,
      cleaveTriggered,
      quote: isCrit ? attacker.criticalQuote : undefined,
      counterHit,
      counterDamage,
      counterCrit,
      targetDied: !defender.isAlive(),
      attackerDied: !attacker.isAlive(),
      expGained: exp
    };
  }

  /**
   * 策略魔法结算（含天气倍率与白银铠减免 50% 策略伤害）
   */
  public static calcSpell(
    caster: Unit,
    target: Unit,
    skill: SkillData,
    grid: BattleGrid,
    weather?: WeatherInfo
  ): SpellResult {
    if (skill.kind === SkillKind.Heal) {
      const healBase = skill.power + caster.getMind() * 0.7;
      const actualHeal = target.heal(Math.round(healBase));
      const exp = 14 + Math.max(0, target.level - caster.level) * 3;
      return {
        hit: true,
        damage: 0,
        healAmount: actualHeal,
        targetDied: false,
        expGained: exp
      };
    }

    if (skill.kind === SkillKind.Buff) {
      if (skill.id === 9) {
        caster.buffAtk = true;
        caster.buffDef = true;
      }
      return {
        hit: true,
        damage: 0,
        healAmount: 0,
        targetDied: false,
        expGained: 15
      };
    }

    // 策略命中率判定 (倚天剑额外提升抗法)
    const terrainInfo = grid.getTerrainDef(target.x, target.y);
    const intDiff = caster.stats.intel - target.stats.intel;
    let hitRate = 0.85 + intDiff * 0.005;
    if (target.hasPassive(TreasurePassive.HeavenShield)) hitRate -= 0.15;
    hitRate = Math.max(0.40, Math.min(0.98, hitRate));
    const hit = Math.random() <= hitRate;

    if (!hit) {
      return {
        hit: false,
        damage: 0,
        healAmount: 0,
        targetDied: false,
        expGained: 6
      };
    }

    // 天气倍率修正
    let weatherMod = 1.0;
    if (weather) {
      if (skill.element === SkillElement.Fire) weatherMod = weather.fireMod;
      else if (skill.element === SkillElement.Water) weatherMod = weather.waterMod;
      else if (skill.element === SkillElement.Wind) weatherMod = weather.windMod;
      else if (skill.element === SkillElement.Earth) weatherMod = weather.earthMod;
    }

    const mindDiff = caster.getMind() * 0.9 - target.getMind() * 0.45;
    let rawDmg = ((skill.power * 1.5) + mindDiff) * weatherMod;
    rawDmg *= (1 - terrainInfo.defBonus * 0.4);

    // 白银铠：策略伤害减半！
    if (target.hasPassive(TreasurePassive.SpellHalve)) {
      rawDmg *= 0.50;
    }

    let finalDmg = Math.max(5, Math.round(rawDmg * (0.95 + Math.random() * 0.10)));
    target.takeDamage(finalDmg);
    const targetDied = !target.isAlive();
    let exp = 15 + Math.max(0, target.level - caster.level) * 4;
    if (targetDied) exp += 25 + (target.isLeader ? 20 : 0);

    return {
      hit: true,
      damage: finalDmg,
      healAmount: 0,
      targetDied,
      expGained: exp
    };
  }
}
