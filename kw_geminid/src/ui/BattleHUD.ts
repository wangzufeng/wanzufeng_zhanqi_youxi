import { Unit } from '../game/model/Unit';
import { BattleGrid } from '../game/model/BattleGrid';
import { SkillData } from '../game/model/Skill';
import { ItemDef, ItemHelper } from '../game/model/Item';
import { SoundManager } from '../core/audio/SoundManager';

export interface ActionMenuCallbacks {
  onMove: () => void;
  onAttack: () => void;
  onSkillSelect: (skill: SkillData) => void;
  onItemSelect: (item: ItemDef) => void;
  onWait: () => void;
  onCancel: () => void;
  onEndTurn: () => void;
  onOpenStageSelect: () => void;
  onOpenFormation: () => void;
  onOpenJukebox: () => void;
  onOpenSave: () => void;
}

export class BattleHUD {
  private container: HTMLElement;
  private topBarEl: HTMLElement;
  private actionMenuEl: HTMLElement;
  private skillSubmenuEl: HTMLElement;
  private unitCardEl: HTMLElement;
  private terrainCardEl: HTMLElement;
  private endTurnBtn: HTMLButtonElement;
  private callbacks: ActionMenuCallbacks;
  private sound: SoundManager;

  public battleSpeed: number = 1;

  constructor(callbacks: ActionMenuCallbacks) {
    this.callbacks = callbacks;
    this.sound = SoundManager.getInstance();
    this.container = document.createElement('div');
    this.container.className = 'battle-hud-layer';

    this.container.innerHTML = `
      <!-- Top Navigation & Control Bar -->
      <div class="top-nav-bar">
        <div class="nav-left">
          <span class="stage-badge" id="hud-stage-name">战役名称</span>
          <span class="turn-badge" id="hud-turn-label">第 1 回合</span>
          <span class="weather-badge" id="hud-weather-label">☀️ 晴天</span>
          <span class="phase-badge" id="hud-phase-label">我军行动</span>
        </div>
        <div class="nav-right">
          <button class="hud-btn" id="btn-speed" title="切换游戏速度">速度 1x</button>
          <button class="hud-btn" id="btn-stages" title="87关选关">选关</button>
          <button class="hud-btn" id="btn-formation" title="出阵与装备">布阵</button>
          <button class="hud-btn" id="btn-jukebox" title="原版音乐殿堂">音乐</button>
          <button class="hud-btn" id="btn-save" title="存档管理">存读</button>
          <button class="hud-btn btn-end-turn" id="btn-end-turn">结束回合</button>
        </div>
      </div>

      <!-- Action Command Menu -->
      <div class="action-menu-panel hidden" id="action-menu">
        <div class="menu-title">指令菜单</div>
        <button class="menu-item" id="menu-move"><span>🏃</span> 移动</button>
        <button class="menu-item" id="menu-attack"><span>⚔️</span> 攻击</button>
        <button class="menu-item" id="menu-skill"><span>🔥</span> 策略</button>
        <button class="menu-item" id="menu-item"><span>🎒</span> 道具</button>
        <button class="menu-item" id="menu-wait"><span>🛡️</span> 待机</button>
        <button class="menu-item btn-cancel" id="menu-cancel"><span>✖️</span> 取消</button>
      </div>

      <!-- Strategy Submenu -->
      <div class="action-submenu hidden" id="skill-submenu">
        <div class="submenu-header">选择策略</div>
        <div class="submenu-list" id="skill-list"></div>
        <button class="submenu-back-btn" id="skill-back">返回</button>
      </div>

      <!-- Bottom-Left Unit Detail Card -->
      <div class="unit-detail-card hidden" id="unit-card">
        <div class="card-avatar-box">
          <img id="card-avatar" src="" alt="avatar" />
          <span class="card-class-badge" id="card-class">兵种</span>
        </div>
        <div class="card-main-info">
          <div class="card-header-row">
            <span class="card-name" id="card-name">武将名</span>
            <span class="card-level" id="card-level">Lv. 1</span>
          </div>
          <div class="card-bar-wrap">
            <div class="bar-label">HP</div>
            <div class="bar-track"><div class="bar-fill hp" id="card-hp-fill"></div></div>
            <span class="bar-num" id="card-hp-text">100/100</span>
          </div>
          <div class="card-bar-wrap">
            <div class="bar-label">MP</div>
            <div class="bar-track"><div class="bar-fill mp" id="card-mp-fill"></div></div>
            <span class="bar-num" id="card-mp-text">30/30</span>
          </div>
          <div class="card-stats-grid">
            <div>攻击 <b id="card-atk">0</b></div>
            <div>防御 <b id="card-def">0</b></div>
            <div>精神 <b id="card-mind">0</b></div>
            <div>移动 <b id="card-move">0</b></div>
          </div>
          <div class="card-equip-row">
            <span class="equip-tag" id="card-weapon">⚔️ 无</span>
            <span class="equip-tag" id="card-armor">🛡️ 无</span>
          </div>
        </div>
      </div>

      <!-- Bottom-Right Terrain Card -->
      <div class="terrain-info-card" id="terrain-card">
        <div class="terrain-header">
          <span class="terrain-name" id="terrain-name">平原</span>
          <span class="terrain-coord" id="terrain-coord">(0, 0)</span>
        </div>
        <div class="terrain-body">
          <div>防御加成: <b id="terrain-def">+0%</b></div>
          <div>每回合恢复: <b id="terrain-recovery">0%</b></div>
        </div>
      </div>
    `;

    document.body.appendChild(this.container);

    this.topBarEl = this.container.querySelector('.top-nav-bar')!;
    this.actionMenuEl = this.container.querySelector('#action-menu')!;
    this.skillSubmenuEl = this.container.querySelector('#skill-submenu')!;
    this.unitCardEl = this.container.querySelector('#unit-card')!;
    this.terrainCardEl = this.container.querySelector('#terrain-card')!;
    this.endTurnBtn = this.container.querySelector('#btn-end-turn')!;

    this.bindEvents();
  }

