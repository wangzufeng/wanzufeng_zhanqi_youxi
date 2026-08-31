import { SoundManager } from '../core/audio/SoundManager';

export class FormationModal {
  private container: HTMLElement;
  private onDeploy: (officerIds: number[]) => void;
  private sound: SoundManager;
  private officers: any[] = [];
  private selectedOfficerIds: number[] = [0, 1, 2, 3, 4, 5, 6, 7];

  constructor(onDeploy: (officerIds: number[]) => void) {
    this.onDeploy = onDeploy;
    this.sound = SoundManager.getInstance();
    this.container = document.createElement('div');
    this.container.className = 'modal-backdrop hidden';

    this.container.innerHTML = `
      <div class="modal-card formation-card">
        <div class="modal-header">
          <h2>⚔️ 出战阵容与武将编成 (已选 <span id="formation-count">8</span>/8)</h2>
          <button class="modal-close-btn" id="formation-close-btn">✖</button>
        </div>
        <div class="formation-body">
          <div class="roster-pool" id="roster-pool"></div>
        </div>
        <div class="modal-footer">
          <button class="hud-btn" id="formation-reset-btn">重置默认</button>
          <button class="hud-btn btn-confirm" id="formation-deploy-btn">确认出阵</button>
        </div>
      </div>
    `;

    document.body.appendChild(this.container);

    this.container.querySelector('#formation-close-btn')?.addEventListener('click', () => this.close());
    this.container.querySelector('#formation-reset-btn')?.addEventListener('click', () => {
      this.selectedOfficerIds = [0, 1, 2, 3, 4, 5, 6, 7];
      this.renderRoster();
    });
    this.container.querySelector('#formation-deploy-btn')?.addEventListener('click', () => {
      this.sound.playVictorySfx();
      this.close();
      this.onDeploy([...this.selectedOfficerIds]);
    });

    this.loadOfficers();
  }

  private async loadOfficers() {
    try {
      const res = await fetch('/assets/data/officers.json');
      if (res.ok) {
        this.officers = await res.json();
      }
    } catch (e) {
      console.warn('Officers load error:', e);
    }
  }

  public show(currentIds?: number[]) {
    if (currentIds && currentIds.length > 0) {
      this.selectedOfficerIds = [...currentIds];
    }
    this.container.classList.remove('hidden');
    this.renderRoster();
  }

  public close() {
    this.container.classList.add('hidden');
  }

  private renderRoster() {
    this.container.querySelector('#formation-count')!.textContent = `${this.selectedOfficerIds.length}`;
    const poolEl = this.container.querySelector('#roster-pool')!;
    poolEl.innerHTML = '';

    // Render first 40 prominent officers
    const maxShow = Math.min(60, this.officers.length || 40);
    for (let i = 0; i < maxShow; i++) {
      const off = this.officers[i] || {
        id: i,
        name: i === 0 ? '曹操' : `武将${i}`,
        faceId: i,
        stats: { force: 80, intel: 75, command: 80 }
      };

      const isSelected = this.selectedOfficerIds.includes(i);
      const item = document.createElement('div');
      item.className = `roster-item ${isSelected ? 'selected' : ''}`;
      item.innerHTML = `
        <img class="roster-avatar" src="/assets/faces/face_${off.faceId ?? i}.png" alt="avatar" />
        <div class="roster-info">
          <div class="roster-name">${off.name}</div>
          <div class="roster-stats">武 ${off.stats?.force ?? 80} | 统 ${off.stats?.command ?? 80} | 智 ${off.stats?.intel ?? 75}</div>
        </div>
        <div class="roster-checkbox">${isSelected ? '✔' : ''}</div>
      `;

      item.addEventListener('click', () => {
        this.sound.playSelectSfx();
        if (this.selectedOfficerIds.includes(i)) {
          if (this.selectedOfficerIds.length > 1) {
            this.selectedOfficerIds = this.selectedOfficerIds.filter(id => id !== i);
          }
        } else {
          if (this.selectedOfficerIds.length < 8) {
            this.selectedOfficerIds.push(i);
          }
        }
        this.renderRoster();
      });

      poolEl.appendChild(item);
    }
  }
}
