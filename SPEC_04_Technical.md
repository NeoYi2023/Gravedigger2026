# SPEC_04 — 技术规范 / Technical Standards（Gravedigger2026）

**关联文档 / Related:** [SPEC_00_Index.md](SPEC_00_Index.md) · [SPEC_03_GameRules.md](SPEC_03_GameRules.md)

---

## 1. 工程与环境

### 简体中文

| 属性 | 值 |
|------|-----|
| Unity 版本 | 2021.3.40f1 |
| Unity 编辑器（本机） | `F:\Unity\Unity 2021.3.40f1\Editor\Unity.exe` |
| 脚本语言 | C# |
| 渲染 | TBD |
| Cursor 工作区根 | `F:\CursorGame_Git\Gravedigger2026` |
| Unity 工程根目录 | `Gravedigger2026/`（相对工作区根） |
| 资源根目录 | `Gravedigger2026/Assets/` |
| 目标平台 | TBD |
| 平台优先级 | TBD |

批处理 / CLI 示例：`"<Unity编辑器>" -batchmode -quit -projectPath "<工作区根>\Gravedigger2026" …`（具体参数按任务另定）。

### English

| Attribute | Value |
|-----------|-------|
| Unity version | 2021.3.40f1 |
| Unity Editor (this machine) | `F:\Unity\Unity 2021.3.40f1\Editor\Unity.exe` |
| Scripting language | C# |
| Rendering | TBD |
| Cursor workspace root | `F:\CursorGame_Git\Gravedigger2026` |
| Unity project root | `Gravedigger2026/` |
| Assets root | `Gravedigger2026/Assets/` |
| Target platforms | TBD |
| Platform priority | TBD |

CLI example: `"<UnityEditor>" -batchmode -quit -projectPath "<workspaceRoot>\Gravedigger2026" …` (args per task).

---

## 2. 目录结构（建议）

### 简体中文

```
Gravedigger2026/Assets/
├── Scenes/
├── Scripts/
│   ├── Core/
│   ├── Gameplay/
│   ├── UI/
│   ├── Input/
│   └── Localization/
├── Prefabs/
│   ├── Dig/               # Digger、Grave 等
│   ├── Defend/
│   │   ├── Warriors/      # 士兵 AppearanceId Prefab
│   │   └── Monsters/      # 怪物 ModelId Prefab
│   └── Maps/              # Dig/Defend 共用地面变体 Ground_01…Ground_05
├── Art/                   # 美术源素材（非运行时 Instantiate 入口）
│   ├── Characters/        # Character Creator 烘焙导出源（见 §15）
│   │   ├── Protagonist/   # Digger / BattleProtagonist
│   │   ├── Appearances/   # Appearances/{AppearanceId}/
│   │   └── Monsters/      # Monsters/{ModelId}/
│   ├── Dig/               # 坟墓、挖坟反馈等非角色源
│   │   ├── Graves/        # Graves/{Grave_Q*}/
│   │   └── Feedback/
│   ├── Maps/              # Maps/Tiles/（Isometric Tile+Sprite）+ Maps/{Ground_0N}/
│   ├── Defend/            # 弹道、护盾等非角色表现源
│   │   ├── Projectile/
│   │   └── Shield/
│   ├── UI/                # 2D 图标统一落点（本版不另建 Sprites/）
│   │   ├── Currency/
│   │   ├── Icons/
│   │   ├── Outlines/
│   │   ├── Tech/
│   │   └── Meta/
│   ├── Placeholder/       # Demo CSV AssetPath 过渡落点
│   ├── VFX/
│   └── Audio/             # 玩法音效源约定
├── Localization/
│   ├── Strings/
│   └── Fonts/
├── Resources/
├── Settings/              # 非表型 ScriptableObject（单例调参、引用槽等）
├── SmallScaleInt/         # 第三方 Character Creator 工具源（仅创作；见 §15）
└── ConfigTables/          # 配置表统一根（见 §14）
    ├── Excel/             # 人配源表（.xlsx）；运行时不加载
    └── Csv/               # 程序读表（.csv）；打表产物；运行时唯一数据源
```

