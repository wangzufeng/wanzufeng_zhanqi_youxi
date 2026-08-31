import { ScenarioBuilder } from '../game/battle/ScenarioBuilder';
import { SoundManager } from '../core/audio/SoundManager';

export class StageSelectModal {
  private container: HTMLElement;
  private onSelectStage: (stageIndex: number) => void;
  private sound: SoundManager;

  constructor(onSelectStage: (stageIndex: number) => void) {
    this.onSelectStage = onSelectStage;
    this.sound = SoundManager.getInstance();
    this.container = document.createElement('div');
    this.container.className = 'modal-backdrop hidden';

    this.container.innerHTML = `
      <div class="modal-card stage-select-card">
        <div class="modal-header">
          <h2>🏯 原版战役全 87 关选择</h2>
          <button class="modal-close-btn" id="stage-close-btn">✖</button>
        </div>
        <div class="modal-search-row">
          <input type="text" id="stage-search" placeholder="搜索关卡名称 (如: 颖川、虎牢关、官渡、赤壁)..." />
        </div>
        <div class="stages-grid" id="stages-grid"></div>
      </div>
    `;

    document.body.appendChild(this.container);

    this.container.querySelector('#stage-close-btn')?.addEventListener('click', () => this.close());
    this.container.querySelector('#stage-search')?.addEventListener('input', e => {
      this.filterStages((e.target as HTMLInputElement).value);
    });
  }

  public show() {
    this.container.classList.remove('hidden');
    this.renderStages();
  }

  public close() {
    this.container.classList.add('hidden');
  }

  private renderStages(filter: string = '') {
    const gridEl = this.container.querySelector('#stages-grid')!;
    gridEl.innerHTML = '';
    const total = ScenarioBuilder.getStagesCount();

    for (let i = 0; i < total; i++) {
      const info = ScenarioBuilder.getStageInfo(i);
      if (filter && !info.title.includes(filter)) continue;

      const item = document.createElement('div');
      item.className = 'stage-item';
      item.innerHTML = `
        <div class="stage-num">第 ${i + 1} 战</div>
        <div class="stage-name">${info.title}</div>
        <div class="stage-meta">地图尺寸: ${info.w} × ${info.h}</div>
      `;

      item.addEventListener('click', () => {
        this.sound.playSelectSfx();
        this.close();
        this.onSelectStage(i);
      });

      gridEl.appendChild(item);
    }
  }

  private filterStages(query: string) {
    this.renderStages(query.trim());
  }
}