  private bindEvents() {
    this.container.querySelector('#btn-end-turn')?.addEventListener('click', () => {
      this.sound.playSelectSfx();
      this.callbacks.onEndTurn();
    });

    this.container.querySelector('#btn-speed')?.addEventListener('click', e => {
      this.sound.playSelectSfx();
      if (this.battleSpeed === 1) this.battleSpeed = 2;
      else if (this.battleSpeed === 2) this.battleSpeed = 4;
      else this.battleSpeed = 1;
      (e.target as HTMLElement).textContent = `速度 ${this.battleSpeed}x`;
    });

    this.container.querySelector('#btn-stages')?.addEventListener('click', () => {
      this.sound.playSelectSfx();
      this.callbacks.onOpenStageSelect();
    });

    this.container.querySelector('#btn-formation')?.addEventListener('click', () => {
      this.sound.playSelectSfx();
      this.callbacks.onOpenFormation();
    });

    this.container.querySelector('#btn-jukebox')?.addEventListener('click', () => {
      this.sound.playSelectSfx();
      this.callbacks.onOpenJukebox();
    });

    this.container.querySelector('#btn-save')?.addEventListener('click', () => {
      this.sound.playSelectSfx();
      this.callbacks.onOpenSave();
    });

    this.container.querySelector('#menu-move')?.addEventListener('click', () => {
      this.sound.playSelectSfx();
      this.callbacks.onMove();
    });

    this.container.querySelector('#menu-attack')?.addEventListener('click', () => {
      this.sound.playSelectSfx();
      this.callbacks.onAttack();
    });

    this.container.querySelector('#menu-skill')?.addEventListener('click', () => {
      this.sound.playSelectSfx();
      this.openSkillSubmenu();
    });

    this.container.querySelector('#menu-item')?.addEventListener('click', () => {
      this.sound.playSelectSfx();
      this.callbacks.onItemSelect(ItemHelper.get(21)); // 恢复用豆
    });

    this.container.querySelector('#menu-wait')?.addEventListener('click', () => {
      this.sound.playSelectSfx();
      this.callbacks.onWait();
    });

    this.container.querySelector('#menu-cancel')?.addEventListener('click', () => {
      this.sound.playCancelSfx();
      this.callbacks.onCancel();
    });

    this.container.querySelector('#skill-back')?.addEventListener('click', () => {
      this.sound.playCancelSfx();
      this.skillSubmenuEl.classList.add('hidden');
      this.actionMenuEl.classList.remove('hidden');
    });
  }

  public updateTopBar(stageName: string, turn: number, isPlayerTurn: boolean, weatherName: string = '晴天') {
    this.container.querySelector('#hud-stage-name')!.textContent = stageName;
    this.container.querySelector('#hud-turn-label')!.textContent = `第 ${turn} 回合`;

    const weatherEl = this.container.querySelector('#hud-weather-label')!;
    weatherEl.textContent = `🌤️ ${weatherName}`;

    const phaseEl = this.container.querySelector('#hud-phase-label')!;
    phaseEl.textContent = isPlayerTurn ? '我军行动' : '敌军行动';
    phaseEl.className = isPlayerTurn ? 'phase-badge player' : 'phase-badge enemy';
    this.endTurnBtn.disabled = !isPlayerTurn;
  }

