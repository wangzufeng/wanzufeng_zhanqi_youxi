import { BattleGrid } from '../model/BattleGrid';
import { Unit } from '../model/Unit';
import { Camera } from './Camera';
import { FXRenderer } from './FXRenderer';

export class CanvasRenderer {
  private canvas: HTMLCanvasElement;
  private ctx: CanvasRenderingContext2D;
  private camera: Camera;
  public fx: FXRenderer;

  // Asset Caches
  private bgImageCache: Map<number, HTMLImageElement> = new Map();
  private spriteSheetCache: Map<number, HTMLImageElement> = new Map();
  private minimapCache: Map<number, HTMLImageElement> = new Map();

  // Grid Highlights
  public reachableTiles: Set<string> = new Set();
  public attackableTiles: Set<string> = new Set();
  public skillTiles: Set<string> = new Set();
  public selectedUnit: Unit | null = null;
  public hoveredGrid: { x: number; y: number } | null = null;
  public plannedPath: { x: number; y: number }[] = [];

  constructor(canvas: HTMLCanvasElement, camera: Camera) {
    this.canvas = canvas;
    this.ctx = canvas.getContext('2d', { alpha: false })!;
    this.camera = camera;
    this.fx = new FXRenderer();
  }

  public loadStageBg(stageId: number): Promise<HTMLImageElement | null> {
    if (this.bgImageCache.has(stageId)) {
      return Promise.resolve(this.bgImageCache.get(stageId)!);
    }
    return new Promise(resolve => {
      const img = new Image();
      img.src = `/assets/battlemaps/bg_${stageId}.png`;
      img.onload = () => {
        this.bgImageCache.set(stageId, img);
        resolve(img);
      };
      img.onerror = () => {
        resolve(null);
      };
    });
  }

  public loadUnitSprite(spriteId: number): Promise<HTMLImageElement | null> {
    if (this.spriteSheetCache.has(spriteId)) {
      return Promise.resolve(this.spriteSheetCache.get(spriteId)!);
    }
    return new Promise(resolve => {
      const img = new Image();
      img.src = `/assets/sprites/unit_${spriteId}.png`;
      img.onload = () => {
        this.spriteSheetCache.set(spriteId, img);
        resolve(img);
      };
      img.onerror = () => {
        resolve(null);
      };
    });
  }

  public clearHighlights() {
    this.reachableTiles.clear();
    this.attackableTiles.clear();
    this.skillTiles.clear();
    this.plannedPath = [];
  }

