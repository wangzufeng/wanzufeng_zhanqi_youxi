import { Camera } from './Camera';

export interface FloatingText {
  worldX: number;
  worldY: number;
  text: string;
  color: string;
  size: number;
  life: number;
  maxLife: number;
}

export interface SpellFX {
  worldX: number;
  worldY: number;
  type: 'fire' | 'heal' | 'water' | 'earth' | 'slash' | 'buff';
  timer: number;
  maxTime: number;
  radius: number;
}

export interface TurnBanner {
  turn: number;
  isPlayerTurn: boolean;
  timer: number;
  maxTime: number;
}

export interface CritCutin {
  speaker: string;
  text: string;
  faceId: number;
  timer: number;
  maxTime: number;
}

export class FXRenderer {
  private floaters: FloatingText[] = [];
  private spells: SpellFX[] = [];
  private banner: TurnBanner | null = null;
  private cutin: CritCutin | null = null;

  public addDamageFloater(worldX: number, worldY: number, damage: number, isCrit: boolean = false) {
    this.floaters.push({
      worldX,
      worldY: worldY - 20,
      text: isCrit ? `暴击 -${damage}!` : `-${damage}`,
      color: isCrit ? '#ffdd22' : '#ff4444',
      size: isCrit ? 22 : 16,
      life: 0,
      maxLife: 1.0
    });
  }

  public addHealFloater(worldX: number, worldY: number, amount: number) {
    this.floaters.push({
      worldX,
      worldY: worldY - 20,
      text: `+${amount}`,
      color: '#44ff66',
      size: 18,
      life: 0,
      maxLife: 1.0
    });
  }

  public addMissFloater(worldX: number, worldY: number) {
    this.floaters.push({
      worldX,
      worldY: worldY - 20,
      text: 'MISS',
      color: '#88ccff',
      size: 16,
      life: 0,
      maxLife: 0.8
    });
  }

  public addSpellFX(worldX: number, worldY: number, type: 'fire' | 'heal' | 'water' | 'earth' | 'slash' | 'buff') {
    this.spells.push({
      worldX,
      worldY,
      type,
      timer: 0,
      maxTime: 0.6,
      radius: 40
    });
  }

  public showTurnBanner(turn: number, isPlayerTurn: boolean) {
    this.banner = {
      turn,
      isPlayerTurn,
      timer: 0,
      maxTime: 1.2
    };
  }

  public showCritCutin(speaker: string, text: string, faceId: number) {
    this.cutin = {
      speaker,
      text,
      faceId,
      timer: 0,
      maxTime: 1.0
    };
  }

  public update(dt: number) {
    // Update floaters
    for (let i = this.floaters.length - 1; i >= 0; i--) {
      const f = this.floaters[i];
      f.life += dt;
      f.worldY -= 25 * dt;
      if (f.life >= f.maxLife) {
        this.floaters.splice(i, 1);
      }
    }

    // Update spells
    for (let i = this.spells.length - 1; i >= 0; i--) {
      const s = this.spells[i];
      s.timer += dt;
      if (s.timer >= s.maxTime) {
        this.spells.splice(i, 1);
      }
    }

    // Update banner
    if (this.banner) {
      this.banner.timer += dt;
      if (this.banner.timer >= this.banner.maxTime) {
        this.banner = null;
      }
    }

    // Update cutin
    if (this.cutin) {
      this.cutin.timer += dt;
      if (this.cutin.timer >= this.cutin.maxTime) {
        this.cutin = null;
      }
    }
  }

