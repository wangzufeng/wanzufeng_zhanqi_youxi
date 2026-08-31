import { BattleGrid } from '../model/BattleGrid';
import { Unit } from '../model/Unit';
import { ScenarioBuilder, ScenarioData } from './ScenarioBuilder';
import { Pathfinding } from './Pathfinding';
import { CombatCalc } from './CombatCalc';
import { EnemyAI } from './EnemyAI';
import { SkillData, SkillElement } from '../model/Skill';
import { ItemDef } from '../model/Item';
import { WeatherType, WeatherInfo, WEATHER_DATA } from '../model/Weather';
import { Camera } from '../render/Camera';
import { CanvasRenderer } from '../render/CanvasRenderer';
import { BattleHUD } from '../../ui/BattleHUD';
import { DialogBox } from '../../ui/DialogBox';
import { SoundManager } from '../../core/audio/SoundManager';
import { StageSelectModal } from '../../ui/StageSelectModal';
import { FormationModal } from '../../ui/FormationModal';
import { JukeboxModal } from '../../ui/JukeboxModal';
import { SaveManager, SaveData } from '../../ui/SaveManager';

export enum BattleState {
  Intro = 'Intro',
  PlayerIdle = 'PlayerIdle',
  SelectMove = 'SelectMove',
  ActionMenu = 'ActionMenu',
  SelectAttack = 'SelectAttack',
  SelectSkill = 'SelectSkill',
  Animating = 'Animating',
  Duel = 'Duel',
  EnemyTurn = 'EnemyTurn',
  Victory = 'Victory',
  Defeat = 'Defeat'
}

export class BattleManager {
  public state: BattleState = BattleState.Intro;
  public stageIndex: number = 0;
  public turn: number = 1;
  public currentWeather: WeatherInfo = WEATHER_DATA[WeatherType.Sunny];

  public scenario!: ScenarioData;
  public grid!: BattleGrid;
  public playerUnits: Unit[] = [];
  public enemyUnits: Unit[] = [];

  private camera: Camera;
  private renderer: CanvasRenderer;
  private hud: BattleHUD;
  private dialogBox: DialogBox;
  private sound: SoundManager;

  private stageSelectModal: StageSelectModal;
  private formationModal: FormationModal;
  private jukeboxModal: JukeboxModal;
  private saveManager: SaveManager;

  // Selected Unit State
  private selectedUnit: Unit | null = null;
  private preMovePos: { x: number; y: number } | null = null;
  private reachableMap: Map<string, { x: number; y: number; cost: number; path: { x: number; y: number }[] }> = new Map();
  private attackTargets: Unit[] = [];
  private selectedSkill: SkillData | null = null;

  constructor(canvas: HTMLCanvasElement) {
    this.sound = SoundManager.getInstance();
    this.dialogBox = new DialogBox();

    this.camera = new Camera(canvas.width, canvas.height);
    this.renderer = new CanvasRenderer(canvas, this.camera);

    this.hud = new BattleHUD({
      onMove: () => this.handleMenuMove(),
      onAttack: () => this.handleMenuAttack(),
      onSkillSelect: (skill: SkillData) => this.handleMenuSkillSelect(skill),
      onItemSelect: (item: ItemDef) => this.handleMenuItemSelect(item),
      onWait: () => this.handleMenuWait(),
      onCancel: () => this.handleMenuCancel(),
      onEndTurn: () => this.endPlayerTurn(),
      onOpenStageSelect: () => this.stageSelectModal.show(),
      onOpenFormation: () => this.formationModal.show(),
      onOpenJukebox: () => this.jukeboxModal.show(),
      onOpenSave: () => this.saveManager.show()
    });

    this.stageSelectModal = new StageSelectModal(stageIdx => {
      this.loadStage(stageIdx);
    });

    this.formationModal = new FormationModal(customOfficerIds => {
      this.loadStage(this.stageIndex, customOfficerIds);
    });

    this.jukeboxModal = new JukeboxModal();

    this.saveManager = new SaveManager(
      slot => this.exportSaveState(slot),
      data => this.importSaveState(data)
    );

    this.bindInputs(canvas);
  }

