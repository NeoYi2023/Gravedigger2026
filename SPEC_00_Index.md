# Gravedigger2026 — SPEC 总索引 / SPEC Master Index

**文档版本 / Document Version:** v0.69.0
**最后更新 / Last Updated:** 2026-08-07  
**当前阶段 / Current Phase:** Demo 开发 / Demo development（Dig D-020 + UM D-030～D-032 + Defend D-040～D-044 + PushMap PM-01～PM-10 + ModeSelect 模式2 入口 + 可选 TechTree UI-012 落地）  

**套件维护路径：** `F:\CursorGame_Git\SPECandSKILL\Gravedigger2026\`  
**日常开发权威：** 复制到 Cursor 工作区根后的 `SPEC_*.md`（工作区：`F:\CursorGame_Git\Gravedigger2026`）

---

## 1. 文档说明

### 简体中文

本项目的 `SPEC_*.md` 是 **Gravedigger2026** 的唯一权威设计文档。公共流程模板来自 `SPECandSKILL\SPEC\`；本目录为项目专属内容。双语规范见公共模板约定。

### English

`SPEC_*.md` is the single source of truth for **Gravedigger2026**. Common workflow templates live in `SPECandSKILL\SPEC\`; this folder holds project-specific content.

---

## 2. 文件清单与阅读顺序

### 简体中文

| 序号 | 文件 | 说明 |
|------|------|------|
| 00 | [SPEC_00_Index.md](SPEC_00_Index.md) | 总索引、变更日志（本文件） |
| 01 | [SPEC_01_Workflow.md](SPEC_01_Workflow.md) | 三阶段开发流程与协作约定 |
| 02 | [SPEC_02_GameOverview.md](SPEC_02_GameOverview.md) | 游戏概述、平台与定位 |
| 03 | [SPEC_03_GameRules.md](SPEC_03_GameRules.md) | 游戏规则主体（Demo 外壳 + 关卡阶段 / 挖坟 / 升级与制造 / 防守 / 科技树 / 推图战框架） |
| 04 | [SPEC_04_Technical.md](SPEC_04_Technical.md) | 技术规范、Demo 边界、配置表字段与工程约定（§14） |

**建议阅读顺序：** 01 → 02 → 03 → 04。

### English

| No. | File | Description |
|-----|------|-------------|
| 00 | [SPEC_00_Index.md](SPEC_00_Index.md) | Master index, changelog |
| 01 | [SPEC_01_Workflow.md](SPEC_01_Workflow.md) | Three-phase workflow |
| 02 | [SPEC_02_GameOverview.md](SPEC_02_GameOverview.md) | Game overview |
| 03 | [SPEC_03_GameRules.md](SPEC_03_GameRules.md) | Game rules (Demo shell + Level stages / Dig / UpgradeManufacture / Defend / TechTree / PushMap framework) |
| 04 | [SPEC_04_Technical.md](SPEC_04_Technical.md) | Technical standards, Demo boundary, config tables + engineering rules (§14) |

---

## 3. 双语书写规范

### 简体中文

- 每个一级章节含 `### 简体中文` 与 `### English`；变更须双块同步并记入 Changelog。

### English

- Each top-level section has both language blocks; keep them in sync and log changes.

---

## 4. 变更日志 / Changelog

### 简体中文