  public showActionMenu(screenX: number, screenY: number, unit: Unit) {
    this.actionMenuEl.style.left = `${Math.min(window.innerWidth - 180, Math.max(20, screenX + 30))}px`;
    this.actionMenuEl.style.top = `${Math.min(window.innerHeight - 240, Math.max(80, screenY - 50))}px`;
    this.actionMenuEl.classList.remove('hidden');
    this.skillSubmenuEl.classList.add('hidden');

    const moveBtn = this.container.querySelector('#menu-move') as HTMLButtonElement;
    if (moveBtn) moveBtn.disabled = unit.hasMoved;

    const skillBtn = this.container.querySelector('#menu-skill') as HTMLButtonElement;
    if (skillBtn) skillBtn.disabled = unit.skills.length === 0;
  }

  public hideActionMenu() {
    this.actionMenuEl.classList.add('hidden');
    this.skillSubmenuEl.classList.add('hidden');
  }

  private currentUnitForSkills: Unit | null = null;
  public setUnitForSkills(u: Unit) {
    this.currentUnitForSkills = u;
  }

  private openSkillSubmenu() {
    if (!this.currentUnitForSkills) return;
    const u = this.currentUnitForSkills;
    const listEl = this.container.querySelector('#skill-list')!;
    listEl.innerHTML = '';

    for (const s of u.skills) {
      const btn = document.createElement('button');
      btn.className = 'submenu-item';
      btn.disabled = u.mp < s.mpCost;
      btn.innerHTML = `
        <div class="item-name">${s.name}</div>
        <div class="item-meta">MP: ${s.mpCost} | 威力: ${s.power}</div>
        <div class="item-desc">${s.description}</div>
      `;
      btn.addEventListener('click', () => {
        this.sound.playSelectSfx();
        this.hideActionMenu();
        this.callbacks.onSkillSelect(s);
      });
      listEl.appendChild(btn);
    }

    this.actionMenuEl.classList.add('hidden');
    this.skillSubmenuEl.style.left = this.actionMenuEl.style.left;
    this.skillSubmenuEl.style.top = this.actionMenuEl.style.top;
    this.skillSubmenuEl.classList.remove('hidden');
  }

  public showUnitCard(unit: Unit) {
    this.unitCardEl.classList.remove('hidden');
    (this.container.querySelector('#card-avatar') as HTMLImageElement).src = `/assets/faces/face_${unit.faceId}.png`;
    this.container.querySelector('#card-name')!.textContent = unit.name;
    this.container.querySelector('#card-class')!.textContent = unit.unitClass;
    this.container.querySelector('#card-level')!.textContent = `Lv. ${unit.level} (EXP ${unit.exp}/100)`;

    const hpRatio = Math.max(0, Math.min(1, unit.hp / unit.maxHp));
    (this.container.querySelector('#card-hp-fill') as HTMLElement).style.width = `${hpRatio * 100}%`;
    this.container.querySelector('#card-hp-text')!.textContent = `${unit.hp}/${unit.maxHp}`;

    const mpRatio = Math.max(0, Math.min(1, unit.mp / unit.maxMp));
    (this.container.querySelector('#card-mp-fill') as HTMLElement).style.width = `${mpRatio * 100}%`;
    this.container.querySelector('#card-mp-text')!.textContent = `${unit.mp}/${unit.maxMp}`;

    this.container.querySelector('#card-atk')!.textContent = `${unit.getAtk()}`;
    this.container.querySelector('#card-def')!.textContent = `${unit.getDef()}`;
    this.container.querySelector('#card-mind')!.textContent = `${unit.getMind()}`;
    this.container.querySelector('#card-move')!.textContent = `${unit.getMove()}`;

    const weapon = ItemHelper.get(unit.weaponId);
    const armor = ItemHelper.get(unit.armorId);
    this.container.querySelector('#card-weapon')!.textContent = `⚔️ ${weapon.name} (Lv.${unit.weaponLevel})`;
    this.container.querySelector('#card-armor')!.textContent = `🛡️ ${armor.name} (Lv.${unit.armorLevel})`;
  }

  public hideUnitCard() {
    this.unitCardEl.classList.add('hidden');
  }

  public updateTerrainCard(x: number, y: number, grid: BattleGrid) {
    if (!grid.inBounds(x, y)) return;
    const t = grid.getTerrainDef(x, y);
    this.container.querySelector('#terrain-name')!.textContent = t.name;
    this.container.querySelector('#terrain-coord')!.textContent = `(${x}, ${y})`;
    this.container.querySelector('#terrain-def')!.textContent = `${t.defBonus >= 0 ? '+' : ''}${Math.round(t.defBonus * 100)}%`;
    this.container.querySelector('#terrain-recovery')!.textContent = `${Math.round(t.hpRecovery * 100)}%`;
  }
}