  public async start() {
    await ScenarioBuilder.init();
    await this.loadStage(0);
  }

  public async loadStage(stageIdx: number, customPlayerOfficers?: number[]) {
    this.stageIndex = stageIdx;
    this.turn = 1;
    this.state = BattleState.Intro;
    this.updateWeather();
    this.clearSelection();

    this.scenario = await ScenarioBuilder.build(stageIdx, customPlayerOfficers);
    this.grid = this.scenario.grid;
    this.playerUnits = this.scenario.playerUnits;
    this.enemyUnits = this.scenario.enemyUnits;

    this.camera.setMapBounds(
      this.grid.width * BattleGrid.TILE_WIDTH,
      this.grid.height * BattleGrid.TILE_HEIGHT
    );

    await this.renderer.loadStageBg(stageIdx);
    for (const u of [...this.playerUnits, ...this.enemyUnits]) {
      this.renderer.loadUnitSprite(u.spriteId);
    }

    const leader = this.playerUnits[0];
    if (leader) {
      const c = this.grid.gridToWorldCenter(leader.x, leader.y);
      this.camera.focusOn(c.x, c.y);
    }

    this.hud.updateTopBar(this.scenario.stageName, this.turn, true, this.currentWeather.name);
    this.sound.playChapterBgm(stageIdx);

    this.dialogBox.show(this.scenario.introDialogs, () => {
      this.startPlayerTurn();
    });
  }

  private updateWeather() {
    const list = [WeatherType.Sunny, WeatherType.Cloudy, WeatherType.Rainy, WeatherType.HeavyRain, WeatherType.Snowy];
    const idx = Math.floor((this.turn - 1) / 3) % list.length;
    this.currentWeather = WEATHER_DATA[list[idx]];
  }

  private startPlayerTurn() {
    this.state = BattleState.PlayerIdle;
    this.updateWeather();
    this.hud.updateTopBar(this.scenario.stageName, this.turn, true, this.currentWeather.name);
    this.renderer.fx.showTurnBanner(this.turn, true);

    // Turn 4 Flank Reinforcement Event
    if (this.turn === 4 && this.stageIndex === 0 && this.enemyUnits.length < 12) {
      this.triggerFlankAmbush();
    }

    for (const u of this.playerUnits) {
      if (u.isAlive()) {
        u.resetTurn();
        const terrain = this.grid.getTerrainDef(u.x, u.y);
        if (terrain.hpRecovery > 0) {
          const rec = Math.round(u.maxHp * terrain.hpRecovery);
          u.heal(rec);
          const c = this.grid.gridToWorldCenter(u.x, u.y);
          this.renderer.fx.addHealFloater(c.x, c.y, rec);
        }
      }
    }
  }

  private triggerFlankAmbush() {
    const ambushUnit = new Unit({
      id: `enemy_ambush_1`,
      officerId: 99,
      name: '黄巾伏兵',
      unitClass: 'Cavalry' as any,
      isEnemy: true,
      level: 3,
      faceId: 25,
      spriteId: 102,
      x: 1,
      y: 3
    });
    this.enemyUnits.push(ambushUnit);
    this.renderer.loadUnitSprite(102);
    this.dialogBox.show([
      { speaker: '斥候', faceId: -1, text: '报！左侧山林发现敌军伏兵骑兵突袭！' }
    ], () => {});
  }

  public endPlayerTurn() {
    if (this.state !== BattleState.PlayerIdle && this.state !== BattleState.ActionMenu) return;
    this.clearSelection();
    this.startEnemyTurn();
  }

