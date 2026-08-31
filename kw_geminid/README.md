# 《三国志曹操传》（kw）Unity 战棋 App 复刻工程

本项目是专为**打包为手机 App（Android APK / iOS）及跨平台运行**而构建的 Unity 战棋游戏工程。使用 Unity Hub 打开即可运行与打包。

---

## 🎮 特性支持

1. **移动端深度触屏适配**：
   - 单指拖拽：平滑平移战场大地图；
   - 双指捏合：无级缩放战场视野；
   - 点按选择：精准选择武将、查看信息面板与下达行动指令；
   - 自动横屏锁定与各尺寸屏幕分辨率自适应。
2. **原版数据无损驱动（Ls11/Ls12 封包解密）**：
   - 87 张原版真实战场地图与 96×24 轴对齐地形（`hexzmap.e5` + `Hm??.e5` + `Spalet.e5` 原色渲染）；
   - 505 张原版武将立绘头像（`Face.e5`）；
   - 520 套原版单位动作（`Pmapobj.e5` 10,400 帧行走/攻击/受击/施法动画）；
   - 1024 位真实武将表、104 种装备宝物与 74 种五行策略（`Data.e5` + `Item.e5`）；
   - 原版战役名、武将列传与致命一击台词（`Imsg.e5`）；
   - 原版背景音乐（`SoundTrk` MP3）与战场音效（`Se*.wav`）。
3. **战棋核心系统**：
   - 移动力消耗、地形攻防加成、兵种相克、物理与策略结算、反击与暴击、智能敌方 AI；
   - 连续多章节战役推进与本地进度存档（`PlayerPrefs`）。

---

## 📱 如何使用 Unity Hub 打开与运行

1. 打开 **Unity Hub**，点击 **Add**（添加项目），选择本目录：`kw_geminid`。
   - 推荐版本：**Unity 2021.3 LTS 或 2022.3 LTS 及以上**。
2. 打开项目后，在顶部菜单栏依次点击：
   - **「战棋复刻 → 1. 生成并保存战斗场景」**（自动生成 `Assets/Scenes/Battle.unity` 并配置到 Build Settings）；
   - **「战棋复刻 → 2. 应用移动端 Player 设置」**（自动配置横屏、包名 `com.kw.caocaozhuan`、产品名称等）；
   - **「战棋复刻 → 3. 导入原版数据」**（自动将上级 `kw/` 中的地图、头像、音乐、数据封包复制到 `StreamingAssets/kw/`）。
3. 点击 Unity 编辑器顶部的 **Play ▶** 按钮，即可立即体验游戏！

---

## 📦 如何打包为手机 App

### 1. 打包 Android (APK)
1. 在 Unity 菜单栏点击 `File → Build Settings...`；
2. 在 Platform 列表中选择 **Android**，点击 **Switch Platform**；
3. 点击 **Build**，选择输出路径即可生成 `.apk` 安装包；
4. 将 APK 传输至安卓手机即可安装畅玩！

### 2. 打包 iOS
1. 在 Unity 菜单栏点击 `File → Build Settings...`；
2. 选择 **iOS**，点击 **Switch Platform**；
3. 点击 **Build** 输出 Xcode 工程；
4. 在 Mac 上使用 Xcode 打开工程，配置开发者证书后即可真机调试或发布。

---

## 📂 源码目录结构

```
kw_geminid/
├── Packages/
│   └── manifest.json                   # Unity 核心依赖包配置
├── Assets/
│   ├── Editor/
│   │   └── ProjectSetupMenu.cs         # Unity 顶部菜单（一键生成场景/移动端设置/导入数据）
│   └── Scripts/
│       ├── Core/                       # 96x24 方格/六角网格坐标与地形表
│       ├── Units/                      # 武将数据、兵种相克、20帧动作状态机
│       ├── Battle/                     # 战斗主流程状态机与敌方 AI
│       ├── Import/                     # Ls11/Ls12 封包解密与原版全资源加载
│       ├── UI/                         # UGUI 移动端横屏 HUD、指令菜单、对话框
│       ├── Audio/                      # 原版 BGM 与音效播放器
│       ├── CameraControls/             # 手机触屏手势（单指拖拽、双指缩放、点按）
│       ├── World/                      # 战场地图与飘字组件
│       └── Bootstrap/                  # 战役流程与自举启动
└── README.md
```
