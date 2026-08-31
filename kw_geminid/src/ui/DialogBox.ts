import { DialogLine } from '../game/battle/ScenarioBuilder';

export class DialogBox {
  private container: HTMLElement;
  private speakerEl: HTMLElement;
  private textEl: HTMLElement;
  private avatarEl: HTMLImageElement;
  private nextBtn: HTMLElement;

  private lines: DialogLine[] = [];
  private currentLineIdx: number = 0;
  private onComplete: (() => void) | null = null;
  private typewriterTimer: number | null = null;
  private isTyping: boolean = false;
  private fullCurrentText: string = '';

  constructor() {
    this.container = document.createElement('div');
    this.container.className = 'dialog-overlay hidden';

    this.container.innerHTML = `
      <div class="dialog-card">
        <div class="dialog-avatar-wrap">
          <img class="dialog-avatar" src="" alt="avatar" />
        </div>
        <div class="dialog-content">
          <div class="dialog-speaker"></div>
          <div class="dialog-text"></div>
        </div>
        <button class="dialog-next-btn">▼ 点击继续</button>
      </div>
    `;

    document.body.appendChild(this.container);

    this.speakerEl = this.container.querySelector('.dialog-speaker')!;
    this.textEl = this.container.querySelector('.dialog-text')!;
    this.avatarEl = this.container.querySelector('.dialog-avatar')!;
    this.nextBtn = this.container.querySelector('.dialog-next-btn')!;

    this.container.addEventListener('click', () => this.handleAdvance());
  }

  public show(lines: DialogLine[], onComplete: () => void) {
    if (!lines || lines.length === 0) {
      onComplete();
      return;
    }
    this.lines = lines;
    this.currentLineIdx = 0;
    this.onComplete = onComplete;
    this.container.classList.remove('hidden');
    this.renderCurrentLine();
  }

  private renderCurrentLine() {
    if (this.currentLineIdx >= this.lines.length) {
      this.close();
      return;
    }

    const line = this.lines[this.currentLineIdx];
    this.speakerEl.textContent = line.speaker;

    if (line.faceId >= 0) {
      this.avatarEl.src = `/assets/faces/face_${line.faceId}.png`;
      this.avatarEl.style.display = 'block';
    } else {
      this.avatarEl.style.display = 'none';
    }

    // Typewriter effect
    this.fullCurrentText = line.text;
    this.textEl.textContent = '';
    this.isTyping = true;
    let charIdx = 0;

    if (this.typewriterTimer) clearInterval(this.typewriterTimer);
    this.typewriterTimer = window.setInterval(() => {
      if (charIdx < this.fullCurrentText.length) {
        this.textEl.textContent += this.fullCurrentText[charIdx++];
      } else {
        this.isTyping = false;
        if (this.typewriterTimer) clearInterval(this.typewriterTimer);
      }
    }, 20);
  }

  private handleAdvance() {
    if (this.isTyping) {
      // Fast forward text
      if (this.typewriterTimer) clearInterval(this.typewriterTimer);
      this.textEl.textContent = this.fullCurrentText;
      this.isTyping = false;
    } else {
      this.currentLineIdx++;
      this.renderCurrentLine();
    }
  }

  public close() {
    if (this.typewriterTimer) clearInterval(this.typewriterTimer);
    this.container.classList.add('hidden');
    if (this.onComplete) {
      const cb = this.onComplete;
      this.onComplete = null;
      cb();
    }
  }
}