  private async startEnemyTurn() {
    this.state = BattleState.EnemyTurn;
    this.hud.updateTopBar(this.scenario.stageName, this.turn, false, this.currentWeather.name);
    this.renderer.fx.showTurnBanner(this.turn, false);

    for (const u of this.enemyUnits) {
      if (u.isAlive()) {
        u.resetTurn();
        const terrain = this.grid.getTerrainDef(u.x, u.y);
        if (terrain.hpRecovery > 0) {
          const rec = Math.round(u.maxHp * terrain.hpRecovery);
          u.heal(rec);
        }
      }
    }

    await this.delay(1000 / this.hud.battleSpeed);

    const livingEnemies = this.enemyUnits.filter(u => u.isAlive());
    for (const enemy of livingEnemies) {
      if (!enemy.isAlive() || this.checkBattleEnd()) break;

      const plan = EnemyAI.planAction(enemy, this.grid, [...this.playerUnits, ...this.enemyUnits]);
      const center = this.grid.gridToWorldCenter(enemy.x, enemy.y);
      this.camera.focusOn(center.x, center.y);

      if (plan.path.length > 1) {
        await this.animateUnitMovement(enemy, plan.path);
        enemy.x = plan.destX;
        enemy.y = plan.destY;
        enemy.hasMoved = true;
      }

      if (plan.actionType === 'attack' && plan.targetUnit) {
        await this.performAttack(enemy, plan.targetUnit);
      } else if (plan.actionType === 'skill' && plan.targetUnit && plan.skill) {
        await this.performSkill(enemy, plan.targetUnit, plan.skill);
      } else if (plan.actionType === 'heal' && plan.targetUnit && plan.skill) {
        await this.performSkill(enemy, plan.targetUnit, plan.skill);
      }

      enemy.hasActed = true;
      await this.delay(300 / this.hud.battleSpeed);
    }

    if (!this.checkBattleEnd()) {
      this.turn++;
      this.startPlayerTurn();
    }
  }

  // ================= Input Handling =================

  private bindInputs(canvas: HTMLCanvasElement) {
    let isDragging = false;
    let startX = 0;
    let startY = 0;

    canvas.addEventListener('mousedown', e => {
      if (e.button === 0) {
        isDragging = false;
        startX = e.clientX;
        startY = e.clientY;
      }
    });

    canvas.addEventListener('mousemove', e => {
      if (!this.grid) return;
      if (e.buttons === 1) {
        const dx = e.clientX - startX;
        const dy = e.clientY - startY;
        if (Math.abs(dx) > 3 || Math.abs(dy) > 3) {
          isDragging = true;
          this.camera.pan(dx, dy);
          startX = e.clientX;
          startY = e.clientY;
        }
      }

      const rect = canvas.getBoundingClientRect();
      const sx = e.clientX - rect.left;
      const sy = e.clientY - rect.top;
      const worldPos = this.camera.screenToWorld(sx, sy);
      const gridPos = this.grid.worldToGrid(worldPos.x, worldPos.y);
      this.renderer.hoveredGrid = gridPos;
      this.hud.updateTerrainCard(gridPos.x, gridPos.y, this.grid);
    });

    canvas.addEventListener('mouseup', e => {
      if (!this.grid) return;
      if (!isDragging && e.button === 0) {
        const rect = canvas.getBoundingClientRect();
        const sx = e.clientX - rect.left;
        const sy = e.clientY - rect.top;
        const worldPos = this.camera.screenToWorld(sx, sy);
        const gridPos = this.grid.worldToGrid(worldPos.x, worldPos.y);
        this.handleClickGrid(gridPos.x, gridPos.y, e.clientX, e.clientY);
      }
      isDragging = false;
    });

    canvas.addEventListener('wheel', e => {
      e.preventDefault();
      const rect = canvas.getBoundingClientRect();
      const sx = e.clientX - rect.left;
      const sy = e.clientY - rect.top;
      const delta = e.deltaY < 0 ? 0.15 : -0.15;
      this.camera.setZoom(this.camera.zoom + delta, sx, sy);
    }, { passive: false });

    window.addEventListener('keydown', e => {
      if (e.key === 'Escape') this.handleMenuCancel();
      else if (e.key === 'w' || e.key === 'ArrowUp') this.camera.pan(0, 40);
      else if (e.key === 's' || e.key === 'ArrowDown') this.camera.pan(0, -40);
      else if (e.key === 'a' || e.key === 'ArrowLeft') this.camera.pan(40, 0);
      else if (e.key === 'd' || e.key === 'ArrowRight') this.camera.pan(-40, 0);
    });
  }

