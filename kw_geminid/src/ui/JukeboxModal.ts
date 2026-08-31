import { SoundManager } from '../core/audio/SoundManager';

export class JukeboxModal {
  private container: HTMLElement;
  private sound: SoundManager;

  constructor() {
    this.sound = SoundManager.getInstance();
    this.container = document.createElement('div');
    this.container.className = 'modal-backdrop hidden';

    this.container.innerHTML = `
      <div class="modal-card jukebox-card">
        <div class="modal-header">
          <h2>🎵 原版音乐殿堂 (SoundTrk 134 曲)</h2>
          <button class="modal-close-btn" id="jukebox-close-btn">✖</button>
        </div>
        <div class="jukebox-controls">
          <span class="now-playing-label" id="now-playing">正在播放: 无</span>
          <div class="volume-slider-row">
            <span>音量:</span>
            <input type="range" id="bgm-volume-slider" min="0" max="100" value="50" />
            <button class="hud-btn" id="bgm-mute-btn">静音</button>
          </div>
        </div>
        <div class="jukebox-track-list" id="jukebox-track-list"></div>
      </div>
    `;

    document.body.appendChild(this.container);

    this.container.querySelector('#jukebox-close-btn')?.addEventListener('click', () => this.close());
    const slider = this.container.querySelector('#bgm-volume-slider') as HTMLInputElement;
    slider.addEventListener('input', () => {
      this.sound.setBgmVolume(Number(slider.value) / 100);
    });

    const muteBtn = this.container.querySelector('#bgm-mute-btn') as HTMLButtonElement;
    muteBtn.addEventListener('click', () => {
      const isMuted = this.sound.toggleMute();
      muteBtn.textContent = isMuted ? '恢复' : '静音';
    });
  }

  public show() {
    this.container.classList.remove('hidden');
    this.renderTrackList();
    this.updatePlayingTrack();
  }

  public close() {
    this.container.classList.add('hidden');
  }

  private updatePlayingTrack() {
    const name = this.sound.getCurrentBgmName() || '未选择';
    this.container.querySelector('#now-playing')!.textContent = `正在播放: ${name}`;
  }

  private renderTrackList() {
    const listEl = this.container.querySelector('#jukebox-track-list')!;
    listEl.innerHTML = '';
    const tracks = this.sound.getBgmList();

    for (let i = 0; i < tracks.length; i++) {
      const t = tracks[i];
      const item = document.createElement('div');
      item.className = 'jukebox-item';
      item.innerHTML = `
        <span class="track-idx">${i + 1}.</span>
        <span class="track-name">${t}</span>
        <button class="track-play-btn">▶ 播放</button>
      `;

      item.addEventListener('click', () => {
        this.sound.playBgm(t);
        this.updatePlayingTrack();
      });

      listEl.appendChild(item);
    }
  }
}