  public render(
    grid: BattleGrid,
    units: Unit[],
    currentStageId: number,
    dt: number
  ) {
    if (!grid) return;
    const ctx = this.ctx;
    const cam = this.camera;
    const W = this.canvas.width;
    const H = this.canvas.height;

    ctx.fillStyle = '#11141a';
    ctx.fillRect(0, 0, W, H);

    // 1. Render Map Background
    const bgImg = this.bgImageCache.get(currentStageId);
    if (bgImg) {
      const p = cam.worldToScreen(0, 0);
      const dw = (grid.width * BattleGrid.TILE_WIDTH) * cam.zoom;
      const dh = (grid.height * BattleGrid.TILE_HEIGHT) * cam.zoom;
      ctx.drawImage(bgImg, p.x, p.y, dw, dh);
    } else {
      // Fallback procedural tiles
      for (let y = 0; y < grid.height; y++) {
        for (let x = 0; x < grid.width; x++) {
          const wp = grid.gridToWorld(x, y);
          const sp = cam.worldToScreen(wp.x, wp.y);
          const tw = BattleGrid.TILE_WIDTH * cam.zoom;
          const th = BattleGrid.TILE_HEIGHT * cam.zoom;

          if (sp.x + tw < 0 || sp.y + th < 0 || sp.x > W || sp.y > H) continue;

          const terrainInfo = grid.getTerrainDef(x, y);
          ctx.fillStyle = terrainInfo.baseColor;
          ctx.fillRect(sp.x, sp.y, tw + 0.5, th + 0.5);

          ctx.strokeStyle = 'rgba(0, 0, 0, 0.15)';
          ctx.lineWidth = 1;
          ctx.strokeRect(sp.x, sp.y, tw, th);
        }
      }
    }

    // 2. Render Highlights (Reachable move, Attack, Skill)
    for (let y = 0; y < grid.height; y++) {
      for (let x = 0; x < grid.width; x++) {
        const key = `${x},${y}`;
        const isReach = this.reachableTiles.has(key);
        const isAtk = this.attackableTiles.has(key);
        const isSkill = this.skillTiles.has(key);

        if (!isReach && !isAtk && !isSkill) continue;

        const wp = grid.gridToWorld(x, y);
        const sp = cam.worldToScreen(wp.x, wp.y);
        const tw = BattleGrid.TILE_WIDTH * cam.zoom;
        const th = BattleGrid.TILE_HEIGHT * cam.zoom;

        if (isReach) {
          ctx.fillStyle = 'rgba(40, 130, 255, 0.40)';
          ctx.fillRect(sp.x, sp.y, tw, th);
          ctx.strokeStyle = 'rgba(80, 180, 255, 0.85)';
          ctx.lineWidth = 1.5;
          ctx.strokeRect(sp.x + 1, sp.y + 1, tw - 2, th - 2);
        } else if (isAtk) {
          ctx.fillStyle = 'rgba(255, 50, 50, 0.45)';
          ctx.fillRect(sp.x, sp.y, tw, th);
          ctx.strokeStyle = 'rgba(255, 100, 100, 0.9)';
          ctx.lineWidth = 1.5;
          ctx.strokeRect(sp.x + 1, sp.y + 1, tw - 2, th - 2);
        } else if (isSkill) {
          ctx.fillStyle = 'rgba(180, 60, 240, 0.45)';
          ctx.fillRect(sp.x, sp.y, tw, th);
          ctx.strokeStyle = 'rgba(210, 100, 255, 0.9)';
          ctx.lineWidth = 1.5;
          ctx.strokeRect(sp.x + 1, sp.y + 1, tw - 2, th - 2);
        }
      }
    }

    // 3. Render Planned Path Line
    if (this.plannedPath.length > 1) {
      ctx.beginPath();
      ctx.strokeStyle = '#ffd700';
      ctx.lineWidth = 3 * cam.zoom;
      ctx.setLineDash([6, 3]);
      for (let i = 0; i < this.plannedPath.length; i++) {
        const pt = this.plannedPath[i];
        const center = grid.gridToWorldCenter(pt.x, pt.y);
        const sp = cam.worldToScreen(center.x, center.y);
        if (i === 0) ctx.moveTo(sp.x, sp.y);
        else ctx.lineTo(sp.x, sp.y);
      }
      ctx.stroke();
      ctx.setLineDash([]);
    }

    // 4. Render Hovered & Selected Cursors
    if (this.hoveredGrid && grid.inBounds(this.hoveredGrid.x, this.hoveredGrid.y)) {
      const wp = grid.gridToWorld(this.hoveredGrid.x, this.hoveredGrid.y);
      const sp = cam.worldToScreen(wp.x, wp.y);
      ctx.strokeStyle = '#ffffff';
      ctx.lineWidth = 2;
      ctx.strokeRect(sp.x, sp.y, BattleGrid.TILE_WIDTH * cam.zoom, BattleGrid.TILE_HEIGHT * cam.zoom);
    }

    if (this.selectedUnit && this.selectedUnit.isAlive()) {
      const wp = grid.gridToWorld(this.selectedUnit.x, this.selectedUnit.y);
      const sp = cam.worldToScreen(wp.x, wp.y);
      ctx.strokeStyle = '#ffd700';
      ctx.lineWidth = 2.5;
      ctx.strokeRect(sp.x - 1, sp.y - 1, (BattleGrid.TILE_WIDTH * cam.zoom) + 2, (BattleGrid.TILE_HEIGHT * cam.zoom) + 2);
    }

    // 5. Render Units (sorted by Y for proper 2.5D depth sorting)
    const livingUnits = units.filter(u => u.isAlive()).sort((a, b) => a.y - b.y);

    for (const u of livingUnits) {
      this.renderUnit(u, grid, cam, dt);
    }

    // 6. Render FX Overlay (Damage text, Spells, Banners)
    this.fx.update(dt);
    this.fx.render(ctx, cam);
  }