  private handleClickGrid(gx: number, gy: number, screenX: number, screenY: number) {
    if (!this.grid || !this.grid.inBounds(gx, gy)) return;

    const clickedUnit = this.getUnitAt(gx, gy);
    if (clickedUnit) {
      this.hud.showUnitCard(clickedUnit);
    }

    if (this.state === BattleState.PlayerIdle) {
      if (clickedUnit && !clickedUnit.isEnemy && !clickedUnit.hasActed) {
        this.sound.playSelectSfx();
        this.selectPlayerUnit(clickedUnit, screenX, screenY);
      }
    } else if (this.state === BattleState.SelectMove) {
      const key = `${gx},${gy}`;
      const reach = this.reachableMap.get(key);
      if (reach && this.selectedUnit) {
        this.sound.playMoveSfx();
        this.state = BattleState.Animating;
        this.animateUnitMovement(this.selectedUnit, reach.path).then(() => {
          this.selectedUnit!.x = gx;
          this.selectedUnit!.y = gy;
          this.selectedUnit!.hasMoved = true;
          this.renderer.clearHighlights();
          this.state = BattleState.ActionMenu;
          const center = this.grid.gridToWorldCenter(gx, gy);
          const sc = this.camera.worldToScreen(center.x, center.y);
          this.hud.showActionMenu(sc.x, sc.y, this.selectedUnit!);
        });
      } else {
        this.sound.playCancelSfx();
        this.clearSelection();
      }
    } else if (this.state === BattleState.SelectAttack) {
      if (clickedUnit && clickedUnit.isEnemy && this.attackTargets.includes(clickedUnit)) {
        this.executePlayerAttack(clickedUnit);
      } else {
        this.sound.playCancelSfx();
        this.openActionMenuForSelected();
      }
    } else if (this.state === BattleState.SelectSkill) {
      if (this.selectedSkill && this.selectedUnit) {
        const dist = this.grid.getDistance(this.selectedUnit.x, this.selectedUnit.y, gx, gy);
        if (dist <= this.selectedSkill.range) {
          const targetUnit = clickedUnit || this.selectedUnit;
          this.executePlayerSkill(targetUnit, this.selectedSkill);
        } else {
          this.sound.playCancelSfx();
          this.openActionMenuForSelected();
        }
      }
    }
  }

  private selectPlayerUnit(u: Unit, screenX: number, screenY: number) {
    this.selectedUnit = u;
    this.preMovePos = { x: u.x, y: u.y };
    this.renderer.selectedUnit = u;
    this.hud.setUnitForSkills(u);

    if (!u.hasMoved) {
      this.reachableMap = Pathfinding.getReachableTiles(u, this.grid, [...this.playerUnits, ...this.enemyUnits]);
      this.renderer.clearHighlights();
      for (const [key] of this.reachableMap) {
        this.renderer.reachableTiles.add(key);
      }
      this.state = BattleState.SelectMove;
    } else {
      this.openActionMenuForSelected(screenX, screenY);
    }
  }

  private openActionMenuForSelected(screenX?: number, screenY?: number) {
    if (!this.selectedUnit) return;
    this.state = BattleState.ActionMenu;
    const center = this.grid.gridToWorldCenter(this.selectedUnit.x, this.selectedUnit.y);
    const sp = this.camera.worldToScreen(center.x, center.y);
    this.hud.showActionMenu(screenX ?? sp.x, screenY ?? sp.y, this.selectedUnit);
  }

  private handleMenuMove() {
    if (!this.selectedUnit || this.selectedUnit.hasMoved) return;
    this.hud.hideActionMenu();
    this.state = BattleState.SelectMove;
    this.reachableMap = Pathfinding.getReachableTiles(this.selectedUnit, this.grid, [...this.playerUnits, ...this.enemyUnits]);
    this.renderer.clearHighlights();
    for (const [key] of this.reachableMap) {
      this.renderer.reachableTiles.add(key);
    }
  }

