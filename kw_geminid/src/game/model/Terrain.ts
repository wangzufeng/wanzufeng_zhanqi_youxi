/**
 * 战场地形系统
 * 严格对应《三国志曹操传》原版地形参数与加成
 */

export enum TerrainType {
  Plain = 'Plain',       // 平原
  Grass = 'Grass',       // 草原
  Forest = 'Forest',     // 树林
  Mountain = 'Mountain', // 山地
  Wasteland = 'Wasteland', // 荒地
  Water = 'Water',       // 河流/水面
  Bridge = 'Bridge',     // 桥梁
  Castle = 'Castle',     // 城池
  Fortress = 'Fortress', // 关隘/城墙
  Barracks = 'Barracks', // 兵营
  Village = 'Village',   // 村庄
  Swamp = 'Swamp',       // 沼泽
  Cliff = 'Cliff'        // 峭壁（不可通行）
}

export interface TerrainInfo {
  type: TerrainType;
  name: string;
  defBonus: number;      // 防御力加成百分比 (例如 0.10 表示 +10%)
  hpRecovery: number;    // 回合开始 HP 回复百分比
  mpRecovery: number;    // 回合开始 MP 回复点数
  baseColor: string;     // 兜底色块颜色
}

export const TERRAIN_DATA: Record<TerrainType, TerrainInfo> = {
  [TerrainType.Plain]: { type: TerrainType.Plain, name: '平原', defBonus: 0.0, hpRecovery: 0, mpRecovery: 0, baseColor: '#88a860' },
  [TerrainType.Grass]: { type: TerrainType.Grass, name: '草原', defBonus: 0.05, hpRecovery: 0, mpRecovery: 0, baseColor: '#6da04b' },
  [TerrainType.Forest]: { type: TerrainType.Forest, name: '树林', defBonus: 0.20, hpRecovery: 0, mpRecovery: 0, baseColor: '#35713c' },
  [TerrainType.Mountain]: { type: TerrainType.Mountain, name: '山地', defBonus: 0.30, hpRecovery: 0, mpRecovery: 0, baseColor: '#8b704c' },
  [TerrainType.Wasteland]: { type: TerrainType.Wasteland, name: '荒地', defBonus: 0.05, hpRecovery: 0, mpRecovery: 0, baseColor: '#a18e6e' },
  [TerrainType.Water]: { type: TerrainType.Water, name: '水面', defBonus: -0.10, hpRecovery: 0, mpRecovery: 0, baseColor: '#3c70a4' },
  [TerrainType.Bridge]: { type: TerrainType.Bridge, name: '桥梁', defBonus: 0.0, hpRecovery: 0, mpRecovery: 0, baseColor: '#967850' },
  [TerrainType.Castle]: { type: TerrainType.Castle, name: '城池', defBonus: 0.30, hpRecovery: 0.20, mpRecovery: 5, baseColor: '#b0a080' },
  [TerrainType.Fortress]: { type: TerrainType.Fortress, name: '关隘', defBonus: 0.35, hpRecovery: 0.15, mpRecovery: 5, baseColor: '#7d7465' },
  [TerrainType.Barracks]: { type: TerrainType.Barracks, name: '兵营', defBonus: 0.25, hpRecovery: 0.20, mpRecovery: 0, baseColor: '#9c8c72' },
  [TerrainType.Village]: { type: TerrainType.Village, name: '村庄', defBonus: 0.15, hpRecovery: 0.10, mpRecovery: 0, baseColor: '#a69068' },
  [TerrainType.Swamp]: { type: TerrainType.Swamp, name: '沼泽', defBonus: -0.10, hpRecovery: 0, mpRecovery: 0, baseColor: '#536859' },
  [TerrainType.Cliff]: { type: TerrainType.Cliff, name: '峭壁', defBonus: 0.0, hpRecovery: 0, mpRecovery: 0, baseColor: '#4a423b' }
};

export class TerrainHelper {
  /**
   * 将 hexzmap.e5 中的地形字节 ID 映射为标准地形枚举
   */
  public static fromId(id: number): TerrainType {
    switch (id) {
      case 0: return TerrainType.Plain;
      case 1: return TerrainType.Grass;
      case 2: return TerrainType.Forest;
      case 3: return TerrainType.Wasteland;
      case 4: return TerrainType.Mountain;
      case 5: return TerrainType.Cliff;
      case 6: return TerrainType.Water;
      case 7: return TerrainType.Bridge;
      case 8: return TerrainType.Castle;
      case 9: return TerrainType.Fortress;
      case 10: return TerrainType.Barracks;
      case 11: return TerrainType.Village;
      case 12: return TerrainType.Swamp;
      case 13: return TerrainType.Forest;
      case 14: return TerrainType.Fortress;
      case 15: return TerrainType.Mountain;
      case 16: return TerrainType.Plain;
      case 20:
      case 22:
      case 24: return TerrainType.Barracks;
      case 23: return TerrainType.Fortress;
      default:
        if (id >= 25 && id <= 35) return TerrainType.Castle;
        return TerrainType.Plain;
    }
  }

  /**
   * 获取某兵种进入指定地形消耗的移动力（>=99 表示不可通行）
   */
  public static getMoveCost(terrain: TerrainType, isCavalry: boolean, isNaval: boolean, isFlyer: boolean = false): number {
    if (isFlyer) return 1;
    if (terrain === TerrainType.Cliff) return 99;

    if (terrain === TerrainType.Water) {
      return isNaval ? 1 : 99;
    }

    if (terrain === TerrainType.Swamp) {
      return isNaval ? 2 : (isCavalry ? 99 : 3);
    }

    if (isCavalry) {
      switch (terrain) {
        case TerrainType.Plain:
        case TerrainType.Grass:
        case TerrainType.Bridge:
          return 1;
        case TerrainType.Wasteland:
        case TerrainType.Village:
        case TerrainType.Castle:
        case TerrainType.Barracks:
          return 2;
        case TerrainType.Forest:
        case TerrainType.Mountain:
          return 3;
        default:
          return 2;
      }
    } else {
      switch (terrain) {
        case TerrainType.Plain:
        case TerrainType.Grass:
        case TerrainType.Bridge:
        case TerrainType.Village:
        case TerrainType.Castle:
        case TerrainType.Barracks:
          return 1;
        case TerrainType.Forest:
        case TerrainType.Mountain:
        case TerrainType.Wasteland:
          return 2;
        default:
          return 1;
      }
    }
  }
}
