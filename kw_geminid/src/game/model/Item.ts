/**
 * 道具与宝物装备系统（含全套原版宝物被动特效）
 */

export enum ItemCategory {
  Weapon = 'Weapon',       // 武器
  Armor = 'Armor',         // 防具
  Accessory = 'Accessory', // 辅助宝物
  Consumable = 'Consumable'// 消耗品
}

export enum TreasurePassive {
  None = 'None',
  CleaveAttack = 'CleaveAttack',           // 方天画戟：引导奋战（击杀目标后追加攻击相邻敌人）
  NoCounter = 'NoCounter',                 // 青龙偃月刀：封杀反击
  PierceAttack = 'PierceAttack',           // 丈八蛇矛：穿透攻击两格
  HeavenShield = 'HeavenShield',           // 倚天剑：提升 15% 物理与策略格挡率
  CritBonus = 'CritBonus',                 // 古锭刀：爆发与暴击率提升 20%
  SpellHalve = 'SpellHalve',               // 白银铠：策略伤害减免 50%
  ImmuneCrit = 'ImmuneCrit',               // 黄金铠：免疫暴击与致命一击
  MpShield = 'MpShield',                   // 龙鳞铠：受到致命伤时消耗 MP 抵消体力扣除
  RushMove = 'RushMove',                   // 赤兔马：移动力 +2，突击移动（无视恶劣地形惩罚）
  MovePlus1 = 'MovePlus1',                 // 绝影 / 爪黄飞电：移动力 +1
  RegenMp = 'RegenMp',                     // 太平要术：每回合开始自动恢复 15 点 MP
  CleanseDebuffs = 'CleanseDebuffs',       // 太平清领道：每回合开始自动解除所有异常状态
  ExpBonus50 = 'ExpBonus50',               // 孟德新书：获得经验值增加 50%
  AllSpells = 'AllSpells',                 // 遁甲天书：能够施展所有基础策略
  AutoBeans = 'AutoBeans'                  // 青囊书 / 豆袋：体力低于 50% 时自动使用恢复用豆
}

export interface ItemDef {
  id: number;
  name: string;
  category: ItemCategory;
  atk: number;
  def: number;
  passive: TreasurePassive;
  hpBonus?: number;
  mpBonus?: number;
  healHp?: number;
  healMp?: number;
  desc: string;
}

