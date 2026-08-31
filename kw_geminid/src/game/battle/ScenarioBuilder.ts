import { BattleGrid } from '../model/BattleGrid';
import { Unit } from '../model/Unit';
import { UnitClassType, ClassHelper } from '../model/ClassType';

export interface DialogLine {
  speaker: string;
  faceId: number;
  text: string;
}

export interface ScenarioData {
  stageId: number;
  stageName: string;
  grid: BattleGrid;
  playerUnits: Unit[];
  enemyUnits: Unit[];
  introDialogs: DialogLine[];
  outroDialogs: DialogLine[];
  victoryCondition: string;
  defeatCondition: string;
  bgmTrack: string;
}

export class ScenarioBuilder {
  private static mapsManifest: any[] = [];
  private static officersManifest: any[] = [];
  private static battlesManifest: string[] = [];

  public static async init() {
    if (this.mapsManifest.length > 0) return;
    try {
      const mapsRes = await fetch('/assets/data/maps.json');
      if (mapsRes.ok) this.mapsManifest = await mapsRes.json();

      const officersRes = await fetch('/assets/data/officers.json');
      if (officersRes.ok) this.officersManifest = await officersRes.json();

      const battlesRes = await fetch('/assets/data/battles.json');
      if (battlesRes.ok) this.battlesManifest = await battlesRes.json();
    } catch (e) {
      console.warn('ScenarioBuilder init fetch error:', e);
    }
  }

  public static getStagesCount(): number {
    return this.mapsManifest.length || 87;
  }

  public static getStageInfo(stageIndex: number): { id: number; title: string; w: number; h: number } {
    const m = this.mapsManifest[stageIndex];
    if (m) {
      return { id: stageIndex, title: m.title || `第 ${stageIndex + 1} 战`, w: m.width, h: m.height };
    }
    return { id: stageIndex, title: `第 ${stageIndex + 1} 战`, w: 20, h: 20 };
  }