  private handleMenuAttack() {
    if (!this.selectedUnit) return;
    this.hud.hideActionMenu();
    this.attackTargets = Pathfinding.getAttackTargets(
      this.selectedUnit,
      this.selectedUnit.x,
      this.selectedUnit.y,
      this.grid,
      [...this.playerUnits, ...this.enemyUnits]
    );

    if (this.attackTargets.length === 0) {
      this.renderer.fx.addMissFloater(
        this.grid.gridToWorldCenter(this.selectedUnit.x, this.selectedUnit.y).x,
        this.grid.gridToWorldCenter(this.selectedUnit.x, this.selectedUnit.y).y
      );
      this.openActionMenuForSelected();
      return;
    }

    this.state = BattleState.SelectAttack;
    this.renderer.clearHighlights();
    for (const t of this.attackTargets) {
      this.renderer.attackableTiles.add(`${t.x},${t.y}`);
    }
  }

  private handleMenuSkillSelect(skill: SkillData) {
    if (!this.selectedUnit) return;
    // 豪雨禁火限制
    if (!this.currentWeather.allowFire && skill.element === SkillElement.Fire) {
      alert('【豪雨天气】火系策略被大雨压制，无法施展！');
      this.openActionMenuForSelected();
      return;
    }

    this.selectedSkill = skill;
    this.state = BattleState.SelectSkill;
    this.renderer.clearHighlights();

    for (let dy = -skill.range; dy <= skill.range; dy++) {
      for (let dx = -skill.range; dx <= skill.range; dx++) {
        const dist = Math.abs(dx) + Math.abs(dy);
        if (dist <= skill.range) {
          const nx = this.selectedUnit.x + dx;
          const ny = this.selectedUnit.y + dy;
          if (this.grid.inBounds(nx, ny)) {
            this.renderer.skillTiles.add(`${nx},${ny}`);
          }
        }
      }
    }
  }

  private handleMenuItemSelect(item: ItemDef) {
    if (!this.selectedUnit) return;
    this.selectedUnit.heal(item.healHp || 50);
    this.sound.playHealSfx();
    const c = this.grid.gridToWorldCenter(this.selectedUnit.x, this.selectedUnit.y);
    this.renderer.fx.addHealFloater(c.x, c.y, item.healHp || 50);
    this.completeUnitAction();
  }

  private handleMenuWait() {
    this.completeUnitAction();
  }

  private handleMenuCancel() {
    if (this.selectedUnit && this.preMovePos) {
      this.selectedUnit.x = this.preMovePos.x;
      this.selectedUnit.y = this.preMovePos.y;
      this.selectedUnit.hasMoved = false;
    }
    this.clearSelection();
  }

  private clearSelection() {
    this.selectedUnit = null;
    this.preMovePos = null;
    this.selectedSkill = null;
    this.attackTargets = [];
    this.renderer.selectedUnit = null;
    this.renderer.clearHighlights();
    this.hud.hideActionMenu();
    this.state = BattleState.PlayerIdle;
  }

  private completeUnitAction() {
    if (this.selectedUnit) {
      this.selectedUnit.hasActed = true;
      this.selectedUnit.hasMoved = true;
    }
    this.clearSelection();
    this.checkBattleEnd();

    const unacted = this.playerUnits.filter(u => u.isAlive() && !u.hasActed);
    if (unacted.length === 0) {
      this.endPlayerTurn();
    }
  }

  private async executePlayerAttack(target: Unit) {
    if (!this.selectedUnit) return;
    const attacker = this.selectedUnit;
    this.renderer.clearHighlights();
    this.state = BattleState.Animating;

    // 单挑判定（武将对决）
    if (attacker.isLeader && target.isLeader && this.turn <= 5) {
      await this.triggerDuelCutscene(attacker, target);
    } else {
      await this.performAttack(attacker, target);
    }

    this.completeUnitAction();
  }

  private async triggerDuelCutscene(attacker: Unit, defender: Unit): Promise<void> {
    this.state = BattleState.Duel;
    this.sound.playCritSfx();
    return new Promise(resolve => {
      this.dialogBox.show([
        { speaker: attacker.name, faceId: attacker.faceId, text: `${attacker.name}：来将通名！可敢与我一决生死？！` },
        { speaker: defender.name, faceId: defender.faceId, text: `${defender.name}：休夸海口！看刀！` },
        { speaker: '决斗特写', faceId: -1, text: `⚔️【单挑爆发】两马交错，刀光剑影！${attacker.name} 斩落敌酋！` }
      ], async () => {
        defender.takeDamage(999);
        const defCenter = this.grid.gridToWorldCenter(defender.x, defender.y);
        this.renderer.fx.addDamageFloater(defCenter.x, defCenter.y, 999, true);
        this.sound.playVictorySfx();
        await this.delay(500);
        resolve();
      });
    });
  }