  private renderUnit(u: Unit, grid: BattleGrid, cam: Camera, dt: number) {
    const ctx = this.ctx;
    const center = grid.gridToWorldCenter(u.x, u.y);
    const sp = cam.worldToScreen(center.x, center.y);

    // Unit sprite dimensions: 48x64
    const SW = 48;
    const SH = 64;
    const drawW = SW * cam.zoom * 1.05;
    const drawH = SH * cam.zoom * 1.05;
    const drawX = sp.x - drawW / 2;
    const drawY = sp.y - drawH + (14 * cam.zoom);

    // Animation frame timer update
    u.animTimer += dt;
    if (u.animState === 'idle') {
      if (u.animTimer > 0.35) {
        u.animTimer = 0;
        u.animFrame = (u.animFrame + 1) % 3; // frames 0, 1, 2
      }
    } else if (u.animState === 'walking') {
      if (u.animTimer > 0.15) {
        u.animTimer = 0;
        u.animFrame = (u.animFrame + 1) % 3;
      }
    } else if (u.animState === 'attacking' || u.animState === 'casting') {
      if (u.animTimer > 0.12) {
        u.animTimer = 0;
        if (u.animFrame < 8) u.animFrame++;
        else {
          u.animState = 'idle';
          u.animFrame = 0;
        }
      }
    } else if (u.animState === 'hurt') {
      u.animFrame = 3;
      if (u.animTimer > 0.4) {
        u.animState = 'idle';
        u.animFrame = 0;
      }
    }

    let frame = u.animFrame;
    if (u.animState === 'hurt') frame = 3;
    if (u.animState === 'attacking' || u.animState === 'casting') {
      frame = Math.max(4, Math.min(8, 4 + u.animFrame));
    }

    const spriteImg = this.spriteSheetCache.get(u.spriteId);
    ctx.save();

    if (u.hasActed) {
      ctx.filter = 'grayscale(70%) brightness(75%)';
    }

    if (spriteImg) {
      // 5 cols x 4 rows in sprite sheet
      const col = frame % 5;
      const row = Math.floor(frame / 5);
      const sx = col * SW;
      const sy = row * SH;

      // Handle horizontal mirroring if facing left
      if (u.direction === 'left') {
        ctx.translate(drawX + drawW, drawY);
        ctx.scale(-1, 1);
        ctx.drawImage(spriteImg, sx, sy, SW, SH, 0, 0, drawW, drawH);
      } else {
        ctx.drawImage(spriteImg, sx, sy, SW, SH, drawX, drawY, drawW, drawH);
      }
    } else {
      // Procedural chess pawn token fallback
      ctx.fillStyle = u.isEnemy ? '#d32f2f' : '#1976d2';
      ctx.beginPath();
      ctx.arc(sp.x, sp.y - 10 * cam.zoom, 16 * cam.zoom, 0, Math.PI * 2);
      ctx.fill();
      ctx.strokeStyle = '#ffffff';
      ctx.lineWidth = 2;
      ctx.stroke();

      ctx.fillStyle = '#ffffff';
      ctx.font = `bold ${Math.round(12 * cam.zoom)}px sans-serif`;
      ctx.textAlign = 'center';
      ctx.fillText(u.name.substring(0, 1), sp.x, sp.y - 6 * cam.zoom);
    }
    ctx.restore();

    // Render Mini Health Bar & Name Banner
    const barW = 44 * cam.zoom;
    const barH = 5 * cam.zoom;
    const barX = sp.x - barW / 2;
    const barY = drawY - 8 * cam.zoom;

    // HP Bar background
    ctx.fillStyle = 'rgba(0, 0, 0, 0.75)';
    ctx.fillRect(barX - 1, barY - 1, barW + 2, barH + 2);

    // HP Fill
    const hpRatio = Math.max(0, u.hp / u.maxHp);
    ctx.fillStyle = u.isEnemy ? '#ff3333' : (hpRatio > 0.3 ? '#22cc44' : '#ffaa00');
    ctx.fillRect(barX, barY, barW * hpRatio, barH);

    // Leader Badge
    if (u.isLeader) {
      ctx.fillStyle = '#ffd700';
      ctx.font = `bold ${Math.round(10 * cam.zoom)}px sans-serif`;
      ctx.textAlign = 'center';
      ctx.fillText('★', barX - 6 * cam.zoom, barY + 4 * cam.zoom);
    }

    // Name tag
    ctx.font = `${Math.round(10 * cam.zoom)}px 'PingFang SC', sans-serif`;
    ctx.textAlign = 'center';
    ctx.strokeStyle = '#000000';
    ctx.lineWidth = 2;
    ctx.strokeText(u.name, sp.x, barY - 3 * cam.zoom);
    ctx.fillStyle = u.isEnemy ? '#ffbbbb' : '#bbddff';
    ctx.fillText(u.name, sp.x, barY - 3 * cam.zoom);
  }
}
