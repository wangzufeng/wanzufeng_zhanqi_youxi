import './style.css';
import { BattleManager } from './game/battle/BattleManager';

window.addEventListener('DOMContentLoaded', () => {
  const canvas = document.getElementById('game-canvas') as HTMLCanvasElement;
  if (!canvas) {
    console.error('Canvas element #game-canvas not found!');
    return;
  }

  function resizeCanvas() {
    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;
  }

  resizeCanvas();
  window.addEventListener('resize', resizeCanvas);

  const battleManager = new BattleManager(canvas);
  battleManager.start();

  let lastTime = performance.now();
  function gameLoop(currentTime: number) {
    const dt = Math.min(0.1, (currentTime - lastTime) / 1000);
    lastTime = currentTime;

    battleManager.update(dt);
    requestAnimationFrame(gameLoop);
  }

  requestAnimationFrame(gameLoop);
});