  private async executePlayerSkill(target: Unit, skill: SkillData) {
    if (!this.selectedUnit) return;
    const caster = this.selectedUnit;
    this.renderer.clearHighlights();
    this.state = BattleState.Animating;

    await this.performSkill(caster, target, skill);
    this.completeUnitAction();
  }

  private async performAttack(attacker: Unit, defender: Unit) {
    attacker.animState = 'attacking';
    attacker.animFrame = 4;
    attacker.direction = attacker.x > defender.x ? 'left' : 'right';
    this.sound.playAttackSfx();

    await this.delay(250 / this.hud.battleSpeed);

    const res = CombatCalc.calcPhysical(attacker, defender, this.grid, [...this.playerUnits, ...this.enemyUnits]);
    const defCenter = this.grid.gridToWorldCenter(defender.x, defender.y);

    if (res.hit) {
      this.sound.playHitSfx();
      defender.animState = 'hurt';
      this.renderer.fx.addDamageFloater(defCenter.x, defCenter.y, res.damage, res.isCrit);
      this.renderer.fx.addSpellFX(defCenter.x, defCenter.y, 'slash');

      if (res.isCrit && res.quote) {
        this.sound.playCritSfx();
        this.renderer.fx.showCritCutin(attacker.name, res.quote, attacker.faceId);
      }
    } else {
      this.renderer.fx.addMissFloater(defCenter.x, defCenter.y);
    }

    if (res.expGained > 0 && !attacker.isEnemy) {
      const leveled = attacker.gainExp(res.expGained);
      if (leveled) this.sound.playLevelUpSfx();
    }

    // 方天画戟引导奋战追击
    if (res.cleaveTriggered) {
      const adjacentEnemies = this.enemyUnits.filter(u => u.isAlive() && this.grid.getDistance(attacker.x, attacker.y, u.x, u.y) === 1);
      if (adjacentEnemies.length > 0) {
        const nextTarget = adjacentEnemies[0];
        await this.delay(300 / this.hud.battleSpeed);
        this.renderer.fx.showCritCutin(attacker.name, '【方天画戟·引导奋战】接招！', attacker.faceId);
        await this.performAttack(attacker, nextTarget);
      }
    }

    await this.delay(400 / this.hud.battleSpeed);

    // 反击判定
    if (res.counterHit && defender.isAlive()) {
      defender.animState = 'attacking';
      defender.animFrame = 4;
      defender.direction = defender.x > attacker.x ? 'left' : 'right';
      this.sound.playAttackSfx();

      await this.delay(250 / this.hud.battleSpeed);
      const attCenter = this.grid.gridToWorldCenter(attacker.x, attacker.y);
      attacker.animState = 'hurt';
      this.sound.playHitSfx();
      this.renderer.fx.addDamageFloater(attCenter.x, attCenter.y, res.counterDamage || 1, res.counterCrit);
      this.renderer.fx.addSpellFX(attCenter.x, attCenter.y, 'slash');

      await this.delay(350 / this.hud.battleSpeed);
    }

    attacker.animState = 'idle';
    defender.animState = defender.isAlive() ? 'idle' : 'dead';
  }

