import { SoundManager } from '../core/audio/SoundManager';

export interface SaveData {
  slot: number;
  date: string;
  stageIndex: number;
  stageName: string;
  turn: number;
  officers: {
    id: number;
    name: string;
    level: number;
    hp: number;
    mp: number;
    exp: number;
    weaponId: number;
    armorId: number;
    accessoryId: number;
  }[];
}

export class SaveManager {
  private container: HTMLElement;
  private sound: SoundManager;
  private onSaveCurrent: (slot: number) => SaveData;
  private onLoadSlot: (data: SaveData) => void;

  constructor(
    onSaveCurrent: (slot: number) => SaveData,
    onLoadSlot: (data: SaveData) => void
  ) {
    this.onSaveCurrent = onSaveCurrent;
    this.onLoadSlot = onLoadSlot;
    this.sound = SoundManager.getInstance();

    this.container = document.createElement('div');
    this.container.className = 'modal-backdrop hidden';

    this.container.innerHTML = `
      <div class="modal-card save-card">
        <div class="modal-header">
          <h2>💾 游戏存档与读档管理</h2>
          <button class="modal-close-btn" id="save-close-btn">✖</button>
        </div>
        <div class="save-slots-list" id="save-slots-list"></div>
        <div class="modal-footer">
          <button class="hud-btn" id="export-save-btn">导出存档文件(JSON)</button>
          <label class="hud-btn file-input-label">
            导入存档
            <input type="file" id="import-save-input" accept=".json" style="display:none;" />
          </label>
        </div>
      </div>
    `;

    document.body.appendChild(this.container);

    this.container.querySelector('#save-close-btn')?.addEventListener('click', () => this.close());
    this.container.querySelector('#export-save-btn')?.addEventListener('click', () => this.exportSaveFile());
    const fileInput = this.container.querySelector('#import-save-input') as HTMLInputElement;
    fileInput.addEventListener('change', e => this.importSaveFile(e));
  }

  public show() {
    this.container.classList.remove('hidden');
    this.renderSlots();
  }

  public close() {
    this.container.classList.add('hidden');
  }

  private renderSlots() {
    const listEl = this.container.querySelector('#save-slots-list')!;
    listEl.innerHTML = '';

    for (let slot = 1; slot <= 5; slot++) {
      const raw = localStorage.getItem(`kw_save_slot_${slot}`);
      const data: SaveData | null = raw ? JSON.parse(raw) : null;

      const item = document.createElement('div');
      item.className = 'save-slot-item';

      if (data) {
        item.innerHTML = `
          <div class="slot-idx">存档位 ${slot}</div>
          <div class="slot-desc">
            <div class="slot-stage">🏯 ${data.stageName} (第 ${data.turn} 回合)</div>
            <div class="slot-date">🕒 ${data.date}</div>
          </div>
          <div class="slot-actions">
            <button class="hud-btn btn-save-action" data-slot="${slot}">覆盖保存</button>
            <button class="hud-btn btn-load-action" data-slot="${slot}">读取进度</button>
            <button class="hud-btn btn-delete-action" data-slot="${slot}">删除</button>
          </div>
        `;
      } else {
        item.innerHTML = `
          <div class="slot-idx">存档位 ${slot}</div>
          <div class="slot-desc empty">空存档位</div>
          <div class="slot-actions">
            <button class="hud-btn btn-save-action" data-slot="${slot}">保存至此</button>
          </div>
        `;
      }

      item.querySelector('.btn-save-action')?.addEventListener('click', () => {
        this.sound.playSelectSfx();
        const saved = this.onSaveCurrent(slot);
        localStorage.setItem(`kw_save_slot_${slot}`, JSON.stringify(saved));
        this.renderSlots();
      });

      item.querySelector('.btn-load-action')?.addEventListener('click', () => {
        this.sound.playVictorySfx();
        if (data) {
          this.close();
          this.onLoadSlot(data);
        }
      });

      item.querySelector('.btn-delete-action')?.addEventListener('click', () => {
        this.sound.playCancelSfx();
        localStorage.removeItem(`kw_save_slot_${slot}`);
        this.renderSlots();
      });

      listEl.appendChild(item);
    }
  }

  private exportSaveFile() {
    const raw = localStorage.getItem('kw_save_slot_1') || localStorage.getItem('kw_save_slot_2') || localStorage.getItem('kw_save_slot_3');
    if (!raw) {
      alert('没有可导出的存档，请先保存一个进度！');
      return;
    }
    const blob = new Blob([raw], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `kw_caocaozhuan_save_${Date.now()}.json`;
    a.click();
    URL.revokeObjectURL(url);
  }

  private importSaveFile(e: Event) {
    const file = (e.target as HTMLInputElement).files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = ev => {
      try {
        const text = ev.target?.result as string;
        const data = JSON.parse(text) as SaveData;
        localStorage.setItem(`kw_save_slot_1`, JSON.stringify(data));
        this.sound.playVictorySfx();
        this.renderSlots();
        alert('存档导入成功，已存入【存档位 1】！');
      } catch (err) {
        alert('存档文件解析失败！');
      }
    };
    reader.readAsText(file);
  }
}