  public static async build(stageIndex: number, customPlayerOfficers?: number[]): Promise<ScenarioData> {
    await this.init();
    const mapDef = this.mapsManifest[stageIndex] || {
      id: stageIndex,
      title: `第 ${stageIndex + 1} 战`,
      width: 20,
      height: 20,
      grid: new Array(400).fill(0)
    };

    const grid = new BattleGrid(mapDef.width, mapDef.height, mapDef.grid);
    const stageName = mapDef.title || `第 ${stageIndex + 1} 战`;

    // 确定出生点位
    const W = mapDef.width;
    const H = mapDef.height;

    // 我方阵容构建
    const defaultRoster = [0, 1, 2, 3, 4, 5, 6, 7];
    const chosenIds = customPlayerOfficers && customPlayerOfficers.length > 0 ? customPlayerOfficers : defaultRoster;

    const playerUnits: Unit[] = [];
    const playerStartPositions = [
      { x: Math.floor(W * 0.4), y: H - 3 },
      { x: Math.floor(W * 0.45), y: H - 2 },
      { x: Math.floor(W * 0.5), y: H - 3 },
      { x: Math.floor(W * 0.55), y: H - 2 },
      { x: Math.floor(W * 0.35), y: H - 2 },
      { x: Math.floor(W * 0.6), y: H - 3 },
      { x: Math.floor(W * 0.3), y: H - 3 },
      { x: Math.floor(W * 0.65), y: H - 2 }
    ];

    for (let i = 0; i < chosenIds.length && i < playerStartPositions.length; i++) {
      const offId = chosenIds[i];
      const offData = this.officersManifest[offId] || {
        name: i === 0 ? '曹操' : `将领${i}`,
        spriteId: i,
        faceId: i,
        classId: i === 0 ? 0 : i % 4,
        stats: { force: 85, intel: 85, command: 90, agility: 75, luck: 80 },
        hpBase: 200,
        level: 1
      };

      const pos = playerStartPositions[i];
      // 确保位置在合法格子内
      const safeX = Math.max(1, Math.min(W - 2, pos.x));
      const safeY = Math.max(1, Math.min(H - 2, pos.y));

      const u = new Unit({
        id: `player_${i}`,
        officerId: offId,
        name: offData.name,
        unitClass: ClassHelper.fromId(offData.classId ?? i),
        isEnemy: false,
        isLeader: i === 0,
        level: 1 + Math.floor(stageIndex * 1.5),
        faceId: offData.faceId ?? i,
        spriteId: offData.spriteId ?? i,
        stats: offData.stats,
        hpBase: offData.hpBase,
        x: safeX,
        y: safeY,
        weaponId: i === 0 ? 2 : (i % 2 === 0 ? 3 : 1), // 曹操倚天剑
        armorId: i === 0 ? 10 : 9,
        accessoryId: i === 0 ? 13 : -1,
        history: offData.history,
        criticalQuote: offData.critical || `${offData.name}：看我这招！`,
        retreatQuote: offData.retire || `${offData.name}：撤退！`
      });
      playerUnits.push(u);
    }

    // 敌方阵容构建
    const enemyUnits: Unit[] = [];
    const enemyBossLevel = 2 + Math.floor(stageIndex * 1.8);
    const enemyBossData = this.officersManifest[20 + stageIndex] || {
      name: '敌方主将',
      spriteId: 100,
      faceId: 20,
      classId: 1,
      stats: { force: 88, intel: 75, command: 85, agility: 75, luck: 70 },
      hpBase: 220
    };

    const enemyPositions = [
      { x: Math.floor(W * 0.5), y: Math.floor(H * 0.25), isLeader: true, name: enemyBossData.name, classType: UnitClassType.Cavalry },
      { x: Math.floor(W * 0.4), y: Math.floor(H * 0.3), isLeader: false, name: '敌军步兵', classType: UnitClassType.Infantry },
      { x: Math.floor(W * 0.6), y: Math.floor(H * 0.3), isLeader: false, name: '敌军步兵', classType: UnitClassType.Infantry },
      { x: Math.floor(W * 0.35), y: Math.floor(H * 0.2), isLeader: false, name: '敌军弓兵', classType: UnitClassType.Archer },
      { x: Math.floor(W * 0.65), y: Math.floor(H * 0.2), isLeader: false, name: '敌军弓兵', classType: UnitClassType.Archer },
      { x: Math.floor(W * 0.5), y: Math.floor(H * 0.15), isLeader: false, name: '敌军军师', classType: UnitClassType.Strategist },
      { x: Math.floor(W * 0.45), y: Math.floor(H * 0.35), isLeader: false, name: '敌军骑兵', classType: UnitClassType.Cavalry },
      { x: Math.floor(W * 0.55), y: Math.floor(H * 0.35), isLeader: false, name: '敌军骑兵', classType: UnitClassType.Cavalry }
    ];

    for (let i = 0; i < enemyPositions.length; i++) {
      const ep = enemyPositions[i];
      const safeX = Math.max(1, Math.min(W - 2, ep.x));
      const safeY = Math.max(1, Math.min(H - 2, ep.y));
      const isBoss = ep.isLeader;

      const eu = new Unit({
        id: `enemy_${i}`,
        officerId: isBoss ? 20 + stageIndex : 999,
        name: ep.name,
        unitClass: ep.classType,
        isEnemy: true,
        isLeader: isBoss,
        level: isBoss ? enemyBossLevel : Math.max(1, enemyBossLevel - 1),
        faceId: isBoss ? (enemyBossData.faceId ?? 20) : 30 + (i % 5),
        spriteId: isBoss ? (enemyBossData.spriteId ?? 102) : 102 + (i % 4),
        stats: isBoss ? enemyBossData.stats : { force: 70, intel: 60, command: 70, agility: 65, luck: 60 },
        hpBase: isBoss ? 220 : 160,
        x: safeX,
        y: safeY,
        weaponId: isBoss ? 3 : 1,
        armorId: isBoss ? 9 : 8,
        criticalQuote: isBoss ? `${ep.name}：谁敢挡我！` : undefined,
        retreatQuote: isBoss ? `${ep.name}：可恨！全军撤退！` : undefined
      });
      enemyUnits.push(eu);
    }

    // 战役剧情对话
    const leaderName = playerUnits[0]?.name || '曹操';
    const introDialogs: DialogLine[] = [
      {
        speaker: '旁白',
        faceId: -1,
        text: `【${stageName}】\n风云变幻，群雄割据，大战一触即发！`
      },
      {
        speaker: leaderName,
        faceId: playerUnits[0]?.faceId ?? 0,
        text: '全军听令！摆开阵型，稳扎稳打，务必生擒敌酋！'
      },
      {
        speaker: enemyBossData.name,
        faceId: enemyBossData.faceId ?? 20,
        text: '哼！曹军自投罗网，今日定叫尔等有来无回！'
      }
    ];

    const outroDialogs: DialogLine[] = [
      {
        speaker: leaderName,
        faceId: playerUnits[0]?.faceId ?? 0,
        text: `【${stageName}】大获全胜！将士们辛苦了，清点战场，继续进军！`
      }
    ];

    return {
      stageId: stageIndex,
      stageName,
      grid,
      playerUnits,
      enemyUnits,
      introDialogs,
      outroDialogs,
      victoryCondition: `击退敌军主将【${enemyBossData.name}】或全歼敌军`,
      defeatCondition: `我军主帅【${leaderName}】阵亡`,
      bgmTrack: '02-AudioTrack 02.mp3'
    };
  }
}
