/**
 * Web Audio / HTML5 Audio 声音管理器
 * 支持 134 首原版 BGM 与 113 个原版战斗音效
 */

export class SoundManager {
  private static instance: SoundManager;
  private bgmAudio: HTMLAudioElement | null = null;
  private currentBgmName: string = '';
  private sfxCache: Map<string, HTMLAudioElement> = new Map();
  private bgmVolume: number = 0.5;
  private sfxVolume: number = 0.7;
  private muted: boolean = false;
  private bgmList: string[] = [];
  private sfxList: string[] = [];

  private constructor() {
    this.loadAudioLists();
  }

  public static getInstance(): SoundManager {
    if (!SoundManager.instance) {
      SoundManager.instance = new SoundManager();
    }
    return SoundManager.instance;
  }

  private async loadAudioLists() {
    try {
      const bgmRes = await fetch('/assets/data/bgm_list.json');
      if (bgmRes.ok) this.bgmList = await bgmRes.json();

      const sfxRes = await fetch('/assets/data/sfx_list.json');
      if (sfxRes.ok) this.sfxList = await sfxRes.json();
    } catch (e) {
      console.warn('Audio list fetch error:', e);
    }
  }

  public getBgmList(): string[] {
    return this.bgmList;
  }

  public playBgm(filename: string) {
    if (!filename) return;
    if (this.currentBgmName === filename && this.bgmAudio && !this.bgmAudio.paused) {
      return;
    }

    if (this.bgmAudio) {
      this.bgmAudio.pause();
      this.bgmAudio = null;
    }

    this.currentBgmName = filename;
    const url = `/assets/audio/bgm/${encodeURIComponent(filename)}`;
    const audio = new Audio(url);
    audio.loop = true;
    audio.volume = this.muted ? 0 : this.bgmVolume;
    this.bgmAudio = audio;

    audio.play().catch(err => {
      // Browser autoplay restriction, will play on next user gesture
      console.log('BGM autoplay waiting for user interaction');
    });
  }

  public playChapterBgm(chapterIndex: number) {
    if (this.bgmList.length === 0) return;
    // Map chapters to iconic tracks
    const bgmTrack = this.bgmList[chapterIndex % this.bgmList.length];
    this.playBgm(bgmTrack);
  }

  public playSfx(sfxName: string) {
    if (this.muted) return;
    const nameLower = sfxName.toLowerCase().endsWith('.wav') ? sfxName.toLowerCase() : `${sfxName.toLowerCase()}.wav`;
    let audio = this.sfxCache.get(nameLower);
    if (!audio) {
      audio = new Audio(`/assets/audio/sfx/${nameLower}`);
      this.sfxCache.set(nameLower, audio);
    } else {
      audio.currentTime = 0;
    }
    audio.volume = this.sfxVolume;
    audio.play().catch(() => {});
  }

  // Common tactical battle SFX shortcuts
  public playAttackSfx() { this.playSfx('se02.wav'); }
  public playHitSfx() { this.playSfx('se03.wav'); }
  public playCritSfx() { this.playSfx('se04.wav'); }
  public playMagicSfx() { this.playSfx('se08.wav'); }
  public playHealSfx() { this.playSfx('se09.wav'); }
  public playMoveSfx() { this.playSfx('se00.wav'); }
  public playSelectSfx() { this.playSfx('se01.wav'); }
  public playCancelSfx() { this.playSfx('se07.wav'); }
  public playRetreatSfx() { this.playSfx('se12.wav'); }
  public playLevelUpSfx() { this.playSfx('se10.wav'); }
  public playVictorySfx() { this.playSfx('se14.wav'); }

  public setBgmVolume(val: number) {
    this.bgmVolume = Math.max(0, Math.min(1, val));
    if (this.bgmAudio && !this.muted) {
      this.bgmAudio.volume = this.bgmVolume;
    }
  }

  public setSfxVolume(val: number) {
    this.sfxVolume = Math.max(0, Math.min(1, val));
  }

  public toggleMute(): boolean {
    this.muted = !this.muted;
    if (this.bgmAudio) {
      this.bgmAudio.volume = this.muted ? 0 : this.bgmVolume;
    }
    return this.muted;
  }

  public isMuted(): boolean {
    return this.muted;
  }

  public getBgmVolume(): number { return this.bgmVolume; }
  public getSfxVolume(): number { return this.sfxVolume; }
  public getCurrentBgmName(): string { return this.currentBgmName; }
}
