import { Unit } from '../model/Unit';
import { BattleGrid } from '../model/BattleGrid';
import { TerrainHelper } from '../model/Terrain';
import { ClassHelper } from '../model/ClassType';

export interface MoveNode {
  x: number;
  y: number;
  costSoFar: number;
  parent: MoveNode | null;
}

export class Pathfinding {
  /**
   * 计算指定单位的可移动范围矩阵（考虑地形移动力消耗、敌方碰撞与 ZOC 控制区）
   */
  public static getReachableTiles(
    unit: Unit,
    grid: BattleGrid,
    allUnits: Unit[]
  ): Map<string, { x: number; y: number; cost: number; path: { x: number; y: number }[] }> {
    const reachable = new Map<string, { x: number; y: number; cost: number; path: { x: number; y: number }[] }>();
    const maxMove = unit.getMove();
    const classData = ClassHelper.get(unit.unitClass);

    // 单位位置索引表
    const unitMap = new Map<string, Unit>();
    for (const u of allUnits) {
      if (u.isAlive()) {
        unitMap.set(`${u.x},${u.y}`, u);
      }
    }

    // 优先队列 / 开集 BFS
    const openSet: MoveNode[] = [{ x: unit.x, y: unit.y, costSoFar: 0, parent: null }];
    const bestCosts = new Map<string, number>();
    bestCosts.set(`${unit.x},${unit.y}`, 0);

    while (openSet.length > 0) {
      // 按 cost 从小到大排序
      openSet.sort((a, b) => a.costSoFar - b.costSoFar);
      const current = openSet.shift()!;
      const curKey = `${current.x},${current.y}`;

      if (current.costSoFar > (bestCosts.get(curKey) ?? Infinity)) {
        continue;
      }

      // 重构到达当前格子的完整路径
      const path: { x: number; y: number }[] = [];
      let temp: MoveNode | null = current;
      while (temp) {
        path.unshift({ x: temp.x, y: temp.y });
        temp = temp.parent;
      }

      // 只有空格或自身所在格可作为终点
      const occUnit = unitMap.get(curKey);
      if (!occUnit || occUnit.id === unit.id) {
        reachable.set(curKey, {
          x: current.x,
          y: current.y,
          cost: current.costSoFar,
          path
        });
      }

      // 敌方 ZOC 判定：如果当前格子与敌军相邻，则消耗所有剩余移动力（无法继续向前穿过）
      const hasAdjacentEnemy = this.isAdjacentToEnemy(current.x, current.y, unit.isEnemy, unitMap, grid);
      if (hasAdjacentEnemy && !(current.x === unit.x && current.y === unit.y)) {
        // 受到 ZOC 阻截，无法继续向外探索
        continue;
      }

      // 遍历四向邻居
      const neighbors = grid.getNeighbors(current.x, current.y);
      for (const next of neighbors) {
        const nextKey = `${next.x},${next.y}`;
        const occ = unitMap.get(nextKey);

        // 不能穿过敌方单位
        if (occ && occ.isEnemy !== unit.isEnemy) {
          continue;
        }

        const terrain = grid.getTerrain(next.x, next.y);
        const cost = TerrainHelper.getMoveCost(terrain, classData.isCavalry, classData.isNaval);
        if (cost >= 99) continue;

        const newCost = current.costSoFar + cost;
        if (newCost <= maxMove) {
          const prevBest = bestCosts.get(nextKey) ?? Infinity;
          if (newCost < prevBest) {
            bestCosts.set(nextKey, newCost);
            openSet.push({
              x: next.x,
              y: next.y,
              costSoFar: newCost,
              parent: current
            });
          }
        }
      }
    }

    return reachable;
  }

  private static isAdjacentToEnemy(
    x: number,
    y: number,
    isEnemy: boolean,
    unitMap: Map<string, Unit>,
    grid: BattleGrid
  ): boolean {
    const neighbors = grid.getNeighbors(x, y);
    for (const n of neighbors) {
      const u = unitMap.get(`${n.x},${n.y}`);
      if (u && u.isEnemy !== isEnemy) {
        return true;
      }
    }
    return false;
  }

  /**
   * 获取指定单位在指定位置时的普通攻击射程范围目标
   */
  public static getAttackTargets(unit: Unit, fromX: number, fromY: number, grid: BattleGrid, allUnits: Unit[]): Unit[] {
    const minRange = unit.getMinRange();
    const maxRange = unit.getMaxRange();
    const targets: Unit[] = [];

    for (const u of allUnits) {
      if (!u.isAlive() || u.isEnemy === unit.isEnemy) continue;
      const dist = grid.getDistance(fromX, fromY, u.x, u.y);
      if (dist >= minRange && dist <= maxRange) {
        targets.push(u);
      }
    }
    return targets;
  }
}