  public render(ctx: CanvasRenderingContext2D, camera: Camera) {
    // 1. Render spell animations in world coordinates
    for (const s of this.spells) {
      const screenPos = camera.worldToScreen(s.worldX, s.worldY);
      const progress = s.timer / s.maxTime;
      const alpha = 1 - progress;

      ctx.save();
      if (s.type === 'fire') {
        const rad = (s.radius + progress * 30) * camera.zoom;
        const grad = ctx.createRadialGradient(screenPos.x, screenPos.y, 2, screenPos.x, screenPos.y, rad);
        grad.addColorStop(0, `rgba(255, 240, 100, ${alpha * 0.9})`);
        grad.addColorStop(0.5, `rgba(255, 80, 20, ${alpha * 0.8})`);
        grad.addColorStop(1, `rgba(180, 0, 0, 0)`);
        ctx.fillStyle = grad;
        ctx.beginPath();
        ctx.arc(screenPos.x, screenPos.y, rad, 0, Math.PI * 2);
        ctx.fill();
      } else if (s.type === 'heal') {
        const rad = (s.radius + progress * 20) * camera.zoom;
        const grad = ctx.createRadialGradient(screenPos.x, screenPos.y, 2, screenPos.x, screenPos.y, rad);
        grad.addColorStop(0, `rgba(180, 255, 180, ${alpha * 0.9})`);
        grad.addColorStop(0.6, `rgba(50, 220, 80, ${alpha * 0.7})`);
        grad.addColorStop(1, `rgba(0, 180, 0, 0)`);
        ctx.fillStyle = grad;
        ctx.beginPath();
        ctx.arc(screenPos.x, screenPos.y, rad, 0, Math.PI * 2);
        ctx.fill();
      } else if (s.type === 'slash') {
        ctx.strokeStyle = `rgba(255, 255, 255, ${alpha})`;
        ctx.lineWidth = 4 * camera.zoom;
        ctx.beginPath();
        ctx.moveTo(screenPos.x - 30 * camera.zoom, screenPos.y - 30 * camera.zoom);
        ctx.lineTo(screenPos.x + 30 * camera.zoom, screenPos.y + 30 * camera.zoom);
        ctx.stroke();
      }
      ctx.restore();
    }

    // 2. Render floating damage/heal numbers
    for (const f of this.floaters) {
      const screenPos = camera.worldToScreen(f.worldX, f.worldY);
      const alpha = Math.max(0, 1 - (f.life / f.maxLife));
      ctx.save();
      ctx.globalAlpha = alpha;
      ctx.font = `bold ${Math.round(f.size * camera.zoom)}px 'PingFang SC', 'Microsoft YaHei', sans-serif`;
      ctx.textAlign = 'center';

      // Outline
      ctx.strokeStyle = '#000000';
      ctx.lineWidth = 3;
      ctx.strokeText(f.text, screenPos.x, screenPos.y);

      ctx.fillStyle = f.color;
      ctx.fillText(f.text, screenPos.x, screenPos.y);
      ctx.restore();
    }

    // 3. Render Turn Banner (Screen space)
    if (this.banner) {
      const W = camera.viewportWidth;
      const H = camera.viewportHeight;
      const progress = this.banner.timer / this.banner.maxTime;
      let alpha = 1;
      if (progress < 0.2) alpha = progress / 0.2;
      else if (progress > 0.8) alpha = (1 - progress) / 0.2;

      ctx.save();
      ctx.globalAlpha = alpha;

      const bannerY = H * 0.35;
      const bannerH = 70;

      // Dark golden background strip
      const grad = ctx.createLinearGradient(0, 0, W, 0);
      grad.addColorStop(0, 'rgba(10, 12, 18, 0)');
      grad.addColorStop(0.2, this.banner.isPlayerTurn ? 'rgba(18, 48, 80, 0.92)' : 'rgba(80, 20, 20, 0.92)');
      grad.addColorStop(0.8, this.banner.isPlayerTurn ? 'rgba(18, 48, 80, 0.92)' : 'rgba(80, 20, 20, 0.92)');
      grad.addColorStop(1, 'rgba(10, 12, 18, 0)');

      ctx.fillStyle = grad;
      ctx.fillRect(0, bannerY, W, bannerH);

      // Gold border lines
      ctx.strokeStyle = this.banner.isPlayerTurn ? '#50a0ff' : '#ff6050';
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.moveTo(W * 0.1, bannerY);
      ctx.lineTo(W * 0.9, bannerY);
      ctx.moveTo(W * 0.1, bannerY + bannerH);
      ctx.lineTo(W * 0.9, bannerY + bannerH);
      ctx.stroke();

      // Text
      ctx.textAlign = 'center';
      ctx.font = `bold 26px 'STKaiti', 'KaiTi', 'Microsoft YaHei', serif`;
      ctx.fillStyle = '#ffffff';
      ctx.fillText(
        `第 ${this.banner.turn} 回合  ·  ${this.banner.isPlayerTurn ? '我军行动' : '敌军行动'}`,
        W / 2,
        bannerY + 44
      );

      ctx.restore();
    }

    // 4. Render Critical Hit Cutin
    if (this.cutin) {
      const W = camera.viewportWidth;
      const H = camera.viewportHeight;
      const progress = this.cutin.timer / this.cutin.maxTime;
      const alpha = progress < 0.1 ? progress / 0.1 : (progress > 0.8 ? (1 - progress) / 0.2 : 1);

      ctx.save();
      ctx.globalAlpha = alpha;
      const cy = H * 0.45;
      ctx.fillStyle = 'rgba(0, 0, 0, 0.85)';
      ctx.fillRect(0, cy - 40, W, 80);

      ctx.strokeStyle = '#ffd700';
      ctx.lineWidth = 3;
      ctx.strokeRect(0, cy - 40, W, 80);

      ctx.textAlign = 'center';
      ctx.font = `bold 28px 'KaiTi', 'Microsoft YaHei', serif`;
      ctx.fillStyle = '#ffeedd';
      ctx.fillText(`【致命一击】${this.cutin.text}`, W / 2, cy + 10);
      ctx.restore();
    }
  }
}