| 日期 | 版本 | 摘要（中文） |
|------|------|-------------|
| 2026-08-07 | v0.69.0 | PushMap 怪物占地散开（PM-10 方案 A）：`MonsterConfig.BodyRadius`（缺省 0.35）；刷出环形/螺旋错开 + 避场上存活怪；移动怪 `NavMeshAgent.radius=BodyRadius` 持续 RVO；`Stationary*` 仅刷出占位；同步 SPEC_03 §3.14、SPEC_04 §6/§9.19/§9.23、CONTEXT |
| 2026-08-07 | v0.68.0 | 配置表数值 CSV 禁浮点噪声（§14.6）：打表对 Excel 数值圆整至多 10 位并去尾零（例 `0.009999…`→`0.01`）；整数仍收成整串；文本单元格不圆整；同步 `XlsxSheetReader` / Python 打表镜像并重打核对 CSV |
| 2026-08-06 | v0.67.1 | PushMap Combat 滚轮缩放 `orthographicSize`：默认仍 2；钳制 `[0.5, 20]`；前滚拉近、后滚拉远；步进 0.5/档；不切换跟随模式、恢复跟随不重置 Size；同步 SPEC_03 §3.14、SPEC_04 §6 |
| 2026-08-06 | v0.67.0 | PushMap Combat 镜头双模式（PM-09 方案 A）：默认粘随距 CurrentObjective 最近忠诚兵（失效后重选；全灭定格）；左键拖拽入手动；底中「恢复跟随」仅手动态显示；`PushMapCameraFollowController`；同步 SPEC_03 §3.14、SPEC_04 §6、CONTEXT |
| 2026-08-06 | v0.66.1 | PushMap `PushMapCamera.orthographicSize` 固定为 **2**（不再用 `max(half)-1.5`）；俯视角度/高度/near-far 仍对齐 Defend；同步 SPEC_04 §6 |
| 2026-08-06 | v0.66.0 | PushMap 士兵推进卡死修复（方案 A）：Capture 探测**不**再暂停推进（圈内有怪只清占领计时）；开战顺序 Bake NavMesh→部署→`FireStartBattleSpawns`；样例图 `DigMapBounds`/`WalkSurface`/`EngageZone` 扩大覆盖目标与刷怪点；同步 SPEC_03 §3.14、SPEC_04 §6/§9.22–§9.23 |
| 2026-08-06 | v0.65.0 | PushMap Combat 战斗相机对齐 Defend：Runtime Ensure `PushMapCamera`（正交俯视 `Euler(90,0,0)`、高度+18、`orthoSize=max(half)-1.5`、near/far）；Prepare 关相机用 FormationCamera，开战启用；修复 Runtime StageRoot 未挂相机导致落到 Boot 透视主相机；同步 SPEC_03 §3.14、SPEC_04 §6/§15.2 |
| 2026-08-06 | v0.64.0 | UM PoolPanel 士兵框 + 再造：可滚动士兵框列表；选中显「再造1个」；`SourceItemIds`/`SourceSpiritCost` + `TryRemanufacture` 后台新增士兵；材料/精魂不足 Tips（中上部 1s）；同步 SPEC_03 §3.11、SPEC_04 §6、CONTEXT |
| 2026-08-06 | v0.63.0 | 配置表 Excel 三行表头（方案 B）：第1行中文名、第2行中文说明、第3行英文列名、第4行起数据；`ConfigTableBaker` 打表剥离说明行，CSV 仍英文单行表头；兼容旧单行表头；同步 SPEC_04 §14.4 |
| 2026-08-06 | v0.62.0 | Defend ModeSelect 模式2正式入口：列出全部 `PushMapGameplayConfig`；确认后 `LevelOperationDriver.TryHandoffModeSelectToPushMap` 卸 Defend → 改写上下文为 PushMap → `PushMapStageModule` Prepare；`GameplayType=PushMap` 直进路径保留；同步 SPEC_03 UI-013/D-044/§3.12/§3.14、SPEC_04 §6、CONTEXT |
| 2026-08-06 | v0.61.0 | PushMap PM-08（方案 A）：空气墙 NavMesh——开战 Runtime Bake 将地图 `AirWall` 注入 `NavMeshBuildSource` Box + Not Walkable（含 Y 轴 45°）；敌我 Agent 均不可穿；复杂多层障碍 polish 不做；SPEC_03 §3.14、SPEC_04 §6/§9.7/§9.22/§13 |
| 2026-08-06 | v0.60.0 | PushMap PM-07（方案 A）：BOSS 通关与奖励钩子——`TryNotifyBossKilled`→`VictorySettled(StageExpReward)` 对齐 Defend 经验/`TryAdvanceStage`；护盾归零不入账；占领 `CaptureLoot`+`DungeonUnlockIds` 存档钩子；Demo 击杀＝忠诚兵进 BOSS `AttackRange`；SPEC_03 §3.14、SPEC_04 §6/§9.22–§9.23 |
| 2026-08-06 | v0.59.0 | PushMap PM-06（方案 A）：AggroMode 四态 Demo 契约——`PushMapMonsterAgentView` 按 `AggroMode` 分支；主动态 `AlertRadius` 仅对忠诚士兵主动发现；被动态 Demo 挑衅契约＝忠诚兵首次进 `AttackRange`→`NotifyProvoked()`；原地态永不移动；命中仍 `AttackMode` 方案 D；士兵击杀怪物/技能/副本不做；SPEC_04 §6/§9.23、SPEC_03 §3.14 增实现边界 |
| 2026-08-06 | v0.58.0 | PushMap PM-05（方案 A）：刷怪与陷阱运行时契约——`PushMapSessionService` 装载 `PushMapSpawnConfig`、无陷阱开战刷 / 陷阱首次触发 / 占领停刷（已刷保留）；事件 `PushMapSpawnRequested`（位置由 View 解析）；表现 `PushMapStageController` + `PushMapMonsterAgentView`（Defend 默认追击语义）；SPEC_04 §6/§9.23、SPEC_03 §3.14 增实现边界；样例 `TrapZoneId` 对齐 `TZ_01`；AggroMode/BOSS 结算后置 PM-06/07 |
| 2026-08-06 | v0.57.0 | PushMap PM-04（方案 A）：目标点链与判定圈占领契约——规则归 `PushMapSessionService`（`CurrentObjective`/`TickCapture`/`ObjectiveCaptured`+`CurrentObjectiveChanged`）；SPEC_03 §3.14 增规则归属与 Demo 无刷怪边界；SPEC_04 §9.22 增占领运行时契约（探测 `PushMapMonsterPresenceProbe`、推进 `PushMapAdvanceView`） |
| 2026-08-06 | v0.56.0 | PushMap PM-03（方案 A）：SPEC_04 §6 落地 Stage 接线——`LevelOperationDriver` 支持 `PushMap`、`MapPrefabPaths` 允许 `PushMap_*`、`PushMapStageModule`+`PushMapSessionService`+`PushMapStageController`（Prepare/开战≥1/Shield/LOC；复用 FormationEditor；无占领/刷怪/BOSS）；样例 `Level_01,4,PushMap,PushMap_01`；Defend 模式2 仍占位 |
| 2026-08-06 | v0.55.1 | 修复 Bake：XlsxSheetReader 兼容 Zip 条目路径 / 与 \（TechEffectConfig 等 Windows Excel 反斜杠导致 GetEntry 失败）；重写该表 Excel；同步 SPEC_04 §14.4 |
| 2026-08-06 | v0.55.0 | PushMap PM-02（方案 A）：`PushMapGameplayConfig` / `PushMapSpawnConfig` Excel+CSV；`MonsterConfig` 加载 `AggroMode`/`AlertRadius` 缺省；`ConfigCsvRepository` 只读加载；同步 SPEC_04 §6/§9.19/§9.22 |
| 2026-08-06 | v0.54.0 | PushMap PM-01（方案 A）：SPEC_04 §9.22/§13 落地地图 Prefab 标记字段契约与样例 `PushMap_Demo_01`（独立 `PushMap_*`，不改 `Ground_*`）；脚本 `Gameplay/PushMap/`；Editor Ensure；无占领/刷怪运行时 |
| 2026-08-06 | v0.53.0 | Dig 命中形方案 B：`DigHitShape` 离线烘焙本地 XZ 凸包；光标圆 ∩ 凸包触发 DigAction（与障碍圆分离）；同步 SPEC_03 §3.10、SPEC_04 §6/§9.2、CONTEXT |
| 2026-08-06 | v0.52.0 | 推图战 PushMap 规则录入：新增 SPEC_03 §3.14（GameplayType；复用 Defend 布阵/护盾/失控/士兵战斗；目标点链+判定圈占领；空气墙/刷怪点/陷阱/BOSS；AggroMode）；SPEC_04 §9.22–§9.23 + MonsterConfig.AggroMode/AlertRadius；边界锁定见待澄清；同步 CONTEXT/SPEC_02/spec-map；issues `.scratch/push-map/` |
| 2026-08-06 | v0.51.0 | 战斗阶段入口增加模式/选关闸门（D-044）：`DefendPhase.ModeSelect`；模式1「保卫战」=现 §3.12；模式2「推图战」占位；关卡列表=该模式全部玩法配置；运作表 Defend 行 `GameplayConfigId`=Recommended；任一模式通关→`TryAdvanceStage`；同步 SPEC_03/04、CONTEXT |
| 2026-08-02 | v0.50.0 | UM 主屏 UI 重做：默认全屏制造 + 升级 Modal（GM升级/X）；底栏库存方格拖拽；中心环绕槽（含坐骑/翅膀方位）；非宝石全满才显示外观预览（Attack→Idle）；同步 SPEC_03 UI-010/§3.11、SPEC_04 §6 |
| 2026-08-02 | v0.49.9 | 地图 Tile Palette 迁入 `Art/Maps/Palettes/SurvivorTiles`；确认 `Ground_01`…`05` Tile/Sprite GUID 使用 `Art/Maps/Tiles`（不再依赖被 ignore 的 SmallScaleInt）；`MapTilemapAssetBuilder` 加固 EnsurePalette |
| 2026-07-31 | v0.49.8 | Dig 光标半径内可挖坟**并行** DigAction（取消单坟全局忙锁；按坟独立时长与扣血）；同步 SPEC_03 §3.10 |
| 2026-07-31 | v0.49.7 | Demo 初始 `DigCursorRadius` **1.5→0.6**（`Tech_Root` / `DigProtagonistCapabilities` 默认）；同步 SPEC_03 §3.10 / TechEffectConfig |
| 2026-07-31 | v0.49.6 | Demo 初始 `DigCursorRadius` **2.5→1.5**（`Tech_Root` / `DigProtagonistCapabilities` 默认）；同步 SPEC_03 §3.10 / TechEffectConfig |
| 2026-07-31 | v0.49.5 | 修复 `UiDigCursorRing` 直径被 CanvasScaler 二次放大：屏幕投影像素 ÷ `canvas.scaleFactor` 写入 `sizeDelta`；描边按屏幕像素恒定换算；同步 SPEC_03 §3.10 / SPEC_04 §9 |
| 2026-07-29 | v0.49.4 | 修复 ToolsButton 首次点击无效：禁止初始隐藏面板在 `Awake` 对自身 `_root` 再 `SetActive(false)`；同步 SPEC_04 §3、ToolsPanel/ConfirmDialog/DigStageSummary |
| 2026-07-29 | v0.49.3 | Defend 士兵无 EngageZone 目标时自动返回开战 `FormationHome`（途中继续选敌；Rebel 不返回）；同步 SPEC_03 §3.12、SPEC_04 §6、CONTEXT |
| 2026-07-29 | v0.49.2 | 修复 UM 库存列行数增多时标签空白：`InventoryColumn`/`SlotColumn` 纵向 ScrollRect + 行 minHeight；同步 SPEC_04 §6 D-031 |
| 2026-07-29 | v0.49.1 | UM Debug「注入制造套件」改为发放 `SoulConfig` 全行各 ×1（Demo `Soul_01`…`Soul_10`）；同步 SPEC_04 §6 D-031 |
| 2026-07-29 | v0.49.0 | Defend 士兵动画驱动：`WarriorAnimView` 播 `IsRun`/`Attack1`/`Die` + 动态 `DirIndex`；`WarriorAgentView` 接线；同步 SPEC_04 §6/§15.5、CONTEXT |
| 2026-07-28 | v0.48.1 | FormationEditor：士兵栏保留已上阵方格+变亮；拖出栏后 Idle 世界跟手；下阵关亮；同步 SPEC_03 §3.11 |
| 2026-07-28 | v0.48.0 | 布阵拖拽编辑器：UM 主屏二区+「布阵/返回」；共享 `FormationEditorRoot`（士兵栏 80×80、拖放上阵/改位/下阵、控制力 HUD）；Defend Prepare 复用；`TryDeployAt`；同步 SPEC_03 §3.6/§3.8/§3.11/§3.12、SPEC_04 §6、CONTEXT |
| 2026-07-28 | v0.47.3 | 怪物 ModelId 有 Art 时组装 `Visual`（Sprite/Animator）并移除占位立方体；`MonsterModelPrefabAssembler` + DefendAssetBuilder；本片 `MonsterModel_01`…`04`；同步 SPEC_04 §15.2 / D-041 |
| 2026-07-28 | v0.47.2 | 修复默认解锁科技未提供 `DigCursorRadius`，导致 Dig 圆圈仅显示最小尺寸且无法命中坟墓；锁定 Demo 初始 `DigDamage=25`、`DigCursorRadius=2.5`；同步 SPEC_03 §3.10 / TechEffectConfig |
| 2026-07-28 | v0.47.1 | Defend HUD：`DefendRoot` 上 Image 关闭且不使用（无全屏遮罩）；同步 Prefab / DefendAssetBuilder / SPEC_04 §6 D-040 |
| 2026-07-28 | v0.47.0 | Editor 打表工具方案 A：`Gravedigger2026/Config/Bake Tables`；纯 C# Open XML 解析 Excel→Csv；四段名映射 + 表头校验；全量 schema 校验后置；同步 SPEC_04 §14/§6、SPEC_03 §3.8、CONTEXT |
| 2026-07-28 | v0.46.5 | 角色 spritesheet 切片误用 NPOT 2048 宽（格宽≈136.53，应为 128）致换帧左漂：导出/Repair 强制源尺寸重切；批量修复 `Art/Characters` meta；同步 SPEC_04 §15.3 |
| 2026-07-28 | v0.46.4 | Digger / BattleProtagonist 换 2D 烘焙整角：游戏 Prefab 根 + `Visual`（Sprite/Animator，`localEuler(90,0,0)`）；Dig 语义→`Special1`；固定 `DirIndex=2`（南）；`DigDiggerView` 驱动循环；Assembler + Builder 禁 Capsule 回退；同步 SPEC_04 §15.2/§15.5 |
| 2026-07-28 | v0.46.3 | Dig 圆圈光标 UI Prefab：`UiDigCursorRing`（双层圆：白半透明填充 + 像素恒定描边）；`DigPrefabCatalog` 绑定；规则仍按圆半径判定；同步 SPEC_03 §3.10 / SPEC_04 §6/§9 |
| 2026-07-27 | v0.46.2 | IsoDiamond 半尺寸改为 `PaintRadius*(cellSize.x,cellSize.y)`（可各向异性；Demo `cellSize≈(1,0.5)`→`(5,2.5)`）；`WalkSurface`/NavMesh 改同形菱形薄网格，修正相对 Tilemap 上下差一半 |
| 2026-07-27 | v0.46.1 | 地图逻辑足迹统一为 IsoDiamond（XZ 曼哈顿菱形，与 Isometric Tilemap 外轮廓对齐）：`DigMapBounds`/`EngageZone`/`WalkSurface`（Y=45° 扁盒）/ NavMesh 旋转盒 / 钟点刷怪边 / Dig 可放置采样；半尺寸=菱形顶点到中心距离；同步 SPEC_03/04、CONTEXT |
| 2026-07-27 | v0.46.0 | 关卡地图表现改为 Unity Isometric Tilemap（正交顶视 XZ；逻辑仍连续非格子）；Tile/Sprite 落 `Art/Maps/Tiles/`（自 Example Environment 复制）；`Ground_*` Prefab 含 Grid+Tilemap+WalkSurface；须 `com.unity.2d.tilemap`；同步 SPEC_03 DigMap、SPEC_04 §2/§9/§13 |
| 2026-07-26 | v0.45.0 | 士兵外观 Prefab：根 + 子 `Visual`（Sprite/Animator，`localEuler(90,0,0)`）对齐俯视相机；`WarriorAgentView` 设 `NavMeshAgent.updateRotation=false`；同步 SPEC_04 §15.2 |
| 2026-07-26 | v0.44.0 | Character Creator 导出补丁：切片强制 `textureType=Sprite` + 资产路径正斜杠；零 Clip 中止空 Controller；Editor 修复菜单重建 `.anim`/`.controller`；同步 SPEC_04 §15.3 |
| 2026-07-26 | v0.43.0 | 落地 `Assets/Art/` 各系统美术源目录脚手架（Characters/Dig/Maps/Defend/UI/Placeholder/VFX/Audio）；预建 AppearanceId/ModelId/Ground/Grave 槽位；本版不另建顶层 `Sprites/`；同步 SPEC_04 §2 |
| 2026-07-26 | v0.42.0 | 科技树画布可选片方案 A（UI-012 / 06）：`ConfigCsvRepository` 加载 TechTree/TechEffect；纯 C# `TechTreeService`（InitiallyUnlocked 自动学会、前置+LearnCost、扣 TechPoint、属性加法重算 `DigProtagonistCapabilities`）；设置入口打开 Prefab 摆位 uGUI 画布（平移/悬停/连线/学习）；`DigStageModule` 改读重算 caps；临时 `Prefabs/Meta/TechTreeCanvasRoot.prefab`；同步 SPEC_03 §3.8/UI-012、SPEC_04 §6 |
| 2026-07-26 | v0.41.0 | Defend 失控开战 roll 与胜负结算方案 A（D-043 / 05d）：开战锁定 Degree/Tier + `FinalLossChance` 独立 roll→Rebel；Rebel 就近打主角/兵/怪（对主角扣盾）；清场→Ended 入账 Demo 阶段经验 100→`TryAdvanceStage`；`Shield≤0`→LevelFailure 不入账并中止关卡；加载 `Combat_LossOfControlConfig.csv`；PermanentDeath 最小（宝石回仓+清布阵+移池）；同步 SPEC_03 §3.8/§3.12、SPEC_04 §6 |
| 2026-07-26 | v0.40.0 | Defend 士兵远程弹道片方案 A（D-042 远程 / 05c2）：Session 登记 `RangedProjectileSpeed`/`RangedTimeoutSeconds` + `TryConfirmRangedHit`；`WarriorAgentView` 与近战共用 EngageZone/ASPD；`ProjectileView` 运动学飞向锁定怪（距离≤hitRadius 命中，超时未命中不扣血）；临时 `Prefabs/Defend/Projectile.prefab`；同步 SPEC_03 §3.8 D-042、SPEC_04 §6 |
| 2026-07-26 | v0.39.0 | Defend 士兵近战片方案 A（D-042 近战）：`WarriorCombatMath` + Session 登记兵/怪 HP；EngageZone 最近选敌；近战前摇 HitConfirm；怪物 `AttackPower` 扣兵 HP；CombatDead 停手；清场条件可检测（不入账）；远程弹道拆 05c2；同步 SPEC_03 §3.8 D-042、SPEC_04 §6 |
| 2026-07-26 | v0.38.0 | Defend 刷怪与寻路方案 A：`ConfigCsvRepository` 加载 WaveSpawn/Monster；`DefendSessionService` 按剩余秒激活刷怪行并发事件；临时固定 `SpawnPoint` + Runtime NavMesh 烘焙；`MonsterAgentView`（NavMeshAgent）接近并以普攻扣 `Shield`；`Shield≤0`→Ended/LevelFailure 钩子；临时 `Prefabs/Defend/Monsters/{ModelId}`；同步 SPEC_03 §3.8 D-041、SPEC_04 §6 |
| 2026-07-26 | v0.37.0 | Defend Prepare/开战/护盾方案 A：`DefendStageModule` + `DefendSessionService` + `DefendPrefabCatalog`；Enter 按 `BattleMapId` Instantiate `Prefabs/Maps/`；Prepare 复用 `BattleFormationService`/`FormationPanelView`；开战 ≥1 上阵 → 部署临时 `BattleProtagonist` + 士兵；`Shield`=`ProtagonistMaxHP`；倒计时可见（本片不刷怪）；同步 SPEC_03 §3.8 D-040、SPEC_04 §6 |
| 2026-07-25 | v0.36.0 | UM 布阵区方案 A：`BattleFormationService`（连续坐标上阵/下阵/改位 + 控制力占用）+ `FormationPanelView`；与士兵池打通；存档级持有供 Defend Prepare 共用；同步 SPEC_03 §3.8 D-032、SPEC_04 §6 |
| 2026-07-25 | v0.35.0 | UM 制造区方案 A：`ManufactureService`（15 严格槽位 / 预览 / 精魂闸门）+ `WarriorPoolService`；追加加载 Soul/Class/Gem/Race/BodyPart/Appearance/Equip/GemSuffix 八表；`WarehouseService` 支持 BodyPart 入账与按 Id 扣减；临时 `Prefabs/Defend/Warriors/{AppearanceId}`；同步 SPEC_03 §3.8 D-031、SPEC_04 §6 |
| 2026-07-25 | v0.34.0 | UM 升级区方案 A：`UpgradeManufactureStageModule` + `ProtagonistProgressService` + 读 `ProtagonistLevelConfig`；Debug 注入经验连升；同步 SPEC_03 §3.8 D-030、SPEC_04 §6 |
| 2026-07-25 | v0.33.0 | Dig 垂直切片方案 A：`DigStageModule` + `DigSessionService` + `DigPrefabCatalog`；按 `DigMapId` Instantiate `Prefabs/Maps/`；挖掘/入账/DigStageSummary→交还驱动；同步 SPEC_03 §3.8 D-020、SPEC_04 §6 |
| 2026-07-25 | v0.32.0 | 关卡驱动方案 A：运行时只读 Csv（Editor=`Assets/ConfigTables/Csv`；Player=`StreamingAssets/ConfigTables/Csv`）；`LevelOperationDriver` + `IStageModule` 钩子；Tools「关卡」启 `Level_01`；UM ConfigId 忽略；MapId 仅解析/日志；同步 SPEC_03 §3.8 D-003/D-004/D-010、SPEC_04 §6/§14 |
| 2026-07-25 | v0.31.1 | SPEC_04 §1 录入本机 Unity 编辑器路径：`F:\Unity\Unity 2021.3.40f1\Editor\Unity.exe` |
| 2026-07-25 | v0.31.0 | Meta 壳方案 A：PlayerPrefs 三槽占用；单场景 Boot + Prefab UI；工具设置/关卡 Toast；Demo Debug 切态验 D-004；同步 SPEC_04 §6 / SPEC_03 §3.8 D-001～D-004 状态 |
| 2026-07-25 | v0.30.0 | Demo 验收扩大为 Meta 壳 + Dig→UM→Defend 流水线垂直切片（§3.8 D-001～D-043）；UM 阶段 `GameplayConfigId`=忽略；Defend Demo 最小刷怪点/NavMesh；同步 SPEC_03/04 §6 / CONTEXT / issues |
| 2026-07-25 | v0.29.0 | Dig/Defend 地图表现共用 `Ground_01`…`Ground_05`：DigGameplayConfig 增 `DigMapId`；`BattleMapId` 合法值改为 `Ground_*`，Prefab 解析 → `Assets/Prefabs/Maps/`；源参考 Example Scene Grid/Ground；同步 SPEC_03/04 / CONTEXT / spec-map / 配表样例 |
| 2026-07-25 | v0.28.0 | SPEC_04 §15 角色美术管线：Character Creator 烘焙整角；工具目录禁入游戏资源；补丁导出→`Art/Characters`→Prefabs；`AppearanceId`/`ModelId`/主角 Prefab 解析；Mount/Wing 打进外观；同步 SPEC_03 / CONTEXT / spec-map |
| 2026-07-25 | v0.27.0 | 套件/工作区路径改为本机 `F:\CursorGame_Git\SPECandSKILL` 与 `F:\CursorGame_Git\Gravedigger2026`；工作区 SPEC/CONTEXT/Skills 回写套件，关闭此前 E: 路径不可达导致的待同步 |
| 2026-07-25 | v0.26.0 | 配置表结构锁定：士兵 Skills=`SkillId;Level|…`；EquipStats；CombatConvertCoeffs；Class/Monster AttackRange 等命中列；GemType 六类；ComboKey；MoveStyle/AttackPriority；IconStyle 三列；BattleMapId/ModelId Prefab 名；TechUiFrameType；UnlockedFeature 开放名单；SkillConfig 不扩效果列；同步 SPEC_03/04 / CONTEXT / spec-map |
| 2026-07-25 | v0.25.0 | SPEC_04 §14：Excel 磁盘名改为四段 `{系统中文}_{表中文}_{系统英文}_{表英文}.xlsx`；CSV 仍为 `{系统英文}_{表英文}.csv`；打表按英文后缀两段映射；§9 各表磁盘名分列；同步 CONTEXT / spec-map |
| 2026-07-25 | v0.24.0 | §3.11 灵魂职业：新增 ClassId / ClassConfig（ClassName、PrimaryStat、CombatConvertCoeffs 占位）；SoulConfig 仅引用 ClassId，移除 ClassName/PrimaryStat；命名与 ClassAffinity 经 ClassConfig；§3.12 Primary 取自职业表，全局派生常量为过渡；同步 SPEC_04 / CONTEXT / spec-map |
| 2026-07-25 | v0.23.0 | §3.12 Demo 第一版战斗边界：士兵与怪物仅普通攻击、不施放技能；SoulConfig 增 AttackMode；法师普攻=射手远程通道（仅 PrimaryStat=Intelligence）；SkillCooldown/Skills 保留不驱动；同步 SPEC_04 / CONTEXT / spec-map |
| 2026-07-25 | v0.22.0 | §3.11/§3.12 士兵战斗派生公式：PrimaryStat；StaticStat/FinalStat 分层；NormalAttackPower=Primary×1.5；AttackSpeed=0.5+60/max(Agi,1)；SkillCooldown=max(0.1,BaseCD−30/max(Int,1))；MaxHP=ceil(BodyLife+Str×3)；SoulConfig.PrimaryStat + SkillConfig.BaseCooldownSeconds；同步 CONTEXT / SPEC_04 / spec-map |
| 2026-07-25 | v0.21.0 | §3.12 士兵战斗：EngageZone 内最近选敌；AttackRange；命中方案 D（近战前摇确认 / 远程弹道）；CombatDead vs PermanentDeath（Ended/LevelFailure 结算）；宝石特例 HP≤0 立即彻底死亡；§3.11 物资仅彻底死亡；AttackPriority 本批不驱动选目标；同步 SPEC_04 / CONTEXT / spec-map |
| 2026-07-25 | v0.20.0 | §3.11/§3.12 失控规则关闭：失控程度=ΣCost/Cap−1；四档；开战锁定与独立 roll；叛变就近目标（含破盾）；最终率=档位+种族+Σ宝石+Σ技能（clamp）；技能加成非零时释放技能再 roll；SPEC_04 §9.20 LossOfControlConfig + §9.21 SkillConfig 骨架；Race/Gem 失控加成字段；同步 CONTEXT / spec-map |
| 2026-07-25 | v0.19.0 | §3.11 躯体材料表扩写（BodyPartConfig：等级/StatBonus/AutoConvert/美术等；Base(S)=Σ StatBonus）；新增躯体外观表 BodyAppearanceConfig 与选取算法（平均等级→四舍五入、职业倾向、IsFallback 保底、全表随机）；WarriorInstance.AppearanceId；LootDrop Id 可解析 BodyPartId；SPEC_04 §9.13+ 节号顺延；同步 CONTEXT / spec-map |
| 2026-07-25 | v0.18.0 | 名词统一：单位中文称谓「战士」→「士兵」（制造/上阵/属性构成等）；英文标识 `Warrior*` / `PreferWarrior` 不变；职业名 `ClassName` 仍可为「战士」；同步 SPEC_03/04、CONTEXT、spec-map |
| 2026-07-24 | v0.17.0 | §3.12 防守补全：护盾（普通攻击次数；初值=ProtagonistMaxHP；归零 LevelFailure）；战斗倒计时刷怪；WaveSpawnConfig / MonsterConfig（SPEC_04 §9.17–§9.18）；DefendGameplayConfig 增 CombatDurationSeconds；同步 CONTEXT / spec-map。**工作区已更新；套件路径 `E:\Work\Cursor\SPECandSKILL\Gravedigger2026\` 本机不可达，待同步** |
| 2026-07-24 | v0.16.0 | §3.13 科技树框架：中心向外、InitiallyUnlocked 默认学会、前置+LearnCost、设置页 2D 画布（UI-012）；SPEC_04 §9.15 TechTreeConfig / §9.16 TechEffectConfig；经验仍 Defend 胜利入账；同步 CONTEXT / spec-map。**工作区已更新；套件路径 `E:\Work\Cursor\SPECandSKILL\Gravedigger2026\` 本机不可达，待同步** |
| 2026-07-24 | v0.15.0 | SPEC_04：配置表公共工程约定——统一 `ConfigTables/Excel`+`Csv`；命名 `系统_表名`；双格式强制；打表菜单约定（§14）；关闭 §9 载体 TBD；§13 表项与非表 SO 分流；同步 CONTEXT / Skill 速查。**工作区已更新；套件路径 `E:\Work\Cursor\SPECandSKILL\Gravedigger2026\` 本机不可达，待同步** |
| 2026-07-24 | v0.14.0 | §3.11 战士制造：槽位/最低要求/预览与精魂闸门；部位加权定种族；六宝石类型互斥且 GemMult 按维 Σ；命名 Prefix+Race+Class+Suffix；死亡宝石全部回仓；SPEC_04 §9.9–§9.14（ClassName/GemType/BodyPart/ExtraEquipment/GemSuffix）；同步 CONTEXT / spec-map |
| 2026-07-24 | v0.13.0 | §3.11 FinalStat 按单项属性汇总（先定 S 再取来源）；力量例；`max(0,…)` 下限；GemMult 改为五维；SPEC_04 §9.10 / WarriorInstance 同步；CONTEXT |
| 2026-07-24 | v0.12.0 | §3.11 种族：由躯体决定；五维 RaceAdjustCoeff；FinalStat+=Base×RaceAdjustCoeff；不计控制力；SPEC_04 §9.11 RaceConfig；同步 CONTEXT / spec-map |
| 2026-07-24 | v0.11.0 | §3.11 宝石：制造可选镶嵌（≤1）；FinalStat+=Base×GemMult；死亡宝石回仓、其余绑定材料销毁；ControlPowerCost 含 GemCost；SPEC_04 §9.10 GemConfig；同步 CONTEXT / spec-map |
| 2026-07-24 | v0.10.0 | §3.11 战士属性构成关闭：战士信息/基础属性/灵魂/额外装备/控制力占用；FinalStat=Base+Equip+SkillBuffCoeff×Base（Buff 仅运行时）；装备制造锁定；SPEC_04 §9.9 SoulConfig + WarriorInstance 快照；同步 CONTEXT / spec-map |
| 2026-07-23 | v0.9.0 | §3.11 / SPEC_04 §9.8：主角升级配置表 ProtagonistLevelConfig（累计经验阈值、预留解锁、科技点、控制力上限、主角 MaxHP）；LevelFailure 无关卡结算奖励且不入账本阶段经验、已获不扣；同步 CONTEXT / spec-map |
| 2026-07-23 | v0.8.2 | Dig 缺口：加权字段通用规则（Weight=0 剔除；Dig 空有效列表放弃该次生成）；MaterialConfig / CurrencyConfig 增 AppearanceIconId、AssetPath、WarehouseQualityOutlineId；能力表改 §9.6、防守表改 §9.7；同步 CONTEXT |
| 2026-07-23 | v0.8.1 | 挖坟缺口补录：障碍物（Digger/Grave 圆半径 Prefab）；Warehouse+精魂入账（堆叠 10000、AutoConvert）；LootDrop=`Id_Count|…`（保留 Id=`Spirit`）；DigProtagonistCapabilities 四项算法（伤害/单次速度 min0.1/光标半径/可挖类型）；SPEC_04 §9.4 MaterialConfig / §9.5 能力伪结构（原 §9.4 防守表改编号 §9.6）；同步 CONTEXT |
| 2026-07-23 | v0.8.0 | §3.10 / §3.9：Dig 无胜负；有效时长=配置基础+科技时长加成；DigStageSummary（UI-011）仅汇总本阶段已获奖励、无额外发放；归零取消进行中 DigAction |
| 2026-07-23 | v0.7.0 | §3.11 框架关闭：经验阶段末入账+溢出保留；战士独立实例；控制力=等级+科技；失控占位不挡开战；布阵连续坐标+共享编辑器；无独立阶段结算；完整科技树/配方/失控效果另专题 |
| 2026-07-23 | v0.6.3 | §3.11 / §3.12：控制力超额（失控）不阻止开战；失控效果仅在战斗中按档次生效 |
| 2026-07-23 | v0.6.2 | §3.12：开战须至少上阵 1 名战士；否则开战按钮禁用或提示不可开战 |
| 2026-07-23 | v0.6.1 | BattleFormation：Defend `Prepare` 可调整位置/上下阵并写回同一套数据（不可制造）；开战按当前布阵部署；同步 §3.11 / §3.12 |
| 2026-07-23 | v0.6.0 | 新增 SPEC_03 §3.12 防守（Defend）：准备态→开战、BattleMap 部署、NavMesh 寻路与 1s 目标修正、末波清场胜利与主角阵亡关卡失败；SPEC_04 §9.4 DefendGameplayConfig；同步 CONTEXT / SPEC_02 / §3.9 |
| 2026-07-23 | v0.5.2 | §3.11 / UI-010：升级与制造主屏 = 同屏三区并列（升级/制造/布阵）+ 底部「完成」按钮 |
| 2026-07-23 | v0.5.1 | §3.11 / §3.9：升级与制造阶段结束 = 玩家主动确认「完成 / 进入下一阶段」（无强制倒计时；本版无最低门槛） |
| 2026-07-23 | v0.5.0 | `SewRevive` 正式更名为 **升级与制造 / UpgradeManufacture**；新增 SPEC_03 §3.11 框架（经验升级→科技点、材料造战士、控制力/失控、战斗布阵持久化）；同步 SPEC_02/04、CONTEXT |
| 2026-07-23 | v0.4.1 | 强化 SPEC_04 §13：**预制体优先（Prefab-first）** 为实际开发默认原则；补齐适用对象、例外与禁止项；决策表默认行改为 Prefab |
| 2026-07-23 | v0.4.0 | 录入挖坟交互与奖励（§3.10）：主角/圆圈光标/0.2s 触发/0.8s 帧动画/扣血与图标样式/固定动画序列/DigReward；新增坟墓品质定义表（SPEC_04 §9.3）；挖坟伤害-科技绑定占位 |
| 2026-07-23 | v0.3.0 | 录入关卡阶段流水线（§3.9）与挖坟生成/倒计时规则（§3.10）；关卡运作表 + 挖坟配置表（SPEC_04 §9）；末阶段结束触发胜利结算 |
| 2026-07-23 | v0.2.0 | 录入最小 Demo：三玩法状态占位、固定 3 槽存档、工具面板（设置/关卡占位）；验收 D-001～D-004 |
| 2026-07-23 | v0.1.0 | 自 SPECandSKILL 套件创建项目 SPEC 骨架；录入 Unity 2021.3.40f1 与工程路径 |

### English

| Date | Version | Summary (English) |
|------|---------|-------------------|
| 2026-08-07 | v0.69.0 | PushMap monster footprint spread (PM-10 Approach A): `MonsterConfig.BodyRadius` (default 0.35); spawn ring/spiral stagger + avoid living footprints; moving monsters `NavMeshAgent.radius=BodyRadius` RVO; `Stationary*` spawn placement only; synced SPEC_03 §3.14, SPEC_04 §6/§9.19/§9.23, CONTEXT |
| 2026-08-07 | v0.68.0 | Config CSV numeric emit: forbid float noise (§14.6); bake rounds Excel numbers to ≤10 decimals and trims zeros (e.g. `0.009999…`→`0.01`); integers stay integer strings; text cells untouched; synced `XlsxSheetReader` / Python bake mirror and rebaked/verified CSV |
| 2026-08-06 | v0.67.1 | PushMap Combat scroll zoom on `orthographicSize`: default still 2; clamp `[0.5, 20]`; scroll forward zoom-in / back zoom-out; step 0.5/notch; does not switch follow mode; ResumeFollow does not reset Size; synced SPEC_03 §3.14, SPEC_04 §6 |
| 2026-08-06 | v0.67.0 | PushMap Combat camera dual modes (PM-09 Approach A): Auto sticky-follow closest loyal to CurrentObjective (repick on invalid; freeze when none); LMB drag → Manual; bottom-center ResumeFollow only in Manual; `PushMapCameraFollowController`; synced SPEC_03 §3.14, SPEC_04 §6, CONTEXT |
| 2026-08-06 | v0.66.1 | PushMap `PushMapCamera.orthographicSize` fixed to **2** (no longer `max(half)-1.5`); angle/height/near-far still match Defend; synced SPEC_04 §6 |
| 2026-08-06 | v0.66.0 | PushMap soldier advance stuck fix (Approach A): Capture probe no longer pauses advance (living monsters in zone only reset capture timer); StartBattle order Bake NavMesh→deploy→`FireStartBattleSpawns`; sample map expands `DigMapBounds`/`WalkSurface`/`EngageZone` to cover objectives/spawns; synced SPEC_03 §3.14, SPEC_04 §6/§9.22–§9.23 |
| 2026-08-06 | v0.65.0 | PushMap Combat camera aligned with Defend: runtime Ensure `PushMapCamera` (ortho top-down `Euler(90,0,0)`, Y+18, `orthoSize=max(half)-1.5`, near/far); disable during Prepare (FormationCamera), enable on StartBattle; fixes Runtime StageRoot missing camera falling back to Boot perspective Main Camera; synced SPEC_03 §3.14, SPEC_04 §6/§15.2 |
| 2026-08-06 | v0.64.0 | UM PoolPanel soldier frame + remanufacture: scrollable warrior list; selected shows「再造1个」; `SourceItemIds`/`SourceSpiritCost` + `TryRemanufacture` adds warrior; insufficient Tips (upper-mid 1s); synced SPEC_03 §3.11, SPEC_04 §6, CONTEXT |
| 2026-08-06 | v0.63.0 | Config Excel three-row header (Approach B): row1 ZH name, row2 ZH notes, row3 EN columns, row4+ data; `ConfigTableBaker` strips doc rows on bake; CSV stays EN single-row header; legacy single-row header still accepted; synced SPEC_04 §14.4 |
| 2026-08-06 | v0.62.0 | Defend ModeSelect Mode2 live entry: list all `PushMapGameplayConfig`; confirm → `LevelOperationDriver.TryHandoffModeSelectToPushMap` exits Defend → rewrite context to PushMap → `PushMapStageModule` Prepare; keep `GameplayType=PushMap` direct path; synced SPEC_03 UI-013/D-044/§3.12/§3.14, SPEC_04 §6, CONTEXT |
| 2026-08-06 | v0.61.0 | PushMap PM-08 (Approach A): AirWall NavMesh — StartBattle runtime bake injects map `AirWall` as `NavMeshBuildSource` Box + Not Walkable (incl. Y 45°); both factions blocked; multi-layer obstacle polish out of scope; SPEC_03 §3.14, SPEC_04 §6/§9.7/§9.22/§13 |
| 2026-08-06 | v0.60.0 | PushMap PM-07 (Approach A): Boss clear & reward hooks — `TryNotifyBossKilled`→`VictorySettled(StageExpReward)` aligned with Defend Exp/`TryAdvanceStage`; Shield≤0 no Exp; capture `CaptureLoot`+`DungeonUnlockIds` save hook; Demo kill = loyal entry into Boss `AttackRange`; SPEC_03 §3.14, SPEC_04 §6/§9.22–§9.23 |
| 2026-08-06 | v0.59.0 | PushMap PM-06 (Approach A): AggroMode four-state Demo contract — `PushMapMonsterAgentView` branches on `AggroMode`; active stances detect via `AlertRadius` on loyal soldiers only; passive Demo provocation = first loyal entry into `AttackRange` → `NotifyProvoked()`; stationary stances never move; hits keep `AttackMode` scheme D; soldier-kill / skills / dungeon not done; SPEC_04 §6/§9.23 + SPEC_03 §3.14 impl boundaries |
| 2026-08-06 | v0.58.0 | PushMap PM-05 (Approach A): spawn & trap runtime contract — SPEC_04 §6/§9.23 (`PushMapSessionService` load/StartBattle fire/`TryNotifyTrapEnter`/`PushMapSpawnRequested`/`ObjectiveCaptured` stop; `PushMapStageController` resolves `SpawnPoint`/`TrapZone` + `PushMapMonsterAgentView`); SPEC_03 §3.14 Demo spawn-AI edge (Defend default chase; AggroMode/Boss settlement deferred PM-06/07); sample `TrapZoneId` aligned to `TZ_01` |
| 2026-08-06 | v0.56.0 | PushMap PM-03 (Approach A): SPEC_04 §6 stage wiring — `LevelOperationDriver` supports `PushMap`, `MapPrefabPaths` accepts `PushMap_*`, `PushMapStageModule`+`PushMapSessionService`+`PushMapStageController` (Prepare / StartBattle ≥1 / Shield / LOC; shared FormationEditor; no capture/spawn/Boss); sample `Level_01,4,PushMap,PushMap_01`; Defend Mode2 stays stub |
| 2026-08-06 | v0.55.1 | Fix Bake: XlsxSheetReader accepts Zip entry paths with / or \ (Windows Excel backslashes broke GetEntry on TechEffectConfig); rewrote that Excel; synced SPEC_04 §14.4 |
| 2026-08-06 | v0.55.0 | PushMap PM-02 (Approach A): `PushMapGameplayConfig` / `PushMapSpawnConfig` Excel+CSV; `MonsterConfig` load `AggroMode`/`AlertRadius` defaults; `ConfigCsvRepository` read-only load; synced SPEC_04 §6/§9.19/§9.22 |
| 2026-08-06 | v0.54.0 | PushMap PM-01 (Approach A): SPEC_04 §9.22/§13 marker field contract + sample `PushMap_Demo_01` (standalone `PushMap_*`, leave `Ground_*` untouched); scripts under `Gameplay/PushMap/`; Editor Ensure; no capture/spawn runtime |
| 2026-08-06 | v0.53.0 | Dig hit-shape Approach B: offline-baked `DigHitShape` local-XZ convex hull; cursor circle ∩ hull triggers DigAction (separate from obstacle circle); synced SPEC_03 §3.10, SPEC_04 §6/§9.2, CONTEXT |
| 2026-08-06 | v0.52.0 | PushMap rule intake: SPEC_03 §3.14 (GameplayType; reuse Defend formation/Shield/LOC/WarriorCombat; objective CaptureZone; AirWall/Spawn/Trap/Boss; AggroMode); SPEC_04 §9.22–§9.23 + MonsterConfig.AggroMode/AlertRadius; CONTEXT/SPEC_02/spec-map; issues `.scratch/push-map/` |
| 2026-08-06 | v0.51.0 | Battle-stage Mode/Level select gate (D-044): `DefendPhase.ModeSelect`; Mode1 Defend=existing §3.12; Mode2 PushMap stub; level list=all mode configs; LevelOperation Defend `GameplayConfigId`=Recommended; either-mode clear→`TryAdvanceStage`; synced SPEC_03/04, CONTEXT |
| 2026-08-02 | v0.50.0 | UM main UI redo: full-screen manufacture + upgrade Modal (GM Upgrade/X); bottom square inventory drag; center slot ring (Mount/Wing positions); visual appearance only when all non-gem slots filled (Attack→Idle); synced SPEC_03 UI-010/§3.11, SPEC_04 §6 |
| 2026-08-02 | v0.49.9 | Move Tile Palette to `Art/Maps/Palettes/SurvivorTiles`; confirm `Ground_01`…`05` Tile/Sprite GUIDs use `Art/Maps/Tiles` (no dependency on gitignored SmallScaleInt); harden `MapTilemapAssetBuilder` EnsurePalette |
| 2026-07-31 | v0.49.8 | Dig: **parallel** DigAction for all diggable graves in cursor radius (drop global busy lock; per-grave duration & damage); synced SPEC_03 §3.10 |
| 2026-07-31 | v0.49.7 | Demo initial `DigCursorRadius` **1.5→0.6** (`Tech_Root` / `DigProtagonistCapabilities` defaults); synced SPEC_03 §3.10 / TechEffectConfig |
| 2026-07-31 | v0.49.6 | Demo initial `DigCursorRadius` **2.5→1.5** (`Tech_Root` / `DigProtagonistCapabilities` defaults); synced SPEC_03 §3.10 / TechEffectConfig |
| 2026-07-31 | v0.49.5 | Fix `UiDigCursorRing` diameter double-scaled by CanvasScaler: convert screen-projected pixels ÷ `canvas.scaleFactor` into `sizeDelta`; keep stroke constant in screen pixels; synced SPEC_03 §3.10 / SPEC_04 §9 |
| 2026-07-29 | v0.49.4 | Fix ToolsButton needing a second click: do not `SetActive(false)` on self `_root` in `Awake` for initially inactive panels; synced SPEC_04 §3, ToolsPanel/ConfirmDialog/DigStageSummary |
| 2026-07-29 | v0.49.3 | Defend: loyal soldiers with no EngageZone target auto-return to StartBattle `FormationHome` (keep retargeting; Rebels do not return); synced SPEC_03 §3.12, SPEC_04 §6, CONTEXT |
| 2026-07-29 | v0.49.2 | Fix UM inventory blank labels when kit grows: vertical ScrollRect + row minHeight on `InventoryColumn`/`SlotColumn`; synced SPEC_04 §6 D-031 |
| 2026-07-29 | v0.49.1 | UM Debug "grant starter kit" now grants every `SoulConfig` row ×1 (Demo `Soul_01`…`Soul_10`); synced SPEC_04 §6 D-031 |
| 2026-07-29 | v0.49.0 | Defend warrior anim drive: `WarriorAnimView` plays `IsRun`/`Attack1`/`Die` + dynamic `DirIndex`; wired from `WarriorAgentView`; synced SPEC_04 §6/§15.5, CONTEXT |
| 2026-07-28 | v0.48.1 | FormationEditor: keep deployed bar cells + highlight; Idle world follow after leaving bar; clear highlight on undeploy; synced SPEC_03 §3.11 |
| 2026-07-28 | v0.48.0 | Formation drag editor: UM two panels + Formation/Return; shared `FormationEditorRoot` (80×80 soldier bar, drag deploy/reposition/undeploy, ControlPower HUD); Defend Prepare reuses; `TryDeployAt`; synced SPEC_03 §3.6/§3.8/§3.11/§3.12, SPEC_04 §6, CONTEXT |
| 2026-07-28 | v0.47.2 | Fixed default-unlocked tech missing `DigCursorRadius`, which left the Dig circle at minimum visual size and prevented grave hits; locked Demo initial `DigDamage=25` and `DigCursorRadius=2.5`; synced SPEC_03 §3.10 / TechEffectConfig |
| 2026-07-28 | v0.47.1 | Defend HUD: disable unused `Image` on `DefendRoot` (no fullscreen overlay); synced Prefab / DefendAssetBuilder / SPEC_04 §6 D-040 |
| 2026-07-28 | v0.47.0 | Editor Bake Tables Approach A: `Gravedigger2026/Config/Bake Tables`; pure-C# Open XML Excel→Csv; four-part name map + header check; full §9 schema validation deferred; synced SPEC_04 §14/§6, SPEC_03 §3.8, CONTEXT |
| 2026-07-28 | v0.46.5 | Character spritesheet sliced with NPOT-padded 2048 width (~136.53 cells vs correct 128) caused leftward frame drift: export/Repair force source-size reslice; batch-fix `Art/Characters` metas; synced SPEC_04 §15.3 |
| 2026-07-28 | v0.46.4 | Digger / BattleProtagonist → 2D baked whole characters: game Prefab root + `Visual` (Sprite/Animator, `localEuler(90,0,0)`); Dig semantics→`Special1`; fixed `DirIndex=2` (South); `DigDiggerView` loops dig; Assembler + Builders ban Capsule regen; synced SPEC_04 §15.2/§15.5 |
| 2026-07-28 | v0.46.3 | Dig circle-cursor UI Prefab `UiDigCursorRing` (dual circle: white semi-transparent fill + fixed-pixel stroke); bound on `DigPrefabCatalog`; hit test remains circular radius; synced SPEC_03 §3.10 / SPEC_04 §6/§9 |
| 2026-07-27 | v0.46.2 | IsoDiamond half-extents = `PaintRadius*(cellSize.x,cellSize.y)` (anisotropic OK; Demo `cellSize≈(1,0.5)`→`(5,2.5)`); `WalkSurface`/NavMesh use matching thin diamond mesh — fixes Z extent ~2× vs Tilemap |
| 2026-07-27 | v0.46.1 | Unify map logic footprint as IsoDiamond (XZ Manhattan diamond aligned to Isometric Tilemap silhouette): `DigMapBounds`/`EngageZone`/`WalkSurface` (Y=45° flat box) / rotated NavMesh box / clock-rim spawns / Dig placeable sampling; half-extents = vertex-to-center distance; synced SPEC_03/04, CONTEXT |
| 2026-07-27 | v0.46.0 | Map presentation → Unity Isometric Tilemap (orthographic XZ top-down; logic still continuous non-grid); Tiles/Sprites under `Art/Maps/Tiles/` (copied from Example Environment); `Ground_*` Prefabs include Grid+Tilemap+WalkSurface; require `com.unity.2d.tilemap`; synced SPEC_03 DigMap, SPEC_04 §2/§9/§13 |
| 2026-07-26 | v0.45.0 | Warrior appearance Prefab: root + child `Visual` (Sprite/Animator, `localEuler(90,0,0)`) for top-down camera; `WarriorAgentView` sets `NavMeshAgent.updateRotation=false`; synced SPEC_04 §15.2 |
| 2026-07-26 | v0.44.0 | Character Creator export patch: force slice `textureType=Sprite` + forward-slash asset paths; abort empty Controller when zero clips; Editor repair menu rebuilds `.anim`/`.controller`; synced SPEC_04 §15.3 |
| 2026-07-26 | v0.43.0 | Scaffold `Assets/Art/` per-system art source folders (Characters/Dig/Maps/Defend/UI/Placeholder/VFX/Audio); pre-create AppearanceId/ModelId/Ground/Grave slots; no top-level `Sprites/` this revision; synced SPEC_04 §2 |
| 2026-07-26 | v0.42.0 | Optional TechTree canvas Approach A (UI-012 / 06): `ConfigCsvRepository` loads TechTree/TechEffect; pure-C# `TechTreeService` (InitiallyUnlocked auto-learn, prereqs+LearnCost, spend TechPoints, additive recalc of `DigProtagonistCapabilities`); Settings opens Prefab-laid-out uGUI canvas (pan/hover/edges/learn); `DigStageModule` reads recalced caps; temp `Prefabs/Meta/TechTreeCanvasRoot.prefab`; synced SPEC_03 §3.8/UI-012, SPEC_04 §6 |
| 2026-07-26 | v0.41.0 | Defend LossOfControl StartBattle roll + win/lose settle Approach A (D-043 / 05d): lock Degree/Tier + per-soldier `FinalLossChance`→Rebel; Rebel nearest hits protagonist/soldiers/enemies (Shield−1 on protagonist); clear→Ended credits Demo stage Exp 100→`TryAdvanceStage`; `Shield≤0`→LevelFailure no Exp + abort Level; load `Combat_LossOfControlConfig.csv`; minimal PermanentDeath (gems→warehouse, clear formation, remove pool); synced SPEC_03 §3.8/§3.12, SPEC_04 §6 |
| 2026-07-26 | v0.40.0 | Defend warrior ranged projectile Approach A (D-042 ranged / 05c2): Session registers `RangedProjectileSpeed`/`RangedTimeoutSeconds` + `TryConfirmRangedHit`; `WarriorAgentView` shares EngageZone/ASPD with melee; `ProjectileView` kinematic fly to locked monster (hit when distance≤hitRadius; timeout miss no damage); temp `Prefabs/Defend/Projectile.prefab`; synced SPEC_03 §3.8 D-042, SPEC_04 §6 |
| 2026-07-26 | v0.39.0 | Defend warrior melee slice Approach A (D-042 melee): `WarriorCombatMath` + Session HP registry; EngageZone nearest target; melee windup HitConfirm; monster `AttackPower` vs warrior HP; CombatDead stops acting; clear-victory detectable (no credit); ranged deferred to 05c2; synced SPEC_03 §3.8 D-042, SPEC_04 §6 |
| 2026-07-26 | v0.38.0 | Defend spawn + path Approach A: `ConfigCsvRepository` loads WaveSpawn/Monster; `DefendSessionService` activates rows by remaining seconds and emits events; temp fixed `SpawnPoint` + runtime NavMesh bake; `MonsterAgentView` (NavMeshAgent) approaches and normal-attacks `Shield`; `Shield≤0`→Ended/LevelFailure hook; temp `Prefabs/Defend/Monsters/{ModelId}`; synced SPEC_03 §3.8 D-041, SPEC_04 §6 |
| 2026-07-26 | v0.37.0 | Defend Prepare/StartBattle/Shield Approach A: `DefendStageModule` + `DefendSessionService` + `DefendPrefabCatalog`; Enter instantiates `Prefabs/Maps/` by `BattleMapId`; Prepare reuses `BattleFormationService`/`FormationPanelView`; StartBattle requires ≥1 deployed → temp `BattleProtagonist` + warriors; `Shield`=`ProtagonistMaxHP`; countdown visible (no spawn this slice); synced SPEC_03 §3.8 D-040, SPEC_04 §6 |
| 2026-07-25 | v0.36.0 | UM formation Approach A: `BattleFormationService` (continuous-coord deploy/undeploy/reposition + ControlPower usage) + `FormationPanelView`; wired to warrior pool; save-scoped for shared Defend Prepare; synced SPEC_03 §3.8 D-032, SPEC_04 §6 |
| 2026-07-25 | v0.35.0 | UM manufacture Approach A: `ManufactureService` (15 strict slots / preview / Spirit gate) + `WarriorPoolService`; loads Soul/Class/Gem/Race/BodyPart/Appearance/Equip/GemSuffix tables; `WarehouseService` credits BodyParts and consumes by Id; temp `Prefabs/Defend/Warriors/{AppearanceId}`; synced SPEC_03 §3.8 D-031, SPEC_04 §6 |
| 2026-07-25 | v0.34.0 | UM upgrade panel Approach A: `UpgradeManufactureStageModule` + `ProtagonistProgressService` + `ProtagonistLevelConfig`; Debug Exp inject → chain level-up; synced SPEC_03 §3.8 D-030, SPEC_04 §6 |
| 2026-07-25 | v0.33.0 | Dig vertical Approach A: `DigStageModule` + `DigSessionService` + `DigPrefabCatalog`; instantiate `Prefabs/Maps/` by `DigMapId`; dig/loot/DigStageSummary→return to Level driver; synced SPEC_03 §3.8 D-020, SPEC_04 §6 |
| 2026-07-25 | v0.32.0 | Level-driver Approach A: runtime CSV-only (Editor=`Assets/ConfigTables/Csv`; Player=`StreamingAssets/ConfigTables/Csv`); `LevelOperationDriver` + `IStageModule` hooks; Tools Level starts `Level_01`; UM ConfigId ignored; MapId resolve/log only; synced SPEC_03 §3.8 D-003/D-004/D-010, SPEC_04 §6/§14 |
| 2026-07-25 | v0.31.1 | SPEC_04 §1: local Unity Editor path `F:\Unity\Unity 2021.3.40f1\Editor\Unity.exe` |
| 2026-07-25 | v0.31.0 | Meta shell Approach A: PlayerPrefs 3-slot occupied; Boot scene + Prefab UI; Tools Settings/Level Toast; Demo Debug cycle for D-004; synced SPEC_04 §6 / SPEC_03 §3.8 D-001–D-004 status |
| 2026-07-25 | v0.30.0 | Demo acceptance expanded to Meta shell + Dig→UM→Defend pipeline vertical (§3.8 D-001–D-043); UM stage `GameplayConfigId`=ignore; Defend Demo-min spawn/NavMesh; synced SPEC_03/04 §6 / CONTEXT / issues |
| 2026-07-25 | v0.29.0 | Dig/Defend map presentation shares `Ground_01`…`Ground_05`: DigGameplayConfig adds `DigMapId`; `BattleMapId` allowed values → `Ground_*`, Prefab resolve → `Assets/Prefabs/Maps/`; source ref Example Scene Grid/Ground; synced SPEC_03/04 / CONTEXT / spec-map / sample tables |
| 2026-07-25 | v0.28.0 | SPEC_04 §15 Character Art Pipeline: Character Creator baked whole characters; ban game assets under vendor tool folder; patched export → `Art/Characters` → Prefabs; `AppearanceId`/`ModelId`/protagonist Prefab resolve; Mount/Wing baked into appearance; synced SPEC_03 / CONTEXT / spec-map |
| 2026-07-25 | v0.27.0 | Kit/workspace paths updated to local `F:\CursorGame_Git\SPECandSKILL` and `F:\CursorGame_Git\Gravedigger2026`; workspace SPEC/CONTEXT/Skills synced back to kit; closes prior E: path unreachable pending sync |
| 2026-07-25 | v0.26.0 | Config-table schema lock: soldier Skills=`SkillId;Level|…`; EquipStats; CombatConvertCoeffs; Class/Monster AttackRange hit columns; six GemTypes; ComboKey; MoveStyle/AttackPriority; IconStyle three columns; BattleMapId/ModelId Prefab names; TechUiFrameType; open UnlockedFeature list; SkillConfig no effect columns yet; synced SPEC_03/04 / CONTEXT / spec-map |
| 2026-07-25 | v0.25.0 | SPEC_04 §14: Excel disk names are four-part `{SystemZH}_{TableZH}_{SystemEN}_{TableEN}.xlsx`; CSV stays `{SystemEN}_{TableEN}.csv`; bake maps via English suffix; §9 disk names split Excel/CSV; synced CONTEXT / spec-map |
| 2026-07-25 | v0.24.0 | §3.11 soul Class: new ClassId / ClassConfig (ClassName, PrimaryStat, CombatConvertCoeffs placeholder); SoulConfig references ClassId only (drops ClassName/PrimaryStat); naming & ClassAffinity via ClassConfig; §3.12 Primary from Class table; global derive constants interim; synced SPEC_04 / CONTEXT / spec-map |
| 2026-07-25 | v0.23.0 | §3.12 Demo v1 combat scope: soldiers & monsters normal attacks only (no skill casts); SoulConfig.AttackMode; Mage normal = Archer Ranged channel (PrimaryStat=Intelligence only); SkillCooldown/Skills retained but unused; synced SPEC_04 / CONTEXT / spec-map |
| 2026-07-25 | v0.22.0 | §3.11/§3.12 soldier combat derives: PrimaryStat; StaticStat/FinalStat layers; NormalAttackPower=Primary×1.5; AttackSpeed=0.5+60/max(Agi,1); SkillCooldown=max(0.1,BaseCD−30/max(Int,1)); MaxHP=ceil(BodyLife+Str×3); SoulConfig.PrimaryStat + SkillConfig.BaseCooldownSeconds; synced CONTEXT / SPEC_04 / spec-map |
| 2026-07-25 | v0.21.0 | §3.12 WarriorCombat: nearest target in EngageZone; AttackRange; hit scheme D (melee windup confirm / ranged projectile); CombatDead vs PermanentDeath (settle on Ended/LevelFailure); gem exception immediate PermanentDeath on HP≤0; §3.11 material fate only on PermanentDeath; AttackPriority unused for targeting this batch; synced SPEC_04 / CONTEXT / spec-map |
| 2026-07-25 | v0.20.0 | Closed §3.11/§3.12 LossOfControl: Degree=ΣCost/Cap−1; four tiers; lock + per-soldier roll at StartBattle; Rebel nearest targeting (Shield −1); FinalChance=tier+race+Σgem+Σskill (clamp); skill-cast re-roll when ΣSkillBonus≠0; SPEC_04 §9.20 LossOfControlConfig + §9.21 SkillConfig skeleton; Race/Gem chance-bonus fields; synced CONTEXT / spec-map |
| 2026-07-25 | v0.19.0 | §3.11 BodyPartConfig expanded (BodyLevel/StatBonus/AutoConvert/art; Base(S)=Σ StatBonus); new BodyAppearanceConfig + pick rules (avg level→round, ClassAffinity, IsFallback, else table-random); WarriorInstance.AppearanceId; LootDrop may resolve BodyPartId; SPEC_04 §9.13+ renumbered; synced CONTEXT / spec-map |
| 2026-07-25 | v0.18.0 | Terminology: CN unit name 战士→士兵 (manufacture/deploy/composition); EN ids `Warrior*` / `PreferWarrior` unchanged; ClassName may still be profession「战士」; synced SPEC_03/04, CONTEXT, spec-map |
| 2026-07-24 | v0.17.0 | §3.12 Defend fill: Shield (normal-hit count; init=`ProtagonistMaxHP`; ≤0 → LevelFailure); countdown spawn; WaveSpawnConfig / MonsterConfig (SPEC_04 §9.17–§9.18); DefendGameplayConfig +`CombatDurationSeconds`; synced CONTEXT / spec-map. **Workspace updated; suite path `E:\Work\Cursor\SPECandSKILL\Gravedigger2026\` unreachable on this machine — sync pending** |
| 2026-07-24 | v0.16.0 | §3.13 TechTree framework: center-out, InitiallyUnlocked default learn, prereqs+LearnCost, Settings 2D canvas (UI-012); SPEC_04 §9.15 TechTreeConfig / §9.16 TechEffectConfig; Exp still Defend victory only; synced CONTEXT / spec-map. **Workspace updated; suite path `E:\Work\Cursor\SPECandSKILL\Gravedigger2026\` unreachable on this machine — sync pending** |
| 2026-07-24 | v0.15.0 | SPEC_04: config-table engineering rules — unified `ConfigTables/Excel`+`Csv`; naming `{System}_{Table}`; dual-format required; Bake Tables menu (§14); closed §9 carrier TBD; §13 splits table rows vs non-table SO; synced CONTEXT / Skill. **Workspace updated; suite path `E:\Work\Cursor\SPECandSKILL\Gravedigger2026\` unreachable on this machine — sync pending** |
| 2026-07-24 | v0.14.0 | §3.11 warrior manufacture: slots/min requirements/preview+Spirit gate; BodyPart weight-1 race pick; 6 type-exclusive gems with per-dim Σ GemMult; name Prefix+Race+Class+Suffix; death returns all gems; SPEC_04 §9.9–§9.14; synced CONTEXT / spec-map |
| 2026-07-24 | v0.13.0 | §3.11 FinalStat per-attribute aggregation (pick S then sources); Strength example; `max(0,…)` floor; GemMult five-dim; SPEC_04 §9.10 / WarriorInstance synced; CONTEXT |
| 2026-07-24 | v0.12.0 | §3.11 Race: from Body; five-dim RaceAdjustCoeff; FinalStat+=Base×RaceAdjustCoeff; no ControlPower term; SPEC_04 §9.11 RaceConfig; synced CONTEXT / spec-map |
| 2026-07-24 | v0.11.0 | §3.11 Gem: optional socket (≤1); FinalStat+=Base×GemMult; on death Gem→Warehouse, other bound materials destroyed; ControlPowerCost includes GemCost; SPEC_04 §9.10 GemConfig; synced CONTEXT / spec-map |
| 2026-07-24 | v0.10.0 | §3.11 warrior attribute composition closed: Info/BaseStats/Soul/ExtraEquipment/ControlPowerCost; FinalStat=Base+Equip+SkillBuffCoeff×Base (runtime Buff only); equip locked at manufacture; SPEC_04 §9.9 SoulConfig + WarriorInstance snapshot; synced CONTEXT / spec-map |
| 2026-07-23 | v0.9.0 | §3.11 / SPEC_04 §9.8: ProtagonistLevelConfig (cumulative Exp, reserved unlocks, TechPoints, ControlPower cap, MaxHP); LevelFailure: no level settlement rewards / no stage Exp; already-owned kept; synced CONTEXT / spec-map |
| 2026-07-23 | v0.8.2 | Dig gaps: weighted-field common rules (Weight=0 dropped; Dig empty effective list abandons spawn); MaterialConfig / CurrencyConfig add AppearanceIconId, AssetPath, WarehouseQualityOutlineId; capabilities → §9.6, Defend → §9.7; synced CONTEXT |
| 2026-07-23 | v0.8.1 | Dig gap fill: DigObstacle (Digger/Grave circle radii on Prefabs); Warehouse + SpiritEssence credit (stack 10000, AutoConvert); LootDrop=`Id_Count|…` (reserved Id=`Spirit`); DigProtagonistCapabilities formulas (damage / dig speed min 0.1 / cursor radius / diggable types); SPEC_04 §9.4 MaterialConfig / §9.5 capabilities (Defend table renumbered §9.6); synced CONTEXT |
| 2026-07-23 | v0.8.0 | §3.10 / §3.9: Dig no win/lose; effective duration = config base + tech duration bonus; DigStageSummary (UI-011) aggregates stage rewards only, no extra grants; cancel in-progress DigAction on timeout |
| 2026-07-23 | v0.7.0 | Closed §3.11 framework: stage-end Exp + overflow kept; warrior instances; ControlPower=level+tech; LossOfControl placeholder non-blocking; continuous shared formation editor; no stage settlement; full tech/recipes/tier effects deferred |
| 2026-07-23 | v0.6.3 | §3.11 / §3.12: Over-ControlPower (LossOfControl) does not block StartBattle; tier effects apply in combat only |
| 2026-07-23 | v0.6.2 | §3.12: StartBattle requires ≥1 deployed warrior; else disable button or show cannot-start hint |
| 2026-07-23 | v0.6.1 | BattleFormation: Defend `Prepare` may edit positions/deploy/undeploy and write back the same dataset (no manufacture); StartBattle uses current formation; synced §3.11 / §3.12 |
| 2026-07-23 | v0.6.0 | Added SPEC_03 §3.12 Defend: Prepare→StartBattle, BattleMap deploy, NavMesh pathing + 1s retarget, last-wave clear victory & protagonist LevelFailure; SPEC_04 §9.4 DefendGameplayConfig; synced CONTEXT / SPEC_02 / §3.9 |
| 2026-07-23 | v0.5.2 | §3.11 / UI-010: UpgradeManufacture main screen = three side-by-side panels + bottom Complete |
| 2026-07-23 | v0.5.1 | §3.11 / §3.9: UpgradeManufacture stage ends on player confirm "Complete / Next stage" (no countdown; no mandatory gate this version) |
| 2026-07-23 | v0.5.0 | Renamed `SewRevive` → **UpgradeManufacture**; added SPEC_03 §3.11 framework (exp→tech points, material warriors, ControlPower/LossOfControl, BattleFormation persistence); synced SPEC_02/04, CONTEXT |
| 2026-07-23 | v0.4.1 | Strengthened SPEC_04 §13: **Prefab-first** as the default authoring principle; added targets, exceptions, and forbidden patterns; decision table default row is Prefab |
| 2026-07-23 | v0.4.0 | Dig interaction & rewards (§3.10): Digger / circle cursor / 0.2s trigger / 0.8s dig anim / HP & icon styles / fixed anim sequence / DigReward; GraveQualityConfig (SPEC_04 §9.3); dig-damage tech binding placeholder |
| 2026-07-23 | v0.3.0 | Level stage pipeline (§3.9) and Dig spawn/countdown (§3.10); LevelOperation + DigGameplayConfig (SPEC_04 §9); VictorySettlement after last stage |
| 2026-07-23 | v0.2.0 | Minimal Demo rules: three gameplay placeholders, 3 save slots, ToolsPanel stubs; acceptance D-001–D-004 |
| 2026-07-23 | v0.1.0 | Created project SPEC skeleton from SPECandSKILL kit; recorded Unity 2021.3.40f1 and project paths |