**Art vs Prefabs：** `Art/` 存源素材（图、Clip、Controller、贴图等）；游戏 Instantiate / Catalog 绑定只引用 `Prefabs/<模块>/`。本版 **不** 单独落地顶层 `Sprites/`；2D 图标统一落在 `Art/UI/`（及 `Art/Placeholder/` 过渡）。角色烘焙细则见 [§15](#15-角色美术管线character-creator-烘焙整角)。

实际目录以工程为准；结构性变更记入 SPEC_00 Changelog。配置表路径与命名强制约定见 [§14](#14-配置表工程约定与打表工具)。角色美术路径与工具目录禁入见 [§15](#15-角色美术管线character-creator-烘焙整角)。

### English

Recommended tree as above. `Art/` holds source art (Characters per [§15](#15-角色美术管线character-creator-烘焙整角), Dig/Maps/Defend/UI/VFX/Audio, plus `Placeholder/` for Demo CSV `AssetPath`); runtime Instantiate uses `Prefabs/` only. Top-level `Sprites/` is **not** used this revision—2D icons live under `Art/UI/`. Also includes `Prefabs/Dig|Defend|Maps/...`, `SmallScaleInt/` tool source, `ConfigTables/Excel/` + `ConfigTables/Csv/`. Record structural changes in SPEC_00 Changelog. Config-table path and naming rules: [§14](#14-配置表工程约定与打表工具). Character art paths and vendor-folder ban: [§15](#15-角色美术管线character-creator-烘焙整角).

---

## 3. 代码规范（摘要）

### 简体中文

| 类别 | 约定 |
|------|------|
| 类 / 方法 / 公共成员 | PascalCase |
| 私有字段 | camelCase，可选 `_` 前缀 |
| 接口 | `I` + PascalCase |
| 命名空间 | `Gravedigger2026.<模块>`（待代码出现后与现有一致） |
| 文件名 | 与主类名一致 |

**初始隐藏面板：** Prefab 上 `m_IsActive=0` 即可；若 View 的 `_root` 即自身 GameObject，禁止在 `Awake` 再 `SetActive(false)`——该 Awake 往往在首次 `Show()` 才执行，会立刻抵消打开（表现为要点第二次才生效）。

审查清单见 [unity-spec-dev-workflow](.cursor/skills/unity-spec-dev-workflow/SKILL.md) §5。

### English

See naming table. Checklist: [unity-spec-dev-workflow](.cursor/skills/unity-spec-dev-workflow/SKILL.md) §5. Namespace `Gravedigger2026.<Module>` unless existing code differs.

**Initially hidden panels:** Prefer Prefab `m_IsActive=0`. If View `_root` is the same GameObject, do **not** call `SetActive(false)` in `Awake` — that Awake often runs on the first `Show()` and immediately cancels the open (looks like a double-click requirement).

---

## 4. 跨平台输入（占位）

### 简体中文

**状态：未定义**

建议玩法依赖输入抽象接口，禁止玩法直接读 `Input.GetKey` / 原始触摸 API。

### English

**Status: Undefined**

Prefer an input abstraction; no raw `Input.GetKey` / touch in gameplay code.

---

## 5. 性能与资源（底线）

### 简体中文

- 缓存 `GetComponent`；`Update` 避免分配；高频对象池化；主线程访问 Unity API

### English

- Cache components; avoid allocations in `Update`; pool frequent spawns; main-thread Unity API only

---

## 6. Demo 实现边界

### 简体中文

**状态：已对照 SPEC_03 §3.8（Meta 壳 + 关卡流水线垂直切片）**

**范围内（须满足 D-001～D-043）：**

| 模块 | 要求 |
|------|------|
| 存档 Meta | 固定 3 槽；本地按槽索引；至少持久化「是否占用」及流水线所需最小字段；新建 / 进入 / 删除（含确认） |
| 进档壳层 | 进入后默认 `GameplayState = Dig` 占位；浮动「工具」；流水线片可从壳层启动样例关卡（§3.8 D-003 / D-010） |
| 工具面板 | 设置、关卡入口；与玩法 View 分离（壳层 UI） |
| 关卡驱动 | 只读 `ConfigTables/Csv/`；`LevelOperationConfig` 升序驱动 Dig → UpgradeManufacture → Defend |
| Dig | §3.10 垂直切片：`DigMapId`→`Prefabs/Maps/`；挖掘 / 奖励 / DigStageSummary（临时美术允许） |
| UpgradeManufacture | §3.11 垂直切片：升级区 / 制造 ≥1 士兵 / 布阵写回；阶段 `GameplayConfigId` **忽略**（见 §9.1） |
| Defend | §3.12 垂直切片：Prepare/开战/护盾；Demo 最小刷怪点 + NavMesh；士兵普攻；胜负/LevelFailure（临时美术允许） |

**范围外：** 完整技能施放与效果表；正式美术 polish；精确 OutsideMap 几何；**完整**存档 schema（仓库/经验/科技等仍 TBD；士兵池+布阵本片已锁定）；科技树节点具体数值/图标 polish 与功能系统名完整枚举；工具后续功能；打表全量 §9 列/类型校验（Demo 仅文件名+表头）；未列入 §3.8 的需求。

**持久化意图（轻量）：** 本地、按槽索引 `0..2`。**Demo Meta 选型已锁定：`PlayerPrefs`**。键：
- `Gravedigger2026.SaveSlot.{0|1|2}.Occupied`（`0`/`1`）
- `Gravedigger2026.SaveSlot.{i}.WarriorPool` — JSON：`NextSerial` + `WarriorInstance` 制造静态快照数组（含 `SourceItemIds` / `SourceSpiritCost` / `EquipStats` / `BodyLife` 等；见 §9.9）
- `Gravedigger2026.SaveSlot.{i}.BattleFormation` — JSON：`{WarriorId, PositionX, PositionZ, RemainingHP}[]`
- `Gravedigger2026.SaveSlot.{i}.DungeonUnlocks` — 管道分隔副本解锁 ID（既有）

**士兵池 / 布阵持久化（方案 A）：** `WarriorPoolService` / `BattleFormationService` 各自 `BindSlot(slot)` 进档加载、`ClearBound` 回档选；池/布阵变更立即 `PlayerPrefs` 写回；删档 `DeleteSlotData` 清键。进档顺序：先绑池再绑布阵；布阵加载时丢弃池中不存在的 `WarriorId`。仓库 / 经验 / 科技等完整 schema 仍 **TBD**。

**Meta 壳实现（方案 A，D-001～D-004）：** 单场景 `Assets/Scenes/Boot.unity`；`SaveSelect` / `InSaveShell` 以 Canvas Prefab 显隐切换（`Assets/Prefabs/Meta/`、`Assets/Prefabs/UI/`）。规则层：`SaveSlotService` + `GameplayStateService`；View 只订阅。工具「设置」→ **打开科技树画布**（见下 UI-012）；工具「关卡」→ **启动样例 `Level_01`**（见下「关卡驱动」）。壳层正式手动切三态仍 **TBD**；Demo 暂提供进档壳 **Debug「切下一态」** 仅用于手验 D-004（不得等同工具「关卡」）；另提供 **Debug「推进阶段」** 手验 D-010（占位结束当前阶段 → 下一阶段 / VictorySettlement）。

**关卡驱动（方案 A，D-010）：** `ConfigCsvRepository` 只读 CSV（路径见 [§14.5](#145-运行时-csv-加载路径demo)）；`LevelOperationDriver` 按 `LevelId` 取行、`StageNumber` 升序运行；进入阶段时设置 `GameplayState`，经 `IStageModule` 进入/离开钩子挂各玩法（Dig / UM / Defend 见下；士兵战斗与胜负仍待后续片）。`GameplayType=UpgradeManufacture` 时 **忽略** `GameplayConfigId`（不查 Dig/Defend 表）。Dig/Defend 解析对应表行后校验 `DigMapId` / `BattleMapId` ∈ `Ground_01`…`Ground_05`，逻辑路径 `Assets/Prefabs/Maps/{Id}.prefab`。UI/日志须可见 LevelId、StageNumber、GameplayType。

**Dig 垂直切片（方案 A，D-020）：** `DigStageModule`（`IStageModule`）Enter 时按 `DigMapId` Instantiate `Assets/Prefabs/Maps/{Id}.prefab`，并挂 `DigStageRoot`（`Assets/Prefabs/Dig/`）。规则层 `DigSessionService`（纯 C#）负责有效时长倒计时、开局/过程生成、DigAction 停留触发与忙碌锁、扣血、仓库/精魂入账、阶段奖励汇总；`DigProtagonistCapabilities` 由 **科技树** 重算后注入（见 UI-012）。DigAction 候选：光标圆 ∩ 坟 `DigHitShape` 本地 XZ 凸包（世界变换后；粗筛用 `BoundingRadius`）；无凸包则回退障碍圆。表现：`DigPrefabCatalog` 绑定 Digger / `Grave_{QualityId}` / 地图变体 / `UiDigCursorRing`；圆圈光标（Prefab 双层、描边像素恒定）、坟墓 HP 样式、DigReward 飞向、DigStageSummary 由 View 订阅。时长归零 → 取消进行中 DigAction（不结算扣血）→ DigStageSummary 确认 → `LevelOperationDriver.TryAdvanceStage`。禁止运行时引用 `SmallScaleInt/`；规则层禁止读 Sprite/像素。

**科技树画布（方案 A，UI-012 可选）：** `ConfigCsvRepository` 追加加载 `Tech_TechTreeConfig.csv` / `Tech_TechEffectConfig.csv`。规则层纯 C# `TechTreeService`（存档级，挂 Meta 壳）持有已学会集合与 `UnlockedFeatureSystems`；进档/`Reset` 时对 `InitiallyUnlocked` 自动学会并应用效果；学习闸门 = 未学会 ∧ TechPoint≥LearnCost ∧ ≥1 已学会前置（由 `UnlockNextTechIds` 求逆）；学会扣点 → 标记 → 解析 `AttributeModifiers` 加法求和 → 写入 `DigProtagonistCapabilities`（Demo `DiggableQualityIds` 仍取全品质表，便于挖坟手验）。表现：临时 `Assets/Prefabs/Meta/TechTreeCanvasRoot.prefab`（节点坐标 Prefab 摆放；uGUI 空白处 LMB 拖移；悬停名+效果描述；连线按正向边；三态框色）；工具「设置」打开画布；Debug 可注入 TechPoints。禁止运行时引用 `SmallScaleInt/`。

**UM 升级区（方案 A，D-030）：** `UpgradeManufactureStageModule` Enter 时 Instantiate `Assets/Prefabs/UpgradeManufacture/UpgradeManufactureStageRoot.prefab`（**默认全屏制造区**；升级为 Modal，顶部「GM升级」打开、右上「X」关闭；布阵经「布阵」打开共享编辑器）。`ConfigCsvRepository` 加载 `Manufacture_ProtagonistLevelConfig.csv`。规则层纯 C# `ProtagonistProgressService` 持有内存态 `Level` / `LifetimeExperience` / `TechPoints` / 生效 `ControlPowerCap` / `ProtagonistMaxHP`；累计阈值连升并应用表行奖励与上限。Debug 注入仍可用；正式 Defend 胜利入账见 D-043。底部「完成」→ `TryAdvanceStage`。禁止运行时引用 `SmallScaleInt/`。

**UM 制造区（方案 A，D-031 → UI 重做 / Pool 再造）：** `ConfigCsvRepository` 追加加载 `Manufacture_SoulConfig` / `ClassConfig` / `GemConfig` / `RaceConfig` / `BodyPartConfig` / `BodyAppearanceConfig` / `ExtraEquipmentConfig` / `GemSuffixNameConfig`。规则层纯 C# `ManufactureService` 持有 15 个严格槽位（头1/躯干1/臂2/腿2/灵魂1/宝石6/坐骑1/翅膀1），按 Id 解析所属表自动路由到合法空槽，类型不符 / 同类型宝石重复 / 库存不足即拒绝；每次槽位变化重算预览（`Base(S)=Σ StatBonus`、`Equip`、`GemMult`、`RaceAdjust`、`StaticStat`、静态 `MaxHP=ceil(BodyLife+StaticStat(Str)×3)`、`TotalSpiritCost`、`ControlPowerCost`、试算种族与外观）。「制造」闸门 = 最低要求（躯干+臂2+腿2+灵魂）且 `SpiritEssence ≥ TotalSpiritCost`（头/宝石/坐骑/翅膀对**提交**仍可选）。制造成功写入 `WarriorInstance.SourceItemIds` + `SourceSpiritCost`；`TryRemanufacture(sourceWarriorId)` 按配方后台校验/扣料/再跑聚合与掷种族外观流水线并 `_pool.Add` **新实例**（不改 `_slots`）；材料不足 / 精魂不足错误码供 Tips。**躯体外观可视预览闸门**（表现层）：除宝石外全部槽位已填（含头/坐骑/翅膀）→ Instantiate 试算 `AppearanceId` Prefab 并 `WarriorAnimView` 播攻击再待机；否则静态占位图。`WarehouseService` 扩展同前；Debug「注入制造套件」同前。表现：`ManufacturePanelView` — PreviewPanel 左、中心环绕槽位方格（左：头/臂1/腿1/翅膀；右：躯干/臂2/腿2/坐骑；预览内底灵魂；下排半尺寸宝石×6）、PoolPanel 右为 **ScrollRect 士兵框列表**（`PoolSoldierFrameView`；选中显「再造1个」）、UmCanvas 中上部 Tips（1s：「材料不足」/「精魂不足」）、底栏库存方格横滑 + Input 拖拽入槽、三操作钮在库存下。布局权威：`UmAssetBuilder` 重建 StageRoot Prefab。外观资源：`UpgradeManufacturePrefabCatalog` 绑定 `AppearanceId → Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab`。禁止运行时引用 `SmallScaleInt/`。

**UM 布阵区（方案 A→拖拽编辑器，D-032）：** 纯 C# `BattleFormationService`（存档级，`BindSlot` + PlayerPrefs JSON）持有 `{WarriorId, PositionX/Z, RemainingHP}`；`TryDeployAt` / `TrySetPosition` / `TryUndeploy`；控制力占用 = Σ `ControlPowerCost` vs `ControlPowerCap`。表现：共享 Prefab `Assets/Prefabs/Formation/FormationEditorRoot.prefab`（士兵栏 80×80 横滑、拖拽上阵/改位/下阵、左上控制力 HUD、UM「返回」/ Defend「开战」）。UM 主屏全屏制造 + Complete 右「布阵」打开编辑器；地图取关卡内下一 Defend 的 `BattleMapId`（缺省 `Ground_01`）。与 Defend Prepare **共用**。禁止运行时引用 `SmallScaleInt/`。

**战斗模式选关（方案 A，D-044）：** `DefendStageModule` Enter 先 Instantiate `Assets/Prefabs/Defend/BattleModeSelectRoot.prefab`（或运行时等价 UI）；`DefendPhase=ModeSelect`；模式1「保卫战」列出全部 `DefendGameplayConfig`；运作表 `GameplayConfigId` 作 Recommended 默认高亮；确认后用所选行覆盖 `LevelStageContext.DefendConfig` 再进入现有 Prepare。模式2「推图战」列出全部 `PushMapGameplayConfig`；确认后调用 `LevelOperationDriver.TryHandoffModeSelectToPushMap(configId)`：`Exit` 当前 Defend 模块 → 保留 `LevelId`/`StageNumber` 改写 `GameplayType=PushMap` + `PushMapConfig` + 地图路径 → `SetState(PushMap)` → `PushMapStageModule.Enter` → `StageChanged`。禁止运行时引用 `SmallScaleInt/`。

**Defend Prepare / 开战 / 护盾（方案 A→共享编辑器，D-040）：** `DefendStageModule` 在 ModeSelect 确认保卫战后 Instantiate `DefendStageRoot` + `Prefabs/Maps/{BattleMapId}`；Prepare 挂同一 `FormationEditorRoot` UI（复用本阶段地图，不双开地图）；开战 ≥1 → 销毁预览后按布阵正式部署；`Shield`/`CombatDurationSeconds` 逻辑不变。禁止运行时引用 `SmallScaleInt/`。

**Defend 刷怪与寻路（方案 A，D-041）：** `ConfigCsvRepository` 追加加载 `Defend_WaveSpawnConfig.csv` / `Defend_MonsterConfig.csv`。开战时 `DefendSessionService` 按 `WaveConfigId` 装载刷怪行；`Combat` 中每当 `RemainingCombatSeconds` 变为某整秒（含开战瞬间）时，触发尚未触发且 `SpawnRemainingSeconds` 相等的行（同秒按 `SpawnOrder` 升序），经事件交给 View Instantiate。Demo 最小出生点：地图 Prefab 上 `DefendSpawnPointSet` 固定点（`ClockDirection`→钟点位；`RegionRandom`→点池随机；Inside/Outside 本片均用固定点，精确 OutsideMap **后置**）。Instantiate 地图后 Runtime 烘焙最小可走 NavMesh（覆盖地图活动区 + 出生点）。怪物 Prefab：`Assets/Prefabs/Defend/Monsters/{ModelId}.prefab`（有 Art 则 §15.2 `Visual` 组装；否则临时立方体；Catalog 绑定）。`MonsterAgentView`（`NavMeshAgent`）按 `TargetSelect` 选目的地（本片士兵位可作为 PreferWarrior/Nearest 候选；无士兵则回退主角），按 `TargetRetargetIntervalSeconds` 重寻路；进 `AttackRange` 后按 `AttackSpeed` 普攻 → `Shield -= 1`（忽略 AttackPower）。`Shield ≤ 0` → `DefendPhase.Ended` + LevelFailure 钩子（打日志；完整关卡中止见 D-043）。士兵普攻 / 清场胜利 **不做**。禁止运行时引用 `SmallScaleInt/`。

**Defend 士兵近战（方案 A，D-042 近战片 + MP-06）：** `WarriorCombatMath` 按 `ClassConfig.PrimaryStat` + `CombatConvertCoeffs`（缺键回退 §3.12 默认）派生 `NormalAttackPower` / `AttackSpeed`。`DefendSessionService` 开战登记士兵 HP（`MaxHP=ceil(BodyLife+StaticStat(Str)×3)`，`RemainingHP` clamp）与刷怪登记怪物 HP；规则层确认近战 `HitConfirm`（前摇结束且目标仍存活、在 `AttackRange` 内）→ 怪 `HP -= NormalAttackPower`；怪对兵 `AttackPower` 直接扣 HP（无护甲）。`HP≤0` 无宝石 → `CombatDead`（停手）；有宝石 → 立即 PermanentDeath 标记（物资去向见 D-043）。表现：`WarriorAgentView` 仅在 EngageZone 内选最近存活怪；追击 `GoalKind=AttackSlot`（`AttackSlotService`+`MassMoveScheduler` Move）；无候选时非叛变士兵 `GoalKind=FormationHome`（返回途中继续选敌，发现目标即中断返回）；`AttackMode=Melee` 走近战前摇；`WarriorAnimView` 播移动/攻击/死亡（见 §15.5）。清场条件（刷怪行全触发 + 已刷怪全灭）→ `ClearVictoryConditionDetected` 事件/日志（**不**入账、**不**切胜利 Ended；见 D-043）。禁止运行时引用 `SmallScaleInt/`。

**Defend 士兵远程弹道（方案 A，D-042 远程 / 05c2）：** 开战登记同时写入 `ClassConfig.RangedProjectileSpeed` / `RangedTimeoutSeconds`。`AttackMode=Ranged` 士兵与近战共用 EngageZone 最近选敌、`AttackSpeed` 周期与无目标时返回 `FormationHome`；进 `AttackRange` 后 Instantiate 临时 `Assets/Prefabs/Defend/Projectile.prefab`（Catalog 绑定）；开火时 `WarriorAnimView` 播普攻 Trigger。`ProjectileView` 运动学飞向锁定怪 RuntimeId：**距离 ≤ hitRadius** 视为碰撞命中 → Session `TryConfirmRangedHit` → 怪 `HP -= NormalAttackPower`；**超时**销毁且不扣血。法师/射手同远程通道（仅 `PrimaryStat` 不同）。禁止运行时引用 `SmallScaleInt/`。

**Defend 失控开战 roll 与胜负结算（方案 A，D-043）：** `ConfigCsvRepository` 加载 `Combat_LossOfControlConfig.csv`。开战瞬间按布阵 `ΣCost/Cap−1` **锁定** Degree/Tier（超额不挡开战）；`Degree>0` 时对各上阵士兵用 `FinalLossChance=clamp(0,1,TierChance+RaceBonus+ΣGemBonus)`（Demo `ΣSkillBonus=0`）独立 roll → `IsRebel`（日志可观察）。Rebel **不受 EngageZone 限制**，就近打存活主角/其他士兵/敌人；对主角普攻 → `Shield-=1`；对兵/怪走士兵普攻通道。清场条件满足 → `DefendPhase.Ended` + PermanentDeath 最小结算（宝石回仓、清布阵、移出池）→ `ProtagonistProgressService.AddExperience(100)`（Demo 固定阶段经验）→ `LevelOperationDriver.TryAdvanceStage`。`Shield≤0` → Ended + 同 PermanentDeath 结算 → **不**入账本阶段经验 → `AbortLevelAsFailure`（无关卡胜利结算；已有资源保留）。禁止运行时引用 `SmallScaleInt/`。

**PushMap 配置表加载（方案 A，PM-02）：** `ConfigCsvRepository` 追加加载 `PushMap_PushMapGameplayConfig.csv` / `PushMap_PushMapSpawnConfig.csv`；`Defend_MonsterConfig` 解析 `AggroMode` / `AlertRadius`（缺省见 §9.19）。样例至少 1 个 `GameplayConfigId` + 多 Spawn 行（无陷阱/陷阱/BOSS）。StageModule / AI / 占领逻辑 **后置**（PM-03+）。禁止运行时引用 `SmallScaleInt/`。

**PushMap Stage 接线（方案 A，PM-03 + D-044 Mode2）：** `LevelOperationDriver.TryBuildContext` 支持 `GameplayType=PushMap`：`GameplayConfigId` → `PushMapGameplayConfig` 主键（直查），`LevelStageContext.PushMapConfig` + `MapPrefabPaths` 允许 `PushMap_*`（同解析 `Assets/Prefabs/Maps/{MapId}.prefab`）。`PushMapStageModule`（`IStageModule`；无独立 ModeSelect）：`GameplayType=PushMap` 直进 `PushMapPhase=Prepare`；亦可由 Defend `BattleModeSelect` 模式2经 `TryHandoffModeSelectToPushMap` 进入同一模块。薄规则 `PushMapSessionService`（**独立但语义对齐 §3.12**：Prepare→Combat；开战 ≥1 上阵；`Shield=ProtagonistMaxHP`；`Shield≤0`→LevelFailure；开战锁定 Degree/Tier 并对上阵士兵按 `FinalLossChance` roll→Rebel 日志可观察）+ `PushMapStageController`（Instantiate `Maps/{MapId}`，复用共享 `FormationEditorRoot` 同一 BattleFormation）。**Combat 战斗相机：** Runtime Ensure 子物体 `PushMapCamera`（与 Defend 同俯视契约：正交、`Euler(90,0,0)`、高度 `mapCenter+(0,18,0)`、`near=0.1`/`far=100`、SolidColor/`depth=5`；开战默认 **`orthographicSize=2`**，异于 Defend 的 `max(halfExtents)-1.5`）；Prepare 关掉（用 FormationCamera），开战启用并重配；禁止开战落到 Boot 透视主相机。**PM-09 镜头跟随（方案 A）：** `PushMapCameraFollowController` 挂于 `PushMapCamera`；Combat `CameraFollowMode=Auto|Manual`——Auto 粘随距 CurrentObjective 最近忠诚 `PushMapAdvanceView`（失效后重选；全灭定格）；左键拖拽（非 UI）切 Manual 并按正交像素→世界 XZ 平移；StageRoot 下 Runtime Ensure 底中「恢复跟随」按钮（锚点约 `(0.5,0.1)`，仅 Manual 显示）→ `EnterAuto`；滚轮缩放 Size（`mouseScrollDelta.y>0` 拉近变小；步进 `0.5`/档；钳制 `[0.5,20]`；指针在 UI 上跳过）；缩放不切换模式、恢复跟随不重置 Size；高度/旋转不变；规则层不参与。样例：`Level_LevelOperationConfig` `Level_01,4,PushMap,PushMap_01`（直进）与 Defend 阶段 Mode2 选 `PushMap_01`（handoff）等价进入 Prepare。禁止运行时引用 `SmallScaleInt/`。

**PushMap 刷怪与陷阱（方案 A，PM-05）：** `PushMapSessionService` 开战装载 `PushMapSpawnConfig` 行；表现层 **Bake NavMesh → 部署 → `FireStartBattleSpawns`**：无陷阱且关联目标未占 → `PushMapSpawnRequested` 事件（位置由 View 解析）；绑定 `TrapZoneId` 的点 → `TryNotifyTrapEnter` 首次触发；`ObjectiveCaptured` → 该关联点本场停刷（已刷保留）。`PushMapStageController` 收集 `SpawnPoint`/`TrapZone`，Instantiate 怪（含 Boss）入 `_monsters`（`PushMapMonsterAgentView`），Update 探测忠诚兵首次进圈。怪物 AI 暂用 Defend 默认追击语义；对主角扣盾经 `PushMapSessionService.ApplyShieldHit`。AggroMode 四态落地 **PM-06**；BOSS 通关结算后置（PM-07）；**不使用** `WaveSpawnConfig` 倒计时。运行时契约见 §9.23。样例 `TrapZoneId` 对齐为 `TZ_01`。禁止运行时引用 `SmallScaleInt/`。

**PushMap 怪物占地散开（方案 A，PM-10）：** `MonsterConfig.BodyRadius`（缺省 `0.35`）；表现层按半径环形/螺旋错开同点与邻近已刷存活怪落点（`NavMesh.SamplePosition`）；`PushMapMonsterAgentView` 设 `NavMeshAgent.radius = min(BodyRadius, max(0.05, AttackRange − 0.1 − 0.05))`（保证相对士兵 Demo 半径 `0.1` 仍可进入 `AttackRange`）；`Stationary*` 仅依赖刷出占位；Defend 刷怪散开后置。**我方士兵** Demo：`NavMeshAgent.radius=0.1`、`height=0.1`（`WarriorAgentView` / `PushMapAdvanceView`）。禁止运行时引用 `SmallScaleInt/`。

**PushMap AggroMode 四态（方案 A，PM-06）：** `PushMapMonsterAgentView` 按 `config.AggroMode` 分支。`ActiveChase`：忠诚士兵进 `AlertRadius` → **AttackSlot** 追击该兵直至怪死（MP-05；非中心 `SetDestination`）；`PassiveChase`：未挑衅静止，`NotifyProvoked()` 后追击；`StationaryActive`：永不移动，忠诚兵进 `AttackRange` 攻击、离开停；`StationaryPassive`：永不移动，须先 `NotifyProvoked()` 且目标仍在 `AttackRange` 才攻。主动发现与挑衅**仅**对忠诚士兵（`!IsRebel`）。挑衅 Demo 契约：`PushMapStageController` 检测忠诚 `PushMapAdvanceView` 首次进入某被动怪 `AttackRange` → 调其 `NotifyProvoked()`（等效「士兵先攻击」；士兵 HP / 命中结算后置）。命中仍 `AttackMode` 方案 D；主动态对主角不进 `AlertRadius` 主动发现，但已交战命中主角仍 `ApplyShieldHit`。普通怪真实士兵伤害 / 技能施放 / 副本玩法正文 **不做**。禁止运行时引用 `SmallScaleInt/`。

**PushMap BOSS 通关与奖励钩子（方案 A，PM-07）：** `PushMapSessionService` 对 `IsBoss` 刷出行累计待击杀数；`TryNotifyBossKilled` 递减，归零 → `PushMapPhase=Ended` + `VictorySettled(StageExpReward)`；表现层对齐 Defend：`AddExperience` → `_onVictoryAdvance`（`TryAdvanceStage`）。`Shield≤0` → 已有 `LevelFailureRequested`，**不**入账。占领：`CaptureLoot` 经 `LootDropParser`/`Warehouse` 入账（无经验）；`DungeonUnlockIds` 写入 `DungeonUnlockService` 存档集合（PlayerPrefs + 日志可验）；通关同样写解锁钩子。Demo 击杀契约：忠诚兵首次进 BOSS `AttackRange` → `NotifyKilled` + `TryNotifyBossKilled`。`IsBoss` 与 `BossPoint` 一致性：缺标记 warn。副本玩法正文 / 完整失败 UI **不做**。禁止运行时引用 `SmallScaleInt/`。

**PushMap 空气墙 NavMesh（方案 A，PM-08）：** 开战 Runtime Bake 在 IsoDiamond 可走面之外，收集地图 `AirWall`，以 `NavMeshBuildSourceShape.Box` + area=`Not Walkable` 注入（尺寸=`HalfExtents×2`；`Matrix4x4.TRS(position, rotation, 1)` → **含 Y 轴 45°**）。扩展 `DefendNavMeshBaker.Bake(..., notWalkableBoxes)`；`PushMapStageController` 开战传入；敌我 `NavMeshAgent`（士兵推进 / 怪物追击）均不可穿。**不做** `NavMeshObstacle` Carve、复杂多层障碍 polish。契约见 §9.22。禁止运行时引用 `SmallScaleInt/`。

**大规模战斗寻路（方案 B，MassCombatPathing / SPEC 已锁）：** 共享目标 **FlowField** + 追击 **AttackSlot** + 友军 **LocalDetour**；容量双方约 200；静态 `AirWall`/可走掩码进场；友军禁止 Carve。实现切片见 `.scratch/mass-pathing/issues/`；运行时契约见 §9.7。**MP-04：** PushMap 忠诚推进已接 FlowField+LocalDetour+`MassMoveScheduler`。**MP-05：** 交战/追击已接 `AttackSlotService`（士兵+怪；槽刷新≤50/帧；无全员每帧 `CalculatePath`）。**MP-06：** Defend `WarriorAgentView`/`MonsterAgentView` 对等接线；忠诚无 Engage 目标→`GoalKind=FormationHome`；追击走槽位；与 PushMap 共用目的地语义。**MP-07：** Debug 压测入口 `MassPathingPerfStress` / `MassPathingPerfStressView`（约 200+200 桩单位 + Stopwatch）；超预算回退见 §9.7。禁止运行时引用 `SmallScaleInt/`。

**架构提示：** `ToolsPanel` 属 Meta 壳层 UI；玩法状态由规则层持有，View 只订阅展示（见 §13）。挖坟：规则层负责生成、计时、DigAction 触发/忙碌锁与扣血；菱形地图与圆圈光标、帧动画、奖励飞向为主角由 View 表现；逻辑层为整体可放置空间（非格子）。UM 阶段不查玩法配置表主键；升级进度本片内存持有。Defend：规则层输出目标/目的地；移动服务执行（规模栈见 §9.7）；Demo 最小可走面见 §9.7 / SPEC_03 §3.12。

### English

**Status: Aligned with SPEC_03 §3.8 (Meta shell + Level-pipeline vertical slice)**

**In scope (D-001–D-044):** 3 fixed slots with local occupied flag + minimal pipeline fields; InSaveShell default Dig placeholder + floating Tools; sample Level start from shell (D-003/D-010); CSV-only LevelOperation drive Dig→UM→Defend; Dig / UM / Defend verticals per §3.8 (temp art OK); UM stage `GameplayConfigId` **ignored** (§9.1); Defend ModeSelect gate (D-044); Defend Demo-min spawn + NavMesh.

**Out of scope:** Full skill casts / effect tables; formal art polish; exact OutsideMap geometry; **full** save schema (Warehouse/Exp/Tech still TBD; warrior pool + formation locked this slice); concrete TechTree node values/icon polish & full feature-system enum; future Tools entries; bake full §9 column/type validation (Demo: filename + header only); anything not in §3.8.

**Persistence intent:** Local by slot index `0..2`. **Demo Meta locked: `PlayerPrefs`**. Keys:
- `Gravedigger2026.SaveSlot.{0|1|2}.Occupied` (`0`/`1`)
- `Gravedigger2026.SaveSlot.{i}.WarriorPool` — JSON: `NextSerial` + `WarriorInstance` manufacture snapshot array (incl. `SourceItemIds` / `SourceSpiritCost` / `EquipStats` / `BodyLife`; see §9.9)
- `Gravedigger2026.SaveSlot.{i}.BattleFormation` — JSON: `{WarriorId, PositionX, PositionZ, RemainingHP}[]`
- `Gravedigger2026.SaveSlot.{i}.DungeonUnlocks` — pipe-separated dungeon unlock IDs (existing)

**Warrior pool / formation persistence (Approach A):** `WarriorPoolService` / `BattleFormationService` each `BindSlot(slot)` on enter-save, `ClearBound` on return to SaveSelect; mutate → immediate `PlayerPrefs` write; delete slot → `DeleteSlotData` clears keys. Enter order: bind pool then formation; drop formation rows whose `WarriorId` is missing from pool. Warehouse / Exp / Tech full schema still **TBD**.

**Meta shell (Approach A, D-001–D-004):** Single scene `Assets/Scenes/Boot.unity`; SaveSelect / InSaveShell via Canvas Prefab show/hide (`Assets/Prefabs/Meta/`, `Assets/Prefabs/UI/`). Rules: `SaveSlotService` + `GameplayStateService`; Views subscribe only. Tools Settings → **opens TechTree canvas** (UI-012 below); Tools Level → **starts sample `Level_01`** (see Level driver below). Formal shell three-state switch still **TBD**; Demo temp **Debug cycle** on InSaveShell for hand-checking D-004 (must not equal Tools Level); **Debug advance stage** for D-010 (placeholder end → next / VictorySettlement).

**Level driver (Approach A, D-010):** `ConfigCsvRepository` reads CSV only (paths: [§14.5](#145-runtime-csv-load-paths-demo)); `LevelOperationDriver` loads rows by `LevelId`, runs ascending `StageNumber`; sets `GameplayState` and calls `IStageModule` enter/leave hooks (Dig / UM / Defend below; warrior combat and win/lose still later). When `GameplayType=UpgradeManufacture`, **ignore** `GameplayConfigId` (no Dig/Defend lookup). Dig/Defend rows validate `DigMapId` / `BattleMapId` ∈ `Ground_01`…`Ground_05` and resolve `Assets/Prefabs/Maps/{Id}.prefab`. UI/log must show LevelId, StageNumber, GameplayType.

**Dig vertical (Approach A, D-020):** `DigStageModule` (`IStageModule`) on Enter instantiates `Assets/Prefabs/Maps/{DigMapId}.prefab` and mounts `DigStageRoot` (`Assets/Prefabs/Dig/`). Rules: pure-C# `DigSessionService` owns effective-duration countdown, initial/process spawn, DigAction dwell + busy lock, damage, Warehouse/Spirit credit, stage reward aggregate; `DigProtagonistCapabilities` injected from **TechTree** recalc (see UI-012). DigAction candidates: cursor circle ∩ grave `DigHitShape` local-XZ convex hull (world-transformed; broadphase `BoundingRadius`); no hull → fall back to obstacle circle. Presentation: `DigPrefabCatalog` binds Digger / `Grave_{QualityId}` / map variants / `UiDigCursorRing`; circle cursor (Prefab dual-layer, fixed-pixel stroke), grave HP styles, DigReward fly-to, DigStageSummary via Views. Duration 0 → cancel in-progress DigAction (no damage) → DigStageSummary confirm → `LevelOperationDriver.TryAdvanceStage`. Do not runtime-reference `SmallScaleInt/`; rules must not read Sprite/pixels.

**TechTree canvas (Approach A, UI-012 optional):** `ConfigCsvRepository` additionally loads `Tech_TechTreeConfig.csv` / `Tech_TechEffectConfig.csv`. Rules: pure-C# `TechTreeService` (save-scoped on Meta shell) holds learned set + `UnlockedFeatureSystems`; on enter-save/`Reset`, auto-learns `InitiallyUnlocked` and applies effects; learn gate = not learned ∧ TechPoint≥LearnCost ∧ ≥1 learned prerequisite (inverse of `UnlockNextTechIds`); on learn spend → mark → parse additive `AttributeModifiers` → write `DigProtagonistCapabilities` (Demo keeps `DiggableQualityIds` = all grave qualities for Dig hand-check). Presentation: temp `Assets/Prefabs/Meta/TechTreeCanvasRoot.prefab` (node positions on Prefab; uGUI LMB-drag pan; hover name+effect desc; edges from forward ids; three-state frame colors); Tools Settings opens canvas; Debug can inject TechPoints. Do not runtime-reference `SmallScaleInt/`.

**UM upgrade panel (Approach A, D-030):** `UpgradeManufactureStageModule` on Enter instantiates `Assets/Prefabs/UpgradeManufacture/UpgradeManufactureStageRoot.prefab` (**full-screen manufacture by default**; upgrade as Modal via top "GM Upgrade", close with top-right "X"; formation via Formation button → shared editor). `ConfigCsvRepository` loads `Manufacture_ProtagonistLevelConfig.csv`. Rules: pure-C# `ProtagonistProgressService` holds in-memory `Level` / `LifetimeExperience` / `TechPoints` / effective `ControlPowerCap` / `ProtagonistMaxHP`; cumulative-threshold chain level-ups apply row rewards/caps. Debug inject remains; formal Defend victory credit in D-043. Bottom Complete → `TryAdvanceStage`. Do not runtime-reference `SmallScaleInt/`.

**UM manufacture panel (Approach A, D-031 → UI redo / pool remake):** `ConfigCsvRepository` additionally loads `Manufacture_SoulConfig` / `ClassConfig` / `GemConfig` / `RaceConfig` / `BodyPartConfig` / `BodyAppearanceConfig` / `ExtraEquipmentConfig` / `GemSuffixNameConfig`. Rules: pure-C# `ManufactureService` owns the 15 strict slots (Head1/Torso1/Arm2/Leg2/Soul1/Gem6/Mount1/Wing1), routes an Id to a legal empty slot by resolving its source table, and rejects on type mismatch / duplicate `GemType` / insufficient stock; every slot change recomputes the preview (`Base(S)=Σ StatBonus`, `Equip`, `GemMult`, `RaceAdjust`, `StaticStat`, static `MaxHP=ceil(BodyLife+StaticStat(Str)×3)`, `TotalSpiritCost`, `ControlPowerCost`, trial Race + Appearance). Manufacture gate = min parts (Torso+2Arm+2Leg+Soul) and `SpiritEssence ≥ TotalSpiritCost` (Head/gems/Mount/Wing still optional for **commit**). On success write `WarriorInstance.SourceItemIds` + `SourceSpiritCost`; `TryRemanufacture(sourceWarriorId)` validates/consumes from recipe in background, re-runs aggregate + Race/Appearance roll, `_pool.Add` **new** instance (does not mutate `_slots`); material/Spirit shortage error codes feed Tips. **Visual BodyAppearance gate** (presentation): all non-gem slots filled (incl. Head/Mount/Wing) → Instantiate trial `AppearanceId` Prefab and drive `WarriorAnimView` attack-then-idle; else static placeholder. `WarehouseService` / Debug kit unchanged. Presentation: `ManufacturePanelView` — PreviewPanel left, center slot ring (left: Head/Arm1/Leg1/Wing; right: Torso/Arm2/Leg2/Mount; Soul inside preview bottom; half-size gems×6 below), PoolPanel right as **ScrollRect soldier-frame list** (`PoolSoldierFrameView`; selected shows「Remake×1」), upper-center Tips on UmCanvas (1s:「材料不足」/「精魂不足」), bottom inventory square bar + Input drag into slots, three action buttons under inventory. Layout authority: `UmAssetBuilder` rebuilds StageRoot Prefab. Appearance: `UpgradeManufacturePrefabCatalog` binds `AppearanceId → Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab`. Do not runtime-reference `SmallScaleInt/`.

**UM formation panel (Approach A→drag editor, D-032):** Pure-C# `BattleFormationService` (save-scoped, `BindSlot` + PlayerPrefs JSON) holds `{WarriorId, PositionX/Z, RemainingHP}`; `TryDeployAt` / `TrySetPosition` / `TryUndeploy`; ControlPower = Σ `ControlPowerCost` vs `ControlPowerCap`. Presentation: shared Prefab `Assets/Prefabs/Formation/FormationEditorRoot.prefab` (80×80 soldier bar scroll, drag deploy/reposition/undeploy, top-left ControlPower HUD, UM Return / Defend StartBattle). UM main = full-screen manufacture + Formation right of Complete; map = next Defend `BattleMapId` in Level (fallback `Ground_01`). Shared with Defend Prepare. Do not runtime-reference `SmallScaleInt/`.

**Battle ModeSelect (Approach A, D-044):** `DefendStageModule` Enter first instantiates `Assets/Prefabs/Defend/BattleModeSelectRoot.prefab` (or runtime-equivalent UI); `DefendPhase=ModeSelect`; Mode1「保卫战」lists all `DefendGameplayConfig`; LevelOperation `GameplayConfigId` = Recommended default highlight; on confirm overwrite `LevelStageContext.DefendConfig` then enter existing Prepare. Mode2「推图战」lists all `PushMapGameplayConfig`; on confirm call `LevelOperationDriver.TryHandoffModeSelectToPushMap(configId)`: `Exit` current Defend module → keep `LevelId`/`StageNumber`, rewrite `GameplayType=PushMap` + `PushMapConfig` + map path → `SetState(PushMap)` → `PushMapStageModule.Enter` → `StageChanged`. Do not runtime-reference `SmallScaleInt/`.

**Defend Prepare / StartBattle / Shield (Approach A→shared editor, D-040):** After ModeSelect confirms Mode1, `DefendStageModule` instantiates `DefendStageRoot` + `Prefabs/Maps/{BattleMapId}`; Prepare hosts same `FormationEditorRoot` UI on that map (no second map); StartBattle ≥1 → destroy preview then formal deploy; Shield/countdown unchanged. Do not runtime-reference `SmallScaleInt/`.

**Defend spawn + path (Approach A, D-041):** `ConfigCsvRepository` additionally loads `Defend_WaveSpawnConfig.csv` / `Defend_MonsterConfig.csv`. On StartBattle, `DefendSessionService` loads rows for `WaveConfigId`; in `Combat`, whenever `RemainingCombatSeconds` becomes a whole second (including StartBattle instant), fires unfired rows with matching `SpawnRemainingSeconds` (`SpawnOrder` ascending within the same second) via events to View. Demo-min spawn: fixed `DefendSpawnPointSet` on map Prefab (`ClockDirection`→clock markers; `RegionRandom`→pool pick; Inside/Outside both use fixed points this slice; exact OutsideMap **deferred**). After map instantiate, runtime-bake a minimal walkable NavMesh covering activity area + spawn points. Monster Prefabs: `Assets/Prefabs/Defend/Monsters/{ModelId}.prefab` (§15.2 `Visual` when Art ready; else temp cubes; Catalog-bound). `MonsterAgentView` (`NavMeshAgent`) picks destination by `TargetSelect` (warrior transforms are PreferWarrior/Nearest candidates; fall back to protagonist), repaths on `TargetRetargetIntervalSeconds`; when in `AttackRange`, normal-attacks at `AttackSpeed` → `Shield -= 1` (ignore AttackPower). `Shield ≤ 0` → `DefendPhase.Ended` + LevelFailure hook (log; full Level abort in D-043). Soldier attacks / clear-spawn victory **out of scope**. Do not runtime-reference `SmallScaleInt/`.

**Defend warrior melee (Approach A, D-042 melee slice + MP-06):** `WarriorCombatMath` derives `NormalAttackPower` / `AttackSpeed` from `ClassConfig.PrimaryStat` + `CombatConvertCoeffs` (missing keys → §3.12 defaults). `DefendSessionService` registers warrior HP at StartBattle (`MaxHP=ceil(BodyLife+StaticStat(Str)×3)`, RemainingHP clamped) and monster HP on spawn; rules confirm melee `HitConfirm` (windup end + target alive + in `AttackRange`) → monster `HP -= NormalAttackPower`; monster→warrior uses `AttackPower` directly (no armor). `HP≤0` without gems → `CombatDead` (stop acting); with gems → immediate PermanentDeath mark (material fate in D-043). Presentation: `WarriorAgentView` picks nearest living monster inside EngageZone; chase via `GoalKind=AttackSlot` (`AttackSlotService`+`MassMoveScheduler` Move); when none, loyal soldiers use `GoalKind=FormationHome` (keep retargeting; abort return on new target); `AttackMode=Melee` uses windup; `WarriorAnimView` plays move/attack/death (§15.5). Clear condition (all wave rows fired + all spawned monsters dead) → `ClearVictoryConditionDetected` event/log (**no** Exp credit, **no** victory Ended; see D-043). Do not runtime-reference `SmallScaleInt/`.

**Defend warrior ranged projectile (Approach A, D-042 ranged / 05c2):** StartBattle registration also stores `ClassConfig.RangedProjectileSpeed` / `RangedTimeoutSeconds`. `AttackMode=Ranged` shares EngageZone nearest targeting, `AttackSpeed` cadence, and no-target return to `FormationHome` with melee; when in `AttackRange`, Instantiate temp `Assets/Prefabs/Defend/Projectile.prefab` (Catalog-bound); fire also triggers `WarriorAnimView` attack. `ProjectileView` flies kinematically toward locked monster RuntimeId: **distance ≤ hitRadius** = collision hit → Session `TryConfirmRangedHit` → monster `HP -= NormalAttackPower`; **timeout** destroys with no damage. Mage/Archer share the same ranged channel (`PrimaryStat` only differs). Do not runtime-reference `SmallScaleInt/`.

**Defend LossOfControl StartBattle roll + win/lose settle (Approach A, D-043):** `ConfigCsvRepository` loads `Combat_LossOfControlConfig.csv`. At StartBattle lock Degree/Tier from formation `ΣCost/Cap−1` (overflow does not block StartBattle); when `Degree>0`, each deployed soldier rolls once with `FinalLossChance=clamp(0,1,TierChance+RaceBonus+ΣGemBonus)` (Demo `ΣSkillBonus=0`) → `IsRebel` (logged). Rebels **ignore EngageZone**, pick nearest living protagonist / other soldiers / enemies; normal hit on protagonist → `Shield-=1`; hits on soldiers/monsters use soldier attack channel. Clear condition → `DefendPhase.Ended` + minimal PermanentDeath (gems→warehouse, clear formation, remove pool) → `ProtagonistProgressService.AddExperience(100)` (Demo fixed stage Exp) → `LevelOperationDriver.TryAdvanceStage`. `Shield≤0` → Ended + same PermanentDeath settle → **no** stage Exp → `AbortLevelAsFailure` (no VictorySettlement; keep already-owned). Do not runtime-reference `SmallScaleInt/`.

**PushMap config load (Approach A, PM-02):** `ConfigCsvRepository` additionally loads `PushMap_PushMapGameplayConfig.csv` / `PushMap_PushMapSpawnConfig.csv`; `Defend_MonsterConfig` parses `AggroMode` / `AlertRadius` (defaults §9.19). Sample ≥1 `GameplayConfigId` + spawn rows (non-trap / trap / BOSS). StageModule / AI / Capture **deferred** (PM-03+). Do not runtime-reference `SmallScaleInt/`.

**PushMap stage wiring (Approach A, PM-03 + D-044 Mode2):** `LevelOperationDriver.TryBuildContext` supports `GameplayType=PushMap`: `GameplayConfigId` → `PushMapGameplayConfig` PK (direct lookup), `LevelStageContext.PushMapConfig`, and `MapPrefabPaths` accepts `PushMap_*` (same `Assets/Prefabs/Maps/{MapId}.prefab` resolve). `PushMapStageModule` (`IStageModule`; no in-stage ModeSelect): `GameplayType=PushMap` enters `PushMapPhase=Prepare` directly; also reachable from Defend `BattleModeSelect` Mode2 via `TryHandoffModeSelectToPushMap`. Thin rule `PushMapSessionService` (**separate but semantically aligned with §3.12**: Prepare→Combat; StartBattle ≥1 deployed; `Shield=ProtagonistMaxHP`; `Shield≤0`→LevelFailure; locks Degree/Tier at StartBattle and rolls each deployed soldier's `FinalLossChance`→Rebel, log-observable) + `PushMapStageController` (instantiates `Maps/{MapId}`, reuses shared `FormationEditorRoot` on the same BattleFormation). **Combat camera:** runtime Ensure child `PushMapCamera` (same top-down contract as Defend: orthographic, `Euler(90,0,0)`, height `mapCenter+(0,18,0)`, `near=0.1`/`far=100`, SolidColor/`depth=5`; StartBattle default **`orthographicSize=2`**, unlike Defend's `max(halfExtents)-1.5`); disable in Prepare (FormationCamera), enable+repose on StartBattle; must not fall back to Boot perspective Main Camera. **PM-09 camera follow (Approach A):** `PushMapCameraFollowController` on `PushMapCamera`; Combat `CameraFollowMode=Auto|Manual` — Auto sticky-follows closest loyal `PushMapAdvanceView` to CurrentObjective (repick on invalid; freeze when none); LMB drag (not over UI) → Manual with ortho pixel→world XZ pan; StageRoot runtime Ensure bottom-center ResumeFollow button (anchor ≈`(0.5,0.1)`, Manual-only) → `EnterAuto`; scroll-wheel zooms Size (`mouseScrollDelta.y>0` zoom-in smaller; step `0.5`/notch; clamp `[0.5,20]`; skip when pointer over UI); zoom does not switch mode; ResumeFollow does not reset Size; height/rotation unchanged; rules layer not involved. Sample: `Level_01,4,PushMap,PushMap_01` (direct) and Defend Mode2 pick `PushMap_01` (handoff) both enter Prepare. Do not runtime-reference `SmallScaleInt/`.

**PushMap spawn & trap (Approach A, PM-05):** `PushMapSessionService` loads `PushMapSpawnConfig` rows at StartBattle; View order **Bake NavMesh → deploy → `FireStartBattleSpawns`**: non-trap rows with uncaptured linked objective → `PushMapSpawnRequested` (position resolved by View); trap-bound points → `TryNotifyTrapEnter` first-enter; `ObjectiveCaptured` → linked points stop spawning (living kept). `PushMapStageController` collects `SpawnPoint`/`TrapZone`, instantiates monsters (incl. Boss) into `_monsters` (`PushMapMonsterAgentView`), and polls loyal soldiers for first trap entry. Monster AI uses Defend default-chase semantics; protagonist shield via `PushMapSessionService.ApplyShieldHit`. AggroMode four-state lands in **PM-06**; Boss-clear settlement deferred (PM-07); **no** `WaveSpawnConfig` countdown. Runtime contract: §9.23. Sample `TrapZoneId` aligned to `TZ_01`. Do not runtime-reference `SmallScaleInt/`.

**PushMap monster footprint spread (Approach A, PM-10):** `MonsterConfig.BodyRadius` (default `0.35`); View staggers same-point / nearby living footprints via ring/spiral + `NavMesh.SamplePosition`; `PushMapMonsterAgentView` sets `NavMeshAgent.radius = min(BodyRadius, max(0.05, AttackRange − 0.1 − 0.05))` so centers can still enter `AttackRange` vs soldier Demo radius `0.1`; `Stationary*` rely on spawn placement only; Defend spawn spread deferred. **Loyal soldiers** Demo: `NavMeshAgent.radius=0.1`, `height=0.1` (`WarriorAgentView` / `PushMapAdvanceView`). Do not runtime-reference `SmallScaleInt/`.

**PushMap AggroMode four-state (Approach A, PM-06):** `PushMapMonsterAgentView` branches on `config.AggroMode`. `ActiveChase`: loyal soldier enters `AlertRadius` → **AttackSlot** chase that soldier until monster death (MP-05; not center `SetDestination`). `PassiveChase`: idle until `NotifyProvoked()`, then chase. `StationaryActive`: never moves; attacks loyal soldier inside `AttackRange`, stops on leave. `StationaryPassive`: never moves; attacks only after `NotifyProvoked()` and target still in `AttackRange`. Active detection + provocation are **loyal-only** (`!IsRebel`). Provocation Demo contract: `PushMapStageController` fires a loyal `PushMapAdvanceView`'s first entry into a passive monster's `AttackRange` → `NotifyProvoked()` (stands in for "soldier attacks first"; soldier HP / hit settlement still deferred). Hits keep `AttackMode` scheme D; active stances do not proactively detect the protagonist via `AlertRadius`, but an engaged protagonist hit still applies `ApplyShieldHit`. Real soldier damage on normal monsters / skill casts / dungeon gameplay body **not** done. Do not runtime-reference `SmallScaleInt/`.

**PushMap Boss clear & reward hooks (Approach A, PM-07):** `PushMapSessionService` tracks pending count from fired `IsBoss` rows; `TryNotifyBossKilled` decrements → at 0: `PushMapPhase=Ended` + `VictorySettled(StageExpReward)`; presentation aligns with Defend: `AddExperience` → `_onVictoryAdvance` (`TryAdvanceStage`). `Shield≤0` → existing `LevelFailureRequested`, **no** Exp. Capture: credit `CaptureLoot` via `LootDropParser`/`Warehouse` (no Exp); write `DungeonUnlockIds` into `DungeonUnlockService` save set (PlayerPrefs + log-verifiable); Boss-clear also writes unlocks. Demo kill contract: first loyal entry into Boss `AttackRange` → `NotifyKilled` + `TryNotifyBossKilled`. Missing `BossPoint` with `IsBoss` → warn. Dungeon gameplay body / full failure UI **not** done. Do not runtime-reference `SmallScaleInt/`.

**PushMap AirWall NavMesh (Approach A, PM-08):** StartBattle runtime bake, in addition to the IsoDiamond walkable mesh, collects map `AirWall`s and injects `NavMeshBuildSourceShape.Box` + area=`Not Walkable` (size=`HalfExtents×2`; `Matrix4x4.TRS(position, rotation, 1)` → **incl. Y 45°**). Extends `DefendNavMeshBaker.Bake(..., notWalkableBoxes)`; `PushMapStageController` passes walls at StartBattle; both factions' `NavMeshAgent`s (soldier advance / monster chase) cannot path through. **No** `NavMeshObstacle` Carve or multi-layer obstacle polish. Contract: §9.22. Do not runtime-reference `SmallScaleInt/`.

**Mass combat pathing (Approach B, MassCombatPathing / SPEC locked):** shared-goal **FlowField** + chase **AttackSlot** + friendly **LocalDetour**; ~200/side; static AirWall/walkable mask into field; no friendly Carve. Impl slices: `.scratch/mass-pathing/issues/`; runtime contract §9.7. **MP-04:** PushMap loyal advance wired to FlowField+LocalDetour+`MassMoveScheduler`. **MP-05:** chase/engage wired to `AttackSlotService` (soldiers+monsters; slot refresh ≤50/frame; no all-units per-frame `CalculatePath`). **MP-06:** Defend `WarriorAgentView`/`MonsterAgentView` parity; loyal no Engage target→`GoalKind=FormationHome`; chase uses slots; same GoalKind semantics as PushMap. **MP-07:** Debug stress entry `MassPathingPerfStress` / `MassPathingPerfStressView` (~200+200 stubs + Stopwatch); over-budget fallbacks in §9.7. Do not runtime-reference `SmallScaleInt/`.

**Architecture note:** ToolsPanel is Meta shell UI; gameplay state owned by rules layer; View subscribes only (§13). Dig: rules owns spawn/timer/DigAction/busy/damage; diamond map, circle cursor, dig anims, DigReward fly-to are View; continuous placeable space. UM stages do not resolve mode-config PKs; upgrade progress is in-memory this slice. Defend: rules outputs target/destination; move service executes (mass stack §9.7); Demo-min walkable surface in §9.7 / SPEC_03 §3.12.

---

## 7. 版本控制

### 简体中文

- Unity 适用 `.gitignore`；不提交 `Library/`、密钥、本地用户设置

### English

- Unity-appropriate `.gitignore`; never commit secrets or `Library/`

---

## 8. 运行时本地化（建议）

### 简体中文

**状态：可选 / 建议启用（待项目确认）**

### English

**Status: Optional / recommended (pending project decision)**

---

## 9. 配置表（关卡运作 / 挖坟 / 坟墓品质 / 材料 / 货币 / 挖坟能力 / 防守 / 刷怪波次 / 怪物 / 主角升级 / 灵魂 / 宝石 / 种族 / 制造部件 / 躯体外观 / 科技树 / 失控 / 技能骨架 / 推图战）

### 简体中文

**状态：已定义字段与编码；配置载体已关闭** — 表驱动统一为 **Excel 源 + CSV 产物**（路径 / 命名 / 打表见 [§14](#14-配置表工程约定与打表工具)）。非表型单例调参仍可用 ScriptableObject（`Assets/Settings/<模块>/`，见 [§13](#13-资源编排与可扩展性)）。

规则语义权威：[SPEC_03 §3.9](SPEC_03_GameRules.md)、[§3.10](SPEC_03_GameRules.md)、[§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md)、[§3.13](SPEC_03_GameRules.md)。

逻辑表名（短名，如 `DigGameplayConfig`）用于 SPEC / 伪代码 / 类型标识；**磁盘文件名**见各小节「磁盘名」行与 [§14](#14-配置表工程约定与打表工具)（Excel：`系统中文_表中文_系统英文_表英文`；CSV：`系统英文_表英文`）。

#### 加权字段通用规则

凡配置中的 **权重值（Weight）** 均适用：

| 规则 | 说明 |
|------|------|
| 非负 | `Weight` 须 ≥ 0；负值视为非法配置（加载时拒绝或忽略该段并打日志，实现时二选一并回写） |
| 零权重剔除 | `Weight = 0` → **强制认定该项不存在**：解析后剔除，不参与加权随机 |
| 有效项 | 仅 `Weight > 0` 的项进入加权池；按权重占比抽取 |
| 空有效列表 | 默认语义由各玩法写明。Dig / `GraveSpawnWeights`：过滤后为空 → **放弃该次生成**（见 [SPEC_03 §3.10](SPEC_03_GameRules.md)） |

#### 9.1 关卡运作表 `LevelOperationConfig`

**磁盘名：**
- **Excel：** `关卡_关卡运作表_Level_LevelOperationConfig.xlsx`
- **CSV：** `Level_LevelOperationConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| LevelId | 关卡ID | `string` 或 `int` | 同 ID 多行 = 该关全部阶段 |
| StageNumber | 阶段编号 | `int` | 同关卡内升序执行；建议同关卡内唯一 |
| GameplayType | 玩法类型 | `enum` / `string` | 如 `Dig` / `UpgradeManufacture` / `Defend` / `PushMap` |
| GameplayConfigId | 玩法配置ID | `string` 或 `int` | **Dig** → `DigGameplayConfig` 主键；**Defend** → **RecommendedConfigId**（ModeSelect 默认高亮；见 [SPEC_03 §3.12](SPEC_03_GameRules.md) D-044）；**PushMap** → `PushMapGameplayConfig` 主键；**UpgradeManufacture** → **忽略**（可不空；运行时**不**查表、**不**解析为 Dig/Defend/PushMap 行；本阶段读全局表如 `ProtagonistLevelConfig` 等）。**不另开** `UpgradeManufactureGameplayConfig`（见 [SPEC_03 §3.9](SPEC_03_GameRules.md)） |

```
LevelOperationConfig {
  LevelId: Id
  StageNumber: int
  GameplayType: Dig | UpgradeManufacture | Defend | PushMap | ...
  GameplayConfigId: Id   // ignored when GameplayType = UpgradeManufacture; PushMap → PushMapGameplayConfig
}
```

#### 9.2 挖坟配置表 `DigGameplayConfig`

**磁盘名：**
- **Excel：** `挖坟_挖坟配置表_Dig_DigGameplayConfig.xlsx`
- **CSV：** `Dig_DigGameplayConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| GameplayConfigId | 玩法配置ID | `string` 或 `int` | 主键；被关卡运作表引用 |
| DigMapId | 挖坟地图ID | `string` | Prefab 逻辑名（无路径、无扩展名）；合法值 **`Ground_01`…`Ground_05`**；运行时解析 → `Assets/Prefabs/Maps/{DigMapId}.prefab`；与 Defend `BattleMapId` 共用同一地面变体池；表现 = **Isometric Tilemap**（Tile/Sprite 在 `Assets/Art/Maps/Tiles/`，自 Example `Environment/Tiles`+`Sprites` 复制）；禁止运行时引用 `SmallScaleInt/`，见 [§13](#13-unity-资源编排与可扩展性约定) / [§15](#15-角色美术管线character-creator-烘焙整角)） |
| LevelDurationSeconds | 关卡时长限制 | `float` 或 `int` | **基础**时长（秒）；有效倒计时 = 本字段 + `DigStageDurationBonus`（见 [SPEC_03 §3.10](SPEC_03_GameRules.md) / §9.6） |
| InitialGraveCount | 开局基础生成坟墓数量 | `int` | 开局独立加权随机次数 N（≥ 0） |
| SpawnRate | 倒计时过程中生成坟墓速率 | 见编码 | 每 N 秒生成 M 个 |
| GraveSpawnWeights | 坟墓出现概率权重 | 见编码 | 品质 ID + 权重列表 |

**`SpawnRate` 编码（固定）：** `N;M`

- `N`：间隔秒数（> 0）
- `M`：该间隔内生成数量（≥ 0）
- 示例：`5;2` → 每 5 秒生成 2 座坟

**`GraveSpawnWeights` 编码（固定）：** `QualityId;Weight|QualityId;Weight|...`

- 段分隔符：`|`
- 段内：`坟墓品质ID;出现权重`
- 权重遵循上方 **加权字段通用规则**：`Weight = 0` 的段剔除；仅对 `Weight > 0` 按占比抽取
- 过滤后有效列表为空 → **放弃该次生成**（开局少一座 / 过程跳过一座；不打断阶段）
- `QualityId` 须能在 `GraveQualityConfig`（§9.3）中解析
- 示例：`1;10|2;5|3;1`；`1;0|2;0` → 有效列表空 → 放弃该次生成

```
DigGameplayConfig {
  GameplayConfigId: Id
  DigMapId: string             // Ground_01…Ground_05 → Assets/Prefabs/Maps/{Id}.prefab
  LevelDurationSeconds: number
  InitialGraveCount: int
  SpawnRate: "N;M"          // every N seconds spawn M
  GraveSpawnWeights: "Q;W|Q;W|..."
}
```

**加权随机：** 先按通用规则得到有效权重列表，再做一次独立抽取（开局 N 次各抽一次；过程生成的每一座各抽一次）。抽取算法实现细节不绑定具体 RNG API。

**落点：** 在 DigMap 整体可放置区域内采样；须避开 `DigObstacle`（Digger + 未消除 Grave）的圆形障碍半径（半径在对应 Prefab 上配置）。单次生成采样失败最多重试 **32** 次，仍失败则放弃该次生成。

**Dig Prefab 约定：** `Assets/Prefabs/Dig/` 下 Digger 与各品质 Grave 预制体暴露圆形障碍半径（`DigObstacleRadius`）；每种 `QualityId` 对应专属 Grave Prefab。Grave 根另挂 `DigHitShape`：本地 XZ 凸包顶点（≤12）+ `BoundingRadius`，由 Editor 菜单 `Gravedigger2026/Dig/Bake All Grave Hit Shapes` 离线烘焙（优先 `Sprite.GetPhysicsShape`，否则 alpha 扫边 → 凸包 → 简化）；换图后须重烘焙。规则层只读烘焙顶点，禁止运行时读 Sprite/像素。Digger 视觉为 Character Creator **烘焙整角**，固定 Prefab 逻辑名 `Digger` → `Assets/Prefabs/Dig/Digger.prefab`；美术导出源见 [§15](#15-角色美术管线character-creator-烘焙整角)。挖坟圆圈光标 UI：`UiDigCursorRing` → `Assets/Prefabs/Dig/UiDigCursorRing.prefab`（双层圆形：Stroke 外径 + Fill 内径差固定**屏幕**像素描边；Fill 白色半透明）；由 `DigPrefabCatalog` 绑定，`DigCursorView` 在 Dig HUD Canvas 下 Instantiate：先将 `DigCursorRadius` 投影为屏幕像素直径，再 ÷ `Canvas.scaleFactor` 写入 `sizeDelta`（Scale With Screen Size 下禁止把屏幕像素当 canvas 单位）；圆形 Sprite 源 `Assets/Art/UI/Dig/Ui_DigCursor_Circle.png`。Dig 地图：`DigMapId` → `Assets/Prefabs/Maps/{DigMapId}.prefab`。

#### 9.3 坟墓品质定义表 `GraveQualityConfig`

**磁盘名：**
- **Excel：** `挖坟_坟墓品质定义表_Dig_GraveQualityConfig.xlsx`
- **CSV：** `Dig_GraveQualityConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| QualityId | 坟墓品质ID | `string` 或 `int` | 主键；被 `GraveSpawnWeights` 引用 |
| MaxHP | 总血量 | `int` 或 `float` | 生成时初始化坟的 maxHP / 当前 HP；具体数值后续填表 |
| LootDrop | 掉落内容 | 见编码 | 挖掘成功（HP=0）时产出的奖励 |
| IconStyleHighId | 高血量图标ID | `string` | 剩余 HP% **>65%** 用；空 = 品质默认 Prefab/图 |
| IconStyleMidId | 中血量图标ID | `string` | 剩余 HP% **30%–65%** 用；空 = 默认 |
| IconStyleLowId | 低血量图标ID | `string` | 剩余 HP% **<30%** 用；空 = 默认 |

```
GraveQualityConfig {
  QualityId: Id
  MaxHP: number
  LootDrop: "Id_Count|Id_Count|..."
  IconStyleHighId: string     // empty = quality default
  IconStyleMidId: string
  IconStyleLowId: string
}
```

**与规则的关系（[SPEC_03 §3.10](SPEC_03_GameRules.md)）：**

- 生成坟时按 `QualityId` 读本表初始化 `GraveHP`。
- 扣血后按剩余 HP% 切换 `GraveIconStyle`（>65% / 30%–65% / <30%）；样式资源分别取自 `IconStyleHighId` / `IconStyleMidId` / `IconStyleLowId`，空则用品质默认 Prefab/图。
- HP 归 0 时按 `LootDrop` 生成 `DigReward` 图标并飞向主角；到达后入账。

**`LootDrop` 编码（固定）：** `Id_Count|Id_Count|...`

- 段分隔符：`|`；段内：`Id_Count`（下划线分隔）。
- `Id` 解析顺序：
  1. **保留精魂 Id** 字符串 **`Spirit`**（大小写敏感）→ 不入材料堆叠，直接加精魂。
  2. **`MaterialConfig.MaterialId`** → 普通材料；`AutoConvert` / UI 图标取自材料表。
  3. **`BodyPartConfig.BodyPartId`** → 躯体材料（与 `MaterialId` **同命名空间，Id 不得冲突**）；堆叠上限同 **10000**；`AutoConvert` 取自躯体表；仓库 / DigReward 外观图可用 `ArtAssetId`。
- `Count`：正整数（≥ 1）。
- 空串、缺下划线、`Count` 非正整数、上述皆未命中的 Id：**忽略该段并打日志**，继续解析其余段。
- 示例：`Iron_3|Spirit_10|Bone_1`

#### 9.4 材料配置表 `MaterialConfig`

**磁盘名：**
- **Excel：** `挖坟_材料配置表_Dig_MaterialConfig.xlsx`
- **CSV：** `Dig_MaterialConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| MaterialId | 材料ID | `string` 或 `int` | 主键；被 `LootDrop` 引用 |
| AutoConvert | 自动兑换 | `int` 或 `float` | 堆叠超上限时，**每 1 个超出材料**兑换的精魂数量（≥ 0；0 = 超出部分丢弃且不兑精魂） |
| AppearanceIconId | 外观图ID | `string` 或 `int` | 仓库 / DigReward 等 UI 用的外观图 Id |
| AssetPath | 素材路径 | `string` | 主素材资源路径（加载约定实现时定） |
| WarehouseQualityOutlineId | 仓库品质外轮廓ID | `string` 或 `int` | **仓库格子**专用品质外轮廓美术资源 Id（专用套，非地图坟图标） |

堆叠上限常量（规则常量，非表字段）：**10000**（见 [SPEC_03 §3.10](SPEC_03_GameRules.md)）。

```
MaterialConfig {
  MaterialId: Id
  AutoConvert: number   // SpiritEssence per 1 excess unit
  AppearanceIconId: Id
  AssetPath: string
  WarehouseQualityOutlineId: Id
}
```

#### 9.5 货币配置表 `CurrencyConfig`

**磁盘名：**
- **Excel：** `挖坟_货币配置表_Dig_CurrencyConfig.xlsx`
- **CSV：** `Dig_CurrencyConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| CurrencyId | 货币ID | `string` 或 `int` | 主键；精魂保留 Id 为字符串 **`Spirit`**（与 `LootDrop` / [SPEC_03 §3.10](SPEC_03_GameRules.md) 对齐） |
| AppearanceIconId | 外观图ID | `string` 或 `int` | UI 外观图 Id |
| AssetPath | 素材路径 | `string` | 主素材资源路径 |
| WarehouseQualityOutlineId | 仓库品质外轮廓ID | `string` 或 `int` | 若货币以同类格子展示，复用仓库格品质外轮廓资源 Id |

```
CurrencyConfig {
  CurrencyId: Id          // e.g. "Spirit"
  AppearanceIconId: Id
  AssetPath: string
  WarehouseQualityOutlineId: Id
}
```

本版至少须有一行 `CurrencyId = Spirit`（精魂）。

#### 9.6 挖坟主角能力（运行时派生；由科技效果重算）

科技学会的结果写入存档主角的 `DigProtagonistCapabilities`（节点表与费用见 [§9.16](#916-科技树配置表-techtreeconfig) / [§9.17](#917-科技项效果配置表-techeffectconfig)；规则见 [SPEC_03 §3.13](SPEC_03_GameRules.md)）：

```
DigProtagonistCapabilities {
  DigDamage: number
  DigDurationReductionSum: number   // seconds; sum of unlock shorten effects
  DigCursorRadius: number
  DiggableQualityIds: set<QualityId>
  DigStageDurationBonus: number     // seconds; additive to LevelDurationSeconds
}
// DigActionDuration = max(0.1, 0.8 - DigDurationReductionSum)
// EffectiveDigDuration = LevelDurationSeconds + DigStageDurationBonus
// Recalc from sum of learned TechEffectConfig.AttributeModifiers (additive per key)
```

#### 9.7 防守配置表 `DefendGameplayConfig`

**磁盘名：**
- **Excel：** `防守_防守配置表_Defend_DefendGameplayConfig.xlsx`
- **CSV：** `Defend_DefendGameplayConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| GameplayConfigId | 玩法配置ID | `string` 或 `int` | 主键；被关卡运作表引用 |
| BattleMapId | 战斗地图ID | `string` | Prefab 逻辑名（无路径、无扩展名）；合法值 **`Ground_01`…`Ground_05`**（与 Dig `DigMapId` 共用地面变体池）；运行时解析 → `Assets/Prefabs/Maps/{BattleMapId}.prefab`（含 EngageZone + Isometric Tilemap 地面 + Demo `WalkSurface`/NavMesh 可走约定，见 [§13](#13-unity-资源编排与可扩展性约定) / [§15](#15-角色美术管线character-creator-烘焙整角)） |
| WaveConfigId | 波次配置ID | `string` 或 `int` | FK → `WaveSpawnConfig`（§9.18）分组键 |
| CombatDurationSeconds | 战斗总时长 | `int` | 开战倒计时初值（整秒）；剩余秒用于刷怪激活；归零不单独判胜负 |
| TargetRetargetIntervalSeconds | 目标修正间隔 | `float` | 怪物与士兵重算可攻击目的地的间隔（秒）；默认 **1** |

```
DefendGameplayConfig {
  GameplayConfigId: Id
  BattleMapId: string          // Ground_01…Ground_05 → Assets/Prefabs/Maps/{Id}.prefab
  WaveConfigId: Id             // FK → WaveSpawnConfig
  CombatDurationSeconds: int   // whole seconds; countdown init
  TargetRetargetIntervalSeconds: number  // default 1
}
```

**寻路技术约定（与 [SPEC_03 §3.12](SPEC_03_GameRules.md) 配套；大规模栈方案 B）：**

- **方案 B（已锁定）：** 共享目的地用 **FlowField**；追击/攻击用 **AttackSlot** + **LocalDetour**；容量双方约 200。细则见下「大规模战斗寻路运行时契约」。
- 采用 Unity **NavMesh**（或可走掩码）表达 **静态** 可走空间与 `AirWall`；**不**要求 400 个单位各自持有高频全图 `CalculatePath`。
- **规则层**只输出：当前目标实体 ID + `GoalKind`（`Objective` / `FormationHome` / `AttackSlot` / `ChaseAnchor`）+ 可选 `AttackRange`；**不**直接写 `Transform` / `Animator`。
- **移动服务**解析 `DesiredDestination`：Objective→FlowField 采样；攻击→AttackSlot 认领；友军阻挡→LocalDetour；表现层应用位移。
- 每隔 `TargetRetargetIntervalSeconds` 由规则层触发目标重选；槽位/场采样可按更短分帧预算更新，但 **禁止** 全员每帧全图重寻路。
- **EngageZone**：挂在 BattleMap **Prefab** 上的 **IsoDiamond（XZ 菱形）选敌区**（比地图稍小；策划调位置/尺寸）；非叛变士兵仅在此区内选最近敌人。见 [SPEC_03 §3.12](SPEC_03_GameRules.md) WarriorCombat。
- 士兵命中方案 D：按 `SoulConfig.AttackMode`（制造写入实例）走近战前摇或远程弹道；`AttackRange` / 前摇 / 弹速 / 超时取自 [§9.9b `ClassConfig`](#99b-职业配置表-classconfig)（规则层确认伤害；View 播动作/弹道）。**第一版 Demo**：士兵与怪物仅普通攻击，不施放技能。
- 障碍烘焙与 NavMesh 表面范围：**Demo 最小** — 按 `DigMapBounds` **IsoDiamond** 半尺寸 Runtime 烘焙可走面；PushMap `AirWall`：开战 Bake 追加 **Not Walkable Box**（§9.22 PM-08）。静态结果写入 FlowField 障碍/可走掩码。复杂障碍与精确 OutsideMap **后置**。
- **Demo 刷怪点最小：** 地图 Prefab 上临时固定出生点或 `InsideMap` 可走面随机；精确 OutsideMap 几何后置。

**大规模战斗寻路运行时契约（方案 B / MassCombatPathing）：**

- **权威行为：** [SPEC_03 §3.12](SPEC_03_GameRules.md)「大规模战斗寻路」；PushMap 推进见 [§3.14](SPEC_03_GameRules.md)
- **选定方案：** B — FlowField（共享目标）+ 局部追击 AttackSlot + LocalDetour；难度 3；切片 `.scratch/mass-pathing/issues/`
- **模块建议路径（实现时）：**
  - `Assets/Scripts/Core/Pathing/` — 纯 C#：`FlowFieldService`、`AttackSlotService`、`SpatialHash2D`、`LocalDetourSolver`、分帧 `MassMoveScheduler`
  - `Assets/Scripts/Gameplay/Pathing/` — 表现桥：从 StageController 注册单位、应用位移；逐步替换/包裹 `PushMapAdvanceView` / `WarriorAgentView` / `*MonsterAgentView` 的重 NavMeshAgent 路径
- **FlowField：**
  - 格点分辨率 Demo 建议世界单位边长 **0.25～0.5**（可配置常量）；覆盖 `DigMapBounds` IsoDiamond
  - 障碍：Bake 后不可走（含 AirWall）→ 场内 cost=∞ / 不可达
  - 目标：PushMap `CurrentObjective` 世界点（或 CaptureZone 中心）；`CurrentObjectiveChanged` / 开战 Bake 完成 → **重建一场**
  - 查询：单位位置 → 最近格 → 读归一化方向；同目标单位共享同一场缓冲
  - **禁止** 每单位独立 Dijkstra/A* 全图
- **AttackSlot：**
  - `slotPos = targetPos + rot(k * 2π/N) * ringRadius`；`ringRadius = max(0.05, AttackRange − slotMargin)`；`slotMargin` Demo 默认 `0.05`
  - `N`：近战默认 **12**，远程默认 **8**（常量；可后置配置表）
  - 合法性：`IAttackSlotWalkable` 钩子（Demo stub；后接 `NavMesh.SamplePosition` 或可走掩码）；与目标占地圆不严重重叠
  - 认领表：`Dictionary<targetRuntimeId, SlotClaim[]>`；释放于死亡/换目标/超时
  - 重算触发：`TargetRetargetInterval`、目标位移 > `slotReclaimMoveThreshold`（Demo 默认 `0.5` 世界单位）
  - API：`TryClaim(attackerId, targetId, attackRange, targetPos, out worldPos, attackMode?, attackerPos?, targetBodyRadius?)`；`Release` / `ReleaseAllForTarget`
- **LocalDetour：**
  - 邻域：`SpatialHash2D` cell ≈ `0.5`；查询半径 ≈ `2 * agentRadius + 0.2`；热路径复用结果列表，禁止全表 O(n²) 互扫
  - 前方扇形阻挡 → 左/右短探测（长度 ≈ `1.0`）；选净空更大侧加切向偏置
  - 可选软分离（低强度）；`separationScale` 交战圈可降（对齐既有「防 RVO 挤抖」意图）
  - **禁止** 友军 `NavMeshObstacle.Carve`
  - API：`Steer(desiredDir, self, neighbors, separationScale?)` → `steerDir`（`self` = 自身 XZ 位置 + 半径；无邻域时 `steer ≈ desired`）
- **性能预算（验收导向）：**
  - 存活可移动单位 ≤ **400** 时：移动逻辑主线程预算目标 **≤ ~2.5 ms/帧**（60 FPS 机；Debug 可打点）
  - 分帧：路径/槽位重算每帧处理 **≤ 50** 单位（轮转）；FlowField 重建不与全员槽位重算同帧叠满
  - 空间哈希邻域；禁止双层全表距离循环
  - **压测入口（MP-07）：** `Assets/Scripts/Core/Pathing/MassPathingPerfStress.cs`（纯 C# Stopwatch，双方各约 200）+ `Assets/Scripts/Gameplay/Pathing/MassPathingPerfStressView.cs`（简化胶囊/方块桩单位）+ Editor `Gravedigger2026/Pathing/Run MassPathing 200v200 Perf Stress`；测的是 `MassMoveScheduler.Tick` + ≤50 槽位刷新，**不含** Animator/全员 `CalculatePath`
  - **超预算回退（按序尝试）：** ① 增大 FlowField `cellSize`（向 0.5）；② 降低 AttackSlot `N`（近战/远程常量）；③ 降低 `MassMoveScheduler.MaxRecalcPerFrame` / 槽刷新预算（加大分帧，接受转向滞后）
- **与现网 Demo 过渡：** MP 切片落地前，现有单 Agent NavMesh 行为仍可运行；切片验收后推进/追击须切到本契约；不得回退为全员 HighQuality RVO 作为规模方案
- **边界不做（本专题）：** 完整 ORCA；动态门/可破坏墙；多层楼寻路；技能位移预测

```
// MassCombatPathing touchpoints (Approach B)
FlowFieldService.Rebuild(goal, walkableMaskInclAirWall)
FlowFieldService.SampleDir(worldPos) -> Vector2
AttackSlotService.TryClaim(attackerId, targetId, attackRange, targetPos, …) -> worldPos
LocalDetourSolver.Steer(desiredDir, self, neighbors, separationScale?) -> steerDir
MassMoveScheduler.Tick(dt) // frame-budgeted
MassPathingPerfStress.Run(perSide≈200) // MP-07 Debug Stopwatch
```

**刷怪 / 怪物表：** 见 §9.18 `WaveSpawnConfig`、§9.19 `MonsterConfig`；失控见 §9.20 `LossOfControlConfig`、§9.21 `SkillConfig`（骨架）；规则见 [SPEC_03 §3.12](SPEC_03_GameRules.md)。

#### 9.8 主角升级配置表 `ProtagonistLevelConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md)。一行 = 一个主角等级。

**磁盘名：**
- **Excel：** `制造_主角升级配置表_Manufacture_ProtagonistLevelConfig.xlsx`
- **CSV：** `Manufacture_ProtagonistLevelConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| Level | 当前等级值 | `int` | 主键；≥ 1；同表内唯一 |
| RequiredTotalExperience | 升到本级需要的经验总值 | `int` 或 `long` | **生涯累计阈值**（非本级增量）；与存档 `LifetimeExperience` 比较；升级 **不**扣减已有经验；1 级行通常为 **0** |
| UnlockedFeatureIds | 升到本级解锁的功能 | 见编码 | **仅预留**；本版无运行时解锁逻辑 |
| TechPointsReward | 升到本级奖励的科技点数 | `int` | 首次进入该等级时发放（≥ 0） |
| ControlPowerCap | 升到本级控制力上限变成的值 | `int` 或 `float` | 该等级下控制力上限绝对值；本版有效上限 = 本字段（科技加成另专题） |
| ProtagonistMaxHP | 升到本级主角的生命值上限 | `int` 或 `float` | 字段名保留；**Defend 开战时作护盾上限**（`Shield` 初值，见 [SPEC_03 §3.12](SPEC_03_GameRules.md)） |

**`UnlockedFeatureIds` 编码（固定）：** `FeatureId|FeatureId|...`

- 段分隔符：`|`
- 空字符串 = 无预留项
- 本版解析后可忽略；不得据此驱动玩法解锁

```
ProtagonistLevelConfig {
  Level: int                              // PK, >= 1
  RequiredTotalExperience: number         // cumulative lifetime threshold
  UnlockedFeatureIds: "Id|Id|..."         // reserved; no runtime unlock this version
  TechPointsReward: int
  ControlPowerCap: number
  ProtagonistMaxHP: number
}
```

**升级解析（与 §3.11 配套）：**

- 存档持有 `Level`、`LifetimeExperience`。
- 当存在 `Level+1` 行且 `LifetimeExperience >= RequiredTotalExperience(Level+1)` 时连升；每升一级应用该行奖励与属性。
- 各行具体数值 **TBD**（本批只定表结构与语义）。

#### 9.9 灵魂配置表 `SoulConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 士兵属性构成 / 命名。一行 = 一种灵魂。

**磁盘名：**
- **Excel：** `制造_灵魂配置表_Manufacture_SoulConfig.xlsx`
- **CSV：** `Manufacture_SoulConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| SoulId | 灵魂ID | `string` 或 `int` | 主键 |
| ClassId | 职业ID | `string` 或 `int` | 必填；FK → `ClassConfig`；制造时写入士兵实例 |
| AttackMode | 攻击模式 | `enum` / `string` | `Melee` \| `Ranged`；士兵普攻命中方案 D 分支（§3.12）；与怪物 `AttackMode` 同枚举。示例：战士类→Melee、射手/法师类→Ranged（法师与射手同远程通道，仅 `ClassConfig.PrimaryStat` 不同） |
| Skills | 可使用技能 | 见编码 | 技能 Id + 等级列表；编码见下；失控加成与 CD 见 [§9.21 `SkillConfig`](#921-技能配置表骨架-skillconfig)；**第一版 Demo 不施放**（字段可留空） |
| AttackPriority | 攻击优先级 | `enum` / `string` | 与怪物 `TargetSelect` **同枚举**：`Nearest` \| `PreferWarrior` \| `PreferProtagonist`；**本批不驱动**选目标（默认 EngageZone 内最近敌人，见 §3.12） |
| MoveStyle | 移动风格 | `enum` / `string` | `Normal` \| `Aggressive` \| `Cautious`；未知值当 `Normal`；本批可不驱动 AI |
| SpiritCost | 精魂消耗 | `int` 或 `float` | 制造时计入总精魂消耗（≥ 0；缺省 0） |
| ControlPowerCost | 控制力占用 | `int` 或 `float` | 该灵魂对士兵 `ControlPowerCost` 的贡献（≥ 0） |

**`Skills` 编码（固定；士兵侧 Soul / Gem / ExtraEquipment 共用）：** `SkillId;Level|SkillId;Level|…`

- 段分隔符：`|`；段内：`技能ID;等级`
- `Level`：正整数（≥ 1）；非法段 **跳过并打日志**
- 空字符串 = 无技能
- 与怪物 `MonsterConfig.Skills`（`SkillId_CdSeconds|…`）**编码不同**（怪物 CD 写在怪物表）

```
SoulConfig {
  SoulId: Id
  ClassId: Id                     // FK → ClassConfig; required
  AttackMode: Melee | Ranged      // soldier normal-attack hit branch (§3.12)
  Skills: "SkillId;Level|..."     // soldier-side; unused cast in Demo v1
  AttackPriority: Nearest | PreferWarrior | PreferProtagonist  // unused for targeting this batch
  MoveStyle: Normal | Aggressive | Cautious  // unknown → Normal
  SpiritCost: number              // >= 0
  ControlPowerCost: number
}
```

**说明：** 原 `InfoTags` **不再**参与 WarriorInfo 主标签生成（主标签 = 定稿种族）。灵魂 **不**改写力量/敏捷/智力本身；通过 `ClassId` 注入职业；通过 `AttackMode` 选择近战/远程命中分支。`ClassName` / `PrimaryStat` / 换算系数见 [§9.9b `ClassConfig`](#99b-职业配置表-classconfig)。

**士兵实例静态快照（制造完成时写入；非独立配置表）：**

```
WarriorInstance {
  Id: Id
  WarriorName: string             // Prefix(es)+RaceName+ClassName+Suffix; ClassName via ClassId → ClassConfig
  RemainingHP: number
  RaceId: Id                      // weight-1 pick from filled BodyParts → RaceConfig
  RaceAdjustCoeff: {              // five dims; missing = 0; may be +/-
    MaxHP: number
    MoveSpeed: number
    Strength: number
    Agility: number
    Intelligence: number
  }                               // copied from RaceConfig at manufacture
  BaseStats: {
    MaxHP: number
    MoveSpeed: number
    Strength: number
    Agility: number
    Intelligence: number
  }                               // Base(S) = Σ StatBonus(S) of filled BodyParts
  AppearanceId: Id                // FK → BodyAppearanceConfig; pick rules §3.11 / §9.13
  SoulId: Id                      // FK → SoulConfig
  ClassId: Id                     // copied from SoulConfig at manufacture; FK → ClassConfig
  AttackMode: Melee | Ranged      // copied from SoulConfig at manufacture
  LockedEquipIds: Id[]            // ExtraEquipment chosen & locked at manufacture
  GemIds: Id[]                    // 0..6; FK → GemConfig; type-exclusive
  GemMult: {                      // five dims; Σ of socketed gems; all 0 if none
    MaxHP: number
    MoveSpeed: number
    Strength: number
    Agility: number
    Intelligence: number
  }
  ControlPowerCost: number        // BodyCost + SoulCost + EquipCost + GemCost at manufacture
  EquipStats: { … five dims }     // manufacture-locked Equip layer flats
  BodyLife: number                // Base(MaxHP)+Equip(MaxHP); HP-dim exception
  SourceItemIds: Id[]             // non-empty slot ItemIds at manufacture (remake recipe)
  SourceSpiritCost: number        // Spirit paid at manufacture (remake gate)
}
```

**说明（存档）：** Demo 按槽将上述快照整段序列化进 `PlayerPrefs`（§6）；`NextSerial` 与池同键，保证再进档 Id 不冲突。
**关联说明：**

- 职业表见 **§9.9b**；躯体材料 / 躯体外观 / 额外装备 / 宝石后缀表见 **§9.12–§9.15**。
- 静态层：`StaticStat(S) = max(0, Base+Equip+Base×GemMult+Base×RaceAdjust)`（不含 Buff）；战斗层：`FinalStat(S)` 另加 `Base×SkillBuff`（先定 `S` 再取来源；各维缺省 0；见 §3.11）。
- **生命维例外**：最终士兵 `MaxHP = ceil(BodyLife + Str×3)`，`BodyLife = Base(MaxHP)+Equip(MaxHP)`；**不**用 `FinalStat(MaxHP)`（§3.11）。
- 战斗派生：`Primary` = `ClassId` → `ClassConfig.PrimaryStat` 对应维；`NormalAttackPower` / `AttackSpeed` / `SkillCooldown` 系数取自 `ClassConfig.CombatConvertCoeffs`（缺键回退 §3.12 全局默认）；`AttackRange` 等命中参数取自同表列。
- 多宝石：实例 `GemMult(S) = Σ` 已镶嵌各宝石的 `GemMult(S)`。
- 士兵 **彻底死亡（PermanentDeath）**：全部 `GemIds` 回仓库；躯体部位/灵魂/外置装备等绑定材料销毁；布阵位清空（见 §3.11）。战斗死亡（无宝石）不触发物资去向；带宝石士兵 HP≤0 立即彻底死亡。
- 种族与职业 **不** 单独计入 `ControlPowerCost`。

#### 9.9b 职业配置表 `ClassConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 士兵职业 / 命名 / 主属性 / 五维→战斗参数换算。一行 = 一种职业。插在 §9.9 与 §9.10 之间；后续节号不变。

**磁盘名：**
- **Excel：** `制造_职业配置表_Manufacture_ClassConfig.xlsx`
- **CSV：** `Manufacture_ClassConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| ClassId | 职业ID | `string` 或 `int` | 主键；被 `SoulConfig.ClassId` 引用 |
| ClassName | 职业名 | `string` | 参与 `WarriorName` 拼接；外观 `ClassAffinity` 精确匹配键；展示用；可为「战士」等，**不是**单位称谓「士兵」 |
| PrimaryStat | 主属性 | `enum` / `string` | `Strength` \| `Agility` \| `Intelligence`；决定普攻 `NormalAttackPower` 所用属性维（§3.12）；示例语义战士→Strength、射手→Agility、法师→Intelligence（以本字段为准，非 ClassName 硬编码） |
| CombatConvertCoeffs | 战斗换算系数 | 见编码 | 将该职业士兵的五维 StaticStat/FinalStat 转为 `NormalAttackPower` / `AttackSpeed` / `SkillCooldown` 等战斗参数时的系数集；编码见下；缺键回退全局默认 |
| AttackRange | 攻击距离 | `float` | 进入攻击态距离（世界单位或项目统一距离单位） |
| MeleeWindupSeconds | 近战前摇 | `float` | ≥ 0；秒；`AttackMode=Melee` 时用 |
| RangedProjectileSpeed | 远程弹速 | `float` | ≥ 0；`AttackMode=Ranged` 时用 |
| RangedTimeoutSeconds | 远程超时 | `float` | ≥ 0；秒；超时未命中 → 未命中 |

**`CombatConvertCoeffs` 编码（固定）：** `键_数值|键_数值|…`

| 键 | 默认（缺键回退） | 公式角色（§3.12） |
|----|------------------|-------------------|
| `NormalAttackPrimaryMult` | `1.5` | `NormalAttackPower = Primary × 本系数` |
| `AttackSpeedBase` | `0.5` | `AttackSpeed = 本系数 + AttackSpeedAgiDiv/max(Agi,1)` |
| `AttackSpeedAgiDiv` | `60` | 见上 |
| `SkillCdIntDiv` | `30` | `SkillCooldown = max(SkillCdFloor, BaseCooldownSeconds − 本系数/max(Int,1))` |
| `SkillCdFloor` | `0.1` | 见上 |

- 示例：`NormalAttackPrimaryMult_1.5|AttackSpeedBase_0.5|AttackSpeedAgiDiv_60|SkillCdIntDiv_30|SkillCdFloor_0.1`
- 空串 = 全部回退默认；非法段跳过并打日志
- **不**含 `AttackRange` / 前摇 / 弹速（独立列）

```
ClassConfig {
  ClassId: Id
  ClassName: string               // WarriorName + ClassAffinity match key
  PrimaryStat: Strength | Agility | Intelligence
  CombatConvertCoeffs: "Key_Value|..."  // missing key → global default
  AttackRange: number
  MeleeWindupSeconds: number
  RangedProjectileSpeed: number
  RangedTimeoutSeconds: number
}
```

**解析：**

- 制造时：`SoulConfig.ClassId` → 写入 `WarriorInstance.ClassId`；命名与外观取本行 `ClassName`。
- 战斗派生：先查本表取 `PrimaryStat` 与 `CombatConvertCoeffs`；命中参数取本行 `AttackRange` 等列。
- 职业列表与具体数值后续填表。

#### 9.10 宝石配置表 `GemConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 士兵属性构成 / 宝石。一行 = 一种宝石。

**磁盘名：**
- **Excel：** `制造_宝石配置表_Manufacture_GemConfig.xlsx`
- **CSV：** `Manufacture_GemConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| GemId | 宝石ID | `string` 或 `int` | 主键 |
| GemType | 宝石类型 | `enum` / `string` | 六类之一：`Ruby` \| `Sapphire` \| `Emerald` \| `Topaz` \| `Amethyst` \| `Diamond`；制造槽 **同类型互斥**；展示名可后补本地化 |
| GemMult.MaxHP | 生命值放大系数 | `float` | 缺省视为 **0**；代入 `Base(MaxHP) × GemMult.MaxHP` |
| GemMult.MoveSpeed | 移动速度放大系数 | `float` | 同上 |
| GemMult.Strength | 力量放大系数 | `float` | 同上 |
| GemMult.Agility | 敏捷放大系数 | `float` | 同上 |
| GemMult.Intelligence | 智力放大系数 | `float` | 同上 |
| Skills | 额外技能 | 见编码 | 额外一套技能（SkillId + 等级）；编码同 [§9.9 `SoulConfig.Skills`](#99-灵魂配置表-soulconfig)：`SkillId;Level\|…`；技能失控加成见 [§9.21](#921-技能配置表骨架-skillconfig) |
| SpiritCost | 精魂消耗 | `int` 或 `float` | 制造时计入总精魂消耗（≥ 0；缺省 0） |
| ControlPowerCost | 控制力占用 | `int` 或 `float` | 该宝石对士兵 `ControlPowerCost` 的贡献（≥ 0） |
| LossOfControlChanceBonus | 失控概率加成 | `float` | 可正可负；缺省 **0**；多宝石时对实例已镶嵌宝石该字段 **求和**（§3.11） |

```
GemConfig {
  GemId: Id
  GemType: Ruby | Sapphire | Emerald | Topaz | Amethyst | Diamond
  GemMult: {
    MaxHP: number
    MoveSpeed: number
    Strength: number
    Agility: number
    Intelligence: number
  }                               // missing dim = 0
  Skills: "SkillId;Level|..."     // same as SoulConfig.Skills
  SpiritCost: number
  ControlPowerCost: number
  LossOfControlChanceBonus: number // +/- ; default 0; Σ across socketed gems
}
```

**库存语义：** 宝石为可回仓物品；士兵 **彻底死亡** 时将实例 `GemIds` **全部归还仓库**（其余绑定材料销毁）；带宝石士兵 HP≤0 立即彻底死亡。制造时实例五维 `GemMult(S) = Σ` 已镶嵌宝石该维（无镶嵌则五维全 0）。获取途径与具体数值后续填表。

#### 9.11 种族配置表 `RaceConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 士兵属性构成 / 种族 / 命名。一行 = 一种种族。

**磁盘名：**
- **Excel：** `制造_种族配置表_Manufacture_RaceConfig.xlsx`
- **CSV：** `Manufacture_RaceConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| RaceId | 种族ID | `string` 或 `int` | 主键；被躯体部位 `RaceId` 引用 |
| DisplayNameKey | 展示名 Key | `string` | UI / `WarriorName` 种族段；约定格式 **`Race.{RaceId}.Name`**；未启用 i18n 时可将本字段直接当展示字符串回退 |
| RaceAdjustCoeff.MaxHP | 生命值调整系数 | `float` | 可正可负；缺省视为 **0** |
| RaceAdjustCoeff.MoveSpeed | 移动速度调整系数 | `float` | 同上 |
| RaceAdjustCoeff.Strength | 力量调整系数 | `float` | 同上 |
| RaceAdjustCoeff.Agility | 敏捷调整系数 | `float` | 同上 |
| RaceAdjustCoeff.Intelligence | 智力调整系数 | `float` | 同上 |
| LossOfControlChanceBonus | 失控概率加成 | `float` | 可正可负；缺省 **0**；代入士兵最终失控率（§3.11） |

```
RaceConfig {
  RaceId: Id
  DisplayNameKey: string?
  RaceAdjustCoeff: {
    MaxHP: number
    MoveSpeed: number
    Strength: number
    Agility: number
    Intelligence: number
  }
  LossOfControlChanceBonus: number   // +/- ; default 0
}
```

**解析：** 制造时对已放入躯体部位（头/躯干/臂/腿）各权重 **1** 按部位 `RaceId` **加权随机**定稿 → 查本表，将五维系数写入 `WarriorInstance.RaceAdjustCoeff`；按项代入 `Base(S) × RaceAdjust(S)`。失控判定时取本行 `LossOfControlChanceBonus`。具体种族列表与数值后续填表。

#### 9.12 躯体材料配置表 `BodyPartConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 制造槽位 / 种族加权 / BaseStats / 仓库入账。一行 = 一种躯体部位材料。亦可称 **躯体材料表**。

**磁盘名：**
- **Excel：** `制造_躯体材料配置表_Manufacture_BodyPartConfig.xlsx`
- **CSV：** `Manufacture_BodyPartConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| BodyPartId | 躯体ID | `string` 或 `int` | 主键；可被 `LootDrop` / 仓库引用；与 `MaterialConfig.MaterialId` **同命名空间，不得冲突** |
| BodyLevel | 躯体等级 | `int` 或 `float` | ≥ 0；参与制造时外观平均等级 |
| BodySlot | 躯体部位 | `enum` | `Head` / `Torso` / `Arm` / `Leg`；决定可放入的制造槽 |
| RaceId | 种族 | `string` 或 `int` | FK → `RaceConfig`；参与加权定种族 |
| ControlPowerCost | 控制力占用值 | `int` 或 `float` | ≥ 0；计入制造 `BodyCost` |
| SpiritCost | 精魂消耗 | `int` 或 `float` | ≥ 0；缺省 0；计入制造总精魂消耗 |
| StatBonus | 增加的属性值 | 见编码 | 五项基础属性平坦加成；制造时按维 **Σ** 得 `Base(S)` |
| AutoConvert | 超上限兑精魂 | `int` 或 `float` | 语义同 `MaterialConfig.AutoConvert`：每 1 个超出材料兑精魂数（≥ 0；0 = 超出丢弃且不兑） |
| Description | 文字介绍 | `string` | 展示文案；若启用 i18n 可为本地化 Key |
| ArtAssetId | 外形美术素材ID | `string` 或 `int` | 部位单件外观 / 仓库 UI 可用资源 Id |

**`StatBonus` 编码（固定）：** `属性项_数值|属性项_数值|…`（与 [§9.17](#917-科技项效果配置表-techeffectconfig) `AttributeModifiers` 同风格；加法；空 = 无加成）。属性项键与五项 BaseStats 对齐（如 `MaxHP` / `MoveSpeed` / `Strength` / `Agility` / `Intelligence`）。

```
BodyPartConfig {
  BodyPartId: Id                  // also warehouse / LootDrop material Id
  BodyLevel: number
  BodySlot: Head | Torso | Arm | Leg
  RaceId: Id                      // FK → RaceConfig
  ControlPowerCost: number
  SpiritCost: number              // >= 0; default 0
  StatBonus: "Attr_Value|..."     // additive; empty = none
  AutoConvert: number             // SpiritEssence per 1 excess unit
  Description: string
  ArtAssetId: Id
}
```

**解析：**

- `Base(S) = Σ` 已放入躯体部位的 `StatBonus(S)`（缺省维 **0**）；见 [SPEC_03 §3.11](SPEC_03_GameRules.md)。
- 仓库入账：`BodyPartId` 按材料堆叠；超限按本行 `AutoConvert` 兑精魂。
- 具体数值行 **TBD**。

#### 9.13 躯体外观配置表 `BodyAppearanceConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 士兵整体外观选取。一行 = 一种预设整体外观造型。

**磁盘名：**
- **Excel：** `制造_躯体外观配置表_Manufacture_BodyAppearanceConfig.xlsx`
- **CSV：** `Manufacture_BodyAppearanceConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| AppearanceId | 外观ID | `string` 或 `int` | 主键；写入 `WarriorInstance.AppearanceId`；**Prefab 逻辑名**（无路径、无扩展名）；运行时解析 → `Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab`；美术源 `Assets/Art/Characters/Appearances/{AppearanceId}/`（见 [§15](#15-角色美术管线character-creator-烘焙整角)） |
| AppearanceLevel | 外观等级 | `int` | 与制造时平均躯体等级（保留 1 位小数后再四舍五入为整数）匹配 |
| RaceId | 隶属种族 | `string` 或 `int` | FK → `RaceConfig` |
| ClassAffinity | 职业倾向 | 见编码 | 精确匹配 `ClassConfig.ClassName`（经士兵灵魂 `ClassId`） |
| Description | 文字介绍 | `string` | 展示文案；若启用 i18n 可为本地化 Key |
| IsFallback | 保底外形 | `int` 或空 | 空 / `0` = 常规；**`1` = 该种族保底**；**每种族至多 1 行** 为 `1` |

**`ClassAffinity` 编码（固定）：** `ClassName` 或 `ClassName|ClassName|…`（管道分隔；精确匹配；空 = 无职业倾向，仅能靠随机进入候选后的回退路径）。

```
BodyAppearanceConfig {
  AppearanceId: Id                 // Prefab logical name → Assets/Prefabs/Defend/Warriors/{Id}.prefab
  AppearanceLevel: int
  RaceId: Id                      // FK → RaceConfig
  ClassAffinity: "Class|Class|..." // empty = no class affinity
  Description: string
  IsFallback: 0 | 1 | empty       // 1 = race fallback; ≤1 per RaceId
}
```

**选取算法（制造定稿；预览用当前槽位按同算法试算）：**

1. 对已放入全部躯体槽（含可选头部；空槽不计）取 `BodyLevel` 算术平均 → **保留 1 位小数** → 再 **四舍五入为整数** `AvgLevelInt`。
2. 候选集 A = `AppearanceLevel == AvgLevelInt` **且** `RaceId ==` 定稿种族。
3. 若 A 非空：子集 B = `ClassAffinity` 含 `ClassConfig.ClassName`（经灵魂 `ClassId`）的行；若 B 非空 → 在 B 中 **均匀随机**；否则在 A 中 **均匀随机**。
4. 若 A 为空：取同种族 `IsFallback == 1` 的行；有则用之。
5. 若仍无：在 **全表** 中 **均匀随机** 一行。

#### 9.14 额外装备配置表 `ExtraEquipmentConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 外置装备 / 命名前缀。一行 = 一种外置装备。

**磁盘名：**
- **Excel：** `制造_额外装备配置表_Manufacture_ExtraEquipmentConfig.xlsx`
- **CSV：** `Manufacture_ExtraEquipmentConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| EquipId | 装备ID | `string` 或 `int` | 主键 |
| EquipSlot | 装备槽 | `enum` | `Mount` / `Wing` |
| NamePrefix | 名字前缀 | `string` | 参与 `WarriorName`；两件都装备则依次拼接 |
| SpiritCost | 精魂消耗 | `int` 或 `float` | ≥ 0；缺省 0 |
| ControlPowerCost | 控制力占用 | `int` 或 `float` | ≥ 0 |
| EquipStats | 平坦属性加成 | 见编码 | 五项同名平坦加成；加法进 Equip 层 |
| Skills | 额外技能 | 见编码 | 编码同 [§9.9 `SoulConfig.Skills`](#99-灵魂配置表-soulconfig)；失控加成见 [§9.21](#921-技能配置表骨架-skillconfig) |

**`EquipStats` 编码（固定）：** 与 [§9.12](#912-躯体材料配置表-bodypartconfig) `StatBonus` / [§9.17](#917-科技项效果配置表-techeffectconfig) `AttributeModifiers` 同风格：`属性项_数值|属性项_数值|…`（键：`MaxHP` / `MoveSpeed` / `Strength` / `Agility` / `Intelligence`；空 = 无；加法）。

**视觉（Mount / Wing）：** 本批默认将坐骑/翅膀外观 **打进** 对应士兵 `AppearanceId` 的烘焙整角变体；**不**做运行时叠装层。运行时模块化叠装另专题。数值与命名字段仍按本表生效。

```
ExtraEquipmentConfig {
  EquipId: Id
  EquipSlot: Mount | Wing
  NamePrefix: string
  SpiritCost: number
  ControlPowerCost: number
  EquipStats: "Attr_Value|..."    // same style as StatBonus
  Skills: "SkillId;Level|..."     // same as SoulConfig.Skills
}
```

#### 9.15 宝石后缀命名表 `GemSuffixNameConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 士兵命名后缀。一行 = 一种已镶嵌宝石组合 → 后缀。

**磁盘名：**
- **Excel：** `制造_宝石后缀命名表_Manufacture_GemSuffixNameConfig.xlsx`
- **CSV：** `Manufacture_GemSuffixNameConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| ComboKey | 组合键 | `string` | 主键；见编码 |
| Suffix | 后缀 | `string` | 拼入 `WarriorName` 末段；无匹配可空 |

**`ComboKey` 编码（固定）：** 取实例已镶嵌宝石的 `GemType` 集合 → **按枚举名字典序排序** → 用 `|` 拼接。例：`Amethyst|Diamond|Ruby`。无宝石 → 不查表，后缀为空。

```
GemSuffixNameConfig {
  ComboKey: string                // sorted GemType names joined by |
  Suffix: string
}
```

**解析：** 制造完成时按实例 `GemIds` → 各 `GemType` → 推导 `ComboKey` 查本表得后缀；无宝石或无匹配行 → 后缀为空。

#### 9.16 科技树配置表 `TechTreeConfig`

规则语义：[SPEC_03 §3.13](SPEC_03_GameRules.md)。一行 = 一个科技项。

**磁盘名：**
- **Excel：** `科技_科技树配置表_Tech_TechTreeConfig.xlsx`
- **CSV：** `Tech_TechTreeConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| TechId | 科技项ID | `string` 或 `int` | 主键 |
| IconId | 科技项图标 | `string` 或 `int` | 图标资源 / 图集键；具体资源路径实现时定 |
| DisplayName | 科技项名字 | `string` | 展示名；若启用 i18n 则为本地化 Key |
| EffectDescription | 科技项效果描述 | `string` | 悬停/详情文案；若启用 i18n 则为本地化 Key |
| UnlockNextTechIds | 解锁后续科技项ID | `string` | 编码 **`Id\|Id\|Id`**（管道分隔）；空 = 叶节点 |
| InitiallyUnlocked | 初始是否解锁 | `bool` | `true` → 新建档 **自动学会** 并应用效果；中心根项须为 `true` |
| LearnCost | 学习费用 | `int` | 科技点；≥ 0；自动学会项通常为 **0** |
| TechUiFrameType | 科技项类型 | `enum` / `string` | `Root` \| `Normal` \| `Key` \| `Capstone`；驱动科技树界面 UI 框样式 |

```
TechTreeConfig {
  TechId: Id
  IconId: Id
  DisplayName: string
  EffectDescription: string
  UnlockNextTechIds: "Id|Id|Id"   // empty = leaf
  InitiallyUnlocked: bool
  LearnCost: int                  // TechPoints; usually 0 if InitiallyUnlocked
  TechUiFrameType: Root | Normal | Key | Capstone
}
```

**约定：**

| 规则 | 说明 |
|------|------|
| 默认镜头 | 表 **第一行** 对应节点 = 打开画布时默认镜头焦点 |
| 中心根项 | 须 `InitiallyUnlocked = true`；建议同时为表第一行 |
| 空间坐标 | **不**在本表；由设置页科技树 Prefab 摆放 |
| 前置 | 不存表；由 `UnlockNextTechIds` 正向边求逆 |

#### 9.17 科技项效果配置表 `TechEffectConfig`

规则语义：[SPEC_03 §3.13](SPEC_03_GameRules.md)。学会某 `TechId` 时应用本表效果。

**磁盘名：**
- **Excel：** `科技_科技项效果配置表_Tech_TechEffectConfig.xlsx`
- **CSV：** `Tech_TechEffectConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| TechId | 科技项ID | `string` 或 `int` | 关联 `TechTreeConfig.TechId`；本版建议 **1:1**；同 ID 多行则效果合并 |
| AttributeModifiers | 增加的属性 | `string` | 编码 **`属性项_数值\|属性项_数值\|…`**（加法）；空 = 无属性增量 |
| UnlockedFeatureSystemName | 解锁的功能系统名 | `string` | 非空 → 写入存档 `UnlockedFeatureSystems`；空 = 无；**开放名单**（PascalCase）；实现时校验已知名，未知名打日志并仍写入集合；名单随玩法扩展回写 SPEC。属性增量（如 `DigDamage`）走 `AttributeModifiers`，**不**走本字段 |

```
TechEffectConfig {
  TechId: Id
  AttributeModifiers: "Attr_Value|Attr_Value|..."  // additive
  UnlockedFeatureSystemName: string               // empty = none; open PascalCase list
}
```

**本版文档化属性键（可扩展）：**

| 属性键 | 写入目标 | 说明 |
|--------|----------|------|
| DigDamage | `DigProtagonistCapabilities.DigDamage` | 挖坟单次伤害 |
| DigDurationReductionSum | `DigProtagonistCapabilities.DigDurationReductionSum` | 缩短单次挖坟时长（秒）；挖坟单次速度 |
| DigCursorRadius | `DigProtagonistCapabilities.DigCursorRadius` | 光标半径（若效果使用） |
| DigStageDurationBonus | `DigProtagonistCapabilities.DigStageDurationBonus` | 挖坟阶段时长加成（秒） |

其它属性键（如控制力上限加成）后续补充；同键多科技 **加法求和** 后写入派生能力。

**功能系统名示例（开放，非封闭）：** `FeatureDigAdvancedQualities` 等；以非空写入存档为准。

#### 9.18 刷怪波次配置表 `WaveSpawnConfig`

规则语义：[SPEC_03 §3.12](SPEC_03_GameRules.md)。一行 = 一次刷怪事件；按 `WaveConfigId` 分组，由 `DefendGameplayConfig.WaveConfigId` 引用。

**磁盘名：**
- **Excel：** `防守_刷怪波次配置表_Defend_WaveSpawnConfig.xlsx`
- **CSV：** `Defend_WaveSpawnConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| WaveConfigId | 波次配置ID | `string` 或 `int` | 分组键；同 ID 多行 = 该防守配置的全部刷怪事件 |
| SpawnOrder | 出怪顺序 | `int` | **仅**当多行 `SpawnRemainingSeconds` 相同时，按升序决定先后；不同剩余秒时忽略相对顺序 |
| SpawnRemainingSeconds | 出怪时间（剩余秒） | `int` | 激活条件：`RemainingCombatSeconds ==` 本字段（整秒） |
| MonsterId | 怪物ID | `string` 或 `int` | FK → `MonsterConfig`（§9.19） |
| SpawnCount | 出现数量 | `int` | ≥ 1 |
| AppearLocation | 出现位置 | `enum` / `string` | `InsideMap` \| `OutsideMap` |
| SpawnMode | 出怪方式 | `enum` / `string` | `RegionRandom` \| `ClockDirection` |
| SpawnClockHour | 几点钟方向 | `int` | 1–12；仅 `SpawnMode = ClockDirection` 时有效；其它方式可空 |

```
WaveSpawnConfig {
  WaveConfigId: Id
  SpawnOrder: int
  SpawnRemainingSeconds: int
  MonsterId: Id
  SpawnCount: int                    // >= 1
  AppearLocation: InsideMap | OutsideMap
  SpawnMode: RegionRandom | ClockDirection
  SpawnClockHour: int?               // 1–12; required when ClockDirection
}
```

**激活规则（与 §3.12 配套）：** `Combat` 中每当剩余整秒等于某行 `SpawnRemainingSeconds` 时触发尚未触发的匹配行；同秒多行按 `SpawnOrder` 升序。出生点精确几何 **TBD**。

#### 9.19 怪物配置表 `MonsterConfig`

规则语义：[SPEC_03 §3.12](SPEC_03_GameRules.md)。一行 = 一种怪物。

**磁盘名：**
- **Excel：** `防守_怪物配置表_Defend_MonsterConfig.xlsx`
- **CSV：** `Defend_MonsterConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| MonsterId | 怪物ID | `string` 或 `int` | 主键；被 `WaveSpawnConfig` 引用 |
| ModelId | 怪物模型ID | `string` | Prefab 逻辑名（无路径、无扩展名）；运行时解析 → `Assets/Prefabs/Defend/Monsters/{ModelId}.prefab`；美术源 `Assets/Art/Characters/Monsters/{ModelId}/`（Character Creator 烘焙整角，见 [§15](#15-角色美术管线character-creator-烘焙整角)） |
| DisplayName | 怪物名称 | `string` | 展示名或本地化 Key（若启用 i18n） |
| TargetSelect | 目标选择 | `enum` / `string` | `Nearest` \| `PreferWarrior` \| `PreferProtagonist`（与 `SoulConfig.AttackPriority` 同枚举） |
| AttackMode | 攻击模式 | `enum` / `string` | `Melee` \| `Ranged` |
| AggroMode | 仇恨模式 | `enum` / `string` | `ActiveChase` \| `PassiveChase` \| `StationaryActive` \| `StationaryPassive`；见 [SPEC_03 §3.14](SPEC_03_GameRules.md)；**加载缺省：** 列缺失或空单元格 → `ActiveChase`；非法值 → 加载失败 |
| AlertRadius | 警戒半径 | `float` | ≥ 0；主动发现半径；**加载缺省：** 列缺失或空 → `AttackRange`；若解析值 < 0 → 加载失败 |
| BodyRadius | 占地半径 | `float` | ≥ 0；XZ 占地圆半径（世界单位）；PushMap 刷出散开与移动怪 `NavMeshAgent.radius` 共用；**加载缺省：** 列缺失或空 → `0.35`；若解析值 < 0 → 加载失败 |
| MaxHP | 怪物血量 | `int` 或 `float` | 生成时初始化怪物 maxHP / 当前 HP |
| MoveSpeed | 怪物移动速度 | `float` | 世界单位/秒或项目统一速度单位 |
| AttackPower | 怪物攻击力 | `int` 或 `float` | **仅**攻击士兵时用于伤害结算；对主角普通攻击不用本字段 |
| AttackSpeed | 攻击速度 | `float` | 攻击频率（具体单位实现时锁定） |
| AttackRange | 攻击距离 | `float` | 进入攻击态距离 |
| MeleeWindupSeconds | 近战前摇 | `float` | ≥ 0；秒；`AttackMode=Melee` 时用 |
| RangedProjectileSpeed | 远程弹速 | `float` | ≥ 0；`AttackMode=Ranged` 时用 |
| RangedTimeoutSeconds | 远程超时 | `float` | ≥ 0；秒；超时未命中 → 未命中 |
| Skills | 怪物技能 | 见编码 | 技能 ID + CD 列表；技能效果列另专题；**第一版 Demo 不生效**（只打普通攻击；实现时可忽略或配空） |
| LootDrop | 怪物掉落 | 见编码 | 击杀产出；编码同 Dig `LootDrop` |

**`Skills` 编码（固定）：** `SkillId_CdSeconds|SkillId_CdSeconds|...`

- 段分隔符：`|`
- 段内：`技能ID_冷却秒`
- 空字符串 = 无技能
- 技能效果定义表本批 **不定**；Demo v1 即使有值也 **不施放**
- **注意：** 与士兵侧 `SkillId;Level|…` **编码不同**（怪物 CD 写在本表）

**`LootDrop` 编码：** 与 [§9.3](#93-坟墓品质定义表-gravequalityconfig) 相同：`Id_Count|Id_Count|...`

```
MonsterConfig {
  MonsterId: Id
  ModelId: string                  // Prefab logical name → Assets/Prefabs/Defend/Monsters/{Id}.prefab; art §15
  DisplayName: string
  TargetSelect: Nearest | PreferWarrior | PreferProtagonist
  AttackMode: Melee | Ranged
  AggroMode: ActiveChase | PassiveChase | StationaryActive | StationaryPassive  // empty → ActiveChase
  AlertRadius: number              // empty → AttackRange
  BodyRadius: number               // empty → 0.35
  MaxHP: number
  MoveSpeed: number
  AttackPower: number              // soldiers only
  AttackSpeed: number
  AttackRange: number
  MeleeWindupSeconds: number
  RangedProjectileSpeed: number
  RangedTimeoutSeconds: number
  Skills: "SkillId_Cd|SkillId_Cd|..."
  LootDrop: "Id_Count|Id_Count|..."
}
```

**加载约定（`ConfigCsvRepository`）：** `AggroMode` / `AlertRadius` / `BodyRadius` 缺省如上；非法枚举或 `AlertRadius < 0` / `BodyRadius < 0` 整表加载失败（§14.5）。PushMap 与 Defend 共用本表。

#### 9.20 失控配置表 `LossOfControlConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) / [§3.12](SPEC_03_GameRules.md) 失控程度段与基础失控概率。一行 = 一个失控程度段（固定 4 行：TierId 1~4）。

**磁盘名：**
- **Excel：** `战斗_失控配置表_Combat_LossOfControlConfig.xlsx`
- **CSV：** `Combat_LossOfControlConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| TierId | 失控程度段 | `int` | 主键；**1~4**（轻度 / 中度 / 重度 / 完全）；与 Degree 区间映射见 SPEC_03 §3.11 |
| DisplayName | 名称 | `string` | 展示名或本地化 Key（若启用 i18n） |
| Description | 描述 | `string` | 档位说明文案或本地化 Key |
| LossOfControlChance | 失控概率 | `float` | 该档 **基础**失控概率；取值 **\[0, 1\]**（0=0%，1=100%）；具体数值 **TBD** |

```
LossOfControlConfig {
  TierId: 1 | 2 | 3 | 4
  DisplayName: string
  Description: string
  LossOfControlChance: number     // [0, 1]
}
```

**解析：** 开战锁定 `LossOfControlDegree` 后映射 `TierId` → 读本表 `LossOfControlChance` 作为 `TierChance`。最终失控率与叛变见 SPEC_03 §3.11 / §3.12。

#### 9.21 技能配置表骨架 `SkillConfig`

规则语义：士兵/怪物技能定义表完整效果字段 **另专题**；本批锁定失控加成与 **基础 CD**，供 Soul / Gem / ExtraEquipment 的 `Skills` 列表解析；实际冷却见 §3.12 公式。**第一版 Demo 不施放技能**，本表与 CD 公式均不驱动战斗。**本批不扩效果列**；完整效果列后续沿用同一磁盘文件名扩写。

**磁盘名：**
- **Excel：** `战斗_技能配置表_Combat_SkillConfig.xlsx`
- **CSV：** `Combat_SkillConfig.csv`（完整技能效果列后续扩写时沿用本文件）

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| SkillId | 技能ID | `string` 或 `int` | 主键；被灵魂/宝石/装备/怪物 `Skills` 引用 |
| BaseCooldownSeconds | 基础冷却 | `float` | ≥ 0；单位秒；士兵实际 CD = `max(SkillCdFloor, BaseCooldownSeconds − SkillCdIntDiv/max(Int,1))`（系数见 `ClassConfig.CombatConvertCoeffs` / §3.12） |
| LossOfControlChanceBonus | 失控概率加成 | `float` | 可正可负；缺省 **0**；士兵最终率中 `ΣSkillBonus` = 其全部技能本字段之和（§3.11） |

```
SkillConfig {
  SkillId: Id
  BaseCooldownSeconds: number     // >= 0; actual CD per §3.12
  LossOfControlChanceBonus: number  // +/- ; default 0
  // skill-effect columns deferred; same file when expanded
}
```

**说明：** 当士兵 `ΣSkillBonus ≠ 0` 时，每次释放技能额外用完整 `FinalLossChance` 再判定一次（§3.11）。技能效果、等级成长等其余列 **另专题**（文件名不变）。


#### 9.22 推图战配置表 `PushMapGameplayConfig`

规则语义：[SPEC_03 §3.14](SPEC_03_GameRules.md)。一行 = 一个推图战关卡配置。

**磁盘名：**
- **Excel：** `推图战_推图战配置表_PushMap_PushMapGameplayConfig.xlsx`
- **CSV：** `PushMap_PushMapGameplayConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| GameplayConfigId | 玩法配置ID | `string` 或 `int` | 主键；被 `LevelOperationConfig` / ModeSelect 引用 |
| MapId | 地图编号 | `string` | Prefab 逻辑名；合法值 **`Ground_01`…`Ground_05`** 或 **`PushMap_*`**；解析 → `Assets/Prefabs/Maps/{MapId}.prefab`；**≠** LevelId |
| DisplayName | 关卡显示名 | `string` | 展示名或本地化 Key |
| StageExpReward | 阶段经验奖励 | `int` | BOSS 通关入账 `LifetimeExperience` 的数值；Demo 可固定 |
| CaptureLoot | 占领默认掉落 | 见编码 | 可选；各目标点占领时发放（若目标点无独立覆盖）；编码同 Dig `LootDrop`：`Id_Count\|…` |
| DungeonUnlockIds | 副本解锁ID列表 | 见编码 | 通关或占领写入存档钩子；段分隔 `\|`；空=无；**副本玩法 TBD** |
| CaptureSeconds | 占领所需秒 | `float` | **加载缺省 5**（列缺失或空）；判定圈连续无怪秒数；< 0 → 加载失败 |
| Notes | 备注 | `string` | 可选 |

```
PushMapGameplayConfig {
  GameplayConfigId: Id
  MapId: Ground_01 | PushMap_*   // Prefabs/Maps/{MapId}.prefab
  DisplayName: string
  StageExpReward: int
  CaptureLoot: "Id_Count|Id_Count|..."
  DungeonUnlockIds: "DungeonId|DungeonId|..."
  CaptureSeconds: number         // default 5
}
```

**地图 Prefab 标记契约（与 §13 配套；方案 A / PM-01）：**

- **脚本：** `Assets/Scripts/Gameplay/PushMap/`，命名空间 `Gravedigger2026.Gameplay.PushMap`
- **样例地图：** `Assets/Prefabs/Maps/PushMap_Demo_01.prefab`（`MapId=PushMap_Demo_01`）；由 Editor 菜单 `Gravedigger2026/PushMap/Ensure Sample Map Prefab` 从 `Ground_01` 复制并挂齐标记；**不**改写 Dig/Defend 共用的 `Ground_*`
- **复用：** `EngageZone`（`Gameplay.Defend`）、`WalkSurface`（`WalkSurfaceIsoDiamond` / `Gameplay.Maps`）；地面仍为 Isometric Tilemap（可用 Tile Palette）
- **本片边界：** 仅标记契约与可 Instantiate 样例；占领/刷怪/空气墙 NavMesh **不做**（PM-04/05/08）

| 组件 | 字段（SerializeField） | 说明 |
|------|------------------------|------|
| `ObjectivePoint` | `ObjectiveOrder:int`（≥1）；引用 `CaptureZone` | 有序目标点；`CaptureZone` 可同节点或子节点 |
| `CaptureZone` | `Radius:float` 默认 **2** | XZ 圆形判定圈；`ContainsXZ`；半径可改 |
| `AirWall` | `HalfExtents:Vector3`（薄墙占位） | 阻挡体作者点；**Y 轴欧拉角**支持 0°/45°/90°…；运行时阻挡见下「空气墙 NavMesh 契约（PM-08）」 |
| `SpawnPoint` | `SpawnPointId:string` | 与 `PushMapSpawnConfig.SpawnPointId` 匹配 |
| `TrapZone` | `TrapZoneId:string`；`Radius:float` | XZ 圆形区域占位；与刷怪表 `TrapZoneId` 匹配 |
| `BossPoint` | （无必填字段；位置=`Transform`） | BOSS 生成点占位；通关逻辑见后续片 |

```
// Prefab marker components (authoring; no capture/spawn runtime in PM-01)
ObjectivePoint { ObjectiveOrder: int>=1; CaptureZone }
CaptureZone    { Radius: number = 2 }          // XZ circle
AirWall        { HalfExtents: Vector3 }      // Transform.eulerAngles.y = 0|45|90|…
SpawnPoint     { SpawnPointId: string }
TrapZone       { TrapZoneId: string; Radius: number }
BossPoint      { }                           // transform.position
// + EngageZone, WalkSurface (existing)
```

**占领运行时契约（方案 A / PM-04）：**

- **规则归属：** 目标链与计时在 `PushMapSessionService`（`Core/PushMap/`）；`ObjectivePoint`/`CaptureZone` 仅标记，运行时不自管 Tick
- **Session 面：** `CaptureSecondsRequired`（`Config.CaptureSeconds`，钳 ≥0.01）；`CurrentObjectiveOrder`（未开或全占=0）；`IsObjectiveCaptured(order)`；`TryBeginObjectiveChain(IEnumerable<int> orders)`（开战调用；排序去重 → 最小未占）；`TickCapture(float dt, bool hasLivingMonsterInCurrentZone)`（有怪清零；否则累计；达标→标记已占+事件+切下一目标）
- **事件：** `ObjectiveCaptured(int order)`（停刷钩子 → PM-05）；`CurrentObjectiveChanged(int newOrder)`（推进/表现）
- **表现：** `PushMapStageController` Combat 收 `ObjectivePoint` 排序喂 Session；每秒 `Update` 探测当前圈内存活怪（默认扫描 `_monsters`：`IsAlive && CaptureZone.ContainsXZ(position)`；Rebel 不算阻挡）；探测经 `PushMapMonsterPresenceProbe`（同目录薄组件，可注入/重置验收占位）；占领日志+HUD 状态
- **推进（MP-04 / 方案 B）：** 忠诚士兵共享 `CurrentObjective` → `FlowFieldService` 单场；`PushMapAdvanceView` 采样场方向 + `MassMoveScheduler`/`LocalDetour` 友军绕行后 `NavMeshAgent.Move`（**禁止**每兵每帧 `SetDestination(Objective)`）；圈内有存活怪**不**暂停推进（探测仅喂 `TickCapture`）；Rebel 不推进
- **追击/交战（MP-05 / 方案 B）：** 忠诚兵遇敌检测内 → `GoalKind=AttackSlot`（`AttackSlotService.TryClaim`）+ LocalDetour，停跟 Objective 场；离开后释放槽恢复 `Objective`。怪物追击目的地同为认领槽（非目标中心）；`MassMoveScheduler.SetGoal`；槽位重算每帧 ≤50 轮转；死亡/`Release`/`ReleaseAllForTarget`
- **Defend 对等（MP-06 / 方案 B）：** `DefendStageController` 持有同一套 `MassMoveScheduler`+`AttackSlotService`；`WarriorAgentView` Engage 内追击→`AttackSlot`，无候选→`GoalKind=FormationHome`（直趋+LocalDetour；返回途中继续选敌即中断）；`MonsterAgentView` 追击走槽位；无全员每帧 `SetDestination`/`CalculatePath`；与 PushMap 目的地语义一致
- **FlowField 重建：** 开战 Bake（AirWall→`StaticBoxWalkableMask`）后 `Configure`+`Rebuild`；`CurrentObjectiveChanged` → 再 `Rebuild`（日志可验单次 RebuildCount）；同目标单位共享一场
- **开战顺序（表现）：** Bake NavMesh（含 AirWall）→ 建可走掩码+FlowField → `TryBeginObjectiveChain` → 部署忠诚兵（注册 Scheduler）→ `FireStartBattleSpawns`（怪注册 Scheduler+AttackSlot）→ 失控 roll
- **可走面：** 样例 `PushMap_Demo_01` 的 `DigMapBounds`/`WalkSurface`/`EngageZone` 须覆盖目标点与刷怪点（含 BossPoint）
- **边界：** 占领探测与推进解耦；无怪时连续 `CaptureSeconds` → 1→2 切换；停刷仅事件/日志；占领奖励/副本解锁见 PM-07；完整命中 polish 后置；压测入口见 MP-07 / §9.7

```
// PM-04 / MP-04+MP-05+MP-06 rules/presentation touchpoints
session.TryBeginObjectiveChain(sortedOrders)
session.TickCapture(dt, probe.HasLivingMonster(currentZone))
event ObjectiveCaptured(order)   // PM-05 stops linked spawns; PM-07 capture loot/unlock
event CurrentObjectiveChanged(order) → FlowFieldService.Rebuild(goal, airWallMask)
PushMapMonsterPresenceProbe { HasLivingMonster(CaptureZone) }  // capture timer only
FlowFieldService + StaticBoxWalkableMask(AirWall OBBs)
AttackSlotService.TryClaim / Release / ReleaseAllForTarget
MassMoveScheduler.SetGoal(Objective|AttackSlot|FormationHome) + Tick  // ≤50 steer + ≤50 slot refresh/frame
PushMapAdvanceView { field or AttackSlot Move; no SetDestination; rebel skip }
PushMapMonsterAgentView { AttackSlot chase via scheduler; AggroMode preserved }
DefendStageController { warriors+monsters on same scheduler; FormationHome return }
WarriorAgentView / MonsterAgentView { AttackSlot or FormationHome Move; no center SetDestination }
```

**BOSS 通关与奖励运行时契约（方案 A / PM-07）：**

- **规则归属：** 胜负与待击杀 BOSS 计数在 `PushMapSessionService`；`BossPoint` 仅位置标记
- **计数：** `FireRow` 且 `IsBoss` → `_pendingBossCount += SpawnCount`；开战后若 `_pendingBossCount>0` 且表现层报告无 `BossPoint` → warn（一致性约定）
- **击杀：** `TryNotifyBossKilled()`（Combat 且未结算）→ 递减；归零 → `Ended` + `VictorySettled(Config.StageExpReward)`；同时写 `DungeonUnlockIds` 钩子事件/回调
- **失败：** 既有 `RequestLevelFailure` → `LevelFailureRequested`；**禁止**再发 `VictorySettled`
- **占领奖励：** `Capture` 时解析 `Config.CaptureLoot`（`LootDropParser`）经表现层/`Warehouse` 入账；写 `DungeonUnlockIds`；**不**加经验
- **存档钩子：** `DungeonUnlockService`（`Core/PushMap/` 或 Meta）：按存档槽 `HashSet` + PlayerPrefs；`TryUnlock(id)` 日志可验；副本玩法正文不做
- **表现：** `PushMapMonsterAgentView.IsBoss`；`PushMapStageController` Demo 击杀＝忠诚兵首次进 BOSS `AttackRange` → `NotifyKilled` + `TryNotifyBossKilled`；订阅 `VictorySettled` → `AddExperience` → `_onVictoryAdvance`；LevelFailure 路径不变

```
// PM-07 rules/presentation touchpoints
session.TryNotifyBossKilled()
event VictorySettled(stageExp)           // StageExpReward
event CaptureRewardsRequested(loot, unlockIds)  // or credit in controller on ObjectiveCaptured
DungeonUnlockService { BindSlot; TryUnlock; UnlockedIds }
PushMapMonsterAgentView.IsBoss
```

**空气墙 NavMesh 契约（方案 A / PM-08）：**

- **权威阻挡：** 开战 Runtime Bake（复用 `DefendNavMeshBaker`）在 IsoDiamond 可走 `Mesh` source 之外，收集地图 `AirWall`，追加 `NavMeshBuildSource`：`shape=Box`、`size=HalfExtents×2`、`transform=TRS(position, rotation, 1)`、`area=Not Walkable`（`NavMesh.GetAreaFromName`；失败回退 `1`）
- **旋转：** Prefab `Transform.eulerAngles.y` 支持 0°/45°/90°…；样例 `AirWall_45` 为 45°
- **作用对象：** 敌我士兵与怪物凡走 `NavMeshAgent` 的路径均不可穿（含 FlowField 掩码阻挡推进方向；怪物追击仍走 NavMesh）；AirWall 同时写入 `StaticBoxWalkableMask` 供 FlowField
- **接线：** `PushMapStageController` StartBattle 收集 `GetComponentsInChildren<AirWall>` → `Bake(..., notWalkableBoxes)` + 同批 OBB 建 FlowField 掩码
- **边界：** **不做** `NavMeshObstacle` Carve、复杂多层障碍 polish；Defend 无 AirWall 时仍走无障碍重载

```
// PM-08 bake touchpoints
DefendNavMeshBaker.Bake(center, halfExtents, notWalkableBoxes)
NavMeshBoxObstacle { Center, Size=HalfExtents*2, Rotation }
AirWall { HalfExtents }  // authoring; Y euler 0|45|90|…
```

#### 9.23 推图战刷怪配置表 `PushMapSpawnConfig`

规则语义：[SPEC_03 §3.14](SPEC_03_GameRules.md)。一行 = 某关卡某刷怪点的一次刷怪定义；同点可多行（多种怪物）。

**磁盘名：**
- **Excel：** `推图战_刷怪配置表_PushMap_PushMapSpawnConfig.xlsx`
- **CSV：** `PushMap_PushMapSpawnConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| GameplayConfigId | 玩法配置ID | `string` 或 `int` | FK → `PushMapGameplayConfig` |
| SpawnPointId | 刷怪点编号 | `string` 或 `int` | 与地图 Prefab `SpawnPoint` 标记匹配 |
| MonsterId | 怪物ID | `string` 或 `int` | FK → `MonsterConfig` |
| SpawnCount | 数量 | `int` | ≥ 1 |
| LinkedObjectiveOrder | 关联目标点序号 | `int` | 可选；该目标占领后本行停刷；空/0=开战即符合「未占领」语义的全局点 |
| TrapZoneId | 陷阱区域编号 | `string` 或 `int` | 可选；空=无陷阱，开战且关联目标未占领时刷；非空=忠诚士兵进入该陷阱才刷 |
| IsBoss | 是否BOSS | `bool` / `0\|1` | 1=该刷怪为 BossPoint 通关目标（须与地图 BossPoint 一致） |
| SpawnOrder | 同点出怪顺序 | `int` | 同 SpawnPointId 多行时升序 |

```
PushMapSpawnConfig {
  GameplayConfigId: Id
  SpawnPointId: Id
  MonsterId: Id
  SpawnCount: int
  LinkedObjectiveOrder: int      // 0 = global / start-eligible
  TrapZoneId: Id | ""            // empty = non-trap StartBattle spawn
  IsBoss: 0 | 1
  SpawnOrder: int
}
```

**刷怪运行时契约（方案 A / PM-05）：**

- **规则归属：** 刷怪资格与触发状态在 `PushMapSessionService`（`Core/PushMap/`）；`SpawnPoint`/`TrapZone` 仅作者标记，运行时不自管判定
- **装载：** `TryStartBattle` 时装载本 `GameplayConfigId` 全部行；**不**立即刷怪。表现层在 Bake NavMesh + 部署完成后调用 `FireStartBattleSpawns()`：无陷阱且关联目标未占的行 → 发 `PushMapSpawnRequested`（同 `SpawnPointId` 按 `SpawnOrder` 升序）；陷阱绑定点进入 `Pending` 待触发
- **事件：** `PushMapSpawnRequested(PushMapSpawnRequest)`；负载含 `SpawnPointId` / `MonsterId` / `SpawnCount` / `LinkedObjectiveOrder` / `IsBoss` / `SpawnOrder` / `Trigger`（`StartBattle` / `Trap`）；**位置由 View 按 `SpawnPointId` / `BossPoint` 解析**，再按 `MonsterConfig.BodyRadius` 与场上存活怪占地圆做散开（环形/螺旋候选 → `NavMesh.SamplePosition`；PM-10）
- **陷阱触发：** `TryNotifyTrapEnter(trapZoneId)`（View 探测忠诚兵首次进圈）→ 未占领 + 未触发 → 该 `SpawnPointId` 全部行触发；每点本场仅一次
- **占领停刷：** `ObjectiveCaptured(order)` → 标记 `LinkedObjectiveOrder == order` 的点本场停刷；已触发怪的生死不受影响；`IsBoss=1` 的行 **不** 受占领停刷（BOSS 见 PM-07）
- **表现：** `PushMapStageController` 收集 `SpawnPoint`（`SpawnPointId`→`Transform.position`）与 `TrapZone`；订阅事件 Instantiate 怪 → `PushMapMonsterAgentView`（入 `_monsters`；Boss 用 `BossPoint` 位置；落点经 `PushMapSpawnSpread`）；Update 探测忠诚（`!IsRebel`）`PushMapAdvanceView` 首次进入 `TrapZone` → `TryNotifyTrapEnter`
- **占地与避障（PM-10）：** Bind 时 `_agent.radius = min(BodyRadius, max(0.05, AttackRange − 士兵Demo半径0.1 − 0.05))`；刷出散开仍用完整 `BodyRadius`；移动怪靠 NavMesh RVO 互避；`Stationary*` 不主动挪位；Defend 刷怪落点散开本片 **不做**；**我方士兵** Demo：`NavMeshAgent.radius=0.1`、`height=0.1`（`WarriorAgentView` / `PushMapAdvanceView`）
- **Demo 遇敌→AttackSlot（MP-05）：** `PushMapAdvanceView` 在忠诚兵中心距存活怪 ≤ `max(AttackRange, BodyRadius+0.1)` 时改 `GoalKind=AttackSlot`（认领环上槽 + LocalDetour `Move`；**停跟** FlowField）；离开后释放槽并恢复 `Objective`。怪物追击同样认领槽，**禁止**全员每帧 `SetDestination`/`CalculatePath` 到目标中心
- **怪物 AI 边界：** 本片怪用 Defend 默认追击语义（就近忠诚兵/主角；进 `AttackRange` 普攻；对主角 `ApplyShieldHit`；对兵日志）；AggroMode 四态后置 **PM-06**（见下「AggroMode 运行时契约」）；`MonsterAgentView` 仍绑定 `DefendSessionService` 不接入
- **AggroMode 运行时契约（PM-06）：** 四态在 `PushMapMonsterAgentView` 按 `config.AggroMode` 分支；`ActiveChase`：忠诚士兵进 `AlertRadius`→移动追击该兵直至怪死；`PassiveChase`：`_provoked=false` 静止，`NotifyProvoked()` 后移动追击；`StationaryActive`：永不移动，忠诚兵进 `AttackRange` 攻击、离开停；`StationaryPassive`：永不移动，须先 `NotifyProvoked()` 且目标仍在 `AttackRange` 才攻。主动发现与挑衅均**仅**对忠诚士兵（`!IsRebel`）。挑衅来源（Demo）：`PushMapStageController` 检测忠诚 `PushMapAdvanceView` 首次进入某被动怪 `AttackRange` → 调其 `NotifyProvoked()`（等效「士兵先攻击」；士兵 HP 后置）。命中仍 `AttackMode` 方案 D；主动态对主角不进 `AlertRadius` 主动发现（仅忠诚兵），但已被追击/就近规则命中主角时仍 `ApplyShieldHit`。普通怪真实士兵伤害 **不做**
- **边界：** 不使用 `WaveSpawnConfig` 倒计时；BOSS 通关 / 经验 / 占领奖励 / 副本解锁钩子见 §9.22 PM-07 契约

```
// PM-05 rules/presentation touchpoints
session.TryNotifyTrapEnter(trapZoneId)        // view first loyal-enter
event PushMapSpawnRequested(request)          // StartBattle | Trap
PushMapSpawnRequest { SpawnPointId, MonsterId, SpawnCount,
                      LinkedObjectiveOrder, IsBoss, SpawnOrder, Trigger }
PushMapMonsterAgentView { Bind(MonsterConfigRow, protagonist, warriors, onHitProtagonist) }
```

### English

**Status: Fields and encodings defined; config carrier closed** — table-driven data uses **Excel source + CSV output** (paths / naming / bake: [§14](#14-配置表工程约定与打表工具)). Non-table singleton tunables may still use ScriptableObject under `Assets/Settings/<Module>/` ([§13](#13-资源编排与可扩展性)).

Rules authority: [SPEC_03 §3.9](SPEC_03_GameRules.md), [§3.10](SPEC_03_GameRules.md), [§3.11](SPEC_03_GameRules.md), [§3.12](SPEC_03_GameRules.md), [§3.13](SPEC_03_GameRules.md), [§3.14](SPEC_03_GameRules.md).

Logical short names (e.g. `DigGameplayConfig`) are for SPEC / pseudocode / type ids; **on-disk filenames** — see each subsection’s **Disk name** lines and [§14](#14-配置表工程约定与打表工具) (Excel: `{SystemZH}_{TableZH}_{SystemEN}_{TableEN}`; CSV: `{SystemEN}_{TableEN}`).

#### Weighted-field common rules

All config **Weight** values follow:

| Rule | Notes |
|------|-------|
| Non-negative | `Weight` must be ≥ 0; negative = illegal (reject on load or skip segment + log; pick one at implementation and write back) |
| Zero drop | `Weight = 0` → **treat entry as absent**: strip after parse; excluded from weighted pick |
| Effective set | Only `Weight > 0` entries enter the pool; pick by weight share |
| Empty effective list | Per-mode semantics. Dig / `GraveSpawnWeights`: empty after filter → **abandon that spawn** (see [SPEC_03 §3.10](SPEC_03_GameRules.md)) |

#### 9.1 LevelOperationConfig

**Disk name:**
- **Excel:** `关卡_关卡运作表_Level_LevelOperationConfig.xlsx`
- **CSV:** `Level_LevelOperationConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| LevelId | 关卡ID | `string` or `int` | Multiple rows per Level |
| StageNumber | 阶段编号 | `int` | Ascending within Level; unique per Level recommended |
| GameplayType | 玩法类型 | `enum` / `string` | e.g. `Dig` / `UpgradeManufacture` / `Defend` / `PushMap` |
| GameplayConfigId | 玩法配置ID | `string` or `int` | **Dig** → `DigGameplayConfig` PK; **Defend** → **RecommendedConfigId** (ModeSelect default highlight; UM next-battle map preview; combat config = player pick — [SPEC_03 §3.12](SPEC_03_GameRules.md) D-044); **UpgradeManufacture** → **ignore** (may be non-empty; runtime must **not** resolve against any mode config / Dig/Defend rows; stage reads global tables such as `ProtagonistLevelConfig`). **No** separate `UpgradeManufactureGameplayConfig` (see [SPEC_03 §3.9](SPEC_03_GameRules.md)) |

#### 9.2 DigGameplayConfig

**Disk name:**
- **Excel:** `挖坟_挖坟配置表_Dig_DigGameplayConfig.xlsx`
- **CSV:** `Dig_DigGameplayConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| GameplayConfigId | 玩法配置ID | `string` or `int` | PK; referenced by Level Operation |
| DigMapId | 挖坟地图ID | `string` | Prefab logical name (no path/ext); allowed **`Ground_01`…`Ground_05`**; resolve → `Assets/Prefabs/Maps/{DigMapId}.prefab`; shared ground-variant pool with Defend `BattleMapId`; presentation = **Isometric Tilemap** (Tiles/Sprites under `Assets/Art/Maps/Tiles/`, copied from Example `Environment/Tiles`+`Sprites`); do not runtime-reference `SmallScaleInt/` — [§13](#13-unity-资源编排与可扩展性约定) / [§15](#15-角色美术管线character-creator-烘焙整角) |
| LevelDurationSeconds | 关卡时长限制 | `float` or `int` | **Base** duration (seconds); effective countdown = this field + `DigStageDurationBonus` (see [SPEC_03 §3.10](SPEC_03_GameRules.md) / §9.6) |
| InitialGraveCount | 开局基础生成坟墓数量 | `int` | N independent weighted rolls at start |
| SpawnRate | 倒计时过程中生成坟墓速率 | encoding | Every N seconds spawn M |
| GraveSpawnWeights | 坟墓出现概率权重 | encoding | Quality Id + weight list |

**`SpawnRate` encoding (fixed):** `N;M` — every N seconds spawn M (example `5;2`).

**`GraveSpawnWeights` encoding (fixed):** `QualityId;Weight|QualityId;Weight|...` (example `1;10|2;5|3;1`). Follow **Weighted-field common rules**: strip `Weight = 0`; pick among `Weight > 0`. Empty effective list → **abandon that spawn**. `QualityId` must resolve in `GraveQualityConfig` (§9.3). Example empty: `1;0|2;0`.

**Weighted pick:** filter to effective list, then one independent draw per grave (initial and ongoing). RNG API unbound.

**Placement:** sample DigMap continuous placeable space; avoid `DigObstacle` circles (Digger + uncleared Graves; radii on Prefabs). Retry up to **32** times per spawn; then abandon that spawn.

**Dig Prefab convention:** under `Assets/Prefabs/Dig/`, Digger and per-quality Grave Prefabs expose circle obstacle radius (`DigObstacleRadius`); one Grave Prefab per `QualityId`. Grave roots also carry `DigHitShape`: local XZ convex hull (≤12 verts) + `BoundingRadius`, offline-baked via Editor menu `Gravedigger2026/Dig/Bake All Grave Hit Shapes` (prefer `Sprite.GetPhysicsShape`, else alpha outline → hull → simplify); re-bake after art changes. Rules read baked verts only — no runtime Sprite/pixel reads. Digger visuals are Character Creator **baked whole characters**; fixed Prefab logical name `Digger` → `Assets/Prefabs/Dig/Digger.prefab`; art export sources: [§15](#15-角色美术管线character-creator-烘焙整角). Dig circle-cursor UI: `UiDigCursorRing` → `Assets/Prefabs/Dig/UiDigCursorRing.prefab` (dual circle layers: Stroke outer + Fill inner with fixed **screen**-pixel stroke gap; Fill white semi-transparent); bound on `DigPrefabCatalog`, instantiated by `DigCursorView` under Dig HUD Canvas: project `DigCursorRadius` to screen-pixel diameter, then ÷ `Canvas.scaleFactor` into `sizeDelta` (do not treat screen pixels as canvas units under Scale With Screen Size); circle Sprite source `Assets/Art/UI/Dig/Ui_DigCursor_Circle.png`. Dig map: `DigMapId` → `Assets/Prefabs/Maps/{DigMapId}.prefab`.

#### 9.3 GraveQualityConfig

**Disk name:**
- **Excel:** `挖坟_坟墓品质定义表_Dig_GraveQualityConfig.xlsx`
- **CSV:** `Dig_GraveQualityConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| QualityId | 坟墓品质ID | `string` or `int` | PK; referenced by `GraveSpawnWeights` |
| MaxHP | 总血量 | `int` or `float` | Init grave maxHP / current HP; concrete values filled later |
| LootDrop | 掉落内容 | encoding | Reward when dig succeeds (HP=0) |
| IconStyleHighId | 高血量图标ID | `string` | Remaining HP% **>65%**; empty = quality default Prefab/icon |
| IconStyleMidId | 中血量图标ID | `string` | Remaining HP% **30%–65%**; empty = default |
| IconStyleLowId | 低血量图标ID | `string` | Remaining HP% **<30%**; empty = default |

```
GraveQualityConfig {
  QualityId: Id
  MaxHP: number
  LootDrop: "Id_Count|Id_Count|..."
  IconStyleHighId: string
  IconStyleMidId: string
  IconStyleLowId: string
}
```

**Rules link ([SPEC_03 §3.10](SPEC_03_GameRules.md)):** spawn inits `GraveHP` from this table; remaining HP% drives `GraveIconStyle` via `IconStyleHighId` / `IconStyleMidId` / `IconStyleLowId` (empty → quality default); HP=0 uses `LootDrop` for `DigReward` fly-to-Digger; credit on arrival.

**`LootDrop` encoding (fixed):** `Id_Count|Id_Count|...`

- Segment separator `|`; within segment `Id_Count` (underscore).
- `Id` resolve order:
  1. Reserved Spirit Id string **`Spirit`** (case-sensitive) → credit SpiritEssence (not Warehouse).
  2. **`MaterialConfig.MaterialId`** → normal material; `AutoConvert` / UI icon from MaterialConfig.
  3. **`BodyPartConfig.BodyPartId`** → body material (same Id namespace as `MaterialId`; **must not collide**); stack cap **10000**; `AutoConvert` from BodyPart row; Warehouse / DigReward icon may use `ArtAssetId`.
- `Count`: positive integer (≥ 1).
- Empty / missing underscore / non-positive Count / Id unmatched above: **skip segment and log**, continue.
- Example: `Iron_3|Spirit_10|Bone_1`

#### 9.4 MaterialConfig

**Disk name:**
- **Excel:** `挖坟_材料配置表_Dig_MaterialConfig.xlsx`
- **CSV:** `Dig_MaterialConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| MaterialId | 材料ID | `string` or `int` | PK; referenced by `LootDrop` |
| AutoConvert | 自动兑换 | `int` or `float` | SpiritEssence per **1 excess** material over stack cap (≥ 0; 0 = discard excess, no Spirit) |
| AppearanceIconId | 外观图ID | `string` or `int` | Icon Id for Warehouse / DigReward UI |
| AssetPath | 素材路径 | `string` | Primary asset path (load convention at implementation) |
| WarehouseQualityOutlineId | 仓库品质外轮廓ID | `string` or `int` | Quality outline asset Id for **warehouse slot** display (dedicated set; not DigMap grave icons) |

Stack cap constant (rules, not a table field): **10000** ([SPEC_03 §3.10](SPEC_03_GameRules.md)).

```
MaterialConfig {
  MaterialId: Id
  AutoConvert: number
  AppearanceIconId: Id
  AssetPath: string
  WarehouseQualityOutlineId: Id
}
```

#### 9.5 CurrencyConfig

**Disk name:**
- **Excel:** `挖坟_货币配置表_Dig_CurrencyConfig.xlsx`
- **CSV:** `Dig_CurrencyConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| CurrencyId | 货币ID | `string` or `int` | PK; Spirit reserved Id string **`Spirit`** (aligned with `LootDrop` / [SPEC_03 §3.10](SPEC_03_GameRules.md)) |
| AppearanceIconId | 外观图ID | `string` or `int` | UI icon Id |
| AssetPath | 素材路径 | `string` | Primary asset path |
| WarehouseQualityOutlineId | 仓库品质外轮廓ID | `string` or `int` | If shown in slot-like UI, reuse warehouse quality outline Id |

```
CurrencyConfig {
  CurrencyId: Id          // e.g. "Spirit"
  AppearanceIconId: Id
  AssetPath: string
  WarehouseQualityOutlineId: Id
}
```

This version requires at least one row with `CurrencyId = Spirit`.

#### 9.6 DigProtagonistCapabilities (runtime derived; recalc from tech effects)

Learned tech results write save-slot `DigProtagonistCapabilities` (node table & costs: [§9.16](#916-科技树配置表-techtreeconfig) / [§9.17](#917-科技项效果配置表-techeffectconfig); rules: [SPEC_03 §3.13](SPEC_03_GameRules.md)):

```
DigProtagonistCapabilities {
  DigDamage: number
  DigDurationReductionSum: number   // seconds; sum of unlock shorten effects
  DigCursorRadius: number
  DiggableQualityIds: set<QualityId>
  DigStageDurationBonus: number     // seconds; additive to LevelDurationSeconds
}
// DigActionDuration = max(0.1, 0.8 - DigDurationReductionSum)
// EffectiveDigDuration = LevelDurationSeconds + DigStageDurationBonus
// Recalc from sum of learned TechEffectConfig.AttributeModifiers (additive per key)
```

#### 9.7 DefendGameplayConfig

**Disk name:**
- **Excel:** `防守_防守配置表_Defend_DefendGameplayConfig.xlsx`
- **CSV:** `Defend_DefendGameplayConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| GameplayConfigId | 玩法配置ID | `string` or `int` | PK; referenced by Level Operation |
| BattleMapId | 战斗地图ID | `string` | Prefab logical name (no path/ext); allowed **`Ground_01`…`Ground_05`** (shared ground-variant pool with Dig `DigMapId`); resolve → `Assets/Prefabs/Maps/{BattleMapId}.prefab` (incl. EngageZone + Isometric Tilemap ground + Demo WalkSurface/NavMesh — [§13](#13-unity-资源编排与可扩展性约定) / [§15](#15-角色美术管线character-creator-烘焙整角)) |
| WaveConfigId | 波次配置ID | `string` or `int` | FK → `WaveSpawnConfig` (§9.18) group key |
| CombatDurationSeconds | 战斗总时长 | `int` | Combat countdown init (whole seconds); remaining seconds drive spawn; hitting 0 does not alone decide win/lose |
| TargetRetargetIntervalSeconds | 目标修正间隔 | `float` | Seconds between attackable-destination recomputes for monsters **and soldiers**; default **1** |

```
DefendGameplayConfig {
  GameplayConfigId: Id
  BattleMapId: string          // Ground_01…Ground_05 → Assets/Prefabs/Maps/{Id}.prefab
  WaveConfigId: Id             // FK → WaveSpawnConfig
  CombatDurationSeconds: int   // whole seconds; countdown init
  TargetRetargetIntervalSeconds: number  // default 1
}
```

**Pathfinding tech (with [SPEC_03 §3.12](SPEC_03_GameRules.md); MassCombatPathing Approach B):**

- **Approach B (locked):** shared goals use **FlowField**; chase/attack use **AttackSlot** + **LocalDetour**; capacity ~200/side. See “Mass combat pathing runtime contract” below.
- Unity **NavMesh** (or walkable mask) expresses **static** walkability and `AirWall`; **do not** require 400 units each running high-frequency full-map `CalculatePath`.
- **Rules layer** outputs: current target entity Id + `GoalKind` (`Objective` / `FormationHome` / `AttackSlot` / `ChaseAnchor`) + optional `AttackRange`; must **not** write `Transform` / `Animator` directly.
- **Move service** resolves `DesiredDestination`: Objective→FlowField sample; attack→AttackSlot claim; friendly block→LocalDetour; presentation applies motion.
- Every `TargetRetargetIntervalSeconds` the rules layer reselects targets; slot/field samples may update on a shorter frame budget, but **forbid** full-map repath every frame for all units.
- **EngageZone**: **IsoDiamond** (XZ diamond) on the BattleMap **Prefab** (slightly smaller than the map; designer-tuned); non-Rebel soldiers pick nearest enemy only inside it. See [SPEC_03 §3.12](SPEC_03_GameRules.md) WarriorCombat.
- Soldier hit scheme D: branch by `SoulConfig.AttackMode` (copied onto instance at manufacture) — melee windup or ranged projectile; `AttackRange` / windup / projectile speed / timeout from [§9.9b `ClassConfig`](#99b-职业配置表-classconfig) (rules confirm damage; View plays anim/projectile). **Demo v1**: soldiers and monsters use normal attacks only; no skill casts.
- Obstacle bake / NavMesh surface: **Demo-min** — runtime bake from `DigMapBounds` **IsoDiamond**; PushMap `AirWall`: StartBattle bake appends **Not Walkable Box** (§9.22 PM-08). Static result feeds FlowField blockers / walkable mask. Complex obstacles and exact OutsideMap **deferred**.
- **Demo-min spawn points:** temp fixed markers on map Prefab or `InsideMap` random on walkable surface; exact OutsideMap geometry deferred.

**Mass combat pathing runtime contract (Approach B / MassCombatPathing):**

- **Behavior authority:** [SPEC_03 §3.12](SPEC_03_GameRules.md) Mass Combat Pathing; PushMap advance [§3.14](SPEC_03_GameRules.md)
- **Chosen approach:** B — FlowField (shared goals) + local chase AttackSlot + LocalDetour; difficulty 3; slices `.scratch/mass-pathing/issues/`
- **Suggested modules (on implement):**
  - `Assets/Scripts/Core/Pathing/` — pure C#: `FlowFieldService`, `AttackSlotService`, `SpatialHash2D`, `LocalDetourSolver`, frame-budgeted `MassMoveScheduler`
  - `Assets/Scripts/Gameplay/Pathing/` — view bridge: register units from StageController, apply motion; gradually replace/wrap heavy NavMeshAgent pathing in `PushMapAdvanceView` / `WarriorAgentView` / `*MonsterAgentView`
- **FlowField:** cell size Demo **0.25–0.5** world units; cover IsoDiamond; AirWall/non-walkable → impassable; rebuild on `CurrentObjectiveChanged` / StartBattle bake; units sample shared buffer — **no** per-unit full-map Dijkstra/A*
- **AttackSlot:** ring at `max(0.05, AttackRange − 0.05)`; N=12 melee / 8 ranged; claim ≤1 per attacker; recompute on retarget / target move > 0.5; walkability via `IAttackSlotWalkable` (stub now; SamplePosition later); `TryClaim(…, targetPos, …)` / `Release` / `ReleaseAllForTarget`
- **LocalDetour:** `SpatialHash2D` cell ≈ `0.5`; query radius ≈ `2*agentRadius+0.2`; forward cone + L/R probes (~`1.0`); optional soft separation via `separationScale` (reduce in engage); **forbid** friendly Carve; hot path reuses lists — no full-table O(n²)
- **API:** `Steer(desiredDir, self, neighbors, separationScale?)` → `steerDir` (`self` = XZ pos + radius; no neighbors → `steer ≈ desired`)
- **Perf budget:** ≤~400 movers → move logic target **≤ ~2.5 ms/frame**; ≤50 path/slot recomputes per frame (round-robin)
  - **Stress entry (MP-07):** `Assets/Scripts/Core/Pathing/MassPathingPerfStress.cs` (pure-C# Stopwatch, ~200/side) + `Assets/Scripts/Gameplay/Pathing/MassPathingPerfStressView.cs` (capsule/cube stubs) + Editor `Gravedigger2026/Pathing/Run MassPathing 200v200 Perf Stress`; measures `MassMoveScheduler.Tick` + ≤50 slot refresh — **not** Animator / all-units `CalculatePath`
  - **Over-budget fallbacks (try in order):** (1) raise FlowField `cellSize` (toward 0.5); (2) lower AttackSlot `N` (melee/ranged constants); (3) lower `MassMoveScheduler.MaxRecalcPerFrame` / slot-refresh budget (more frame-slicing; accept steer lag)
- **Transition:** until MP slices land, current single-Agent NavMesh may run; after acceptance, advance/chase must follow this contract — do not keep full HighQuality RVO as the scale solution
- **Out of scope:** full ORCA; destructible doors; multi-floor pathing; skill dash prediction

```
FlowFieldService.Rebuild(goal, walkableMaskInclAirWall)
FlowFieldService.SampleDir(worldPos) -> Vector2
AttackSlotService.TryClaim(attackerId, targetId, attackRange, targetPos, …) -> worldPos
LocalDetourSolver.Steer(desiredDir, self, neighbors, separationScale?) -> steerDir
MassMoveScheduler.Tick(dt)
MassPathingPerfStress.Run(perSide≈200) // MP-07 Debug Stopwatch
```

**Spawn / monster tables:** see §9.18 `WaveSpawnConfig`, §9.19 `MonsterConfig`; LossOfControl: §9.20 `LossOfControlConfig`, §9.21 `SkillConfig` (skeleton); rules in [SPEC_03 §3.12](SPEC_03_GameRules.md).

#### 9.8 ProtagonistLevelConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md). One row = one protagonist level.

**Disk name:**
- **Excel:** `制造_主角升级配置表_Manufacture_ProtagonistLevelConfig.xlsx`
- **CSV:** `Manufacture_ProtagonistLevelConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| Level | 当前等级值 | `int` | PK; ≥ 1; unique in table |
| RequiredTotalExperience | 升到本级需要的经验总值 | `int` or `long` | **Cumulative lifetime threshold** (not per-level delta); compared to save `LifetimeExperience`; level-up does **not** deduct owned Exp; level-1 row usually **0** |
| UnlockedFeatureIds | 升到本级解锁的功能 | see encoding | **Reserved only**; no runtime unlock this version |
| TechPointsReward | 升到本级奖励的科技点数 | `int` | Granted when first entering this level (≥ 0) |
| ControlPowerCap | 升到本级控制力上限变成的值 | `int` or `float` | Absolute ControlPower cap at this level; this version effective cap = this field (tech bonus later) |
| ProtagonistMaxHP | 升到本级主角的生命值上限 | `int` or `float` | Field name kept; **Defend StartBattle uses as Shield cap** (`Shield` init, see [SPEC_03 §3.12](SPEC_03_GameRules.md)) |

**`UnlockedFeatureIds` encoding (fixed):** `FeatureId|FeatureId|...`

- Segment separator: `|`
- Empty string = no reserved entries
- May be ignored at runtime this version; must not drive unlocks

```
ProtagonistLevelConfig {
  Level: int
  RequiredTotalExperience: number
  UnlockedFeatureIds: "Id|Id|..."
  TechPointsReward: int
  ControlPowerCap: number
  ProtagonistMaxHP: number
}
```

**Level-up resolution (with §3.11):**

- Save holds `Level`, `LifetimeExperience`.
- While a `Level+1` row exists and `LifetimeExperience >= RequiredTotalExperience(Level+1)`, chain level-ups; each step applies that row's reward and attributes.
- Concrete per-row numbers **TBD** (schema/semantics only this batch).

#### 9.9 SoulConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) soldier attribute composition / naming. One row = one soul.

**Disk name:**
- **Excel:** `制造_灵魂配置表_Manufacture_SoulConfig.xlsx`
- **CSV:** `Manufacture_SoulConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| SoulId | 灵魂ID | `string` or `int` | PK |
| ClassId | 职业ID | `string` or `int` | Required; FK → `ClassConfig`; written to soldier instance at manufacture |
| AttackMode | 攻击模式 | `enum` / `string` | `Melee` \| `Ranged`; soldier normal-attack hit scheme D branch (§3.12); same enum as monster `AttackMode`. Examples: Warrior-like→Melee; Archer/Mage-like→Ranged (Mage and Archer share Ranged channel; only `ClassConfig.PrimaryStat` differs) |
| Skills | 可使用技能 | encoding | Skill Id + level list; encoding below; LossOfControl bonus and CD in [§9.21 `SkillConfig`](#921-skillconfig-skeleton); **unused cast in Demo v1** (may leave empty) |
| AttackPriority | 攻击优先级 | `enum` / `string` | Same enum as monster `TargetSelect`: `Nearest` \| `PreferWarrior` \| `PreferProtagonist`; **does not drive** targeting this batch (default nearest in EngageZone, §3.12) |
| MoveStyle | 移动风格 | `enum` / `string` | `Normal` \| `Aggressive` \| `Cautious`; unknown → `Normal`; AI drive optional this batch |
| SpiritCost | 精魂消耗 | `int` or `float` | Added to manufacture Spirit total (≥ 0; default 0) |
| ControlPowerCost | 控制力占用 | `int` or `float` | This soul's contribution to soldier `ControlPowerCost` (≥ 0) |

**`Skills` encoding (fixed; shared by Soul / Gem / ExtraEquipment):** `SkillId;Level|SkillId;Level|…`

- Segment separator `|`; within segment `SkillId;Level`
- `Level`: positive integer (≥ 1); illegal segments **skip and log**
- Empty string = no skills
- **Different** from monster `MonsterConfig.Skills` (`SkillId_CdSeconds|…`)

```
SoulConfig {
  SoulId: Id
  ClassId: Id                     // FK → ClassConfig; required
  AttackMode: Melee | Ranged      // soldier normal-attack hit branch (§3.12)
  Skills: "SkillId;Level|..."     // soldier-side; unused cast in Demo v1
  AttackPriority: Nearest | PreferWarrior | PreferProtagonist
  MoveStyle: Normal | Aggressive | Cautious
  SpiritCost: number
  ControlPowerCost: number
}
```

**Note:** Former `InfoTags` no longer builds primary WarriorInfo (primary label = finalized Race). Soul does **not** rewrite Strength/Agility/Intelligence; it injects Class via `ClassId` and selects `AttackMode` for Melee/Ranged hit branch. `ClassName` / `PrimaryStat` / convert coeffs live in [§9.9b `ClassConfig`](#99b-classconfig).

**Soldier instance static snapshot (written at manufacture; not a config table):**

```
WarriorInstance {
  Id: Id
  WarriorName: string             // Prefix(es)+RaceName+ClassName+Suffix; ClassName via ClassId → ClassConfig
  RemainingHP: number
  RaceId: Id
  RaceAdjustCoeff: { MaxHP, MoveSpeed, Strength, Agility, Intelligence }
  BaseStats: { MaxHP, MoveSpeed, Strength, Agility, Intelligence }  // Base(S)=Σ StatBonus(S)
  AppearanceId: Id                // FK → BodyAppearanceConfig
  SoulId: Id
  ClassId: Id                     // copied from SoulConfig; FK → ClassConfig
  AttackMode: Melee | Ranged      // copied from SoulConfig at manufacture
  LockedEquipIds: Id[]
  GemIds: Id[]
  GemMult: { MaxHP, MoveSpeed, Strength, Agility, Intelligence }  // Σ of socketed; 0 if none
  ControlPowerCost: number
  EquipStats: { … five dims }     // manufacture-locked Equip layer flats
  BodyLife: number                // Base(MaxHP)+Equip(MaxHP); HP-dim exception
  SourceItemIds: Id[]             // remake recipe
  SourceSpiritCost: number        // remake Spirit gate
}
```

**Save note:** Demo serializes the full snapshot per slot into `PlayerPrefs` (§6); `NextSerial` shares the pool key so re-enter does not collide Ids.
**Related:**

- Class schema: **§9.9b**; BodyPart / BodyAppearance / ExtraEquipment / GemSuffix schemas: **§9.12–§9.15**.
- Static layer: `StaticStat(S) = max(0, Base+Equip+Base×GemMult+Base×RaceAdjust)` (no Buff); combat layer: `FinalStat(S)` also adds `Base×SkillBuff` (pick `S` first; missing dims = 0; see §3.11).
- **HP-dim exception:** final soldier `MaxHP = ceil(BodyLife + Str×3)`, `BodyLife = Base(MaxHP)+Equip(MaxHP)`; do **not** use `FinalStat(MaxHP)` (§3.11).
- Combat derives: `Primary` = `ClassId` → `ClassConfig.PrimaryStat` dim; `NormalAttackPower` / `AttackSpeed` / `SkillCooldown` coeffs from `ClassConfig.CombatConvertCoeffs` (missing key → §3.12 global defaults); hit params from same table columns.
- Multi-gem: instance `GemMult(S) = Σ` of socketed gems' `GemMult(S)`.
- On **PermanentDeath**: all `GemIds` return to Warehouse; BodyParts/Soul/ExtraEquipment and other bound materials are destroyed; formation slot cleared (see §3.11). CombatDead (no gems) does not trigger material fate; gemmed soldiers PermanentDeath immediately on HP≤0.
- Race and Class do **not** add a separate ControlPowerCost term.

#### 9.9b ClassConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) soldier Class / naming / PrimaryStat / five-dim→combat-param convert. One row = one Class. Inserted between §9.9 and §9.10; subsequent section numbers unchanged.

**Disk name:**
- **Excel:** `制造_职业配置表_Manufacture_ClassConfig.xlsx`
- **CSV:** `Manufacture_ClassConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| ClassId | 职业ID | `string` or `int` | PK; referenced by `SoulConfig.ClassId` |
| ClassName | 职业名 | `string` | `WarriorName` segment; exact-match key for appearance `ClassAffinity`; display; may be profession「战士」, **not** unit name「士兵」 |
| PrimaryStat | 主属性 | `enum` / `string` | `Strength` \| `Agility` \| `Intelligence`; selects dim for `NormalAttackPower` (§3.12); example semantics Warrior→Strength, Archer→Agility, Mage→Intelligence (this field wins; not ClassName hardcoding) |
| CombatConvertCoeffs | 战斗换算系数 | encoding | Coeff set for combat derives; encoding below; missing key → global defaults |
| AttackRange | 攻击距离 | `float` | Distance to enter attack state |
| MeleeWindupSeconds | 近战前摇 | `float` | ≥ 0; seconds; used when `AttackMode=Melee` |
| RangedProjectileSpeed | 远程弹速 | `float` | ≥ 0; used when `AttackMode=Ranged` |
| RangedTimeoutSeconds | 远程超时 | `float` | ≥ 0; seconds; timeout → miss |

**`CombatConvertCoeffs` encoding (fixed):** `Key_Value|Key_Value|…`

| Key | Default (missing) | Role in §3.12 |
|-----|-------------------|---------------|
| `NormalAttackPrimaryMult` | `1.5` | `NormalAttackPower = Primary × coeff` |
| `AttackSpeedBase` | `0.5` | `AttackSpeed = base + AttackSpeedAgiDiv/max(Agi,1)` |
| `AttackSpeedAgiDiv` | `60` | see above |
| `SkillCdIntDiv` | `30` | `SkillCooldown = max(SkillCdFloor, BaseCooldownSeconds − div/max(Int,1))` |
| `SkillCdFloor` | `0.1` | see above |

- Example: `NormalAttackPrimaryMult_1.5|AttackSpeedBase_0.5|AttackSpeedAgiDiv_60|SkillCdIntDiv_30|SkillCdFloor_0.1`
- Empty string = all defaults; illegal segments skip and log
- Does **not** include `AttackRange` / windup / projectile (separate columns)

```
ClassConfig {
  ClassId: Id
  ClassName: string
  PrimaryStat: Strength | Agility | Intelligence
  CombatConvertCoeffs: "Key_Value|..."
  AttackRange: number
  MeleeWindupSeconds: number
  RangedProjectileSpeed: number
  RangedTimeoutSeconds: number
}
```

**Parse:**

- At manufacture: `SoulConfig.ClassId` → write `WarriorInstance.ClassId`; naming / appearance use this row's `ClassName`.
- Combat derives: look up `PrimaryStat` and `CombatConvertCoeffs`; hit params from `AttackRange` etc. columns.
- Class list / concrete numbers filled later.

#### 9.10 GemConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) soldier attribute composition / Gem. One row = one gem.

**Disk name:**
- **Excel:** `制造_宝石配置表_Manufacture_GemConfig.xlsx`
- **CSV:** `Manufacture_GemConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| GemId | 宝石ID | `string` or `int` | PK |
| GemType | 宝石类型 | `enum` / `string` | One of: `Ruby` \| `Sapphire` \| `Emerald` \| `Topaz` \| `Amethyst` \| `Diamond`; **type-exclusive** slots; display names may be localized later |
| GemMult.MaxHP | 生命值放大系数 | `float` | Missing = **0**; used as `Base(MaxHP) × GemMult.MaxHP` |
| GemMult.MoveSpeed | 移动速度放大系数 | `float` | Same |
| GemMult.Strength | 力量放大系数 | `float` | Same |
| GemMult.Agility | 敏捷放大系数 | `float` | Same |
| GemMult.Intelligence | 智力放大系数 | `float` | Same |
| Skills | 额外技能 | encoding | Extra skill set; same encoding as [§9.9 `SoulConfig.Skills`](#99-灵魂配置表-soulconfig): `SkillId;Level\|…`; LossOfControl bonus via [§9.21](#921-skillconfig-skeleton) |
| SpiritCost | 精魂消耗 | `int` or `float` | ≥ 0; default 0 |
| ControlPowerCost | 控制力占用 | `int` or `float` | ≥ 0 |
| LossOfControlChanceBonus | 失控概率加成 | `float` | May be +/-; missing = **0**; **Σ** across socketed gems on the instance (§3.11) |

```
GemConfig {
  GemId: Id
  GemType: Ruby | Sapphire | Emerald | Topaz | Amethyst | Diamond
  GemMult: { MaxHP, MoveSpeed, Strength, Agility, Intelligence }
  Skills: "SkillId;Level|..."
  SpiritCost: number
  ControlPowerCost: number
  LossOfControlChanceBonus: number
}
```

**Inventory:** Gems are Warehouse-returnable; on **PermanentDeath**, **return all** instance `GemIds` to Warehouse (other bound materials destroyed); gemmed soldiers PermanentDeath immediately on HP≤0. At manufacture, instance five-dim `GemMult(S) = Σ` of socketed gems (all zeros if none). Acquisition routes and concrete numbers filled later.

#### 9.11 RaceConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) soldier attribute composition / Race / naming. One row = one race.

**Disk name:**
- **Excel:** `制造_种族配置表_Manufacture_RaceConfig.xlsx`
- **CSV:** `Manufacture_RaceConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| RaceId | 种族ID | `string` or `int` | PK; referenced by BodyPart `RaceId` |
| DisplayNameKey | 展示名 Key | `string` | UI / `WarriorName` race segment; convention **`Race.{RaceId}.Name`**; if i18n off, may use field as display fallback |
| RaceAdjustCoeff.MaxHP | 生命值调整系数 | `float` | May be +/-; missing treated as **0** |
| RaceAdjustCoeff.MoveSpeed | 移动速度调整系数 | `float` | Same |
| RaceAdjustCoeff.Strength | 力量调整系数 | `float` | Same |
| RaceAdjustCoeff.Agility | 敏捷调整系数 | `float` | Same |
| RaceAdjustCoeff.Intelligence | 智力调整系数 | `float` | Same |
| LossOfControlChanceBonus | 失控概率加成 | `float` | May be +/-; missing = **0**; feeds soldier FinalLossChance (§3.11) |

```
RaceConfig {
  RaceId: Id
  DisplayNameKey: string?
  RaceAdjustCoeff: { MaxHP, MoveSpeed, Strength, Agility, Intelligence }
  LossOfControlChanceBonus: number
}
```

**Resolve:** At manufacture, weight-**1** pick among filled BodyParts' `RaceId`s (Head/Torso/Arm/Leg) → look up this table → copy five dims into `WarriorInstance.RaceAdjustCoeff`; each dim feeds `Base(S) × RaceAdjust(S)`. LossOfControl rolls read this row's `LossOfControlChanceBonus`. Concrete race list and values filled later.

#### 9.12 BodyPartConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) manufacture slots / race pick / BaseStats / Warehouse credit. One row = one body-part material (body material table).

**Disk name:**
- **Excel:** `制造_躯体材料配置表_Manufacture_BodyPartConfig.xlsx`
- **CSV:** `Manufacture_BodyPartConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| BodyPartId | 躯体ID | `string` or `int` | PK; usable by `LootDrop` / Warehouse; same Id namespace as `MaterialConfig.MaterialId` (**no collisions**) |
| BodyLevel | 躯体等级 | `int` or `float` | ≥ 0; feeds appearance average level |
| BodySlot | 躯体部位 | `enum` | `Head` / `Torso` / `Arm` / `Leg` |
| RaceId | 种族 | `string` or `int` | FK → `RaceConfig` |
| ControlPowerCost | 控制力占用值 | `int` or `float` | ≥ 0; contributes to manufacture `BodyCost` |
| SpiritCost | 精魂消耗 | `int` or `float` | ≥ 0; default 0; manufacture Spirit total |
| StatBonus | 增加的属性值 | encoding | Flat BaseStat bonuses; `Base(S) = Σ` at manufacture |
| AutoConvert | 超上限兑精魂 | `int` or `float` | Same semantics as `MaterialConfig.AutoConvert` |
| Description | 文字介绍 | `string` | Display copy; localization Key if i18n |
| ArtAssetId | 外形美术素材ID | `string` or `int` | Part visual / Warehouse UI asset Id |

**`StatBonus` encoding (fixed):** `Attr_Value|Attr_Value|…` (same style as [§9.17](#917-科技项效果配置表-techeffectconfig) `AttributeModifiers`; additive; empty = none). Keys align with five BaseStats (`MaxHP` / `MoveSpeed` / `Strength` / `Agility` / `Intelligence`).

```
BodyPartConfig {
  BodyPartId: Id
  BodyLevel: number
  BodySlot: Head | Torso | Arm | Leg
  RaceId: Id
  ControlPowerCost: number
  SpiritCost: number
  StatBonus: "Attr_Value|..."
  AutoConvert: number
  Description: string
  ArtAssetId: Id
}
```

**Resolve:** `Base(S) = Σ` filled parts' `StatBonus(S)` (missing dim **0**). Warehouse stacks by `BodyPartId`; overflow uses row `AutoConvert`. Concrete values **TBD**.

#### 9.13 BodyAppearanceConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) whole-body visual pick. One row = one preset overall appearance.

**Disk name:**
- **Excel:** `制造_躯体外观配置表_Manufacture_BodyAppearanceConfig.xlsx`
- **CSV:** `Manufacture_BodyAppearanceConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| AppearanceId | 外观ID | `string` or `int` | PK; written to `WarriorInstance.AppearanceId`; **Prefab logical name** (no path/ext); resolve → `Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab`; art source `Assets/Art/Characters/Appearances/{AppearanceId}/` (see [§15](#15-角色美术管线character-creator-烘焙整角)) |
| AppearanceLevel | 外观等级 | `int` | Matches rounded average BodyLevel |
| RaceId | 隶属种族 | `string` or `int` | FK → `RaceConfig` |
| ClassAffinity | 职业倾向 | encoding | Exact match to `ClassConfig.ClassName` (via soldier soul `ClassId`) |
| Description | 文字介绍 | `string` | Display copy; localization Key if i18n |
| IsFallback | 保底外形 | `int` or empty | Empty/`0` = normal; **`1` = race fallback**; **at most one `1` per RaceId** |

**`ClassAffinity` encoding (fixed):** `ClassName` or `ClassName|ClassName|…` (pipe; exact match; empty = no class affinity).

```
BodyAppearanceConfig {
  AppearanceId: Id                 // Prefab logical name → Assets/Prefabs/Defend/Warriors/{Id}.prefab
  AppearanceLevel: int
  RaceId: Id
  ClassAffinity: "Class|Class|..."
  Description: string
  IsFallback: 0 | 1 | empty
}
```

**Pick algorithm (manufacture finalize; preview uses same algo on current slots):**

1. Mean `BodyLevel` over all filled body slots (incl. optional Head; empty slots excluded) → keep **1 decimal** → **round half-up to int** `AvgLevelInt`.
2. Set A = rows with `AppearanceLevel == AvgLevelInt` **and** `RaceId ==` finalized race.
3. If A non-empty: subset B = rows whose `ClassAffinity` contains `ClassConfig.ClassName` (via soul `ClassId`); if B non-empty → uniform random in B; else uniform random in A.
4. If A empty: use same-race row with `IsFallback == 1` if present.
5. If still none: uniform random over **entire table**.

#### 9.14 ExtraEquipmentConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) external gear / name prefix. One row = one ExtraEquipment.

**Disk name:**
- **Excel:** `制造_额外装备配置表_Manufacture_ExtraEquipmentConfig.xlsx`
- **CSV:** `Manufacture_ExtraEquipmentConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| EquipId | 装备ID | `string` or `int` | PK |
| EquipSlot | 装备槽 | `enum` | `Mount` / `Wing` |
| NamePrefix | 名字前缀 | `string` | `WarriorName`; concatenate if both equipped |
| SpiritCost | 精魂消耗 | `int` or `float` | ≥ 0; default 0 |
| ControlPowerCost | 控制力占用 | `int` or `float` | ≥ 0 |
| EquipStats | 平坦属性加成 | encoding | Flat five-stat bonuses; additive into Equip layer |
| Skills | 额外技能 | encoding | Same as [§9.9 `SoulConfig.Skills`](#99-灵魂配置表-soulconfig); LossOfControl bonus via [§9.21](#921-skillconfig-skeleton) |

**`EquipStats` encoding (fixed):** same style as [§9.12](#912-躯体材料配置表-bodypartconfig) `StatBonus` / [§9.17](#917-科技项效果配置表-techeffectconfig) `AttributeModifiers`: `Attr_Value|Attr_Value|…` (keys: `MaxHP` / `MoveSpeed` / `Strength` / `Agility` / `Intelligence`; empty = none; additive).

**Visuals (Mount / Wing):** This batch bakes mount/wing looks **into** the corresponding soldier `AppearanceId` whole-character export; **no** runtime layered overlays. Runtime modular overlays are a later topic. Stat/name fields on this table still apply.

```
ExtraEquipmentConfig {
  EquipId: Id
  EquipSlot: Mount | Wing
  NamePrefix: string
  SpiritCost: number
  ControlPowerCost: number
  EquipStats: "Attr_Value|..."
  Skills: "SkillId;Level|..."
}
```

#### 9.15 GemSuffixNameConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) soldier name suffix. One row = one socketed-gem combo → suffix.

**Disk name:**
- **Excel:** `制造_宝石后缀命名表_Manufacture_GemSuffixNameConfig.xlsx`
- **CSV:** `Manufacture_GemSuffixNameConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| ComboKey | 组合键 | `string` | PK; encoding below |
| Suffix | 后缀 | `string` | Final `WarriorName` segment; empty if no match |

**`ComboKey` encoding (fixed):** take socketed gems' `GemType` set → **sort by enum name** → join with `|`. Example: `Amethyst|Diamond|Ruby`. No gems → skip table; empty suffix.

```
GemSuffixNameConfig {
  ComboKey: string                // sorted GemType names joined by |
  Suffix: string
}
```

**Resolve:** At manufacture complete, derive `ComboKey` from instance `GemIds` → each `GemType` → lookup suffix; no gems / no match → empty suffix.

#### 9.16 TechTreeConfig

Rules: [SPEC_03 §3.13](SPEC_03_GameRules.md). One row = one TechItem.

**Disk name:**
- **Excel:** `科技_科技树配置表_Tech_TechTreeConfig.xlsx`
- **CSV:** `Tech_TechTreeConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| TechId | 科技项ID | `string` or `int` | PK |
| IconId | 科技项图标 | `string` or `int` | Icon asset / atlas key; concrete paths at implementation |
| DisplayName | 科技项名字 | `string` | Display name; localization Key if i18n enabled |
| EffectDescription | 科技项效果描述 | `string` | Hover/detail copy; localization Key if i18n enabled |
| UnlockNextTechIds | 解锁后续科技项ID | `string` | Encoding **`Id\|Id\|Id`** (pipe-separated); empty = leaf |
| InitiallyUnlocked | 初始是否解锁 | `bool` | `true` → **auto-learn** on new save and apply effects; center root must be `true` |
| LearnCost | 学习费用 | `int` | TechPoints; ≥ 0; usually **0** if InitiallyUnlocked |
| TechUiFrameType | 科技项类型 | `enum` / `string` | `Root` \| `Normal` \| `Key` \| `Capstone`; drives TechTree UI frame style |

```
TechTreeConfig {
  TechId: Id
  IconId: Id
  DisplayName: string
  EffectDescription: string
  UnlockNextTechIds: "Id|Id|Id"   // empty = leaf
  InitiallyUnlocked: bool
  LearnCost: int
  TechUiFrameType: Root | Normal | Key | Capstone
}
```

**Conventions:**

| Rule | Notes |
|------|-------|
| Default camera | Table **first row** node = default canvas camera focus |
| Center root | Must have `InitiallyUnlocked = true`; recommended as first row |
| Layout | **Not** in this table; Prefab places positions on Settings TechTree |
| Prerequisites | Not stored; invert `UnlockNextTechIds` forward edges |

#### 9.17 TechEffectConfig

Rules: [SPEC_03 §3.13](SPEC_03_GameRules.md). Applied when a `TechId` is learned.

**Disk name:**
- **Excel:** `科技_科技项效果配置表_Tech_TechEffectConfig.xlsx`
- **CSV:** `Tech_TechEffectConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| TechId | 科技项ID | `string` or `int` | FK to `TechTreeConfig.TechId`; this version prefer **1:1**; multi-rows same Id merge |
| AttributeModifiers | 增加的属性 | `string` | Encoding **`Attr_Value\|Attr_Value\|…`** (additive); empty = no attr deltas |
| UnlockedFeatureSystemName | 解锁的功能系统名 | `string` | Non-empty → write save `UnlockedFeatureSystems`; empty = none; **open list** (PascalCase); validate known names at implement time, log unknown but still write; extend SPEC as features grow. Attr deltas (e.g. `DigDamage`) use `AttributeModifiers`, **not** this field |

```
TechEffectConfig {
  TechId: Id
  AttributeModifiers: "Attr_Value|Attr_Value|..."  // additive
  UnlockedFeatureSystemName: string               // empty = none; open PascalCase list
}
```

**Documented attribute keys this version (extensible):**

| Attr key | Writes | Notes |
|----------|--------|-------|
| DigDamage | `DigProtagonistCapabilities.DigDamage` | Per DigAction damage |
| DigDurationReductionSum | `DigProtagonistCapabilities.DigDurationReductionSum` | Shorten DigAction duration (seconds); dig speed |
| DigCursorRadius | `DigProtagonistCapabilities.DigCursorRadius` | Cursor radius (if used) |
| DigStageDurationBonus | `DigProtagonistCapabilities.DigStageDurationBonus` | Dig stage duration bonus (seconds) |

Further keys (e.g. ControlPower cap bonus) later; same key across techs **sums additively** into derived caps.

**Feature system name examples (open, not closed):** `FeatureDigAdvancedQualities`, etc.; non-empty write to save is authoritative.

#### 9.18 WaveSpawnConfig

Rules: [SPEC_03 §3.12](SPEC_03_GameRules.md). One row = one spawn event; grouped by `WaveConfigId`, referenced by `DefendGameplayConfig.WaveConfigId`.

**Disk name:**
- **Excel:** `防守_刷怪波次配置表_Defend_WaveSpawnConfig.xlsx`
- **CSV:** `Defend_WaveSpawnConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| WaveConfigId | 波次配置ID | `string` or `int` | Group key; multi-rows = all spawn events for that Defend config |
| SpawnOrder | 出怪顺序 | `int` | Applies **only** when multiple rows share the same `SpawnRemainingSeconds`; ascending order |
| SpawnRemainingSeconds | 出怪时间（剩余秒） | `int` | Activate when `RemainingCombatSeconds ==` this field (whole seconds) |
| MonsterId | 怪物ID | `string` or `int` | FK → `MonsterConfig` (§9.19) |
| SpawnCount | 出现数量 | `int` | ≥ 1 |
| AppearLocation | 出现位置 | `enum` / `string` | `InsideMap` \| `OutsideMap` |
| SpawnMode | 出怪方式 | `enum` / `string` | `RegionRandom` \| `ClockDirection` |
| SpawnClockHour | 几点钟方向 | `int` | 1–12; required when `SpawnMode = ClockDirection`; else optional/empty |

```
WaveSpawnConfig {
  WaveConfigId: Id
  SpawnOrder: int
  SpawnRemainingSeconds: int
  MonsterId: Id
  SpawnCount: int
  AppearLocation: InsideMap | OutsideMap
  SpawnMode: RegionRandom | ClockDirection
  SpawnClockHour: int?               // 1–12; required when ClockDirection
}
```

**Activation (with §3.12):** During `Combat`, when remaining whole seconds equal a row’s `SpawnRemainingSeconds`, fire matching not-yet-fired rows; same-second rows by `SpawnOrder` ascending. Exact spawn geometry **TBD**.

#### 9.19 MonsterConfig

Rules: [SPEC_03 §3.12](SPEC_03_GameRules.md). One row = one monster type.

**Disk name:**
- **Excel:** `防守_怪物配置表_Defend_MonsterConfig.xlsx`
- **CSV:** `Defend_MonsterConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| MonsterId | 怪物ID | `string` or `int` | PK; referenced by `WaveSpawnConfig` |
| ModelId | 怪物模型ID | `string` | Prefab logical name (no path/ext); resolve → `Assets/Prefabs/Defend/Monsters/{ModelId}.prefab`; art source `Assets/Art/Characters/Monsters/{ModelId}/` (Character Creator baked whole characters; see [§15](#15-角色美术管线character-creator-烘焙整角)) |
| DisplayName | 怪物名称 | `string` | Display name or localization key (if i18n enabled) |
| TargetSelect | 目标选择 | `enum` / `string` | `Nearest` \| `PreferWarrior` \| `PreferProtagonist` (same enum as `SoulConfig.AttackPriority`) |
| AttackMode | 攻击模式 | `enum` / `string` | `Melee` \| `Ranged` |
| AggroMode | 仇恨模式 | `enum` / `string` | `ActiveChase` \| `PassiveChase` \| `StationaryActive` \| `StationaryPassive`; see [SPEC_03 §3.14](SPEC_03_GameRules.md); **load default:** missing/empty → `ActiveChase`; illegal → load fail |
| AlertRadius | 警戒半径 | `float` | ≥ 0; active detect radius; **load default:** missing/empty → `AttackRange`; value < 0 → load fail |
| BodyRadius | 占地半径 | `float` | ≥ 0; XZ footprint radius (world units); shared by PushMap spawn spread and moving-monster `NavMeshAgent.radius`; **load default:** missing/empty → `0.35`; value < 0 → load fail |
| MaxHP | 怪物血量 | `int` or `float` | Init monster maxHP / current HP on spawn |
| MoveSpeed | 怪物移动速度 | `float` | World units/sec or project-unified speed unit |
| AttackPower | 怪物攻击力 | `int` or `float` | Used **only** when attacking soldiers; not used for normal attacks on protagonist |
| AttackSpeed | 攻击速度 | `float` | Attack rate (unit locked at implementation) |
| AttackRange | 攻击距离 | `float` | Distance to enter attack state |
| MeleeWindupSeconds | 近战前摇 | `float` | ≥ 0; seconds; used when `AttackMode=Melee` |
| RangedProjectileSpeed | 远程弹速 | `float` | ≥ 0; used when `AttackMode=Ranged` |
| RangedTimeoutSeconds | 远程超时 | `float` | ≥ 0; seconds; timeout → miss |
| Skills | 怪物技能 | see encoding | Skill Id + CD list; skill-effect columns later topic; **unused in Demo v1** (normal attacks only; ignore or leave empty at implement time) |
| LootDrop | 怪物掉落 | see encoding | On kill; same encoding as Dig `LootDrop` |

**`Skills` encoding (fixed):** `SkillId_CdSeconds|SkillId_CdSeconds|...`

- Segment separator: `|`
- Segment: `SkillId_CooldownSeconds`
- Empty string = no skills
- Skill-effect definition table **not** defined this batch; Demo v1 does **not** cast even if populated
- **Note:** **Different** from soldier-side `SkillId;Level|…` (monster CD lives on this table)

**`LootDrop` encoding:** same as [§9.3](#93-gravequalityconfig): `Id_Count|Id_Count|...`

```
MonsterConfig {
  MonsterId: Id
  ModelId: string                  // Prefab logical name → Assets/Prefabs/Defend/Monsters/{Id}.prefab; art §15
  DisplayName: string
  TargetSelect: Nearest | PreferWarrior | PreferProtagonist
  AttackMode: Melee | Ranged
  AggroMode: ActiveChase | PassiveChase | StationaryActive | StationaryPassive  // empty → ActiveChase
  AlertRadius: number              // empty → AttackRange
  BodyRadius: number               // empty → 0.35
  MaxHP: number
  MoveSpeed: number
  AttackPower: number              // soldiers only
  AttackSpeed: number
  AttackRange: number
  MeleeWindupSeconds: number
  RangedProjectileSpeed: number
  RangedTimeoutSeconds: number
  Skills: "SkillId_Cd|SkillId_Cd|..."
  LootDrop: "Id_Count|Id_Count|..."
}
```

**Load rules (`ConfigCsvRepository`):** `AggroMode` / `AlertRadius` / `BodyRadius` defaults as above; illegal enum or `AlertRadius < 0` / `BodyRadius < 0` fails whole-table load (§14.5). PushMap and Defend share this table.

#### 9.20 LossOfControlConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) / [§3.12](SPEC_03_GameRules.md) LossOfControl tiers and base chance. One row = one tier (fixed 4 rows: TierId 1–4).

**Disk name:**
- **Excel:** `战斗_失控配置表_Combat_LossOfControlConfig.xlsx`
- **CSV:** `Combat_LossOfControlConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| TierId | 失控程度段 | `int` | PK; **1–4** (Mild / Moderate / Severe / Full); Degree→tier mapping in SPEC_03 §3.11 |
| DisplayName | 名称 | `string` | Display name or localization Key |
| Description | 描述 | `string` | Tier description or localization Key |
| LossOfControlChance | 失控概率 | `float` | Tier **base** chance; range **\[0, 1\]**; concrete values filled later |

```
LossOfControlConfig {
  TierId: 1 | 2 | 3 | 4
  DisplayName: string
  Description: string
  LossOfControlChance: number     // [0, 1]
}
```

**Resolve:** After StartBattle locks `LossOfControlDegree` → map to `TierId` → read `LossOfControlChance` as `TierChance`. Final chance and Rebel: SPEC_03 §3.11 / §3.12.

#### 9.21 SkillConfig (skeleton)

Rules: Full soldier/monster skill-effect schema **later**; this batch locks LossOfControl bonus and **base cooldown** so Soul / Gem / ExtraEquipment `Skills` lists can resolve; actual CD uses §3.12 formula. **Demo v1 does not cast skills** — this table and CD formula do not drive combat. **No effect columns this batch**; future columns extend the same on-disk filenames.

**Disk name:**
- **Excel:** `战斗_技能配置表_Combat_SkillConfig.xlsx`
- **CSV:** `Combat_SkillConfig.csv` (future skill-effect columns extend this file)

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| SkillId | 技能ID | `string` or `int` | PK; referenced by Soul/Gem/Equip/Monster `Skills` |
| BaseCooldownSeconds | 基础冷却 | `float` | ≥ 0; seconds; soldier actual CD = `max(SkillCdFloor, BaseCooldownSeconds − SkillCdIntDiv/max(Int,1))` (coeffs from `ClassConfig.CombatConvertCoeffs` / §3.12) |
| LossOfControlChanceBonus | 失控概率加成 | `float` | May be +/-; missing = **0**; soldier `ΣSkillBonus` = sum of this field over all their skills (§3.11) |

```
SkillConfig {
  SkillId: Id
  BaseCooldownSeconds: number
  LossOfControlChanceBonus: number
  // skill-effect columns deferred; same file when expanded
}
```

**Note:** If soldier `ΣSkillBonus ≠ 0`, each skill cast re-rolls with full `FinalLossChance` (§3.11). Other skill columns (effects, level growth) **later topic** (filenames unchanged).

---


#### 9.22 PushMapGameplayConfig

Rules: [SPEC_03 §3.14](SPEC_03_GameRules.md). One row = one PushMap level config.

**Disk name:**
- **Excel:** `推图战_推图战配置表_PushMap_PushMapGameplayConfig.xlsx`
- **CSV:** `PushMap_PushMapGameplayConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| GameplayConfigId | 玩法配置ID | `string` or `int` | PK; referenced by LevelOperation / ModeSelect |
| MapId | 地图编号 | `string` | Prefab logical name; allowed **`Ground_01`…`Ground_05`** or **`PushMap_*`**; resolve → `Assets/Prefabs/Maps/{MapId}.prefab`; **≠** LevelId |
| DisplayName | 关卡显示名 | `string` | Display or i18n key |
| StageExpReward | 阶段经验奖励 | `int` | Exp credited on Boss clear; Demo may use fixed |
| CaptureLoot | 占领默认掉落 | encoding | Optional; Dig-style `Id_Count\|…` |
| DungeonUnlockIds | 副本解锁ID列表 | encoding | `\|`-separated; empty=none; dungeon gameplay **TBD** |
| CaptureSeconds | Capture seconds | `float` | **load default 5** (missing/empty); < 0 → load fail |
| Notes | 备注 | `string` | Optional |

```
PushMapGameplayConfig {
  GameplayConfigId: Id
  MapId: Ground_01 | PushMap_*
  DisplayName: string
  StageExpReward: int
  CaptureLoot: "Id_Count|Id_Count|..."
  DungeonUnlockIds: "DungeonId|..."
  CaptureSeconds: number
}
```

**Map Prefab marker contract (Approach A / PM-01):**

- **Scripts:** `Assets/Scripts/Gameplay/PushMap/`, namespace `Gravedigger2026.Gameplay.PushMap`
- **Sample map:** `Assets/Prefabs/Maps/PushMap_Demo_01.prefab` (`MapId=PushMap_Demo_01`); Editor menu `Gravedigger2026/PushMap/Ensure Sample Map Prefab` copies `Ground_01` and attaches markers; **do not** rewrite Dig/Defend shared `Ground_*`
- **Reuse:** `EngageZone` (`Gameplay.Defend`), `WalkSurface` (`WalkSurfaceIsoDiamond` / `Gameplay.Maps`); ground remains Isometric Tilemap (Tile Palette OK)
- **Slice boundary:** marker contract + instantiable sample only; capture/spawn/AirWall NavMesh **out of scope** (PM-04/05/08)

| Component | Fields (SerializeField) | Notes |
|-----------|-------------------------|-------|
| `ObjectivePoint` | `ObjectiveOrder:int` (≥1); ref `CaptureZone` | Ordered objective; `CaptureZone` on same or child |
| `CaptureZone` | `Radius:float` default **2** | XZ circle; `ContainsXZ`; tunable |
| `AirWall` | `HalfExtents:Vector3` | Authoring blocker; **Y euler** 0°/45°/90°…; runtime block → "AirWall NavMesh contract (PM-08)" below |
| `SpawnPoint` | `SpawnPointId:string` | Matches `PushMapSpawnConfig.SpawnPointId` |
| `TrapZone` | `TrapZoneId:string`; `Radius:float` | XZ circle; matches spawn-table `TrapZoneId` |
| `BossPoint` | (none required; pose=`Transform`) | Boss spawn placeholder |

```
ObjectivePoint { ObjectiveOrder: int>=1; CaptureZone }
CaptureZone    { Radius: number = 2 }
AirWall        { HalfExtents: Vector3 }      // Transform.eulerAngles.y = 0|45|90|…
SpawnPoint     { SpawnPointId: string }
TrapZone       { TrapZoneId: string; Radius: number }
BossPoint      { }
// + EngageZone, WalkSurface (existing)
```

**Capture runtime contract (Approach A / PM-04):**

- **Rules ownership:** objective chain + timer live in `PushMapSessionService` (`Core/PushMap/`); `ObjectivePoint`/`CaptureZone` are authoring markers, no runtime self-tick
- **Session surface:** `CaptureSecondsRequired` (`Config.CaptureSeconds`, clamped ≥0.01); `CurrentObjectiveOrder` (0 = none/all captured); `IsObjectiveCaptured(order)`; `TryBeginObjectiveChain(IEnumerable<int> orders)`; `TickCapture(float dt, bool hasLivingMonsterInCurrentZone)`
- **Events:** `ObjectiveCaptured(int order)` (stop-spawn hook → PM-05); `CurrentObjectiveChanged(int newOrder)`
- **Presentation:** `PushMapStageController` collects sorted `ObjectivePoint`s; per `Update` probes living monsters in current zone (default scan `_monsters`: `IsAlive && CaptureZone.ContainsXZ`); probe via `PushMapMonsterPresenceProbe` (injectable placeholder for reset acceptance); capture logs + HUD
- **Advance (MP-04 / Approach B):** loyal soldiers share `CurrentObjective` → one `FlowFieldService` field; `PushMapAdvanceView` samples field dir + `MassMoveScheduler`/`LocalDetour` then `NavMeshAgent.Move` (**no** per-soldier per-frame `SetDestination(Objective)`); living monsters in capture zone do **not** pause advance (probe feeds `TickCapture` only); Rebels do not advance
- **Chase/engage (MP-05 / Approach B):** loyal soldiers in engage detect → `GoalKind=AttackSlot` (`AttackSlotService.TryClaim`) + LocalDetour, leave Objective field; on clear release slot and resume `Objective`. Monster chase destination is claimed slot (not target center); `MassMoveScheduler.SetGoal`; slot refresh ≤50/frame round-robin; death/`Release`/`ReleaseAllForTarget`
- **Defend parity (MP-06 / Approach B):** `DefendStageController` owns the same `MassMoveScheduler`+`AttackSlotService`; `WarriorAgentView` Engage chase→`AttackSlot`, no candidate→`GoalKind=FormationHome` (straight+LocalDetour; abort return on new target); `MonsterAgentView` chase uses slots; no all-units per-frame `SetDestination`/`CalculatePath`; same GoalKind semantics as PushMap
- **FlowField rebuild:** after StartBattle bake (AirWall→`StaticBoxWalkableMask`) `Configure`+`Rebuild`; `CurrentObjectiveChanged` → `Rebuild` again (log-verifiable RebuildCount); same-goal units share one field
- **StartBattle order (View):** Bake NavMesh (incl. AirWall) → walkable mask+FlowField → `TryBeginObjectiveChain` → deploy loyal soldiers (register Scheduler) → `FireStartBattleSpawns` (monsters register Scheduler+AttackSlot) → LOC rolls
- **Walkable:** sample `PushMap_Demo_01` `DigMapBounds`/`WalkSurface`/`EngageZone` must cover objectives and spawn points (incl. BossPoint)
- **Boundary:** capture probe decoupled from advance; no monsters → 1→2 switch after `CaptureSeconds`; stop-spawn is event/log only; capture loot/unlock → PM-07; hit polish deferred; stress entry → MP-07 / §9.7

```
// PM-04 / MP-04+MP-05+MP-06 rules/presentation touchpoints
session.TryBeginObjectiveChain(sortedOrders)
session.TickCapture(dt, probe.HasLivingMonster(currentZone))
event ObjectiveCaptured(order)   // PM-05 stops linked spawns; PM-07 capture loot/unlock
event CurrentObjectiveChanged(order) → FlowFieldService.Rebuild(goal, airWallMask)
PushMapMonsterPresenceProbe { HasLivingMonster(CaptureZone) }  // capture timer only
FlowFieldService + StaticBoxWalkableMask(AirWall OBBs)
AttackSlotService.TryClaim / Release / ReleaseAllForTarget
MassMoveScheduler.SetGoal(Objective|AttackSlot|FormationHome) + Tick  // ≤50 steer + ≤50 slot refresh/frame
PushMapAdvanceView { field or AttackSlot Move; no SetDestination; rebel skip }
PushMapMonsterAgentView { AttackSlot chase via scheduler; AggroMode preserved }
DefendStageController { warriors+monsters on same scheduler; FormationHome return }
WarriorAgentView / MonsterAgentView { AttackSlot or FormationHome Move; no center SetDestination }
```

**Boss-clear & reward runtime contract (Approach A / PM-07):**

- **Rules ownership:** win/lose + pending Boss count in `PushMapSessionService`; `BossPoint` is position-only
- **Count:** `FireRow` with `IsBoss` → `_pendingBossCount += SpawnCount`; if pending > 0 and View reports no `BossPoint` → warn
- **Kill:** `TryNotifyBossKilled()` (Combat, not settled) → decrement; at 0 → `Ended` + `VictorySettled(Config.StageExpReward)` + write `DungeonUnlockIds` hook
- **Fail:** existing `RequestLevelFailure` → `LevelFailureRequested`; **must not** also fire `VictorySettled`
- **Capture rewards:** on `Capture`, parse `Config.CaptureLoot` (`LootDropParser`) → credit via presentation/`Warehouse`; write `DungeonUnlockIds`; **no** Exp
- **Save hook:** `DungeonUnlockService` (`Core/PushMap/`): per-slot `HashSet` + PlayerPrefs; `TryUnlock(id)` log-verifiable; dungeon gameplay body not done
- **Presentation:** `PushMapMonsterAgentView.IsBoss`; Demo kill = first loyal entry into Boss `AttackRange` → `NotifyKilled` + `TryNotifyBossKilled`; subscribe `VictorySettled` → `AddExperience` → `_onVictoryAdvance`

```
// PM-07 rules/presentation touchpoints
session.TryNotifyBossKilled()
event VictorySettled(stageExp)
DungeonUnlockService { BindSlot; TryUnlock; UnlockedIds }
PushMapMonsterAgentView.IsBoss
```

**AirWall NavMesh contract (Approach A / PM-08):**

- **Authoritative block:** StartBattle runtime bake (shared `DefendNavMeshBaker`) adds, besides the IsoDiamond walkable `Mesh` source, one `NavMeshBuildSource` per map `AirWall`: `shape=Box`, `size=HalfExtents×2`, `transform=TRS(position, rotation, 1)`, `area=Not Walkable` (`NavMesh.GetAreaFromName`; fallback `1`)
- **Rotation:** Prefab `Transform.eulerAngles.y` supports 0°/45°/90°…; sample `AirWall_45` is 45°
- **Applies to:** both factions' `NavMeshAgent` pathing cannot cross (FlowField mask blocks advance dirs; monster chase still uses NavMesh); AirWall also feeds `StaticBoxWalkableMask` for FlowField
- **Wiring:** `PushMapStageController` StartBattle collects `GetComponentsInChildren<AirWall>` → `Bake(..., notWalkableBoxes)` + same OBBs for FlowField mask
- **Boundary:** **no** `NavMeshObstacle` Carve or multi-layer obstacle polish; Defend without AirWalls still uses the no-obstacle overload

```
// PM-08 bake touchpoints
DefendNavMeshBaker.Bake(center, halfExtents, notWalkableBoxes)
NavMeshBoxObstacle { Center, Size=HalfExtents*2, Rotation }
AirWall { HalfExtents }  // authoring; Y euler 0|45|90|…
```

#### 9.23 PushMapSpawnConfig

Rules: [SPEC_03 §3.14](SPEC_03_GameRules.md). One row = one spawn definition; multi-rows per SpawnPointId OK.

**Disk name:**
- **Excel:** `推图战_刷怪配置表_PushMap_PushMapSpawnConfig.xlsx`
- **CSV:** `PushMap_PushMapSpawnConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| GameplayConfigId | 玩法配置ID | `string` or `int` | FK → PushMapGameplayConfig |
| SpawnPointId | 刷怪点编号 | `string` or `int` | Matches Prefab marker |
| MonsterId | 怪物ID | `string` or `int` | FK → MonsterConfig |
| SpawnCount | 数量 | `int` | ≥ 1 |
| LinkedObjectiveOrder | 关联目标点序号 | `int` | Optional; stop spawn after Capture; 0=global |
| TrapZoneId | 陷阱区域编号 | `string` or `int` | Empty=non-trap StartBattle; else trap-triggered |
| IsBoss | 是否BOSS | `bool` / `0\|1` | 1=Boss clear target |
| SpawnOrder | 同点出怪顺序 | `int` | Ascending within same SpawnPointId |

```
PushMapSpawnConfig {
  GameplayConfigId: Id
  SpawnPointId: Id
  MonsterId: Id
  SpawnCount: int
  LinkedObjectiveOrder: int
  TrapZoneId: Id | ""
  IsBoss: 0 | 1
  SpawnOrder: int
}
```

**Spawn runtime contract (Approach A / PM-05):**

- **Rules ownership:** spawn eligibility + trigger state live in `PushMapSessionService` (`Core/PushMap/`); `SpawnPoint`/`TrapZone` are authoring markers only
- **Load & fire:** `TryStartBattle` loads all rows for the current `GameplayConfigId` but does **not** spawn yet. View calls `FireStartBattleSpawns()` after Bake NavMesh + deploy: non-trap rows whose linked objective is uncaptured fire `PushMapSpawnRequested` (ascending `SpawnOrder` per `SpawnPointId`); trap-bound points go `Pending`
- **Event:** `PushMapSpawnRequested(PushMapSpawnRequest)`; payload carries `SpawnPointId` / `MonsterId` / `SpawnCount` / `LinkedObjectiveOrder` / `IsBoss` / `SpawnOrder` / `Trigger` (`StartBattle` / `Trap`); **position resolved by View** via `SpawnPointId` / `BossPoint`, then staggered by `MonsterConfig.BodyRadius` vs living footprint circles (ring/spiral candidates → `NavMesh.SamplePosition`; PM-10)
- **Trap trigger:** `TryNotifyTrapEnter(trapZoneId)` (View detects first loyal-enter) → if objective uncaptured + not yet fired → all rows for that `SpawnPointId` fire; once per point per battle
- **Capture stop:** `ObjectiveCaptured(order)` → marks points with `LinkedObjectiveOrder == order` to stop; already-spawned monsters unaffected; `IsBoss=1` rows are **not** capture-stopped (Boss in PM-07)
- **Presentation:** `PushMapStageController` collects `SpawnPoint` (`SpawnPointId`→position) and `TrapZone`; subscribes to events to instantiate monsters → `PushMapMonsterAgentView` (into `_monsters`; Boss uses `BossPoint`; positions via `PushMapSpawnSpread`); Update polls loyal (`!IsRebel`) `PushMapAdvanceView` first entry into `TrapZone` → `TryNotifyTrapEnter`
- **Footprint & avoidance (PM-10):** Bind sets `_agent.radius = min(BodyRadius, max(0.05, AttackRange − soldier Demo radius 0.1 − 0.05))`; spawn spread still uses full `BodyRadius`; moving monsters use NavMesh RVO; `Stationary*` do not relocate; Defend spawn spread **out of scope** this slice; **loyal soldiers** Demo: `NavMeshAgent.radius=0.1`, `height=0.1` (`WarriorAgentView` / `PushMapAdvanceView`)
- **Demo engage→AttackSlot (MP-05):** while loyal center distance to a living monster ≤ `max(AttackRange, BodyRadius+0.1)`, `PushMapAdvanceView` switches to `GoalKind=AttackSlot` (claim ring slot + LocalDetour `Move`; **leave** FlowField); on clear release slot and resume `Objective`. Monster chase likewise claims slots — **forbid** all-units per-frame `SetDestination`/`CalculatePath` to target center
- **Monster AI boundary:** this slice uses Defend default-chase semantics (nearest loyal warrior / protagonist; normal attack in `AttackRange`; protagonist via `ApplyShieldHit`; warrior damage logged); AggroMode four-state deferred to **PM-06** (contract below); `MonsterAgentView` (bound to `DefendSessionService`) is not wired in
- **AggroMode runtime contract (PM-06):** `PushMapMonsterAgentView` branches on `config.AggroMode`; `ActiveChase`: loyal soldier enters `AlertRadius` → chase that soldier until death; `PassiveChase`: `_provoked=false` idle, `NotifyProvoked()` → chase; `StationaryActive`: never moves, attacks loyal soldier inside `AttackRange`, stops on leave; `StationaryPassive`: never moves, attacks only after `NotifyProvoked()` and target still in `AttackRange`. Active detection and provocation are **loyal-only** (`!IsRebel`). Provocation source (Demo): `PushMapStageController` fires a loyal `PushMapAdvanceView`'s first entry into a passive monster's `AttackRange` → `NotifyProvoked()` (stands in for "soldier attacks first"; soldier HP deferred). Hits still use `AttackMode` scheme D; active stances do **not** proactively detect the protagonist via `AlertRadius` (loyal soldiers only), but a pursued/nearest-rule protagonist hit still applies `ApplyShieldHit`. Real soldier damage on normal monsters **not** done
- **Boundary:** no `WaveSpawnConfig` countdown; Boss-clear / Exp / capture loot / dungeon unlock hooks → §9.22 PM-07 contract

## 13. 资源编排与可扩展性

### 简体中文

**原则（强制倾向）：预制体优先（Prefab-first）。** 实际代码与场景开发中，凡会以 GameObject 层级出现的玩法实体、可复用 UI、可生成物、可摆放交互物，**默认用 Prefab + 挂载 Controller** 制作与引用，放在 `Assets/Prefabs/<模块>/`。优先在编辑器中拼装 Prefab，再由代码 `Instantiate` / 引用槽位驱动；**避免**在代码里动态 `new GameObject` 拼层级，或在多个 Scene 中手工复制同一套层级。

**适用默认 Prefab 的典型对象：** 主角/圆圈光标、坟墓（含障碍半径）、奖励飞字、工具面板与可复用面板、关卡内可生成物、战斗主角/士兵/怪物、**DigMap / BattleMap（含 EngageZone；共用 `Ground_01`…`Ground_05`）** 等。Dig 模块建议路径：`Assets/Prefabs/Dig/`；UpgradeManufacture 模块建议路径：`Assets/Prefabs/UpgradeManufacture/`（`UpgradeManufactureStageRoot`）；**地图变体**统一路径：`Assets/Prefabs/Maps/{Ground_0N}.prefab`（`DigMapId` / `BattleMapId` 均解析至此）。PushMap `MapId` 亦可为 `PushMap_*`，同目录解析；Demo 样例 **`PushMap_Demo_01`**（Editor Ensure，见 §9.22）；PushMap 地图 Prefab 另须支持标记：`ObjectivePoint`/`CaptureZone`/`AirWall`（可 45°；开战 Bake 注入 Not Walkable Box，见 §9.22 PM-08）/`SpawnPoint`/`TrapZone`/`BossPoint`（见 §9.22 / [SPEC_03 §3.14](SPEC_03_GameRules.md)）。地图**表现**为 Unity **Isometric Tilemap**（Grid `CellLayout=Isometric`，Demo `CellSize≈(1,0.5,2)`，Grid 旋转使砖面落在 XZ，配合 Dig/Defend 正交顶视相机）；Tile/Sprite 源在 `Assets/Art/Maps/Tiles/`（自 Example Scene `Environment/Tiles`+`Sprites` 复制）；Prefab 另含不可见 `WalkSurface`（**IsoDiamond**：XZ 菱形薄网格，供 Demo NavMesh 约定）、`DigMapBounds` / EngageZone（同形菱形足迹；半尺寸=`PaintRadius*(cellSize.x,cellSize.y)`）及刷怪点。Editor 可用 Tile Palette 手刷，或 Builder 程序铺默认图案；**禁止**运行时直接引用 `SmallScaleInt/`，见 [§15](#15-角色美术管线character-creator-烘焙整角)。工程须含 `com.unity.2d.tilemap`（编辑器刷砖）。角色视觉 Prefab 约定：`Digger` → `Assets/Prefabs/Dig/Digger.prefab`；`BattleProtagonist` → `Assets/Prefabs/Defend/BattleProtagonist.prefab`；士兵 → `Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab`；怪物 → `Assets/Prefabs/Defend/Monsters/{ModelId}.prefab`（美术管线见 [§15](#15-角色美术管线character-creator-烘焙整角)）。

**可不做成 Prefab 的例外：** Scene 唯一常驻 Manager / 引导用一次性布局；纯逻辑无表现的 Service（非 MonoBehaviour 或仅场景单例入口）。

| 问题 | 是 → 做法 |
|------|-----------|
| 玩法/UI GameObject 层级（默认）？ | **Prefab** + Controller → `Assets/Prefabs/<模块>/` |
| 多次 Instantiate / 多 Scene 复用？ | 必须 Prefab；禁止 Scene 间复制层级 |
| 策划**配置表**行数据？ | Excel + CSV → `Assets/ConfigTables/`（见 [§14](#14-配置表工程约定与打表工具)） |
| 策划可调**非表型**单例/引用槽？ | ScriptableObject → `Assets/Settings/<模块>/` |
| UI 文本？ | 本地化 Key（若启用 §8） |
| 高频 spawn/destroy？ | Prefab + **对象池** |
| 玩法状态变更？ | 规则层 + 事件；View 不驱动规则 |

**禁止：** 多 Scene 复制同一层级；硬编码策划数据；`GameObject.Find`（Manager 除外）；规则层直接操作 `Transform`/`Animator`；用纯代码拼装本应以 Prefab 交付的可视层级（调试临时对象除外）；将新配置表散落在 `Settings/` 或其他非 `ConfigTables/` 路径。

配置表（§9）落地须遵循 [§14](#14-配置表工程约定与打表工具)：运行时只读 CSV；禁止在脚本中硬编码关卡/挖坟/防守/升级/科技等表数值。

### English

**Principle (strong default): Prefab-first.** For gameplay entities, reusable UI, spawnables, and placeable interactables that exist as GameObject hierarchies, **author and reference Prefabs + Controllers** under `Assets/Prefabs/<Module>/`. Prefer assembling Prefabs in the Editor and driving them via `Instantiate` / serialized slots. **Do not** build visual hierarchies with runtime `new GameObject` trees, or hand-duplicate the same hierarchy across Scenes.

**Typical Prefab targets:** Digger / circle cursor, Graves (with obstacle radius), DigReward VFX/UI, ToolsPanel and reusable panels, in-level spawnables, BattleProtagonist / Soldiers (Warrior) / Monsters, **DigMap / BattleMap (incl. EngageZone; shared `Ground_01`…`Ground_05`)**. Dig module path: `Assets/Prefabs/Dig/`; UpgradeManufacture module path: `Assets/Prefabs/UpgradeManufacture/` (`UpgradeManufactureStageRoot`); **map variants** unified path: `Assets/Prefabs/Maps/{Ground_0N}.prefab` (`DigMapId` / `BattleMapId` both resolve here). PushMap `MapId` may also be `PushMap_*` in the same folder; Demo sample **`PushMap_Demo_01`** (Editor Ensure; marker contract §9.22; `AirWall` StartBattle bake → Not Walkable Box, §9.22 PM-08). Map **presentation** is Unity **Isometric Tilemap** (`CellLayout=Isometric`, Demo `CellSize≈(1,0.5,2)`, Grid rotated onto XZ for Dig/Defend orthographic top-down); Tile/Sprite sources under `Assets/Art/Maps/Tiles/` (copied from Example Scene `Environment/Tiles`+`Sprites`); Prefab also has invisible `WalkSurface` (**IsoDiamond**: thin XZ diamond mesh for Demo NavMesh), `DigMapBounds` / EngageZone (same diamond footprint; half-extents=`PaintRadius*(cellSize.x,cellSize.y)`), spawn points. Editor: Tile Palette hand-paint or Builder default fill; **do not** runtime-reference `SmallScaleInt/` — [§15](#15-角色美术管线character-creator-烘焙整角). Require `com.unity.2d.tilemap` for editor painting. Character visual Prefabs: `Digger` → `Assets/Prefabs/Dig/Digger.prefab`; `BattleProtagonist` → `Assets/Prefabs/Defend/BattleProtagonist.prefab`; soldiers → `Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab`; monsters → `Assets/Prefabs/Defend/Monsters/{ModelId}.prefab` (art pipeline: [§15](#15-角色美术管线character-creator-烘焙整角)).

**Exceptions:** Scene-unique Managers / one-off layout; pure logic Services (non-MonoBehaviour or single scene entry).

| Question | If yes → |
|----------|----------|
| Gameplay/UI GameObject hierarchy (default)? | **Prefab** + Controller → `Assets/Prefabs/<Module>/` |
| Multiple Instantiate / multi-Scene reuse? | Prefab required; no cross-Scene hierarchy copy |
| Designer **config-table** rows? | Excel + CSV → `Assets/ConfigTables/` (see [§14](#14-配置表工程约定与打表工具)) |
| Designer-tunable **non-table** singleton / ref slots? | ScriptableObject → `Assets/Settings/<Module>/` |
| UI text? | Localization keys (if §8 enabled) |
| High-frequency spawn/destroy? | Prefab + **object pool** |
| Gameplay state changes? | Rules layer + events; View must not drive rules |

**Forbidden:** duplicating the same hierarchy across Scenes; hardcoding designer data; `GameObject.Find` (except Managers); rules layer directly driving `Transform`/`Animator`; assembling in code what should ship as a Prefab (except temporary debug objects); placing new config tables outside `ConfigTables/`.

When implementing §9 tables, follow [§14](#14-配置表工程约定与打表工具): runtime reads CSV only; no hardcoded Level/Dig/Defend/upgrade/tech table values in scripts.

---

## 14. 配置表工程约定与打表工具

### 简体中文

**状态：打表工具已实现（方案 A）；Excel 三行表头（方案 B）已约定；数值单元格 CSV 序列化（禁止浮点噪声）已约定。**

凡 **§9** 及后续新增的**配置表**，统一遵守本节。

#### 14.1 统一路径（两文件夹）

```
Gravedigger2026/Assets/ConfigTables/
├── Excel/     # 人配源表（.xlsx）；仅作源文件，运行时不加载
└── Csv/       # 程序读表（.csv）；打表产物；运行时唯一数据源
```

- 新配置表**禁止**散落在 `Assets/Settings/<模块>/` 或其他路径。
- Excel 导入设置：`.xlsx` 可为 DefaultAsset；SPEC 要求：Excel **不作为运行时资源**读取。

#### 14.2 命名（Excel 四段 / CSV 两段）

- **Excel 磁盘名（无扩展名）** = `{SystemZH}_{TableZH}_{SystemEN}_{TableEN}`  
  例：`挖坟_挖坟配置表_Dig_DigGameplayConfig.xlsx`
- **CSV 磁盘名（无扩展名）** = `{SystemEN}_{TableEN}`（**不变**）  
  例：`Dig_DigGameplayConfig.csv`
- 逻辑短名（如 `DigGameplayConfig`）可用于类型/伪代码；**CSV 必须带系统英文前缀**；**Excel 必须在其前再加系统中文名 + 配置表中文名**。
- `SystemZH` / `TableZH` **禁止**含 `_`（避免打表解析歧义）。

| SystemEN | SystemZH | 适用范围（现有 §9 CSV 基名示例） |
|----------|----------|----------------------------------|
| `Level` | 关卡 | `Level_LevelOperationConfig` |
| `Dig` | 挖坟 | `Dig_DigGameplayConfig`、`Dig_GraveQualityConfig`、`Dig_MaterialConfig`、`Dig_CurrencyConfig` |
| `Defend` | 防守 | `Defend_DefendGameplayConfig`、`Defend_WaveSpawnConfig`、`Defend_MonsterConfig` |
| `PushMap` | 推图战 | `PushMap_PushMapGameplayConfig`、`PushMap_PushMapSpawnConfig` |
| `Manufacture` | 制造 | `Manufacture_ProtagonistLevelConfig`、`Manufacture_SoulConfig`、`Manufacture_ClassConfig`、`Manufacture_GemConfig`、`Manufacture_RaceConfig`、`Manufacture_BodyPartConfig`、`Manufacture_BodyAppearanceConfig`、`Manufacture_ExtraEquipmentConfig`、`Manufacture_GemSuffixNameConfig` |
| `Tech` | 科技 | `Tech_TechTreeConfig`、`Tech_TechEffectConfig` |
| `Combat` | 战斗 | `Combat_LossOfControlConfig`、`Combat_SkillConfig` |

配置表中文名（`TableZH`）取自 §9 小节标题（如「挖坟配置表」「关卡运作表」）。各表完整 Excel / CSV 名以 §9「磁盘名」行为准；新增表须先定 `SystemZH` + `TableZH` + `SystemEN` 再落盘。

#### 14.3 双格式强制

- 每张配置表必须同时维护 **Excel 源** + **CSV 产物**（文件名按 §14.2，**不必同名**）。
- 运行时 / 加载管线**只读** `ConfigTables/Csv/`。
- 策划只改 Excel；改完必须打表。**CSV 为生成物**，禁止以手改 CSV 作为长期数据源。

#### 14.4 打表工具（Bake Tables）

| 项 | 约定 |
|----|------|
| 职责 | 一键将 `ConfigTables/Excel/` 下全部 `.xlsx` 转为对应 `.csv` 写入 `ConfigTables/Csv/`（选中单表 **后置**） |
| 命名映射 | Excel 基名 `{SystemZH}_{TableZH}_{SystemEN}_{TableEN}` → CSV 基名 `{SystemEN}_{TableEN}`（取英文后缀两段） |
| 入口 | Unity 顶部菜单：`Gravedigger2026/Config/Bake Tables`（中文可用「打表」） |
| 脚本 | `Assets/Editor/Config/ConfigTableBaker.cs` + `XlsxSheetReader.cs` |
| 失败策略 | 文件名不符四段规则 / 首 Sheet 无英文表头时**整批中止**；先全部解析入内存再写盘（避免半成品）；Console 报错须含**完整 Excel 名** |
| Demo 校验 | 仅文件名四段 + 非空英文表头行；§9 缺列 / 类型非法校验 **后置**（运行时 `ConfigCsvRepository` 仍做加载期校验） |
| Excel 库 | 方案 A：Editor 纯 C# 解析 Open XML（`System.IO.Compression` + XML）；零第三方包；读 workbook 第一个 worksheet；**Zip 条目路径**须同时兼容 `/` 与 `\`（部分 Windows Excel 写出反斜杠；`ZipArchive.GetEntry` 按原文精确匹配） |
| 数值写出 | Excel 数值单元格写入 CSV 时须遵守 [§14.6](#146-数值单元格-csv-序列化禁止浮点噪声)（禁止二进制浮点噪声字面量） |
| 忽略 | `~$*.xlsx`（Excel 锁文件） |

**Excel 三行表头（方案 B，人读说明 + 机器列名）：**

| 行 | 内容 | 是否进入 CSV |
|----|------|----------------|
| 第 1 行 | 字段中文名 | **否**（打表剥离） |
| 第 2 行 | 字段中文说明 | **否**（打表剥离） |
| 第 3 行 | 字段英文名（与运行时列名一致） | **是**（成为 CSV 表头） |
| 第 4 行起 | 数据 | **是** |

- **英文表头识别：** 打表扫描首 Sheet 前至多 **3** 行，取首个「非空单元格均为合法英文列名」的行作为机器表头。合法列名格式：`^[A-Za-z_][A-Za-z0-9_.]*$`（允许点号，如 `GemMult.MaxHP`）。其上 0～2 行视为说明行并剥离；其下全部为数据行。
- **兼容：** 若第 1 行已是英文表头（旧单行格式），不要求必须有中文说明行；CSV 产物形状与现网一致（英文单行表头 + 数据）。
- **权威语义：** 字段中文名 / 说明以 [§9](#9-配置表关卡运作--挖坟--坟墓品质--材料--货币--挖坟能力--防守--刷怪波次--怪物--主角升级--灵魂--宝石--种族--制造部件--躯体外观--科技树--失控--技能骨架--推图战) 各表「字段 (EN) \| 中文 \| 类型 \| 说明」为准；Excel 第 1/2 行应与之对齐。
- **禁止：** 说明行进入 CSV；运行时改读中文表头。

```
Excel (ConfigTables/Excel/{SystemZH}_{TableZH}_{SystemEN}_{TableEN}.xlsx)
  → Editor Bake Tables（剥离至多 2 行说明，保留英文表头+数据）
  → CSV (ConfigTables/Csv/{SystemEN}_{TableEN}.csv)  // 英文单行表头
  → Runtime Config Loader
```

#### 14.5 运行时 CSV 加载路径（Demo）

| 环境 | 根路径 | 说明 |
|------|--------|------|
| Editor / 开发 Play | `{Application.dataPath}/ConfigTables/Csv/` | 即工程内 `Assets/ConfigTables/Csv/` |
| Player 构建 | `{Application.streamingAssetsPath}/ConfigTables/Csv/` | 须在构建前将同名 CSV 镜像到 `Assets/StreamingAssets/ConfigTables/Csv/`（拷贝工具可另开；本片加载器按序探测两根路径） |

- 逻辑仍只读 CSV；**禁止**运行时读 Excel。
- 缺表 / 缺列 / 非法枚举 → 加载失败并打日志，不静默用空表推进关卡。

#### 14.6 数值单元格 CSV 序列化（禁止浮点噪声）

**适用范围：** CSV 产物、打表工具（含 Editor Bake 与对齐的脚本镜像）、Agent/脚本向配置表写出的数值字面量。

**禁止：** 在 CSV（及同源写出）中出现二进制浮点噪声字面量，例如 `0.009999999999999995`、`0.06999999999999999`、`-0.010000000000000002`。

**Excel 数值单元格（无 `t` 或可解析为数字）写出算法：**

1. 以 `InvariantCulture` 解析为 `double`；`NaN` / `Infinity` 保持原串或按现有失败策略。
2. 若 `|x - round(x)| < 1e-9` 且 `|x| < 1e15` → 输出**整数字符串**（例：`80.0` → `80`）。
3. 否则：`Round(x, 10, MidpointRounding.AwayFromZero)`，再以去尾零的固定小数格式写出（等价 `0.##########` / trim）；**禁止**科学计数法；**禁止**长尾 9/0 噪声。
4. 共享字符串 / 显式文本单元格中的**干净短小数**原样保留；若文本内嵌二进制浮点噪声字面量（如编码字段 `MoveSpeed_0.30000000000000004`），打表须按同算法改写为短小数。

**规范化示例：** `0.009999999999999995` → `0.01`；`0.06999999999999999` → `0.07`；`-0.010000000000000002` → `-0.01`；`MoveSpeed_0.30000000000000004` → `MoveSpeed_0.3`。

### English

**Status: Bake tool implemented (Approach A); Excel three-row header (Approach B) specified; numeric CSV serialization (no float noise) specified.**

All **§9** and future **config tables** must follow this section.

#### 14.1 Unified path (two folders)

```
Gravedigger2026/Assets/ConfigTables/
├── Excel/     # Human-authored source (.xlsx); not loaded at runtime
└── Csv/       # Program-readable (.csv); bake output; sole runtime data source
```

- New config tables must **not** live under `Assets/Settings/<Module>/` or other paths.
- Excel import: `.xlsx` may be DefaultAsset; Excel must **not** be a runtime load target.

#### 14.2 Naming (Excel four-part / CSV two-part)

- **Excel basename** = `{SystemZH}_{TableZH}_{SystemEN}_{TableEN}`  
  e.g. `挖坟_挖坟配置表_Dig_DigGameplayConfig.xlsx`
- **CSV basename** = `{SystemEN}_{TableEN}` (**unchanged**)  
  e.g. `Dig_DigGameplayConfig.csv`
- Logical short names (e.g. `DigGameplayConfig`) may be used for types/pseudocode; **CSV requires the English system prefix**; **Excel requires SystemZH + TableZH before that**.
- `SystemZH` / `TableZH` must **not** contain `_` (bake parse safety).

| SystemEN | SystemZH | Scope (existing §9 CSV basename examples) |
|----------|----------|-------------------------------------------|
| `Level` | 关卡 | `Level_LevelOperationConfig` |
| `Dig` | 挖坟 | `Dig_DigGameplayConfig`, `Dig_GraveQualityConfig`, `Dig_MaterialConfig`, `Dig_CurrencyConfig` |
| `Defend` | 防守 | `Defend_DefendGameplayConfig`, `Defend_WaveSpawnConfig`, `Defend_MonsterConfig` |
| `PushMap` | 推图战 | `PushMap_PushMapGameplayConfig`, `PushMap_PushMapSpawnConfig` |
| `PushMap` | 推图战 | `PushMap_PushMapGameplayConfig`, `PushMap_PushMapSpawnConfig` |
| `Manufacture` | 制造 | `Manufacture_ProtagonistLevelConfig`, `Manufacture_SoulConfig`, `Manufacture_ClassConfig`, `Manufacture_GemConfig`, `Manufacture_RaceConfig`, `Manufacture_BodyPartConfig`, `Manufacture_BodyAppearanceConfig`, `Manufacture_ExtraEquipmentConfig`, `Manufacture_GemSuffixNameConfig` |
| `Tech` | 科技 | `Tech_TechTreeConfig`, `Tech_TechEffectConfig` |
| `Combat` | 战斗 | `Combat_LossOfControlConfig`, `Combat_SkillConfig` |

`TableZH` comes from the §9 subsection title (e.g.「挖坟配置表」). Per-table full Excel/CSV names: see §9 **Disk name** lines. New tables must choose `SystemZH` + `TableZH` + `SystemEN` before landing files.

#### 14.3 Dual-format required

- Every config table must maintain **Excel source** + **CSV product** (names per §14.2; **need not match**).
- Runtime / loaders read **only** `ConfigTables/Csv/`.
- Designers edit Excel only, then bake. **CSV is generated**; do not hand-edit CSV as the long-term source of truth.

#### 14.4 Bake tool

| Item | Rule |
|------|------|
| Duty | One-click convert all `.xlsx` under `ConfigTables/Excel/` to matching `.csv` under `ConfigTables/Csv/` (per-selection bake **deferred**) |
| Name map | Excel `{SystemZH}_{TableZH}_{SystemEN}_{TableEN}` → CSV `{SystemEN}_{TableEN}` (last two English segments) |
| Entry | Unity menu: `Gravedigger2026/Config/Bake Tables` (ZH label may be「打表」) |
| Scripts | `Assets/Editor/Config/ConfigTableBaker.cs` + `XlsxSheetReader.cs` |
| Failure | Abort whole batch on non-four-part Excel names / missing English header row; parse all into memory before writing (no partial writes); Console errors must include **full Excel name** |
| Demo validation | Filename four-part + non-empty English header row only; §9 missing-column / illegal-type checks **deferred** (runtime `ConfigCsvRepository` still validates on load) |
| Excel lib | Approach A: Editor pure-C# Open XML (`System.IO.Compression` + XML); zero third-party packages; first worksheet in workbook; **Zip entry paths** must accept both `/` and `\` (some Windows Excel writers store backslashes; `ZipArchive.GetEntry` matches literally) |
| Numeric emit | When writing Excel numeric cells to CSV, follow [§14.6](#146-numeric-cell-csv-serialization-no-float-noise) (forbid binary float-noise literals) |
| Ignore | `~$*.xlsx` (Excel lock files) |

**Excel three-row header (Approach B — human ZH + machine EN):**

| Row | Content | In CSV? |
|-----|---------|---------|
| 1 | Field Chinese name | **No** (stripped on bake) |
| 2 | Field Chinese notes | **No** (stripped on bake) |
| 3 | Field English name (runtime column id) | **Yes** (CSV header) |
| 4+ | Data | **Yes** |

- **English header detection:** Bake scans at most the first **3** rows of the first sheet and picks the first row whose non-empty cells all match a legal English column id. Pattern: `^[A-Za-z_][A-Za-z0-9_.]*$` (dots allowed, e.g. `GemMult.MaxHP`). 0–2 rows above are documentation and stripped; all rows below are data.
- **Compat:** If row 1 is already the English header (legacy single-row format), ZH doc rows are optional; CSV shape stays English header + data.
- **Authority:** Field ZH name / notes follow [§9](#9-配置表关卡运作--挖坟--坟墓品质--材料--货币--挖坟能力--防守--刷怪波次--怪物--主角升级--灵魂--宝石--种族--制造部件--躯体外观--科技树--失控--技能骨架--推图战) per-table Field tables; Excel rows 1–2 should stay aligned.
- **Forbidden:** Doc rows in CSV; runtime reading Chinese headers.

```
Excel (ConfigTables/Excel/{SystemZH}_{TableZH}_{SystemEN}_{TableEN}.xlsx)
  → Editor Bake Tables (strip ≤2 doc rows; keep English header + data)
  → CSV (ConfigTables/Csv/{SystemEN}_{TableEN}.csv)  // English single-row header
  → Runtime Config Loader
```

#### 14.5 Runtime CSV load paths (Demo)

| Environment | Root path | Notes |
|-------------|-----------|-------|
| Editor / Play Mode | `{Application.dataPath}/ConfigTables/Csv/` | i.e. project `Assets/ConfigTables/Csv/` |
| Player build | `{Application.streamingAssetsPath}/ConfigTables/Csv/` | Mirror same CSV files under `Assets/StreamingAssets/ConfigTables/Csv/` before build (copy tool may be separate; this slice’s loader probes both roots in order) |

- Still CSV-only; **never** read Excel at runtime.
- Missing table / column / illegal enum → load fails with log; do not silently advance Levels on empty tables.

#### 14.6 Numeric cell CSV serialization (no float noise)

**Scope:** CSV products, bake tools (Editor Bake and aligned script mirrors), and Agent/script numeric literals written into config tables.

**Forbidden:** Binary float-noise literals in CSV (and equivalent emitters), e.g. `0.009999999999999995`, `0.06999999999999999`, `-0.010000000000000002`.

**Excel numeric cells (no `t`, or parseable as number) emit algorithm:**

1. Parse as `double` with `InvariantCulture`; keep raw string or follow existing failure policy for `NaN` / `Infinity`.
2. If `|x - round(x)| < 1e-9` and `|x| < 1e15` → emit **integer** string (e.g. `80.0` → `80`).
3. Else: `Round(x, 10, MidpointRounding.AwayFromZero)`, then emit with trailing zeros trimmed (equivalent to `0.##########` / trim); **no** scientific notation; **no** long trailing 9/0 noise.
4. Clean short decimals inside shared strings / explicit text stay as-is; if text embeds binary float-noise literals (e.g. encoded field `MoveSpeed_0.30000000000000004`), bake must rewrite them with the same algorithm.

**Normalization examples:** `0.009999999999999995` → `0.01`; `0.06999999999999999` → `0.07`; `-0.010000000000000002` → `-0.01`; `MoveSpeed_0.30000000000000004` → `MoveSpeed_0.3`.

---

## 15. 角色美术管线（Character Creator 烘焙整角）

### 简体中文

**状态：管线约定已关闭** — 全角色视觉采用 SmallScaleInt **Character Creator - Fantasy** 烘焙整角；游戏引用资源不得落在工具包目录。

#### 15.1 工具与禁入

| 项 | 约定 |
|----|------|
| 工具根 | `Assets/SmallScaleInt/Character creator - Fantasy/` |
| 允许内容 | 厂商源部件（`spritesheets` 等）、Editor 示例场景/脚本、文档、厂商示例 |
| **禁止** | 任何**游戏实际引用**的角色 spritesheet、`.anim`、`.controller`、游戏 Prefab、运行时材质/图集 落在该目录（**含**其下 `Created Spritesheets/`） |
| 形态 | **烘焙整角**（拼装 → 导出整角 spritesheet + AnimationClips + Animator + 可选厂商 Prefab）；**不**以运行时多层换装为默认方案 |
| 覆盖角色 | Digger、BattleProtagonist、士兵（`AppearanceId`）、怪物（`ModelId`） |

#### 15.2 项目落盘目录

```
Assets/Art/Characters/
  Protagonist/                 # 挖坟/战斗主角导出源（spritesheet + clips + animator）
  Appearances/{AppearanceId}/  # 士兵外观
  Monsters/{ModelId}/          # 怪物
Assets/Prefabs/Dig/Digger.prefab
Assets/Prefabs/Defend/BattleProtagonist.prefab
Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab
Assets/Prefabs/Defend/Monsters/{ModelId}.prefab
```

- Art 目录存烘焙产物（图、Clip、Controller）；游戏 Instantiate 只引用 `Prefabs/` 下游戏 Prefab。
- 主角固定逻辑名：`Digger`、`BattleProtagonist`（本版不另开多皮肤表）。

**角色游戏 Prefab 组装（俯视相机，士兵 / 主角 / 怪物共用结构）：**

| 节点 | 职责 |
|------|------|
| 根 | 位移；士兵运行时挂 `WarriorAgentView` + `WarriorAnimView` + `NavMeshAgent`（可代码 Add；Demo `radius=0.1` / `height=0.1`）；Digger 根挂 `DigObstacleRadius` + `DigDiggerView` |
| 子 `Visual` | `SpriteRenderer` + `Animator`（Controller 来自 Art 烘焙）；`localEulerAngles = (90, 0, 0)`，使 Sprite 朝 +Y，对齐 Dig/Defend/PushMap 俯视相机（相机 `Euler(90,0,0)`）；`sortingOrder` 高于地面 Tilemap（Demo ≥ 200） |

- View 层：`NavMeshAgent.updateRotation = false`（八向靠 Animator `DirIndex`，见 §15.5；士兵由 `WarriorAnimView` 驱动）。
- **禁止**用 `GenericTopDownController` 作默认玩法控制器（见 §15.4）。
- **Digger / BattleProtagonist** 已换为上述 `Visual` 结构（Art：`Protagonist/Digger`、`Protagonist/BattleProtagonist`）；**禁止** `DigAssetBuilder` / `DefendAssetBuilder` 再生成 Capsule/`Body` Mesh 占位覆盖这两 Prefab。
- **怪物（`ModelId`）**：根挂运行时 `MonsterAgentView` + `NavMeshAgent`（可代码 Add）；子 `Visual` 同上。当 `Art/Characters/Monsters/{ModelId}/` 已有烘焙 Controller（及 Idle Sprite）时，**必须**组装为 `Visual` 并**删除**占位 `Body` Mesh；无 Art 时允许保留临时立方体。本片已落地：`MonsterModel_01`…`MonsterModel_04`。
- Editor：`Tools/Gravedigger/Art/Assemble Protagonist Prefabs`（`ProtagonistPrefabAssembler`）；`Tools/Gravedigger/Art/Assemble Monster Model Prefabs`（`MonsterModelPrefabAssembler`，仅组装 Art 就绪的 `ModelId`）。`DefendAssetBuilder` 生成 Catalog 时对有 Art 的怪物调用后者，**禁止**用临时立方体覆盖已组装 Prefab。

#### 15.3 导出路径改造（强制）

厂商 `SpritesheetGenerator` 默认将 `outputParent` 写到工具内 `Created Spritesheets/`。**本项目维护补丁**：把 Editor 导出根改为 `Assets/Art/Characters/...`（按角色类型分子目录；导出文件夹名与 `AppearanceId` / `ModelId` / 主角约定对齐）。Clips / Controller / 可选导出 Prefab 随同写入该 Art 子目录；再组装或复制为 §15.2 的游戏 Prefab。

**切片 / Clip 补丁（强制，否则 Windows 下 `.anim`/`.controller` 会空导出）：**

| 问题 | 现象 | 补丁 |
|------|------|------|
| 切片未设 `TextureImporter.textureType = Sprite` | meta 可有 `spriteMode: Multiple` 与 spritesheet 名，但 `textureType` 仍为 Default(0)；`LoadAllAssetRepresentationsAtPath` 得不到 Sprite → **不写任何 `.anim`**，却仍生成 **空 BlendTree**（`m_Childs: []`）的 `.controller` | 切片与 `ApplyFrameCountOption` 重导时必须 `textureType = Sprite`，并与厂商源 `spritesheets/`（`textureType: 8`）一致 |
| `AssetImporter.GetAtPath` 使用反斜杠路径 | Windows 上 importer 可能为 null，切片静默跳过 | 资产路径统一 `.Replace("\\", "/")` |
| 无 Sprite 仍建 Controller | 空状态机误当成功 | `AnimatorClipBuilder`：零 Clip 时 **LogError 并中止**，不写空 `.controller` |
| 切片用 NPOT 膨胀后的 `tex.width`（常见 2048） | 源 PNG 为 1920×1024（15×8→格宽 **128**），却按 `2048/15≈136.53` 切；换帧时角色相对中心持续左漂，循环后跳回 | 切片前设 `npotScale=None`；用 `TextureImporter.GetSourceTextureWidthAndHeight`（或重导后再读）算格宽；`CharacterCreatorExportRepair`：已有 120 格但 `rect.width` 与 `sourceWidth/columns` 差 >0.5 时 **强制重建 rect** |

**修复已坏导出：** Editor 菜单 `Tools/Gravedigger/Art/Repair Character Creator Export`（对选中或指定角色文件夹：纠正 importer → 重导 → 重建 Clips/Controller）。批量：`Tools/Gravedigger/Art/Repair All Character Creator Exports (Art/Characters)`。

**升级风险：** 重新导入 `.unitypackage` 会覆盖补丁脚本 → 每次导入后须 `diff` `SpritesheetGenerator.cs` / `AnimatorClipBuilder.cs`（及相关写盘路径）并重打补丁。

#### 15.4 流水线

```mermaid
flowchart LR
  VendorTool[Vendor_Creator_Source] --> BakeExport[Patched_Export]
  BakeExport --> ArtFolder[Art_Characters_Id]
  ArtFolder --> GamePrefab[Prefabs_Dig_or_Defend]
  GamePrefab --> ConfigBind[AppearanceId_or_ModelId]
```

| 阶段 | 说明 |
|------|------|
| 创作 | 仅在工具目录内拼装部件/配色；不把成品当作游戏引用 |
| 导出 | 经补丁路径写入 `Art/Characters/...` |
| Prefab | 组装到 `Prefabs/Dig` 或 `Prefabs/Defend/...`；挂项目玩法 Controller（**不用** `GenericTopDownController` 作默认玩法控制器；厂商脚本可作视觉壳参考） |
| 配置 | `AppearanceId` / `ModelId` / 固定主角 Prefab 名绑定；规则层不硬编码动画资源路径 |

#### 15.5 动画映射（Demo 锁定）

Creator 默认导出含 Idle / Walk / Run / Attack* / Special1 / Die 等。挖坟循环动画**无**原生 Dig clip：由**表现层**（`DigDiggerView` 序列化字段）将 Dig 语义映射到导出 Trigger **`Special1`**；厂商状态多为单次 Trigger，View 在 digging 期间于 clip 结束时重触发以实现循环。待机 = 默认 Idle（清 locomotion bool）。**禁止**规则层写死动画名字符串；**禁止**在无独立 Dig 资源时假装已有专用 Dig 资产。

**Defend 士兵（`WarriorAnimView`，Demo 锁定）：**

| 语义 | Creator 参数（默认；View 序列化可改） | 说明 |
|------|--------------------------------------|------|
| 移动 | Bool `IsRun` | `NavMeshAgent` 速度超阈值 → true；停步清 locomotion Bool → Idle |
| 普攻 | Trigger `Attack1` | 近战前摇开始 / 远程开火时触发；规则层不写动画名 |
| 死亡 | Trigger `Die` | CombatDead / PermanentDeath 后触发一次并锁存；尸体留场 |
| 朝向 | Int `DirIndex` | 按移动或瞄准 XZ 向量换算（见下表）；零向量不改 |

**`DirIndex`（Creator 烘焙 Controller，Demo 锁定）：**

| `DirIndex` | 朝向 |
|------------|------|
| 0 | E |
| 1 | W |
| 2 | S |
| 3 | N |
| 4 | NE |
| 5 | NW |
| 6 | SE |
| 7 | SW |

Demo：`Digger` / `BattleProtagonist` 固定 **`DirIndex = 2`（南）**；士兵由 `WarriorAnimView` 按移动/瞄准动态设 `DirIndex`。BattleProtagonist 本片仅 Idle 站桩（无受击/死亡驱动）。怪物本片仍不驱动 Animator（死亡 `SetActive(false)`）。

#### 15.6 Mount / Wing

见 [§9.14](#914-额外装备配置表-extraequipmentconfig)：视觉打进对应 `AppearanceId` 烘焙变体；运行时叠装另专题。

### English

**Status: Pipeline rules closed** — All character visuals use SmallScaleInt **Character Creator - Fantasy** baked whole characters; game-referenced assets must **not** live under the vendor tool folder.

#### 15.1 Tool folder & ban

| Item | Rule |
|------|------|
| Tool root | `Assets/SmallScaleInt/Character creator - Fantasy/` |
| Allowed | Vendor source parts (`spritesheets`, etc.), Editor sample scenes/scripts, docs, vendor samples |
| **Forbidden** | Any **game-referenced** character spritesheets, `.anim`, `.controller`, game Prefabs, runtime materials/atlases under that folder (**including** `Created Spritesheets/`) |
| Form | **Baked whole characters** (assemble → export full spritesheets + AnimationClips + Animator + optional vendor Prefab); runtime layered gear is **not** the default |
| Covers | Digger, BattleProtagonist, soldiers (`AppearanceId`), monsters (`ModelId`) |

#### 15.2 Project paths

Same tree as ZH §15.2. Art holds bake outputs; runtime Instantiate uses `Prefabs/` only. Fixed protagonist logical names: `Digger`, `BattleProtagonist`.

**Character game Prefab assembly (top-down; soldiers, protagonists, and monsters share layout):**

| Node | Role |
|------|------|
| Root | Translation; warriors get runtime `WarriorAgentView` + `WarriorAnimView` + `NavMeshAgent` (may AddComponent; Demo `radius=0.1` / `height=0.1`); Digger root keeps `DigObstacleRadius` + `DigDiggerView` |
| Child `Visual` | `SpriteRenderer` + `Animator` (Controller from Art bake); `localEulerAngles = (90, 0, 0)` so the sprite faces +Y toward the Dig/Defend/PushMap top-down camera (`Euler(90,0,0)`); `sortingOrder` above ground Tilemap (Demo ≥ 200) |

- View: `NavMeshAgent.updateRotation = false` (8-dir via Animator `DirIndex`, §15.5; soldiers driven by `WarriorAnimView`).
- **Do not** use `GenericTopDownController` as the default gameplay controller (§15.4).
- **Digger / BattleProtagonist** use the `Visual` layout above (Art: `Protagonist/Digger`, `Protagonist/BattleProtagonist`); **do not** let `DigAssetBuilder` / `DefendAssetBuilder` regenerate Capsule/`Body` Mesh over those Prefabs.
- **Monsters (`ModelId`)**: root gets runtime `MonsterAgentView` + `NavMeshAgent` (may AddComponent); child `Visual` as above. When `Art/Characters/Monsters/{ModelId}/` has a baked Controller (and Idle Sprite), **must** assemble `Visual` and **remove** placeholder `Body` Mesh; temp cubes remain only when Art is missing. This slice: `MonsterModel_01`…`MonsterModel_04`.
- Editor: `Tools/Gravedigger/Art/Assemble Protagonist Prefabs` (`ProtagonistPrefabAssembler`); `Tools/Gravedigger/Art/Assemble Monster Model Prefabs` (`MonsterModelPrefabAssembler`, only ModelIds with Art ready). `DefendAssetBuilder` calls the latter for Art-ready monsters when building Catalog and **must not** overwrite assembled Prefabs with temp cubes.

#### 15.3 Export path patch (mandatory)

Vendor `SpritesheetGenerator` defaults `outputParent` to in-package `Created Spritesheets/`. **This project maintains a patch** so Editor export roots at `Assets/Art/Characters/...` (subfolders by role; folder names align with `AppearanceId` / `ModelId` / protagonist convention). Clips / Controller / optional export Prefab land under that Art subfolder; then assemble/copy into §15.2 game Prefabs.

**Slice / Clip patch (mandatory — otherwise Windows exports empty `.anim`/`.controller`):**

| Issue | Symptom | Patch |
|-------|---------|-------|
| Slice omits `TextureImporter.textureType = Sprite` | Meta may show Multiple + spritesheet names while `textureType` stays Default(0); no Sprite sub-assets → **no `.anim`**, yet an empty BlendTree `.controller` (`m_Childs: []`) is still written | Force `textureType = Sprite` on slice and `ApplyFrameCountOption` reimport; match vendor `spritesheets/` (`textureType: 8`) |
| Backslash paths in `AssetImporter.GetAtPath` | Importer may be null on Windows; slice silently skipped | Normalize asset paths with `.Replace("\\", "/")` |
| Controller built with zero clips | Empty state machine looks “successful” | `AnimatorClipBuilder`: **LogError and abort** when clip count is zero |
| Slice uses NPOT-padded `tex.width` (often 2048) | Source PNG is 1920×1024 (15×8 → cell **128**), but sliced as `2048/15≈136.53`; frames drift left vs pivot, then jump back on loop | Set `npotScale=None` before slice; use `TextureImporter.GetSourceTextureWidthAndHeight` (or reimport then read) for cell size; `CharacterCreatorExportRepair`: if 120 cells exist but `rect.width` differs from `sourceWidth/columns` by >0.5 → **force rebuild rects** |

**Repair broken exports:** Editor menu `Tools/Gravedigger/Art/Repair Character Creator Export` (fix importer → reimport → rebuild Clips/Controller for selected/target character folder). Batch: `Tools/Gravedigger/Art/Repair All Character Creator Exports (Art/Characters)`.

**Upgrade risk:** Re-importing the `.unitypackage` overwrites the patch → after each import, `diff` `SpritesheetGenerator.cs` / `AnimatorClipBuilder.cs` (and related write paths) and re-apply.

#### 15.4 Pipeline

Same mermaid as ZH. Author in vendor folder only; export via patched path; Prefabs under Dig/Defend; **do not** treat `GenericTopDownController` as the default gameplay controller.

#### 15.5 Animation mapping (Demo lock)

Creator exports Idle / Walk / Run / Attack* / Special1 / Die, etc. Dig loop has **no** native Dig clip: **View layer** (`DigDiggerView` serialized field) maps Dig semantics to export Trigger **`Special1`**; vendor states are mostly one-shot Triggers — View re-fires when the clip ends while digging to loop. Idle = default Idle (clear locomotion bools). **No** hardcoded anim names in rules layer; **do not** pretend a dedicated Dig asset exists without one.

**Defend soldiers (`WarriorAnimView`, Demo lock):**

| Semantics | Creator param (default; View serialized) | Notes |
|-----------|------------------------------------------|-------|
| Move | Bool `IsRun` | true when `NavMeshAgent` speed above threshold; clear locomotion bools → Idle when stopped |
| Attack | Trigger `Attack1` | on melee windup start / ranged fire; rules never hardcode anim names |
| Death | Trigger `Die` | once on CombatDead / PermanentDeath, then latched; corpse stays |
| Facing | Int `DirIndex` | from move or aim XZ (table below); zero vector leaves unchanged |

**`DirIndex` (Creator bake Controller, Demo lock):**

| `DirIndex` | Facing |
|------------|--------|
| 0 | E |
| 1 | W |
| 2 | S |
| 3 | N |
| 4 | NE |
| 5 | NW |
| 6 | SE |
| 7 | SW |

Demo: `Digger` / `BattleProtagonist` fixed **`DirIndex = 2` (South)**; soldiers get dynamic `DirIndex` from `WarriorAnimView` (move/aim). BattleProtagonist this slice: Idle only (no hit/death drive). Monsters this slice: no Animator drive (death still `SetActive(false)`).

#### 15.6 Mount / Wing

See [§9.14](#914-额外装备配置表-extraequipmentconfig): bake into `AppearanceId` variants; runtime overlays later.
