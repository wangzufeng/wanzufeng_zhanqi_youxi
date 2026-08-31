import { TerrainType, TerrainHelper, TERRAIN_DATA } from './Terrain';
import { Unit } from './Unit';

export class BattleGrid {
  public width: number;
  public height: number;
  public cells: TerrainType[];
  public rawIds: number[];

  public static readonly TILE_WIDTH = 96;
  public static readonly TILE_HEIGHT = 24;

  constructor(width: number, height: number, rawTerrainData: number[]) {
    this.width = width;
    this.height = height;
    this.rawIds = [...rawTerrainData];
    this.cells = new Array(width * height);

    for (let i = 0; i < width * height; i++) {
      const id = i < rawTerrainData.length ? rawTerrainData[i] : 0;
      this.cells[i] = TerrainHelper.fromId(id);
    }
  }

  public inBounds(x: number, y: number): boolean {
    return x >= 0 && x < this.width && y >= 0 && y < this.height;
  }

  public getIndex(x: number, y: number): number {
    return y * this.width + x;
  }

  public getTerrain(x: number, y: number): TerrainType {
    if (!this.inBounds(x, y)) return TerrainType.Cliff;
    return this.cells[this.getIndex(x, y)];
  }

  public getTerrainDef(x: number, y: number) {
    const t = this.getTerrain(x, y);
    return TERRAIN_DATA[t] || TERRAIN_DATA[TerrainType.Plain];
  }

  public getNeighbors(x: number, y: number): { x: number; y: number }[] {
    const results: { x: number; y: number }[] = [];
    const dirs = [
      { dx: 0, dy: -1 },
      { dx: 0, dy: 1 },
      { dx: -1, dy: 0 },
      { dx: 1, dy: 0 }
    ];
    for (const d of dirs) {
      const nx = x + d.dx;
      const ny = y + d.dy;
      if (this.inBounds(nx, ny)) {
        results.push({ x: nx, y: ny });
      }
    }
    return results;
  }

  public getDistance(x1: number, y1: number, x2: number, y2: number): number {
    return Math.abs(x1 - x2) + Math.abs(y1 - y2);
  }

  public gridToWorld(x: number, y: number): { x: number; y: number } {
    return {
      x: x * BattleGrid.TILE_WIDTH,
      y: y * BattleGrid.TILE_HEIGHT
    };
  }

  public gridToWorldCenter(x: number, y: number): { x: number; y: number } {
    return {
      x: x * BattleGrid.TILE_WIDTH + BattleGrid.TILE_WIDTH / 2,
      y: y * BattleGrid.TILE_HEIGHT + BattleGrid.TILE_HEIGHT / 2
    };
  }

  public worldToGrid(worldX: number, worldY: number): { x: number; y: number } {
    return {
      x: Math.floor(worldX / BattleGrid.TILE_WIDTH),
      y: Math.floor(worldY / BattleGrid.TILE_HEIGHT)
    };
  }
}