  private async performSkill(caster: Unit, target: Unit, skill: SkillData) {
    caster.mp -= skill.mpCost;
    caster.animState = 'casting';
    caster.animFrame = 5;
    this.sound.playMagicSfx();

    await this.delay(300 / this.hud.battleSpeed);

    const res = CombatCalc.calcSpell(caster, target, skill, this.grid, this.currentWeather);
    const targetCenter = this.grid.gridToWorldCenter(target.x, target.y);

    if (skill.element === SkillElement.Fire) {
      this.renderer.fx.addSpellFX(targetCenter.x, targetCenter.y, 'fire');
    } else if (skill.element === SkillElement.Heal) {
      this.renderer.fx.addSpellFX(targetCenter.x, targetCenter.y, 'heal');
      this.sound.playHealSfx();
    } else if (skill.element === SkillElement.Water) {
      this.renderer.fx.addSpellFX(targetCenter.x, targetCenter.y, 'water');
    }

    if (res.damage > 0) {
      target.animState = 'hurt';
      this.sound.playHitSfx();
      this.renderer.fx.addDamageFloater(targetCenter.x, targetCenter.y, res.damage);
    } else if (res.healAmount > 0) {
      this.renderer.fx.addHealFloater(targetCenter.x, targetCenter.y, res.healAmount);
    }

    if (res.expGained > 0 && !caster.isEnemy) {
      const leveled = caster.gainExp(res.expGained);
      if (leveled) this.sound.playLevelUpSfx();
    }

    await this.delay(500 / this.hud.battleSpeed);
    caster.animState = 'idle';
    target.animState = target.isAlive() ? 'idle' : 'dead';
  }

  private async animateUnitMovement(unit: Unit, path: { x: number; y: number }[]): Promise<void> {
    unit.animState = 'walking';
    for (let i = 1; i < path.length; i++) {
      const next = path[i];
      unit.direction = next.x < unit.x ? 'left' : (next.x > unit.x ? 'right' : (next.y < unit.y ? 'up' : 'down'));
      unit.x = next.x;
      unit.y = next.y;
      await this.delay(120 / this.hud.battleSpeed);
    }
    unit.animState = 'idle';
  }

  private checkBattleEnd(): boolean {
    const leaderDead = !this.playerUnits[0] || !this.playerUnits[0].isAlive();
    if (leaderDead) {
      this.state = BattleState.Defeat;
      this.dialogBox.show([
        { speaker: '系统', faceId: -1, text: '我军主帅阵亡……战役失败！' }
      ], () => {
        this.loadStage(this.stageIndex);
      });
      return true;
    }

    const enemyLeaderDead = this.enemyUnits.filter(u => u.isLeader && u.isAlive()).length === 0;
    const allEnemiesDead = this.enemyUnits.filter(u => u.isAlive()).length === 0;

    if (enemyLeaderDead || allEnemiesDead) {
      this.state = BattleState.Victory;
      this.sound.playVictorySfx();
      this.dialogBox.show(this.scenario.outroDialogs, () => {
        const nextStage = (this.stageIndex + 1) % ScenarioBuilder.getStagesCount();
        this.loadStage(nextStage);
      });
      return true;
    }

    return false;
  }

  private getUnitAt(x: number, y: number): Unit | null {
    for (const u of [...this.playerUnits, ...this.enemyUnits]) {
      if (u.isAlive() && u.x === x && u.y === y) return u;
    }
    return null;
  }

  private delay(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  public update(dt: number) {
    if (!this.grid) return;
    this.renderer.render(this.grid, [...this.playerUnits, ...this.enemyUnits], this.stageIndex, dt);
  }

  // Save / Load System
  private exportSaveState(slot: number): SaveData {
    return {
      slot,
      date: new Date().toLocaleString(),
      stageIndex: this.stageIndex,
      stageName: this.scenario?.stageName || `第 ${this.stageIndex + 1} 战`,
      turn: this.turn,
      officers: this.playerUnits.map(u => ({
        id: u.officerId,
        name: u.name,
        level: u.level,
        hp: u.hp,
        mp: u.mp,
        exp: u.exp,
        weaponId: u.weaponId,
        armorId: u.armorId,
        accessoryId: u.accessoryId
      }))
    };
  }

  private importSaveState(data: SaveData) {
    const ids = data.officers.map(o => o.id);
    this.loadStage(data.stageIndex, ids).then(() => {
      this.turn = data.turn;
      for (let i = 0; i < data.officers.length && i < this.playerUnits.length; i++) {
        const o = data.officers[i];
        const u = this.playerUnits[i];
        u.level = o.level;
        u.hp = o.hp;
        u.mp = o.mp;
        u.exp = o.exp;
        u.weaponId = o.weaponId;
        u.armorId = o.armorId;
        u.accessoryId = o.accessoryId;
      }
      this.hud.updateTopBar(this.scenario.stageName, this.turn, true, this.currentWeather.name);
    });
  }
}