export const DEFAULT_ITEMS: ItemDef[] = [
  { id: 0, name: '短剑', category: ItemCategory.Weapon, atk: 8, def: 0, passive: TreasurePassive.None, desc: '基础步兵佩剑。' },
  { id: 1, name: '铁剑', category: ItemCategory.Weapon, atk: 15, def: 0, passive: TreasurePassive.None, desc: '精铁打造的长剑。' },
  { id: 2, name: '倚天剑', category: ItemCategory.Weapon, atk: 28, def: 5, passive: TreasurePassive.HeavenShield, desc: '曹操佩剑【宝物】：提升 15% 物理与策略格挡率。' },
  { id: 3, name: '青龙偃月刀', category: ItemCategory.Weapon, atk: 35, def: 0, passive: TreasurePassive.NoCounter, desc: '关羽重兵【宝物】：重创敌人并封杀其反击能力。' },
  { id: 4, name: '丈八蛇矛', category: ItemCategory.Weapon, atk: 32, def: 0, passive: TreasurePassive.PierceAttack, desc: '张飞佩矛【宝物】：攻击时穿透贯穿两格直线敌人。' },
  { id: 5, name: '方天画戟', category: ItemCategory.Weapon, atk: 38, def: 0, passive: TreasurePassive.CleaveAttack, desc: '吕布之戟【宝物】：引导奋战，击退敌人后立即追击相邻下一个目标！' },
  { id: 6, name: '古锭刀', category: ItemCategory.Weapon, atk: 26, def: 0, passive: TreasurePassive.CritBonus, desc: '孙坚遗刃【宝物】：大幅提升暴击与致命一击概率。' },
  { id: 7, name: '短弓', category: ItemCategory.Weapon, atk: 6, def: 0, passive: TreasurePassive.None, desc: '普通木弓。' },
  { id: 8, name: '李广弓', category: ItemCategory.Weapon, atk: 28, def: 0, passive: TreasurePassive.NoCounter, desc: '飞将军神弓【宝物】：远程穿心，敌方无法反击。' },
  { id: 9, name: '皮甲', category: ItemCategory.Armor, atk: 0, def: 8, passive: TreasurePassive.None, desc: '简易鞣皮胸甲。' },
  { id: 10, name: '铁铠', category: ItemCategory.Armor, atk: 0, def: 18, passive: TreasurePassive.None, desc: '坚固的铁甲。' },
  { id: 11, name: '白银铠', category: ItemCategory.Armor, atk: 0, def: 25, passive: TreasurePassive.SpellHalve, desc: '白银锻造【宝物】：所受策略魔法伤害减免 50%！' },
  { id: 12, name: '黄金铠', category: ItemCategory.Armor, atk: 0, def: 30, passive: TreasurePassive.ImmuneCrit, desc: '金光战甲【宝物】：坚不可摧，免疫所有暴击与致命一击。' },
  { id: 13, name: '龙鳞铠', category: ItemCategory.Armor, atk: 0, def: 32, passive: TreasurePassive.MpShield, desc: '真龙逆鳞【宝物】：体力受创时以 MP 代替扣除。' },
  { id: 14, name: '赤兔马', category: ItemCategory.Accessory, atk: 0, def: 0, passive: TreasurePassive.RushMove, desc: '日行千里【宝物】：移动力 +2，突击移动无视恶劣地形。' },
  { id: 15, name: '绝影', category: ItemCategory.Accessory, atk: 0, def: 0, passive: TreasurePassive.MovePlus1, desc: '大宛名驹【宝物】：移动力 +1。' },
  { id: 16, name: '爪黄飞电', category: ItemCategory.Accessory, atk: 0, def: 0, passive: TreasurePassive.MovePlus1, desc: '曹操爱马【宝物】：移动力 +1。' },
  { id: 17, name: '太平要术', category: ItemCategory.Accessory, atk: 0, def: 0, passive: TreasurePassive.RegenMp, desc: '仙家道经【宝物】：每回合自动恢复 15 点 MP。' },
  { id: 18, name: '太平清领道', category: ItemCategory.Accessory, atk: 0, def: 0, passive: TreasurePassive.CleanseDebuffs, desc: '养生仙书【宝物】：每回合开始自动解除所有不良状态。' },
  { id: 19, name: '孟德新书', category: ItemCategory.Accessory, atk: 0, def: 0, passive: TreasurePassive.ExpBonus50, desc: '曹操所著兵书【宝物】：战斗获得经验值增加 50%！' },
  { id: 20, name: '遁甲天书', category: ItemCategory.Accessory, atk: 0, def: 0, passive: TreasurePassive.AllSpells, desc: '奇门秘卷【宝物】：装备者可施展全系基础策略。' },
  { id: 21, name: '恢复用豆', category: ItemCategory.Consumable, atk: 0, def: 0, passive: TreasurePassive.None, healHp: 50, desc: '食用回复 50 点体力。' },
  { id: 22, name: '恢复用米', category: ItemCategory.Consumable, atk: 0, def: 0, passive: TreasurePassive.None, healHp: 150, desc: '食用回复 150 点体力。' },
  { id: 23, name: '仙桃', category: ItemCategory.Consumable, atk: 0, def: 0, passive: TreasurePassive.None, healHp: 999, desc: '九天仙桃，完全回复所有体力。' }
];

export class ItemHelper {
  public static get(id: number): ItemDef {
    return DEFAULT_ITEMS.find(i => i.id === id) || {
      id,
      name: `宝物${id}`,
      category: ItemCategory.Accessory,
      atk: 0,
      def: 0,
      passive: TreasurePassive.None,
      desc: '三国珍藏秘宝。'
    };
  }
}
